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

  // Endpoint de sauvegarde de niveau (déclenché par onExitReached).
  const saveLevelUrl = `${environment.apiURL}/api/Game/save-level`;

  // Faux HubConnection renvoyé par build().
  // `start` est réassignable par test pour simuler succès/échec.
  let fakeConnection: {
    on: jasmine.Spy;
    start: jasmine.Spy;
    connectionId: string | null;
  };

  // Espions posés sur le PROTOTYPE du builder. On ne peut pas remplacer
  // `signalR.HubConnectionBuilder` (export de module en lecture seule), alors on
  // stubbe ses méthodes : withUrl/withAutomaticReconnect renvoient le builder réel
  // (chaînage `this`), build renvoie notre fausse connexion.
  let withUrlSpy: jasmine.Spy;
  let withAutoReconnectSpy: jasmine.Spy;
  let buildSpy: jasmine.Spy;

  const hubURL = `${environment.apiURL}/hub`; // https://localhost:7186/hub

  // Récupère le callback enregistré via hubConnection.on(<event>, cb).
  // Permet de « rejouer » un événement serveur en appelant ce callback.
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
    sessionStorage.clear(); // état de session déterministe avant chaque test

    fakeConnection = {
      on: jasmine.createSpy('on'),
      start: jasmine.createSpy('start').and.returnValue(Promise.resolve()),
      connectionId: 'conn-test',
    };

    const proto = signalR.HubConnectionBuilder.prototype;
    withUrlSpy = spyOn(proto, 'withUrl').and.callFake(function (
      this: signalR.HubConnectionBuilder,
    ) {
      return this; // chaînage : renvoie le builder réel
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
    // L'injection déclenche le constructeur → startConnection() + on...().
    service = TestBed.inject(ServerHubService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify(); // aucune requête HTTP laissée en attente
    sessionStorage.clear();
  });

  // ===== Groupe A — Création =====

  // A1 : le service s'instancie sans ouvrir de vrai WebSocket (SignalR mocké).
  it('should create', () => {
    expect(service).toBeTruthy();
  });

  // ===== Groupe B — Construction de la connexion (startConnection) =====

  // B1 : withUrl reçoit l'URL du hub et force le transport WebSocket.
  it('should configure the hub URL with the WebSocket transport', () => {
    expect(withUrlSpy).toHaveBeenCalledWith(hubURL, {
      transport: signalR.HttpTransportType.WebSockets,
    });
  });

  // B2 : la reconnexion automatique est activée.
  it('should enable automatic reconnect', () => {
    expect(withAutoReconnectSpy).toHaveBeenCalled();
  });

  // B3 : la connexion est construite via build().
  it('should build the hub connection', () => {
    expect(buildSpy).toHaveBeenCalled();
  });

  // B4 : la connexion est démarrée via start().
  it('should start the hub connection', () => {
    expect(fakeConnection.start).toHaveBeenCalled();
  });

  // ===== Groupe C — Journalisation succès / échec (startConnection) =====

  // C1 : au succès de start(), on logue le message de connexion (logEnabled=true).
  it('should log a success message when the connection starts', fakeAsync(() => {
    const logSpy = spyOn(console, 'log');
    fakeConnection.start.and.returnValue(Promise.resolve());

    service.startConnection(); // relance avec un then contrôlé
    flushMicrotasks();         // force l'exécution du .then

    expect(logSpy).toHaveBeenCalledWith('Connected to SignalR Hub');
  }));

  // C2 : à l'échec de start(), on logue une erreur explicite (logEnabled=true).
  it('should log an error message when the connection fails', fakeAsync(() => {
    const errorSpy = spyOn(console, 'error');
    fakeConnection.start.and.returnValue(Promise.reject('net down'));

    service.startConnection();
    flushMicrotasks();         // force l'exécution du .catch

    expect(errorSpy).toHaveBeenCalledWith(
      'Error while starting SignalR connection: net down',
    );
  }));

  // C3 : un échec de start() est « avalé » — aucune exception ne remonte.
  it('should swallow start() failures (service stays usable)', fakeAsync(() => {
    spyOn(console, 'error');
    fakeConnection.start.and.returnValue(Promise.reject('boom'));

    expect(() => {
      service.startConnection();
      flushMicrotasks();
    }).not.toThrow();
    expect(service).toBeTruthy();
  }));

  // C4 : quand logEnabled=false, aucune journalisation (succès comme échec).
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

    environment.logEnabled = original; // restauration
  }));

  // ===== Groupe D — onScoreUpdated (listener 'ScoreUpdated') =====

  // D1 : un listener est bien enregistré sur l'événement 'ScoreUpdated'.
  it('should register a listener for the ScoreUpdated event', () => {
    expect(fakeConnection.on).toHaveBeenCalledWith(
      'ScoreUpdated',
      jasmine.any(Function),
    );
  });

  // D2 : playerGold$ démarre à 0 (valeur initiale du BehaviorSubject).
  it('should expose an initial gold value of 0', () => {
    let value: number | undefined;
    service.playerGold$.subscribe((v) => (value = v));
    expect(value).toBe(0);
  });

  // D3 : quand l'événement arrive, la valeur reçue est poussée sur playerGold$.
  it('should push the received gold onto playerGold$', () => {
    const values: number[] = [];
    service.playerGold$.subscribe((v) => values.push(v));

    getRegisteredHandler('ScoreUpdated')(42);

    // 0 = valeur initiale, puis 42 = score reçu du serveur.
    expect(values).toEqual([0, 42]);
  });

  // D4 : plusieurs mises à jour successives sont toutes émises, dans l'ordre.
  it('should emit each successive score update', () => {
    const handler = getRegisteredHandler('ScoreUpdated');
    const values: number[] = [];
    service.playerGold$.subscribe((v) => values.push(v));

    handler(10);
    handler(25);

    expect(values).toEqual([0, 10, 25]);
  });

  // ===== Groupe E — onExitReached (listener 'ExitReached') =====

  // E1 : un listener est bien enregistré sur l'événement 'ExitReached'.
  it('should register a listener for the ExitReached event', () => {
    expect(fakeConnection.on).toHaveBeenCalledWith(
      'ExitReached',
      jasmine.any(Function),
    );
  });

  // E2 : valeurs initiales des deux flux (BehaviorSubject), sans rejouer
  // l'événement → aucune requête HTTP n'est émise.
  it('should expose the initial gameOver and level-button messages', () => {
    let gameOver: string | undefined;
    let btn: string | undefined;
    service.gameOverMessage$.subscribe((v) => (gameOver = v));
    service.levelBtnMessage$.subscribe((v) => (btn = v));

    expect(gameOver).toBe('');
    expect(btn).toBe('Restart level');
  });

  // E3 : rejouer ExitReached émet les messages de victoire sur les deux flux.
  it('should emit the win message and the next-level button on ExitReached', () => {
    const gameOver: string[] = [];
    const btn: string[] = [];
    service.gameOverMessage$.subscribe((v) => gameOver.push(v));
    service.levelBtnMessage$.subscribe((v) => btn.push(v));

    getRegisteredHandler('ExitReached')(3);

    expect(gameOver).toEqual(['', 'You win !!!']);
    expect(btn).toEqual(['Restart level', 'Next level']);

    // sendSaveLevelCommand a émis une requête : on la solde pour satisfaire verify().
    httpMock.expectOne(saveLevelUrl).flush({});
  });

  // E4 : rejouer ExitReached déclenche sendSaveLevelCommand (POST save-level).
  it('should POST the next level to the save-level endpoint on ExitReached', () => {
    sessionStorage.setItem('authToken', 'tok-123');

    getRegisteredHandler('ExitReached')(5);

    const req = httpMock.expectOne(saveLevelUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ Pseudo: '', CurrentLevel: 5 });
    expect(req.request.headers.get('Authorization')).toBe('Bearer tok-123');
    req.flush({});
  });

  // ===== Groupe F — sendLoadLevelCommand (appel direct) =====

  // Endpoints de chargement de niveau (dépendent de l'authentification).
  const loadRandomLevelUrl = `${environment.apiURL}/api/Game/load-random-level`;
  const loadLevelUrl = `${environment.apiURL}/api/Game/load-level`;

  // F1 : sans authToken → niveau aléatoire, sans header Authorization.
  it('should POST to load-random-level with session/connection headers when anonymous', () => {
    // sessionStorage déjà vidé par beforeEach → pas d'authToken.
    service.sendLoadLevelCommand('sess-1', 'conn-1');

    const req = httpMock.expectOne(loadRandomLevelUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({});
    expect(req.request.headers.get('X-Session-Id')).toBe('sess-1');
    expect(req.request.headers.get('X-Connection-Id')).toBe('conn-1');
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush({});
  });

  // F2 : avec authToken → niveau sauvegardé, header Authorization présent.
  it('should POST to load-level with the Authorization header when authenticated', () => {
    sessionStorage.setItem('authToken', 'tok-9');

    service.sendLoadLevelCommand('sess-2', 'conn-2');

    const req = httpMock.expectOne(loadLevelUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({});
    expect(req.request.headers.get('X-Session-Id')).toBe('sess-2');
    expect(req.request.headers.get('X-Connection-Id')).toBe('conn-2');
    expect(req.request.headers.get('Authorization')).toBe('Bearer tok-9');
    req.flush({});
  });

  // F3 : une erreur HTTP fait rejeter la Promise (l'erreur n'est pas avalée).
  it('should reject when the load request fails', async () => {
    const promise = service.sendLoadLevelCommand('sess-3', 'conn-3');

    httpMock
      .expectOne(loadRandomLevelUrl)
      .flush('boom', { status: 500, statusText: 'Server Error' });

    await expectAsync(promise).toBeRejected();
  });
});
