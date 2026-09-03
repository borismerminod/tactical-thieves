import { TestBed, fakeAsync, flushMicrotasks } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import * as signalR from '@microsoft/signalr';

import { ServerHubService } from './server-hub.service';
import { environment } from '../../environments/environment';

describe('ServerHubService', () => {
  let service: ServerHubService;
  let httpMock: HttpTestingController;

  // Level-save endpoint (triggered by onExitReached).
  const saveLevelUrl = `${environment.apiURL}/api/Game/save-level`;

  // Fake HubConnection returned by build().
  // `start` is reassignable per test to simulate success/failure.
  let fakeConnection: {
    on: jasmine.Spy;
    start: jasmine.Spy;
    connectionId: string | null;
  };

  // Spies placed on the builder's PROTOTYPE. We cannot replace
  // `signalR.HubConnectionBuilder` (read-only module export), so we
  // stub its methods: withUrl/withAutomaticReconnect return the real builder
  // (`this` chaining), build returns our fake connection.
  let withUrlSpy: jasmine.Spy;
  let withAutoReconnectSpy: jasmine.Spy;
  let buildSpy: jasmine.Spy;

  const hubURL = `${environment.apiURL}/hub`; // https://localhost:7186/hub

  // Retrieves the callback registered via hubConnection.on(<event>, cb).
  // Lets us "replay" a server event by calling that callback.
  function getRegisteredHandler(event: string): (...args: any[]) => void {
    const call = fakeConnection.on.calls
      .allArgs()
      .find(([name]) => name === event);
    if (!call) {
      throw new Error(`Aucun listener enregistré pour l'événement '${event}'`);
    }
    return call[1];
  }

  beforeEach(() => {
    sessionStorage.clear(); // deterministic session state before each test

    fakeConnection = {
      on: jasmine.createSpy('on'),
      start: jasmine.createSpy('start').and.returnValue(Promise.resolve()),
      connectionId: 'conn-test',
    };

    const proto = signalR.HubConnectionBuilder.prototype;
    withUrlSpy = spyOn(proto, 'withUrl').and.callFake(function (
      this: signalR.HubConnectionBuilder,
    ) {
      return this; // chaining: returns the real builder
    });
    withAutoReconnectSpy = spyOn(proto, 'withAutomaticReconnect').and.callFake(
      function (this: signalR.HubConnectionBuilder) {
        return this;
      },
    );
    buildSpy = spyOn(proto, 'build').and.returnValue(fakeConnection as any);

    TestBed.configureTestingModule({
      providers: [
        ServerHubService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    // Injection triggers the constructor → startConnection() + on...().
    service = TestBed.inject(ServerHubService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify(); // no HTTP request left pending
    sessionStorage.clear();
  });

  // ===== Group A — Creation =====

  // A1: the service instantiates without opening a real WebSocket (SignalR mocked).
  it('should create', () => {
    expect(service).toBeTruthy();
  });

  // ===== Group B — Connection setup (startConnection) =====

  // B1: withUrl receives the hub URL and forces the WebSocket transport.
  it('should configure the hub URL with the WebSocket transport', () => {
    expect(withUrlSpy).toHaveBeenCalledWith(hubURL, {
      transport: signalR.HttpTransportType.WebSockets,
    });
  });

  // B2: automatic reconnect is enabled.
  it('should enable automatic reconnect', () => {
    expect(withAutoReconnectSpy).toHaveBeenCalled();
  });

  // B3: the connection is built via build().
  it('should build the hub connection', () => {
    expect(buildSpy).toHaveBeenCalled();
  });

  // B4: the connection is started via start().
  it('should start the hub connection', () => {
    expect(fakeConnection.start).toHaveBeenCalled();
  });

  // ===== Group C — Success / failure logging (startConnection) =====

  // C1: on start() success, we log the connection message (logEnabled=true).
  it('should log a success message when the connection starts', fakeAsync(() => {
    const logSpy = spyOn(console, 'log');
    fakeConnection.start.and.returnValue(Promise.resolve());

    service.startConnection(); // relaunch with a controlled then
    flushMicrotasks();         // force the .then to run

    expect(logSpy).toHaveBeenCalledWith('Connected to SignalR Hub');
  }));

  // C2: on start() failure, we log an explicit error (logEnabled=true).
  it('should log an error message when the connection fails', fakeAsync(() => {
    const errorSpy = spyOn(console, 'error');
    fakeConnection.start.and.returnValue(Promise.reject('net down'));

    service.startConnection();
    flushMicrotasks();         // force the .catch to run

    expect(errorSpy).toHaveBeenCalledWith(
      'Error while starting SignalR connection: net down',
    );
  }));

  // C3: a start() failure is "swallowed" — no exception bubbles up.
  it('should swallow start() failures (service stays usable)', fakeAsync(() => {
    spyOn(console, 'error');
    fakeConnection.start.and.returnValue(Promise.reject('boom'));

    expect(() => {
      service.startConnection();
      flushMicrotasks();
    }).not.toThrow();
    expect(service).toBeTruthy();
  }));

  // C4: when logEnabled=false, nothing is logged (success or failure).
  it('should not log anything when logEnabled is false', fakeAsync(() => {
    const original = environment.logEnabled;
    environment.logEnabled = false;

    const logSpy = spyOn(console, 'log');
    const errorSpy = spyOn(console, 'error');
    fakeConnection.start.and.returnValue(Promise.resolve());

    service.startConnection();
    flushMicrotasks();

    expect(logSpy).not.toHaveBeenCalledWith('Connected to SignalR Hub');
    expect(errorSpy).not.toHaveBeenCalled();

    environment.logEnabled = original; // restore
  }));

  // ===== Group D — onScoreUpdated (listener 'ScoreUpdated') =====

  // D1: a listener is registered on the 'ScoreUpdated' event.
  it('should register a listener for the ScoreUpdated event', () => {
    expect(fakeConnection.on).toHaveBeenCalledWith(
      'ScoreUpdated',
      jasmine.any(Function),
    );
  });

  // D2: playerGold$ starts at 0 (the BehaviorSubject's initial value).
  it('should expose an initial gold value of 0', () => {
    let value: number | undefined;
    service.playerGold$.subscribe((v) => (value = v));
    expect(value).toBe(0);
  });

  // D3: when the event arrives, the received value is pushed onto playerGold$.
  it('should push the received gold onto playerGold$', () => {
    const values: number[] = [];
    service.playerGold$.subscribe((v) => values.push(v));

    getRegisteredHandler('ScoreUpdated')(42);

    // 0 = initial value, then 42 = score received from the server.
    expect(values).toEqual([0, 42]);
  });

  // D4: several successive updates are all emitted, in order.
  it('should emit each successive score update', () => {
    const handler = getRegisteredHandler('ScoreUpdated');
    const values: number[] = [];
    service.playerGold$.subscribe((v) => values.push(v));

    handler(10);
    handler(25);

    expect(values).toEqual([0, 10, 25]);
  });

  // ===== Group E — onExitReached (listener 'ExitReached') =====

  // E1: a listener is registered on the 'ExitReached' event.
  it('should register a listener for the ExitReached event', () => {
    expect(fakeConnection.on).toHaveBeenCalledWith(
      'ExitReached',
      jasmine.any(Function),
    );
  });

  // E2: initial values of the two streams (BehaviorSubject), without replaying
  // the event → no HTTP request is emitted.
  it('should expose the initial gameOver and level-button messages', () => {
    let gameOver: string | undefined;
    let btn: string | undefined;
    service.gameOverMessage$.subscribe((v) => (gameOver = v));
    service.levelBtnMessage$.subscribe((v) => (btn = v));

    expect(gameOver).toBe('');
    expect(btn).toBe('Restart level');
  });

  // E3: replaying ExitReached emits the victory messages on both streams.
  it('should emit the win message and the next-level button on ExitReached', () => {
    const gameOver: string[] = [];
    const btn: string[] = [];
    service.gameOverMessage$.subscribe((v) => gameOver.push(v));
    service.levelBtnMessage$.subscribe((v) => btn.push(v));

    getRegisteredHandler('ExitReached')(3);

    expect(gameOver).toEqual(['', 'You win !!!']);
    expect(btn).toEqual(['Restart level', 'Next level']);

    // sendSaveLevelCommand emitted a request: we settle it to satisfy verify().
    httpMock.expectOne(saveLevelUrl).flush({});
  });

  // E4: replaying ExitReached triggers sendSaveLevelCommand (POST save-level).
  it('should POST the next level to the save-level endpoint on ExitReached', () => {
    sessionStorage.setItem('authToken', 'tok-123');

    getRegisteredHandler('ExitReached')(5);

    const req = httpMock.expectOne(saveLevelUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ Pseudo: '', CurrentLevel: 5 });
    expect(req.request.headers.get('Authorization')).toBe('Bearer tok-123');
    req.flush({});
  });

  // ===== Group F — onGameStart (listener 'GameStart') =====

  // Loading endpoints (triggered indirectly by the onGameStart guard).
  const loadRandomLevelUrl = `${environment.apiURL}/api/Game/load-random-level`;
  const loadLevelUrl = `${environment.apiURL}/api/Game/load-level`;

  // F1: a listener is registered on the 'GameStart' event.
  it('should register a listener for the GameStart event', () => {
    expect(fakeConnection.on).toHaveBeenCalledWith(
      'GameStart',
      jasmine.any(Function),
    );
  });

  // F2: GameStart resets the three streams to their starting values.
  it('should reset gameOver, gold and level-button messages on GameStart', () => {
    // Dirty the state via public events.
    getRegisteredHandler('ScoreUpdated')(50);
    getRegisteredHandler('ExitReached')(2);
    httpMock.expectOne(saveLevelUrl).flush({}); // settle the ExitReached save-level

    // sessionId not set (beforeEach) → guard false, no loading.
    getRegisteredHandler('GameStart')('sess-x');

    let gold: number | undefined;
    let gameOver: string | undefined;
    let btn: string | undefined;
    service.playerGold$.subscribe((v) => (gold = v));
    service.gameOverMessage$.subscribe((v) => (gameOver = v));
    service.levelBtnMessage$.subscribe((v) => (btn = v));

    expect(gold).toBe(0);
    expect(gameOver).toBe('');
    expect(btn).toBe('Restart level');
  });

  // F3: guard true (sessionId matches + connectionId not null) → loading.
  it('should load the level when the session id matches and a connection id exists', () => {
    sessionStorage.setItem('sessionId', 'sess-42');
    // fakeConnection.connectionId === 'conn-test' (not null).

    getRegisteredHandler('GameStart')('sess-42');

    const req = httpMock.expectOne(loadRandomLevelUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.headers.get('X-Session-Id')).toBe('sess-42');
    expect(req.request.headers.get('X-Connection-Id')).toBe('conn-test');
    req.flush({});
  });

  // F4: different sessionId → guard false, no loading.
  it('should not load the level when the session id does not match', () => {
    sessionStorage.setItem('sessionId', 'sess-A');

    getRegisteredHandler('GameStart')('sess-B');

    httpMock.expectNone(loadRandomLevelUrl);
    httpMock.expectNone(loadLevelUrl);
    expect().nothing(); // expectNone self-verifies: we declare the intent to Jasmine
  });

  // F5: connectionId null → guard false, no loading.
  it('should not load the level when there is no connection id', () => {
    sessionStorage.setItem('sessionId', 'sess-42');
    fakeConnection.connectionId = null;

    getRegisteredHandler('GameStart')('sess-42');

    httpMock.expectNone(loadRandomLevelUrl);
    httpMock.expectNone(loadLevelUrl);
    expect().nothing(); // expectNone self-verifies: we declare the intent to Jasmine
  });

  // ===== Group G — onThievesDied (listener 'ThievesDied') =====

  // G1: a listener is registered on the 'ThievesDied' event.
  it('should register a listener for the ThievesDied event', () => {
    expect(fakeConnection.on).toHaveBeenCalledWith(
      'ThievesDied',
      jasmine.any(Function),
    );
  });

  // G2: replaying ThievesDied emits the failure message on gameOverMessage$.
  it('should emit the retry message on ThievesDied', () => {
    const messages: string[] = [];
    service.gameOverMessage$.subscribe((v) => messages.push(v));

    getRegisteredHandler('ThievesDied')();

    // "" = initial value, then "Try again !!!" = failure message.
    expect(messages).toEqual(['', 'Try again !!!']);
  });

  // ===== Group H — onUnityAlreadyTaken (listener 'UnityAlreadyTaken') =====

  // H1: the constructor does NOT call onUnityAlreadyTaken → no listener at
  // mount time (open point: the 'UnityAlreadyTaken' event is currently ignored).
  it('should NOT register the UnityAlreadyTaken listener on construction', () => {
    expect(fakeConnection.on).not.toHaveBeenCalledWith(
      'UnityAlreadyTaken',
      jasmine.any(Function),
    );
  });

  // H2: calling onUnityAlreadyTaken() registers the listener.
  it('should register the UnityAlreadyTaken listener when called explicitly', () => {
    service.onUnityAlreadyTaken();

    expect(fakeConnection.on).toHaveBeenCalledWith(
      'UnityAlreadyTaken',
      jasmine.any(Function),
    );
  });

  // H3: replaying the event logs the message (logEnabled=true).
  it('should log when the Unity is already taken', () => {
    const logSpy = spyOn(console, 'log');
    service.onUnityAlreadyTaken();

    getRegisteredHandler('UnityAlreadyTaken')();

    expect(logSpy).toHaveBeenCalledWith('Unity déjà prise par un autre client');
  });

  // H4: when logEnabled=false, the message is not logged.
  it('should stay silent when logEnabled is false', () => {
    const original = environment.logEnabled;
    environment.logEnabled = false;

    const logSpy = spyOn(console, 'log');
    service.onUnityAlreadyTaken();

    getRegisteredHandler('UnityAlreadyTaken')();

    expect(logSpy).not.toHaveBeenCalledWith(
      'Unity déjà prise par un autre client',
    );

    environment.logEnabled = original; // restore
  });

});
