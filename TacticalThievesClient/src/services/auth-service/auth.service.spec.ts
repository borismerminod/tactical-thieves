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

  // Vide la file de microtâches (HTTP resolve → WebAuthn → POST suivant).
  const tick = () => new Promise<void>((res) => setTimeout(res, 0));

  // Capte la dernière valeur émise par un observable
  // (BehaviorSubject → valeur courante immédiate à la souscription).
  function latest<T>(obs: Observable<T>): { value: T } {
    const box = { value: undefined as unknown as T };
    obs.subscribe((v) => (box.value = v));
    return box;
  }

  beforeEach(() => {
    // Le constructeur d'AuthService lit sessionStorage pour l'état initial des
    // observables → on le vide AVANT de créer le service (état déterministe).
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
    httpMock.verify(); // aucune requête en attente non traitée
    sessionStorage.clear();
  });

  // --- Fabriques de données factices -----------------------------------

  // Réponse simulée de RegisterStart. challenge et user.id DOIVENT être en
  // base64url valide : formatRegisterStartOptions les passe à base64urlToBuffer.
  function makeStartResponse() {
    return {
      challenge: 'AAAA', // base64url valide
      rp: { name: 'TacticalThieves', id: 'localhost' },
      user: { id: 'AAAA', name: 'Alice', displayName: 'Alice' },
      pubKeyCredParams: [{ type: 'public-key', alg: -7 }],
      // pas d'excludeCredentials → on évite la branche de mapping
    };
  }

  // Fausse credential renvoyée par navigator.credentials.create.
  // rawId / clientDataJSON / attestationObject doivent être des ArrayBuffer
  // (ils passent par bufferToBase64url).
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

  // Passkey « valide » par défaut ; on surcharge les champs au besoin.
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

  // Déroule un register nominal et renvoie la Promise<boolean>.
  // finishBody pilote la valeur de retour.
  async function runRegister(finishBody: unknown): Promise<boolean> {
    spyOn(navigator.credentials, 'create').and.resolveTo(makeFakeCredential());
    const promise = service.register('Alice');

    httpMock.expectOne(startUrl).flush(makeStartResponse());
    await tick(); // formatage + WebAuthn + POST RegisterFinish
    httpMock.expectOne(finishUrl).flush(finishBody as any);

    return promise;
  }

  // ===== Groupe A — Contrat observable =====

  // A1 : le service s'instancie correctement.
  it('should create', () => {
    expect(service).toBeTruthy();
  });

  // A2 : register déclenche un POST RegisterStart bien formé.
  it('register: should POST to RegisterStart with the username and credentials', async () => {
    spyOn(navigator.credentials, 'create').and.resolveTo(makeFakeCredential());
    const promise = service.register('Alice');

    const startReq = httpMock.expectOne(startUrl);
    expect(startReq.request.method).toBe('POST');
    expect(startReq.request.body).toEqual({ username: 'Alice' });
    expect(startReq.request.withCredentials).toBeTrue();

    startReq.flush(makeStartResponse());
    await tick();
    httpMock.expectOne(finishUrl).flush(makePasskey()); // on solde le flux
    await promise;
  });

  // A3 : WebAuthn est appelé avec des options formatées (ArrayBuffer).
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

  // A4 : après le WebAuthn, register poste vers RegisterFinish.
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

  // ===== Groupe B — Valeur de retour =====

  // B1 : passkey complète et valide → true.
  it('register: should return true for a valid registered passkey', async () => {
    expect(await runRegister(makePasskey())).toBeTrue();
  });

  // B2 : réponse nulle → false.
  it('register: should return false when RegisterFinish returns null', async () => {
    expect(await runRegister(null)).toBeFalse();
  });

  // B3 : type incorrect → false.
  it('register: should return false when type is not "public-key"', async () => {
    expect(await runRegister(makePasskey({ type: 'public-key-WRONG' }))).toBeFalse();
  });

  // B4 : id manquant → false.
  it('register: should return false when the id is empty', async () => {
    expect(await runRegister(makePasskey({ id: '' }))).toBeFalse();
  });

  // B5 : publicKey manquante → false.
  it('register: should return false when the publicKey is empty', async () => {
    expect(await runRegister(makePasskey({ publicKey: '' }))).toBeFalse();
  });

  // ===== Groupe C — Propagation des erreurs =====

  // C1 : erreur HTTP sur RegisterStart → rejet, pas de WebAuthn ni RegisterFinish.
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

  // C2 : WebAuthn rejeté (annulation) → rejet, pas de RegisterFinish.
  it('register: should reject when WebAuthn is cancelled and skip RegisterFinish', async () => {
    spyOn(navigator.credentials, 'create').and.rejectWith(
      new DOMException('cancelled', 'NotAllowedError'),
    );
    const promise = service.register('Alice');

    httpMock.expectOne(startUrl).flush(makeStartResponse());

    await expectAsync(promise).toBeRejected();
    httpMock.expectNone(finishUrl);
  });

  // C3 : erreur HTTP sur RegisterFinish → rejet.
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

  // --- Fabriques de données factices (login) ---------------------------

  // Réponse simulée de LoginStart. challenge en base64url valide
  // (formatLoginStartOptions le passe à base64urlToBuffer).
  function makeLoginStartResponse() {
    return {
      challenge: 'AAAA',
      rpId: 'localhost',
      timeout: 60000,
      userVerification: 'preferred',
      // pas d'allowCredentials → on évite la branche de mapping
    };
  }

  // Fausse assertion renvoyée par navigator.credentials.get.
  // Les champs binaires sont des ArrayBuffer (passent par bufferToBase64url).
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

  // Déroule un login nominal et renvoie la Promise<boolean>.
  // finishBody pilote la réponse de LoginFinish (présence/absence du token).
  async function runLogin(
    username: string,
    finishBody: unknown,
  ): Promise<boolean> {
    spyOn(navigator.credentials, 'get').and.resolveTo(makeFakeAssertion());
    const promise = service.login(username);

    httpMock.expectOne(loginStartUrl).flush(makeLoginStartResponse());
    await tick(); // formatage + WebAuthn get + POST LoginFinish
    httpMock.expectOne(loginFinishUrl).flush(finishBody as any);

    return promise;
  }

  // ===== Groupe D — Contrat observable (login) =====

  // D1 : POST LoginStart bien formé.
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

  // D2 : WebAuthn get appelé avec challenge converti en ArrayBuffer.
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

  // D3 : POST LoginFinish après l'assertion.
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

  // ===== Groupe E — Succès / échec selon le token =====

  // E1 : token présent → true + effets de bord (sessionStorage + observables).
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

  // E2 : token absent → false + aucun effet de bord.
  it('login: should return false and write nothing when the token is missing', async () => {
    const loggedIn = latest(service.isLoggedIn$);

    const ok = await runLogin('Alice', { token: '', username: 'srvName' });

    expect(ok).toBeFalse();
    expect(sessionStorage.getItem('authToken')).toBeNull();
    expect(sessionStorage.getItem('username')).toBeNull();
    expect(loggedIn.value).toBeFalse();
  });

  // E3 : sessionStorage stocke result.username, mais username$ émet l'argument.
  it('login: should store server username but emit the argument username', async () => {
    const usernameObs = latest(service.username$);

    await runLogin('Alice', { token: 'jwt', username: 'srvName' });

    expect(sessionStorage.getItem('username')).toBe('srvName'); // serveur
    expect(usernameObs.value).toBe('Alice'); // argument
  });

  // ===== Groupe F — Propagation des erreurs (login) =====

  // F1 : erreur HTTP LoginStart → rejet, pas de WebAuthn ni LoginFinish.
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

  // F2 : WebAuthn get rejeté → rejet, pas de LoginFinish.
  it('login: should reject when WebAuthn get is cancelled and skip LoginFinish', async () => {
    spyOn(navigator.credentials, 'get').and.rejectWith(
      new DOMException('cancelled', 'NotAllowedError'),
    );
    const promise = service.login('Alice');

    httpMock.expectOne(loginStartUrl).flush(makeLoginStartResponse());

    await expectAsync(promise).toBeRejected();
    httpMock.expectNone(loginFinishUrl);
  });

  // F3 : erreur HTTP LoginFinish → rejet.
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

  // ===== Groupe G — logout =====

  // G1 : logout efface entièrement le sessionStorage.
  it('logout: should clear sessionStorage on logout', () => {
    sessionStorage.setItem('authToken', 'jwt');
    sessionStorage.setItem('username', 'Alice');

    service.logout();

    expect(sessionStorage.length).toBe(0);
  });

  // G2 : logout émet false sur isLoggedIn$.
  it('logout: should emit false on isLoggedIn$ after logout', () => {
    const loggedIn = latest(service.isLoggedIn$);

    service.logout();

    expect(loggedIn.value).toBeFalse();
  });

  // G3 : logout émet '' sur username$.
  it('logout: should emit an empty username after logout', () => {
    const usernameObs = latest(service.username$);

    service.logout();

    expect(usernameObs.value).toBe('');
  });
});
