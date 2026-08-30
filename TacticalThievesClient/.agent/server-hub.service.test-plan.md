# Plan de test — `ServerHubService` · `startConnection` · `onScoreUpdated` · `onExitReached` · `sendLoadLevelCommand`

Ce document décrit les cas de test unitaires du service
[`ServerHubService`](../src/services/server-hub-service/server-hub.service.ts).
Il couvre pour l'instant **`startConnection`**, **`onScoreUpdated`**,
**`onExitReached`** et **`sendLoadLevelCommand`**.

> **Périmètre progressif.** On couvre ici `startConnection` (groupes A–C),
> `onScoreUpdated` (groupe D), `onExitReached` (groupe E) et `sendLoadLevelCommand`
> (groupe F). Les autres méthodes (`sendSaveLevelCommand`, `sendClaimUnity`,
> `onGameStart`, `onUnityAlreadyTaken`, `onThievesDied`) seront traitées ensuite.

> **⏳ Statut du groupe F.** La description, la stratégie, le tableau de cas et le
> **code des tests** (F1–F3) sont rédigés **dans ce plan**, mais **pas encore
> reportés dans le `.spec.ts`** : ils y seront ajoutés après ta validation.

Le style s'aligne sur les plans/specs existants
([`player-controls.service.test-plan.md`](./player-controls.service.test-plan.md),
[`auth.service.test-plan.md`](./auth.service.test-plan.md) et
[`auth.service.spec.ts`](../src/services/auth-service/auth.service.spec.ts)) :
commentaires en français, dépendances mockées, tests regroupés par intention.

## Ce que fait `startConnection`

```ts
public startConnection(): void {
  this.hubConnection = new signalR.HubConnectionBuilder()
    .withUrl(this.hubURL, {
      transport: signalR.HttpTransportType.WebSockets, // force WebSocket
    })
    .withAutomaticReconnect()
    .build();

  this.hubConnection
    .start()
    .then(() => {
        if (environment.logEnabled)
          console.log('Connected to SignalR Hub')
    })
    .catch((err) => {
      if (environment.logEnabled)
        console.error('Error while starting SignalR connection: ' + err)
    });
}
```

En clair, la méthode :

1. **construit** une `HubConnection` via `HubConnectionBuilder` :
   - `.withUrl(hubURL, { transport: WebSockets })` où
     `hubURL = ${environment.apiURL}/hub` (soit `https://localhost:7186/hub`),
   - `.withAutomaticReconnect()` (reconnexion auto),
   - `.build()` ;
2. **stocke** la connexion dans `this.hubConnection` ;
3. **démarre** la connexion (`start()`), puis :
   - en **succès** → `console.log('Connected to SignalR Hub')` **si** `logEnabled`,
   - en **échec** → `console.error('Error while starting SignalR connection: ' + err)`
     **si** `logEnabled`.

La méthode ne renvoie rien (`void`) et **avale** les erreurs de démarrage (le
`catch` se contente de journaliser) : un échec réseau ne fait donc **pas** planter
l'instanciation du service.

## Le défi : le constructeur déclenche tout

```ts
constructor(private http: HttpClient) {
  this.startConnection()   // ← crée la connexion + start()
  this.onScoreUpdated()    // ← this.hubConnection.on(...)
  this.onExitReached()     // ← this.hubConnection.on(...)
  this.onGameStart()       // ← this.hubConnection.on(...)
  this.onThievesDied()     // ← this.hubConnection.on(...)
}
```

**Simplement instancier le service** appelle `startConnection`, ce qui tenterait
d'ouvrir un vrai WebSocket vers `https://localhost:7186/hub`. Et juste après, les
`on...()` appellent `this.hubConnection.on(...)` : sans connexion valide,
l'instanciation **explose**.

> **Conclusion** : on doit **mocker SignalR** (`HubConnectionBuilder` et la
> `HubConnection` produite) **avant** d'instancier le service. C'est la seule
> frontière externe à neutraliser pour tester `startConnection` en isolation.

## Stratégie de test — boîte noire, SignalR mocké

On **ne peut pas** remplacer `signalR.HubConnectionBuilder` lui-même :
`spyOn(signalR, 'HubConnectionBuilder')` échoue avec
`HubConnectionBuilder is not declared writable or has no setter` — l'export du
module est en **lecture seule** (namespace ES figé).

On espionne donc les **méthodes du prototype** du builder. Le `new
signalR.HubConnectionBuilder()` crée un vrai builder, mais ses méthodes sont
stubbées :

- `withUrl` / `withAutomaticReconnect` → renvoient le builder réel (chaînage via
  `return this`), et on espionne leurs arguments ;
- `build` → renvoie une **fausse `HubConnection`** ;
- la fausse connexion expose :
  - `start()` → une `Promise` qu'on contrôle (résolue ou rejetée selon le cas),
  - `on()` → un simple espion (indispensable pour que les `on...()` du
    constructeur ne plantent pas).

```ts
const proto = signalR.HubConnectionBuilder.prototype;
spyOn(proto, 'withUrl').and.callFake(function (this: signalR.HubConnectionBuilder) { return this; });
spyOn(proto, 'withAutomaticReconnect').and.callFake(function (this: signalR.HubConnectionBuilder) { return this; });
spyOn(proto, 'build').and.returnValue(fakeConnection as any);
```

### Contrôle du timing (`then`/`catch`)

`start()` renvoie une `Promise` ; les callbacks `.then`/`.catch` s'exécutent sur
une **micro-tâche**. On utilise donc `fakeAsync` + `flushMicrotasks()` pour forcer
leur exécution de façon déterministe avant d'asserter la journalisation.

### `HttpClient`

Le service injecte `HttpClient` (pour d'autres méthodes). On le fournit via
`provideHttpClient()` + `provideHttpClientTesting()` ; `startConnection` n'émet
aucune requête HTTP, donc rien à `flush` ici.

### `environment.logEnabled`

Les branches de journalisation dépendent de `environment.logEnabled` (valeur `true`
dans l'environnement courant). Pour tester la branche « silencieuse », on
bascule **temporairement** `logEnabled` à `false` puis on le **restaure** en fin
de test.

## Ce que fait `onScoreUpdated`

```ts
public onScoreUpdated(): void {
  this.hubConnection.on('ScoreUpdated', (gold: number) => {
    if (environment.logEnabled)
      console.log('Score reçu du serveur:', gold);
    this.playerGoldSource.next(gold)
  });
}
```

La méthode **enregistre un listener** sur l'événement serveur `'ScoreUpdated'`.
Quand le serveur pousse un score, le callback :

- journalise `'Score reçu du serveur:'` + la valeur **si** `logEnabled` ;
- **émet** la valeur reçue sur le flux `playerGold$` (via
  `playerGoldSource.next(gold)`).

`playerGold$` est un `BehaviorSubject<number>` initialisé à **0** : tout nouvel
abonné reçoit d'abord `0`, puis les scores successifs.

### Tester un listener : rejouer l'événement

`onScoreUpdated` ne fait rien de visible tant que le serveur n'émet pas
`'ScoreUpdated'`. Comme `hubConnection.on` est un **espion**, on récupère le
callback qui lui a été passé, puis on l'appelle nous-mêmes pour **simuler**
l'événement serveur :

```ts
function getRegisteredHandler(event: string): (...args: any[]) => void {
  const call = fakeConnection.on.calls
    .allArgs()
    .find(([name]) => name === event);
  if (!call) throw new Error(`Aucun listener enregistré pour '${event}'`);
  return call[1]; // le callback (2e argument de on(name, cb))
}
```

> Le listener est enregistré **par le constructeur** (`this.onScoreUpdated()`),
> donc `fakeConnection.on` a déjà reçu `('ScoreUpdated', cb)` au moment du test.
> On observe l'effet en s'abonnant à `service.playerGold$` avant de rejouer
> l'événement.

## Ce que fait `onExitReached`

```ts
public onExitReached() : void {
  this.hubConnection.on('ExitReached', (nextLevel: number) => {
    if (environment.logEnabled)
      console.log("Exit reached by thief")

    this.gameOverMessage.next("You win !!!")
    this.levelBtnMessage.next("Next level")
    this.sendSaveLevelCommand(nextLevel)
  })
}
```

Comme `onScoreUpdated`, la méthode **enregistre un listener** — ici sur
`'ExitReached'`. Mais son callback a **trois** effets (contre un seul pour D) :

1. **émet** `"You win !!!"` sur `gameOverMessage$` ;
2. **émet** `"Next level"` sur `levelBtnMessage$` ;
3. **déclenche** `sendSaveLevelCommand(nextLevel)`, qui **poste** vers le serveur.

Valeurs initiales des deux flux (des `BehaviorSubject`) :

| Flux | Type | Valeur initiale |
|------|------|-----------------|
| `gameOverMessage$` | `BehaviorSubject<string>` | `""` |
| `levelBtnMessage$` | `BehaviorSubject<string>` | `"Restart level"` |

### La nouveauté : un effet HTTP

`sendSaveLevelCommand` effectue un vrai `http.post` :

```ts
public async sendSaveLevelCommand(nextLevel: number) : Promise<void> {
  const authToken = sessionStorage.getItem("authToken");
  const body = { Pseudo: "", CurrentLevel : nextLevel }
  await firstValueFrom(
    this.http.post(`${environment.apiURL}/api/Game/save-level`, body, {
      headers: { Authorization: `Bearer ${authToken}` }
    })
  );
}
```

Rejouer `ExitReached` **émet donc une requête HTTP** vers
`POST .../api/Game/save-level`, avec :

- corps `{ Pseudo: "", CurrentLevel: <nextLevel> }` ;
- header `Authorization: Bearer <authToken>` (lu dans `sessionStorage`).

On introduit donc `HttpTestingController` (`provideHttpClientTesting` est déjà
fourni) pour :

- vérifier URL / méthode / corps / header de la requête ;
- la **solder** (`flush`) et garantir (`verify()` en `afterEach`) qu'aucune
  requête n'est laissée en attente.

> **Conséquence** : tout test qui **rejoue** `ExitReached` doit `flush` la requête
> `save-level` qui en découle, sinon `httpMock.verify()` échouera. Les tests qui
> ne vérifient **que** les valeurs initiales (E2) ne rejouent pas l'événement et
> n'émettent donc aucune requête.

### Nettoyage de `sessionStorage`

`sendSaveLevelCommand` lit `sessionStorage.getItem("authToken")`. On **vide**
`sessionStorage` en `beforeEach`/`afterEach` pour des tests déterministes, et on
pose explicitement `authToken` quand on veut vérifier le header (E4).

## Ce que fait `sendLoadLevelCommand`

```ts
public async sendLoadLevelCommand(sessionId: string, connectionId: string): Promise<void> {
  const authToken = sessionStorage.getItem("authToken");

  const headers: any = {
    "X-Connection-Id": connectionId,
    "X-Session-Id": sessionId
  };

  if (authToken) {
    headers["Authorization"] = `Bearer ${authToken}`;
  }

  const endpoint = authToken === null
    ? `${environment.apiURL}/api/Game/load-random-level`
    : `${environment.apiURL}/api/Game/load-level`;

  await firstValueFrom(this.http.post(endpoint, {}, { headers }));
}
```

Contrairement aux groupes D/E, ce n'est **pas un listener** : c'est une méthode
**appelée directement** (par `onGameStart`, avec `sessionId` et `connectionId`).
Elle poste un **corps vide** `{}` vers un endpoint qui dépend de l'authentification :

| Cas | `authToken` (sessionStorage) | Endpoint | Header `Authorization` |
|-----|------------------------------|----------|------------------------|
| Anonyme | absent (`null`) | `POST .../api/Game/load-random-level` | **absent** |
| Authentifié | présent (`"tok-…"`) | `POST .../api/Game/load-level` | `Bearer <token>` |

Dans **tous** les cas, les headers `X-Connection-Id` (= `connectionId`) et
`X-Session-Id` (= `sessionId`) sont posés à partir des **paramètres** reçus.

### Deux conditions divergentes sur `authToken` — point ouvert

Le choix du header et le choix de l'endpoint n'utilisent **pas** le même test :

- header : `if (authToken)` → **truthy** ;
- endpoint : `authToken === null` → **strictement `null`**.

Ces conditions coïncident pour les deux cas réalistes (`null` vs `"tok-…"`), mais
**divergent** pour `authToken === ""` (chaîne vide) : on obtiendrait l'endpoint
**authentifié** `load-level` **sans** header `Authorization`. En pratique
`sessionStorage.getItem` ne renvoie `""` que si on a explicitement stocké une
chaîne vide — cas peu probable. Je le **signale** (voir F-edge) plutôt que de le
verrouiller : c'est peut-être une incohérence à corriger côté service.

### Stratégie de test

- Frontière mockée : le `HttpClient` via `HttpTestingController` (déjà en place
  depuis le groupe E).
- **Ce n'est pas un observable retourné** mais une `Promise` : `firstValueFrom`
  **s'abonne immédiatement**, donc l'appel émet la requête HTTP de façon
  synchrone. On peut appeler `service.sendLoadLevelCommand(...)` puis
  `httpMock.expectOne(...)` sans `await`, et `flush` la réponse.
- On pilote la branche via `sessionStorage` : rien de posé → cas anonyme ;
  `sessionStorage.setItem("authToken", "tok-…")` → cas authentifié.

## Cas de test

### Groupe A — Création

| Id | Cas | Vérification |
|----|-----|--------------|
| A1 | `should create` | le service s'instancie (`toBeTruthy`) sans ouvrir de vrai WebSocket (SignalR mocké). Valide implicitement que `startConnection` + les `on...()` du constructeur ne plantent pas. |

### Groupe B — Construction de la connexion

| Id | Cas | Vérification |
|----|-----|--------------|
| B1 | URL et transport | `withUrl` appelé avec `https://localhost:7186/hub` et `{ transport: HttpTransportType.WebSockets }`. |
| B2 | reconnexion automatique | `withAutomaticReconnect` appelé. |
| B3 | build | `build` appelé (la connexion est construite). |
| B4 | démarrage | `start` appelé (la connexion est démarrée). |

### Groupe C — Journalisation succès / échec

| Id | Cas | Vérification |
|----|-----|--------------|
| C1 | succès + `logEnabled` | `start()` résolue → `console.log('Connected to SignalR Hub')`. |
| C2 | échec + `logEnabled` | `start()` rejetée → `console.error` reçoit `'Error while starting SignalR connection: ' + err`. |
| C3 | échec « avalé » | `start()` rejetée → **aucune** exception ne remonte (le service reste utilisable, `toBeTruthy`). |
| C4 | `logEnabled = false` | `start()` résolue → **aucun** `console.log`/`console.error`. |

### Groupe D — `onScoreUpdated` (listener `ScoreUpdated`)

| Id | Cas | Vérification |
|----|-----|--------------|
| D1 | listener enregistré | `hubConnection.on` appelé avec `'ScoreUpdated'` et une fonction. |
| D2 | valeur initiale | `playerGold$` émet `0` à l'abonnement (BehaviorSubject). |
| D3 | émission du score reçu | rejouer `ScoreUpdated(42)` → `playerGold$` émet `[0, 42]`. |
| D4 | mises à jour successives | rejouer `10` puis `25` → `playerGold$` émet `[0, 10, 25]` dans l'ordre. |

### Groupe E — `onExitReached` (listener `ExitReached`)

| Id | Cas | Vérification |
|----|-----|--------------|
| E1 | listener enregistré | `hubConnection.on` appelé avec `'ExitReached'` et une fonction. |
| E2 | valeurs initiales | `gameOverMessage$` émet `""` et `levelBtnMessage$` émet `"Restart level"` à l'abonnement. |
| E3 | messages de victoire | rejouer `ExitReached(3)` → `gameOverMessage$` émet `["", "You win !!!"]` et `levelBtnMessage$` émet `["Restart level", "Next level"]` (puis on solde la requête `save-level`). |
| E4 | commande de sauvegarde | rejouer `ExitReached(5)` (avec `authToken='tok-123'`) → `POST .../api/Game/save-level`, corps `{ Pseudo: "", CurrentLevel: 5 }`, header `Authorization: Bearer tok-123`. |

### Groupe F — `sendLoadLevelCommand` (appel direct)

| Id | Cas | Vérification |
|----|-----|--------------|
| F1 | anonyme → niveau aléatoire | sans `authToken`, `sendLoadLevelCommand('sess-1','conn-1')` → `POST .../api/Game/load-random-level`, corps `{}`, `X-Session-Id='sess-1'`, `X-Connection-Id='conn-1'`, **pas** de header `Authorization`. |
| F2 | authentifié → niveau sauvegardé | `authToken='tok-9'` → `POST .../api/Game/load-level`, corps `{}`, headers `X-Session-Id`/`X-Connection-Id` + `Authorization='Bearer tok-9'`. |
| F3 | propagation d'erreur | si le `POST` répond `500`, la `Promise` renvoyée **rejette** (l'erreur n'est pas avalée). |
| F-edge | `authToken=''` (point ouvert) | comportement **actuel** : endpoint `load-level` **sans** header `Authorization` (divergence `=== null` vs *truthy*). À trancher : verrouiller le comportement tel quel, ou corriger le service ? |

> **Décision attendue pour F-edge** : je propose de **ne pas** l'inclure dans le
> premier jet (comportement douteux), et d'ouvrir plutôt une correction du service
> si tu confirmes que c'est un bug. Dis-moi.

## Code des tests

```ts
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

  // ===== Groupe B — Construction de la connexion =====

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

  // ===== Groupe C — Journalisation succès / échec =====

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
```

> **F-edge (`authToken === ''`)** : non couvert volontairement — le comportement
> actuel (endpoint `load-level` sans header `Authorization`) semble être une
> incohérence du service. À traiter séparément si confirmé comme bug.

## Exécution

```bash
npm test -- --include='**/server-hub.service.spec.ts' --watch=false
```

Tous les cas A1, B1–B4, C1–C4, D1–D4, E1–E4 et F1–F3 doivent passer, sans casser
les tests existants.

## Notes / points ouverts

- **Mock obligatoire de SignalR** : le constructeur appelle `startConnection` puis
  quatre `on...()`. Sans mock, l'instanciation ouvre un vrai WebSocket et les
  `on(...)` échouent. On neutralise cette frontière en stubbant les méthodes du
  prototype du builder.
- **Pas de `spyOn(signalR, 'HubConnectionBuilder')`** : le namespace du module est
  en lecture seule → jasmine lève `is not declared writable or has no setter`.
  D'où le choix d'espionner `HubConnectionBuilder.prototype.{withUrl,
  withAutomaticReconnect, build}` (méthodes bien réinscriptibles).
- **Tester un listener en le rejouant** : `onScoreUpdated` n'a d'effet que
  lorsque le serveur émet `'ScoreUpdated'`. On récupère le callback passé à
  l'espion `on` (`on.calls.allArgs()`) et on l'appelle nous-mêmes ; l'effet
  (`playerGoldSource.next`) est observé via un abonnement à `playerGold$`. Même
  patron réutilisable pour `onExitReached`, `onGameStart`, `onThievesDied`.
- **`playerGold$` = BehaviorSubject(0)** : tout abonné reçoit **d'abord** `0`,
  d'où les tableaux attendus `[0, 42]` / `[0, 10, 25]` (D3/D4). Si un jour la
  valeur initiale changeait, ces tests le signaleraient. Idem pour
  `gameOverMessage$` (`""`) et `levelBtnMessage$` (`"Restart level"`) en E2/E3.
- **Listener + effet HTTP (E)** : `onExitReached` combine émission d'observables
  **et** appel `sendSaveLevelCommand`. Rejouer l'événement émet une requête
  `POST save-level` ; tout test qui rejoue doit donc la `flush` (E3/E4), sinon
  `httpMock.verify()` (afterEach) échoue. E2 ne rejoue pas → aucune requête.
- **`afterEach` `httpMock.verify()` + `sessionStorage.clear()`** : introduits avec
  le groupe E. `verify()` détecte toute requête inattendue ou non soldée ;
  `clear()` évite qu'un `authToken` résiduel (E4) fausse un test suivant. La
  construction du service n'émet **aucune** requête HTTP (les `on...()` se
  contentent d'enregistrer des listeners), donc ces ajouts sont sans risque pour
  les groupes A–D.
- **`fakeConnection.on` indispensable** : même si on ne teste que
  `startConnection`, les `onScoreUpdated/onExitReached/onGameStart/onThievesDied`
  du constructeur appellent `hubConnection.on(...)`. L'espion `on` évite un crash
  au montage.
- **Timing des promesses** : `.then`/`.catch` de `start()` tournent sur une
  micro-tâche → `fakeAsync` + `flushMicrotasks()` rendent C1/C2/C4 déterministes.
- **Erreurs avalées (C3)** : `startConnection` ne propage pas l'échec de `start()`
  (le `catch` journalise seulement). C'est un choix de conception : le service
  s'instancie même serveur injoignable. C3 verrouille ce comportement.
- **Restaurer `environment.logEnabled`** : C4 modifie un objet **partagé** ; sans
  restauration, on pollue les tests suivants. D'où la sauvegarde/restauration.
- **`connectionId`** : posé à `'conn-test'` sur la fausse connexion pour anticiper
  les tests futurs (`onGameStart`, `sendClaimUnity`) qui le lisent ; inutile pour
  `startConnection` seul, mais évite un `undefined` gênant plus tard.
- **Prochaines étapes** : couvrir les listeners (`on...`) en déclenchant les
  callbacks enregistrés sur `fakeConnection.on` (récupérables via
  `on.calls.allArgs()`), puis les commandes HTTP (`sendLoadLevelCommand`,
  `sendSaveLevelCommand`, `sendClaimUnity`) avec `HttpTestingController`, sur le
  modèle de [`player-controls.service.test-plan.md`](./player-controls.service.test-plan.md).
```