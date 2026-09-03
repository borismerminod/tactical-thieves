import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { Observable } from 'rxjs';

import { AuthService } from './auth.service';
import { environment } from '../../environments/environment';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  const startUrl = `${environment.apiURL}/api/auth/RegisterStart`;
  const finishUrl = `${environment.apiURL}/api/auth/RegisterFinish`;
  const loginStartUrl = `${environment.apiURL}/api/auth/LoginStart`;
  const loginFinishUrl = `${environment.apiURL}/api/auth/LoginFinish`;

  // Drains the microtask queue (HTTP resolve → WebAuthn → next POST).
  const tick = () => new Promise<void>((res) => setTimeout(res, 0));

  // Captures the latest value emitted by an observable
  // (BehaviorSubject → current value delivered immediately on subscription).
  function latest<T>(obs: Observable<T>): { value: T } {
    const box = { value: undefined as unknown as T };
    obs.subscribe((v) => (box.value = v));
    return box;
  }

  beforeEach(() => {
    // AuthService's constructor reads sessionStorage for the observables' initial
    // state → we clear it BEFORE creating the service (deterministic state).
    sessionStorage.clear();

    TestBed.configureTestingModule({
      providers: [
        AuthService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify(); // no pending unhandled request
    sessionStorage.clear();
  });

  // --- Fake data factories ----------------------------------------------

  // Simulated RegisterStart response. challenge and user.id MUST be valid
  // base64url: formatRegisterStartOptions passes them to base64urlToBuffer.
  function makeStartResponse() {
    return {
      challenge: 'AAAA', // valid base64url
      rp: { name: 'TacticalThieves', id: 'localhost' },
      user: { id: 'AAAA', name: 'Alice', displayName: 'Alice' },
      pubKeyCredParams: [{ type: 'public-key', alg: -7 }],
      // no excludeCredentials → we avoid the mapping branch
    };
  }

  // Fake credential returned by navigator.credentials.create.
  // rawId / clientDataJSON / attestationObject must be ArrayBuffers
  // (they go through bufferToBase64url).
  function makeFakeCredential(): PublicKeyCredential {
    return {
      id: 'cred-id',
      rawId: new Uint8Array([1, 2, 3]).buffer,
      type: 'public-key',
      getClientExtensionResults: () => ({}),
      response: {
        clientDataJSON: new Uint8Array([4, 5, 6]).buffer,
        attestationObject: new Uint8Array([7, 8, 9]).buffer,
        getTransports: () => ['internal'],
      },
    } as unknown as PublicKeyCredential;
  }

  // "Valid" passkey by default; we override the fields as needed.
  function makePasskey(over: Record<string, unknown> = {}) {
    return {
      id: 'key-id',
      type: 'public-key',
      publicKey: 'pub-key',
      signCount: 0,
      transports: [],
      aaGuid: '',
      attestationFormat: '',
      attestationObject: '',
      attestationClientDataJson: '',
      isBackedUp: false,
      isBackupEligible: false,
      user: { name: 'Alice', id: 'uid', displayName: 'Alice' },
      ...over,
    };
  }

  // Runs a nominal register and returns the Promise<boolean>.
  // finishBody drives the return value.
  async function runRegister(finishBody: unknown): Promise<boolean> {
    spyOn(navigator.credentials, 'create').and.resolveTo(makeFakeCredential());
    const promise = service.register('Alice');

    httpMock.expectOne(startUrl).flush(makeStartResponse());
    await tick(); // formatting + WebAuthn + POST RegisterFinish
    httpMock.expectOne(finishUrl).flush(finishBody as any);

    return promise;
  }

  // ===== Group A — Observable contract =====

  // A1: the service instantiates correctly.
  it('should create', () => {
    expect(service).toBeTruthy();
  });

  // A2: register triggers a well-formed RegisterStart POST.
  it('register: should POST to RegisterStart with the username and credentials', async () => {
    spyOn(navigator.credentials, 'create').and.resolveTo(makeFakeCredential());
    const promise = service.register('Alice');

    const startReq = httpMock.expectOne(startUrl);
    expect(startReq.request.method).toBe('POST');
    expect(startReq.request.body).toEqual({ username: 'Alice' });
    expect(startReq.request.withCredentials).toBeTrue();

    startReq.flush(makeStartResponse());
    await tick();
    httpMock.expectOne(finishUrl).flush(makePasskey()); // we settle the flow
    await promise;
  });

  // A3: WebAuthn is called with formatted options (ArrayBuffer).
  it('register: should call WebAuthn with formatted options (challenge & user.id as ArrayBuffer)', async () => {
    const createSpy = spyOn(navigator.credentials, 'create').and.resolveTo(
      makeFakeCredential(),
    );
    const promise = service.register('Alice');

    httpMock.expectOne(startUrl).flush(makeStartResponse());
    await tick();

    expect(createSpy).toHaveBeenCalledTimes(1);
    const publicKey = (createSpy.calls.mostRecent().args[0] as any).publicKey;
    expect(publicKey.challenge instanceof ArrayBuffer).toBeTrue();
    expect(publicKey.user.id instanceof ArrayBuffer).toBeTrue();

    httpMock.expectOne(finishUrl).flush(makePasskey());
    await promise;
  });

  // A4: after WebAuthn, register posts to RegisterFinish.
  it('register: should POST to RegisterFinish after the WebAuthn step', async () => {
    spyOn(navigator.credentials, 'create').and.resolveTo(makeFakeCredential());
    const promise = service.register('Alice');

    httpMock.expectOne(startUrl).flush(makeStartResponse());
    await tick();

    const finishReq = httpMock.expectOne(finishUrl);
    expect(finishReq.request.method).toBe('POST');
    expect(finishReq.request.withCredentials).toBeTrue();

    finishReq.flush(makePasskey());
    await promise;
  });

  // ===== Group B — Return value =====

  // B1: complete and valid passkey → true.
  it('register: should return true for a valid registered passkey', async () => {
    expect(await runRegister(makePasskey())).toBeTrue();
  });

  // B2: null response → false.
  it('register: should return false when RegisterFinish returns null', async () => {
    expect(await runRegister(null)).toBeFalse();
  });

  // B3: wrong type → false.
  it('register: should return false when type is not "public-key"', async () => {
    expect(await runRegister(makePasskey({ type: 'public-key-WRONG' }))).toBeFalse();
  });

  // B4: missing id → false.
  it('register: should return false when the id is empty', async () => {
    expect(await runRegister(makePasskey({ id: '' }))).toBeFalse();
  });

  // B5: missing publicKey → false.
  it('register: should return false when the publicKey is empty', async () => {
    expect(await runRegister(makePasskey({ publicKey: '' }))).toBeFalse();
  });

  // ===== Group C — Error propagation =====

  // C1: HTTP error on RegisterStart → rejection, no WebAuthn nor RegisterFinish.
  it('register: should reject on RegisterStart HTTP error and skip the rest', async () => {
    const createSpy = spyOn(navigator.credentials, 'create');
    const promise = service.register('Alice');

    httpMock
      .expectOne(startUrl)
      .flush('boom', { status: 500, statusText: 'Server Error' });

    await expectAsync(promise).toBeRejected();
    expect(createSpy).not.toHaveBeenCalled();
    httpMock.expectNone(finishUrl);
  });

  // C2: WebAuthn rejected (cancellation) → rejection, no RegisterFinish.
  it('register: should reject when WebAuthn is cancelled and skip RegisterFinish', async () => {
    spyOn(navigator.credentials, 'create').and.rejectWith(
      new DOMException('cancelled', 'NotAllowedError'),
    );
    const promise = service.register('Alice');

    httpMock.expectOne(startUrl).flush(makeStartResponse());

    await expectAsync(promise).toBeRejected();
    httpMock.expectNone(finishUrl);
  });

  // C3: HTTP error on RegisterFinish → rejection.
  it('register: should reject on RegisterFinish HTTP error', async () => {
    spyOn(navigator.credentials, 'create').and.resolveTo(makeFakeCredential());
    const promise = service.register('Alice');

    httpMock.expectOne(startUrl).flush(makeStartResponse());
    await tick();
    httpMock
      .expectOne(finishUrl)
      .flush('boom', { status: 500, statusText: 'Server Error' });

    await expectAsync(promise).toBeRejected();
  });

  // =====================================================================
  // login
  // =====================================================================

  // --- Fake data factories (login) --------------------------------------

  // Simulated LoginStart response. challenge in valid base64url
  // (formatLoginStartOptions passes it to base64urlToBuffer).
  function makeLoginStartResponse() {
    return {
      challenge: 'AAAA',
      rpId: 'localhost',
      timeout: 60000,
      userVerification: 'preferred',
      // no allowCredentials → we avoid the mapping branch
    };
  }

  // Fake assertion returned by navigator.credentials.get.
  // The binary fields are ArrayBuffers (they go through bufferToBase64url).
  function makeFakeAssertion(): PublicKeyCredential {
    return {
      id: 'assert-id',
      rawId: new Uint8Array([1, 2, 3]).buffer,
      type: 'public-key',
      getClientExtensionResults: () => ({}),
      response: {
        authenticatorData: new Uint8Array([1]).buffer,
        clientDataJSON: new Uint8Array([2]).buffer,
        signature: new Uint8Array([3]).buffer,
        userHandle: null,
      },
    } as unknown as PublicKeyCredential;
  }

  // Runs a nominal login and returns the Promise<boolean>.
  // finishBody drives the LoginFinish response (presence/absence of the token).
  async function runLogin(
    username: string,
    finishBody: unknown,
  ): Promise<boolean> {
    spyOn(navigator.credentials, 'get').and.resolveTo(makeFakeAssertion());
    const promise = service.login(username);

    httpMock.expectOne(loginStartUrl).flush(makeLoginStartResponse());
    await tick(); // formatting + WebAuthn get + POST LoginFinish
    httpMock.expectOne(loginFinishUrl).flush(finishBody as any);

    return promise;
  }

  // ===== Group D — Observable contract (login) =====

  // D1: well-formed LoginStart POST.
  it('login: should POST to LoginStart with the username and credentials', async () => {
    spyOn(navigator.credentials, 'get').and.resolveTo(makeFakeAssertion());
    const promise = service.login('Alice');

    const startReq = httpMock.expectOne(loginStartUrl);
    expect(startReq.request.method).toBe('POST');
    expect(startReq.request.body).toEqual({ username: 'Alice' });
    expect(startReq.request.withCredentials).toBeTrue();

    startReq.flush(makeLoginStartResponse());
    await tick();
    httpMock.expectOne(loginFinishUrl).flush({ token: 'jwt', username: 'srv' });
    await promise;
  });

  // D2: WebAuthn get called with the challenge converted to an ArrayBuffer.
  it('login: should call WebAuthn get with a formatted challenge (ArrayBuffer)', async () => {
    const getSpy = spyOn(navigator.credentials, 'get').and.resolveTo(
      makeFakeAssertion(),
    );
    const promise = service.login('Alice');

    httpMock.expectOne(loginStartUrl).flush(makeLoginStartResponse());
    await tick();

    expect(getSpy).toHaveBeenCalledTimes(1);
    const publicKey = (getSpy.calls.mostRecent().args[0] as any).publicKey;
    expect(publicKey.challenge instanceof ArrayBuffer).toBeTrue();

    httpMock.expectOne(loginFinishUrl).flush({ token: 'jwt', username: 'srv' });
    await promise;
  });

  // D3: POST LoginFinish after the assertion.
  it('login: should POST to LoginFinish after the WebAuthn step', async () => {
    spyOn(navigator.credentials, 'get').and.resolveTo(makeFakeAssertion());
    const promise = service.login('Alice');

    httpMock.expectOne(loginStartUrl).flush(makeLoginStartResponse());
    await tick();

    const finishReq = httpMock.expectOne(loginFinishUrl);
    expect(finishReq.request.method).toBe('POST');
    expect(finishReq.request.withCredentials).toBeTrue();

    finishReq.flush({ token: 'jwt', username: 'srv' });
    await promise;
  });

  // ===== Group E — Success / failure depending on the token =====

  // E1: token present → true + side effects (sessionStorage + observables).
  it('login: should store token and flag logged-in on success', async () => {
    const loggedIn = latest(service.isLoggedIn$);

    const ok = await runLogin('Alice', {
      token: 'jwt-123',
      username: 'srvName',
    });

    expect(ok).toBeTrue();
    expect(sessionStorage.getItem('authToken')).toBe('jwt-123');
    expect(loggedIn.value).toBeTrue();
  });

  // E2: token absent → false + no side effect.
  it('login: should return false and write nothing when the token is missing', async () => {
    const loggedIn = latest(service.isLoggedIn$);

    const ok = await runLogin('Alice', { token: '', username: 'srvName' });

    expect(ok).toBeFalse();
    expect(sessionStorage.getItem('authToken')).toBeNull();
    expect(sessionStorage.getItem('username')).toBeNull();
    expect(loggedIn.value).toBeFalse();
  });

  // E3: sessionStorage stores result.username, but username$ emits the argument.
  it('login: should store server username but emit the argument username', async () => {
    const usernameObs = latest(service.username$);

    await runLogin('Alice', { token: 'jwt', username: 'srvName' });

    expect(sessionStorage.getItem('username')).toBe('srvName'); // server
    expect(usernameObs.value).toBe('Alice'); // argument
  });

  // ===== Group F — Error propagation (login) =====

  // F1: LoginStart HTTP error → rejection, no WebAuthn nor LoginFinish.
  it('login: should reject on LoginStart HTTP error and skip the rest', async () => {
    const getSpy = spyOn(navigator.credentials, 'get');
    const promise = service.login('Alice');

    httpMock
      .expectOne(loginStartUrl)
      .flush('boom', { status: 500, statusText: 'Server Error' });

    await expectAsync(promise).toBeRejected();
    expect(getSpy).not.toHaveBeenCalled();
    httpMock.expectNone(loginFinishUrl);
  });

  // F2: WebAuthn get rejected → rejection, no LoginFinish.
  it('login: should reject when WebAuthn get is cancelled and skip LoginFinish', async () => {
    spyOn(navigator.credentials, 'get').and.rejectWith(
      new DOMException('cancelled', 'NotAllowedError'),
    );
    const promise = service.login('Alice');

    httpMock.expectOne(loginStartUrl).flush(makeLoginStartResponse());

    await expectAsync(promise).toBeRejected();
    httpMock.expectNone(loginFinishUrl);
  });

  // F3: LoginFinish HTTP error → rejection.
  it('login: should reject on LoginFinish HTTP error', async () => {
    spyOn(navigator.credentials, 'get').and.resolveTo(makeFakeAssertion());
    const promise = service.login('Alice');

    httpMock.expectOne(loginStartUrl).flush(makeLoginStartResponse());
    await tick();
    httpMock
      .expectOne(loginFinishUrl)
      .flush('boom', { status: 500, statusText: 'Server Error' });

    await expectAsync(promise).toBeRejected();
  });

  // =====================================================================
  // logout
  // =====================================================================

  // ===== Group G — logout =====

  // G1: logout fully clears sessionStorage.
  it('logout: should clear sessionStorage on logout', () => {
    sessionStorage.setItem('authToken', 'jwt');
    sessionStorage.setItem('username', 'Alice');

    service.logout();

    expect(sessionStorage.length).toBe(0);
  });

  // G2: logout emits false on isLoggedIn$.
  it('logout: should emit false on isLoggedIn$ after logout', () => {
    const loggedIn = latest(service.isLoggedIn$);

    service.logout();

    expect(loggedIn.value).toBeFalse();
  });

  // G3: logout emits '' on username$.
  it('logout: should emit an empty username after logout', () => {
    const usernameObs = latest(service.username$);

    service.logout();

    expect(usernameObs.value).toBe('');
  });
});
