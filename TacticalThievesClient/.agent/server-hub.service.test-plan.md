# Plan de test — `ServerHubService` (service complet)

Ce document décrit les cas de test unitaires du service
[`ServerHubService`](../src/services/server-hub-service/server-hub.service.ts).
Il couvre **`startConnection`**, **`onScoreUpdated`**, **`onExitReached`**,
**`onGameStart`**, **`onThievesDied`** et **`onUnityAlreadyTaken`** — soit tous
les listeners publics du service.

> **Périmètre.** `startConnection` (groupes A–C), `onScoreUpdated` (groupe D),
> `onExitReached` (groupe E), `onGameStart` (groupe F), `onThievesDied` (groupe G)
> et `onUnityAlreadyTaken` (groupe H).
>
> Les commandes HTTP `sendLoadLevelCommand`, `sendSaveLevelCommand` et
> `sendClaimUnity` sont **privées** : elles ne sont pas testées en appel direct,
> mais **indirectement** via les listeners publics qui les déclenchent
> (`sendSaveLevelCommand` via `onExitReached`, groupe E ; `sendLoadLevelCommand`
> via `onGameStart`, groupe F).

> **✅ Statut.** Tous les groupes (A–H) sont rédigés dans ce plan **et** reportés
> dans le `.spec.ts`. Le service est intégralement couvert côté listeners publics.

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

## Ce que fait `onGameStart`

```ts
public onGameStart() : void {
  this.hubConnection.on('GameStart', (sessionID: string) => {
    console.log("Game start")
    this.gameOverMessage.next("")
    this.playerGoldSource.next(0)
    this.levelBtnMessage.next("Restart level")

    if (sessionStorage.getItem("sessionId") === sessionID && this.hubConnection.connectionId !== null) {
      this.sendLoadLevelCommand(sessionID, this.hubConnection.connectionId)
    }
  })
}
```

Comme D et E, la méthode **enregistre un listener** — ici sur `'GameStart'`. Son
callback **réinitialise l'état de jeu** puis **charge conditionnellement** le niveau :

1. journalise `"Game start"` (⚠️ **sans** garde `logEnabled`, contrairement aux
   autres journaux du service — voir points ouverts) ;
2. **réinitialise** les trois flux à leur valeur de départ :
   - `gameOverMessage$` → `""`,
   - `playerGold$` → `0`,
   - `levelBtnMessage$` → `"Restart level"` ;
3. **si** `sessionStorage.getItem("sessionId") === sessionID` **et**
   `hubConnection.connectionId !== null` → appelle `sendLoadLevelCommand(sessionID,
   connectionId)`, qui **poste** vers le serveur (chargement du niveau).

### La garde de chargement

Le chargement n'est déclenché que si **deux** conditions sont réunies :

| Condition | Source | Pilotée dans les tests par |
|-----------|--------|----------------------------|
| `sessionId` stocké == `sessionID` reçu | `sessionStorage.getItem("sessionId")` | `sessionStorage.setItem("sessionId", …)` |
| `connectionId` non `null` | `hubConnection.connectionId` | `fakeConnection.connectionId` |

`sendLoadLevelCommand` étant **privée**, on ne l'appelle pas directement : on
observe son **effet HTTP**. Sans `authToken` en session, l'endpoint est
`POST .../api/Game/load-random-level`, avec les headers `X-Session-Id`
(= `sessionID`) et `X-Connection-Id` (= `connectionId`).

> **Conséquence** : un test qui rejoue `GameStart` **avec** garde vraie émet une
> requête `load-random-level` à `flush` (F3). Les tests à garde fausse (sessionId
> différent, ou `connectionId` null) n'émettent **aucune** requête (F4/F5).

## Ce que fait `onThievesDied`

```ts
public onThievesDied() : void {
  this.hubConnection.on("ThievesDied", () => {
    if (environment.logEnabled)
      console.log("All thieves died")
    this.gameOverMessage.next("Try again !!!")
  })
}
```

Comme D/E/F, la méthode **enregistre un listener** — ici sur `'ThievesDied'` — et
elle est **appelée par le constructeur** (`this.onThievesDied()`), donc le listener
est déjà en place au montage. Son callback :

- journalise `"All thieves died"` **si** `logEnabled` ;
- **émet** `"Try again !!!"` sur `gameOverMessage$`.

`gameOverMessage$` est un `BehaviorSubject<string>` initialisé à `""` : un abonné
qui rejoue ensuite `ThievesDied` observe la séquence `["", "Try again !!!"]`. Aucun
effet HTTP : le groupe G n'émet donc **aucune** requête.

## Ce que fait `onUnityAlreadyTaken`

```ts
public onUnityAlreadyTaken(): void {
  this.hubConnection.on("UnityAlreadyTaken", () => {
    if (environment.logEnabled)
      console.log("Unity déjà prise par un autre client");
  });
}
```

La méthode enregistre un listener sur `'UnityAlreadyTaken'` dont le callback **ne
fait que journaliser** (si `logEnabled`) — aucun flux, aucun effet HTTP.

> **⚠️ Point ouvert — listener non branché.** Contrairement à `onScoreUpdated`,
> `onExitReached`, `onGameStart` et `onThievesDied`, **`onUnityAlreadyTaken` n'est
> PAS appelée par le constructeur**. Son listener n'est donc **jamais enregistré**
> au montage : l'événement serveur `'UnityAlreadyTaken'` est **actuellement ignoré**
> par l'application. C'est probablement un oubli de câblage (le constructeur devrait
> appeler `this.onUnityAlreadyTaken()`). On le **signale** et on le **verrouille**
> par un test (H1 : aucun listener au montage), puis on teste le comportement de la
> méthode en l'appelant **explicitement** (H2–H4).

### Tester une méthode au callback purement journalisant

Comme il n'y a pas d'effet observable (flux/HTTP), on teste `onUnityAlreadyTaken`
via un **espion sur `console.log`** : on appelle `service.onUnityAlreadyTaken()`
pour enregistrer le listener, on récupère le callback via `getRegisteredHandler`,
on le rejoue, puis on vérifie la journalisation. La branche « silencieuse » se teste
en basculant temporairement `environment.logEnabled` à `false` (restauré en fin de
test, comme en C4).

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

### Groupe F — `onGameStart` (listener `GameStart`)

| Id | Cas | Vérification |
|----|-----|--------------|
| F1 | listener enregistré | `hubConnection.on` appelé avec `'GameStart'` et une fonction. |
| F2 | réinitialisation de l'état | après avoir « sali » l'état (`ScoreUpdated(50)`, `ExitReached(2)` soldé), rejouer `GameStart('sess-x')` (sessionId non posé → pas de chargement) → `playerGold$`=`0`, `gameOverMessage$`=`""`, `levelBtnMessage$`=`"Restart level"`. |
| F3 | chargement si garde vraie | `sessionId='sess-42'` en session + `connectionId='conn-test'` → rejouer `GameStart('sess-42')` → `POST .../api/Game/load-random-level`, `X-Session-Id='sess-42'`, `X-Connection-Id='conn-test'`. |
| F4 | pas de chargement si sessionId différent | `sessionId='sess-A'` en session → rejouer `GameStart('sess-B')` → **aucune** requête `load-*`. |
| F5 | pas de chargement si connectionId null | `sessionId='sess-42'` + `fakeConnection.connectionId=null` → rejouer `GameStart('sess-42')` → **aucune** requête `load-*`. |

### Groupe G — `onThievesDied` (listener `ThievesDied`)

| Id | Cas | Vérification |
|----|-----|--------------|
| G1 | listener enregistré | `hubConnection.on` appelé avec `'ThievesDied'` et une fonction (enregistré par le constructeur). |
| G2 | message d'échec | rejouer `ThievesDied()` → `gameOverMessage$` émet `["", "Try again !!!"]` ; **aucune** requête HTTP. |

### Groupe H — `onUnityAlreadyTaken` (listener `UnityAlreadyTaken`)

| Id | Cas | Vérification |
|----|-----|--------------|
| H1 | **non branché au montage** (point ouvert) | après construction, `hubConnection.on` **n'a pas** été appelé avec `'UnityAlreadyTaken'` (le constructeur n'appelle pas `onUnityAlreadyTaken`). |
| H2 | enregistrement explicite | appeler `service.onUnityAlreadyTaken()` → `hubConnection.on` appelé avec `'UnityAlreadyTaken'` et une fonction. |
| H3 | journalisation (`logEnabled=true`) | après `onUnityAlreadyTaken()`, rejouer l'événement → `console.log('Unity déjà prise par un autre client')`. |
| H4 | silencieux (`logEnabled=false`) | même scénario avec `logEnabled=false` → **aucun** `console.log` du message. |

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

  // ===== Groupe F — onGameStart (listener 'GameStart') =====

  // Endpoints de chargement (déclenchés indirectement par la garde de onGameStart).
  const loadRandomLevelUrl = `${environment.apiURL}/api/Game/load-random-level`;
  const loadLevelUrl = `${environment.apiURL}/api/Game/load-level`;

  // F1 : un listener est bien enregistré sur l'événement 'GameStart'.
  it('should register a listener for the GameStart event', () => {
    expect(fakeConnection.on).toHaveBeenCalledWith(
      'GameStart',
      jasmine.any(Function),
    );
  });

  // F2 : GameStart réinitialise les trois flux à leur valeur de départ.
  it('should reset gameOver, gold and level-button messages on GameStart', () => {
    // Salir l'état via des événements publics.
    getRegisteredHandler('ScoreUpdated')(50);
    getRegisteredHandler('ExitReached')(2);
    httpMock.expectOne(saveLevelUrl).flush({}); // solder le save-level de ExitReached

    // sessionId non posé (beforeEach) → garde fausse, aucun chargement.
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

  // F3 : garde vraie (sessionId correspond + connectionId non null) → chargement.
  it('should load the level when the session id matches and a connection id exists', () => {
    sessionStorage.setItem('sessionId', 'sess-42');
    // fakeConnection.connectionId === 'conn-test' (non null).

    getRegisteredHandler('GameStart')('sess-42');

    const req = httpMock.expectOne(loadRandomLevelUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.headers.get('X-Session-Id')).toBe('sess-42');
    expect(req.request.headers.get('X-Connection-Id')).toBe('conn-test');
    req.flush({});
  });

  // F4 : sessionId différent → garde fausse, aucun chargement.
  it('should not load the level when the session id does not match', () => {
    sessionStorage.setItem('sessionId', 'sess-A');

    getRegisteredHandler('GameStart')('sess-B');

    httpMock.expectNone(loadRandomLevelUrl);
    httpMock.expectNone(loadLevelUrl);
    expect().nothing(); // expectNone s'auto-vérifie : on déclare l'intention à Jasmine
  });

  // F5 : connectionId null → garde fausse, aucun chargement.
  it('should not load the level when there is no connection id', () => {
    sessionStorage.setItem('sessionId', 'sess-42');
    fakeConnection.connectionId = null;

    getRegisteredHandler('GameStart')('sess-42');

    httpMock.expectNone(loadRandomLevelUrl);
    httpMock.expectNone(loadLevelUrl);
    expect().nothing(); // expectNone s'auto-vérifie : on déclare l'intention à Jasmine
  });

  // ===== Groupe G — onThievesDied (listener 'ThievesDied') =====

  // G1 : un listener est bien enregistré sur l'événement 'ThievesDied'.
  it('should register a listener for the ThievesDied event', () => {
    expect(fakeConnection.on).toHaveBeenCalledWith(
      'ThievesDied',
      jasmine.any(Function),
    );
  });

  // G2 : rejouer ThievesDied émet le message d'échec sur gameOverMessage$.
  it('should emit the retry message on ThievesDied', () => {
    const messages: string[] = [];
    service.gameOverMessage$.subscribe((v) => messages.push(v));

    getRegisteredHandler('ThievesDied')();

    // "" = valeur initiale, puis "Try again !!!" = message d'échec.
    expect(messages).toEqual(['', 'Try again !!!']);
  });

  // ===== Groupe H — onUnityAlreadyTaken (listener 'UnityAlreadyTaken') =====

  // H1 : le constructeur n'appelle PAS onUnityAlreadyTaken → aucun listener au
  // montage (point ouvert : l'événement 'UnityAlreadyTaken' est actuellement ignoré).
  it('should NOT register the UnityAlreadyTaken listener on construction', () => {
    expect(fakeConnection.on).not.toHaveBeenCalledWith(
      'UnityAlreadyTaken',
      jasmine.any(Function),
    );
  });

  // H2 : appeler onUnityAlreadyTaken() enregistre le listener.
  it('should register the UnityAlreadyTaken listener when called explicitly', () => {
    service.onUnityAlreadyTaken();

    expect(fakeConnection.on).toHaveBeenCalledWith(
      'UnityAlreadyTaken',
      jasmine.any(Function),
    );
  });

  // H3 : rejouer l'événement journalise le message (logEnabled=true).
  it('should log when the Unity is already taken', () => {
    const logSpy = spyOn(console, 'log');
    service.onUnityAlreadyTaken();

    getRegisteredHandler('UnityAlreadyTaken')();

    expect(logSpy).toHaveBeenCalledWith('Unity déjà prise par un autre client');
  });

  // H4 : quand logEnabled=false, aucune journalisation du message.
  it('should stay silent when logEnabled is false', () => {
    const original = environment.logEnabled;
    environment.logEnabled = false;

    const logSpy = spyOn(console, 'log');
    service.onUnityAlreadyTaken();

    getRegisteredHandler('UnityAlreadyTaken')();

    expect(logSpy).not.toHaveBeenCalledWith(
      'Unity déjà prise par un autre client',
    );

    environment.logEnabled = original; // restauration
  });

});
```

> **Note — commandes HTTP privées.** `sendLoadLevelCommand`, `sendSaveLevelCommand`
> et `sendClaimUnity` sont **privées** : on ne les teste **pas** en appel direct.
> `sendSaveLevelCommand` reste exercée **indirectement** par `onExitReached`
> (E3/E4), qui émet le `POST .../api/Game/save-level`.

## Exécution

```bash
npm test -- --include='**/server-hub.service.spec.ts' --watch=false
```

Tous les cas A1, B1–B4, C1–C4, D1–D4, E1–E4, F1–F5, G1–G2 et H1–H4 doivent passer
(28 specs), sans casser les tests existants.

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
- **`connectionId`** : posé à `'conn-test'` sur la fausse connexion ; c'est lui que
  `onGameStart` lit pour le header `X-Connection-Id` (F3) et pour sa garde de
  chargement. F5 le repasse à `null` pour vérifier que la garde bloque alors le
  chargement.
- **Commandes HTTP privées** : `sendLoadLevelCommand`, `sendSaveLevelCommand` et
  `sendClaimUnity` sont **privées** — elles ne sont **pas** testées en appel direct.
  Leur comportement est couvert **au travers des méthodes publiques** qui les
  déclenchent (`sendSaveLevelCommand` via `onExitReached`, groupe E ;
  `sendLoadLevelCommand` via `onGameStart`, groupe F).
- **onGameStart : garde de chargement** : la garde combine
  `sessionStorage.getItem("sessionId")` et `hubConnection.connectionId`. On la pilote
  via `sessionStorage.setItem("sessionId", …)` et `fakeConnection.connectionId`.
  Garde vraie → une requête `load-random-level` à `flush` (F3) ; garde fausse →
  `httpMock.expectNone(...)` (F4/F5). Sans `authToken`, l'endpoint est
  `load-random-level`.
- **`console.log("Game start")` non gardé** : contrairement aux autres journaux du
  service (gardés par `environment.logEnabled`), celui de `onGameStart` est
  **inconditionnel**. Incohérence mineure **signalée** — non verrouillée par un test.
- **onThievesDied (G)** : listener enregistré par le constructeur ; seul effet
  observable = `gameOverMessage$.next("Try again !!!")`. On vérifie la séquence
  `["", "Try again !!!"]` (BehaviorSubject initial `""`). Aucun effet HTTP.
- **onUnityAlreadyTaken NON branché (H1)** : le constructeur appelle
  `startConnection`, `onScoreUpdated`, `onExitReached`, `onGameStart` et
  `onThievesDied` — **mais pas** `onUnityAlreadyTaken`. Son listener n'est donc
  jamais posé au montage et l'événement `'UnityAlreadyTaken'` est **ignoré**.
  Probable oubli de câblage : H1 le **verrouille** (aucun listener au montage) et le
  **signale**. À trancher séparément — si on corrige le service (ajouter
  `this.onUnityAlreadyTaken()` au constructeur), H1 devra devenir « listener
  enregistré au montage » (comme D/E/F/G).
- **Callback purement journalisant (H3/H4)** : `onUnityAlreadyTaken` n'a aucun effet
  observable (flux/HTTP) ; on teste via un espion `console.log` et on pilote la
  branche silencieuse en basculant `environment.logEnabled` (restauré en fin de
  test, comme C4).
- **Prochaines étapes** : reporter les groupes **G** et **H** de ce plan dans le
  `.spec.ts` (après validation). Tous les listeners publics du service sont alors
  couverts.
```