# Plan de test — `PlayerControlsService`

Ce document décrit les cas de test unitaires du service
[`PlayerControlsService`](../src/services/player-controls-service/player-controls.service.ts).
Les tests s'inspirent du style des plans/specs existants
([`auth.service.test-plan.md`](./auth.service.test-plan.md) et
[`player-controls.component.spec.ts`](../src/app/unity-game/player-controls/player-controls.component.spec.ts) :
commentaires en français, dépendances mockées, tests regroupés).

## Ce que fait le service

[`player-controls.service.ts`](../src/services/player-controls-service/player-controls.service.ts)
est un service `@Injectable({ providedIn: 'root' })` qui injecte un `HttpClient`
et expose **quatre** méthodes. Chacune **renvoie** un `Observable` issu d'un
`http.post` (le service ne s'abonne **pas** lui-même : c'est l'appelant qui
souscrit) :

| Méthode | Requête | Corps | Header `X-Session-Id` |
|---------|---------|-------|-----------------------|
| `sendMove()` | `POST .../api/Game/move` | `{}` | — |
| `sendEndTurn()` | `POST .../api/Game/end-turn` | `{}` | oui (depuis `sessionStorage`) |
| `sendRestartLevel()` | `POST .../api/Game/restart` | `{}` | oui (depuis `sessionStorage`) |

> Le bouton **stealth** (`sendStealth()`) est volontairement laissé de côté pour
> l'instant ; il pourra être couvert plus tard sur le même modèle que `sendMove`.

### Détail du header de session

`sendEndTurn` et `sendRestartLevel` lisent `sessionStorage.getItem("sessionId")`
et posent l'en-tête `X-Session-Id` :

```ts
const sessionId = sessionStorage.getItem("sessionId");
const headers = { 'X-Session-Id': sessionId ? sessionId : '' };
```

- si `sessionId` existe → `X-Session-Id: <valeur>` ;
- si `sessionId` est absent (`null`) → `X-Session-Id: ''` (chaîne vide, **jamais**
  la chaîne `"null"`).

`sendMove` n'ajoute **aucun** header.

## Stratégie de test — boîte noire

La **seule frontière** du service est le `HttpClient`. On la mocke avec
`HttpTestingController` (`provideHttpClientTesting`), qui permet de :

- vérifier l'URL, la méthode, le corps et les headers de chaque requête sortante ;
- simuler la réponse serveur (`flush`) ou une erreur HTTP ;
- garantir (`verify()`) qu'aucune requête inattendue n'a été émise.

> **Important** : les méthodes renvoient un `Observable` **froid**. Tant que
> personne ne `subscribe`, **aucune** requête HTTP n'est réellement émise. Chaque
> test doit donc `subscribe(...)` à l'observable retourné, sinon
> `httpMock.expectOne(...)` échouera (aucune requête en attente).

### Nettoyage de `sessionStorage`

Comme `sendEndTurn`/`sendRestartLevel` lisent `sessionStorage`, on **vide**
`sessionStorage` en `beforeEach` (avant chaque test) et en `afterEach`, pour des
tests déterministes indépendants de l'ordre d'exécution.

## Cas de test

### Groupe A — Création

| Id | Cas | Vérification |
|----|-----|--------------|
| A1 | `should create` | le service s'instancie (`toBeTruthy`). |

### Groupe B — `sendMove` (sans header)

| Id | Cas | Vérification |
|----|-----|--------------|
| B1 | requête bien formée | après `subscribe`, `POST .../api/Game/move`, corps `{}`. |
| B2 | pas de header de session | la requête ne porte pas de `X-Session-Id`. |
| B3 | retourne la réponse serveur | la valeur `flush`ée est bien reçue par l'abonné. |

### Groupe D — `sendEndTurn` (avec header de session)

| Id | Cas | Vérification |
|----|-----|--------------|
| D1 | requête bien formée | après `subscribe`, `POST .../api/Game/end-turn`, corps `{}`. |
| D2 | header présent quand `sessionId` existe | `sessionStorage['sessionId'] = 'sess-123'` → `X-Session-Id === 'sess-123'`. |
| D3 | header vide quand `sessionId` absent | pas de `sessionId` → `X-Session-Id === ''` (chaîne vide, pas `"null"`). |

### Groupe E — `sendRestartLevel` (avec header de session)

| Id | Cas | Vérification |
|----|-----|--------------|
| E1 | requête bien formée | après `subscribe`, `POST .../api/Game/restart`, corps `{}`. |
| E2 | header présent quand `sessionId` existe | `sessionStorage['sessionId'] = 'sess-999'` → `X-Session-Id === 'sess-999'`. |
| E3 | header vide quand `sessionId` absent | pas de `sessionId` → `X-Session-Id === ''`. |

### Groupe F — Propagation des erreurs

| Id | Cas | Vérification |
|----|-----|--------------|
| F1 | erreur HTTP sur `sendMove` (500) | l'abonné reçoit le callback `error` (l'observable propage l'erreur, sans la « manger »). |

## Code des tests

```ts
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';

import { PlayerControlsService } from './player-controls.service';
import { environment } from '../../environments/environment';

describe('PlayerControlsService', () => {
  let service: PlayerControlsService;
  let httpMock: HttpTestingController;

  // URLs des endpoints du jeu.
  const moveUrl    = `${environment.apiURL}/api/Game/move`;
  const endTurnUrl = `${environment.apiURL}/api/Game/end-turn`;
  const restartUrl = `${environment.apiURL}/api/Game/restart`;

  beforeEach(() => {
    sessionStorage.clear(); // état de session déterministe avant chaque test

    TestBed.configureTestingModule({
      providers: [
        PlayerControlsService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(PlayerControlsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify(); // aucune requête en attente non traitée
    sessionStorage.clear();
  });

  // ===== Groupe A — Création =====

  // A1 : le service s'instancie correctement.
  it('should create', () => {
    expect(service).toBeTruthy();
  });

  // ===== Groupe B — sendMove =====

  // B1 : sendMove poste vers /api/Game/move avec un corps vide.
  it('should POST an empty body to the move endpoint', () => {
    service.sendMove().subscribe();

    const req = httpMock.expectOne(moveUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({});
    req.flush({});
  });

  // B2 : sendMove n'ajoute pas de header de session.
  it('should not set an X-Session-Id header on move', () => {
    service.sendMove().subscribe();

    const req = httpMock.expectOne(moveUrl);
    expect(req.request.headers.has('X-Session-Id')).toBeFalse();
    req.flush({});
  });

  // B3 : l'observable retourné transmet la réponse du serveur à l'abonné.
  it('should relay the server response to the subscriber', () => {
    const serverResponse = { reaction: 'moved' };
    let received: unknown;

    service.sendMove().subscribe((res) => (received = res));

    httpMock.expectOne(moveUrl).flush(serverResponse);
    expect(received).toEqual(serverResponse);
  });

  // ===== Groupe D — sendEndTurn (header de session) =====

  // D1 : sendEndTurn poste vers /api/Game/end-turn avec un corps vide.
  it('should POST an empty body to the end-turn endpoint', () => {
    service.sendEndTurn().subscribe();

    const req = httpMock.expectOne(endTurnUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({});
    req.flush({});
  });

  // D2 : le sessionId présent dans sessionStorage est envoyé dans X-Session-Id.
  it('should send the stored sessionId in the X-Session-Id header on end-turn', () => {
    sessionStorage.setItem('sessionId', 'sess-123');

    service.sendEndTurn().subscribe();

    const req = httpMock.expectOne(endTurnUrl);
    expect(req.request.headers.get('X-Session-Id')).toBe('sess-123');
    req.flush({});
  });

  // D3 : sans sessionId, X-Session-Id vaut '' (chaîne vide, jamais "null").
  it('should send an empty X-Session-Id header when no sessionId is stored', () => {
    // sessionStorage déjà vidé par beforeEach.
    service.sendEndTurn().subscribe();

    const req = httpMock.expectOne(endTurnUrl);
    expect(req.request.headers.get('X-Session-Id')).toBe('');
    req.flush({});
  });

  // ===== Groupe E — sendRestartLevel (header de session) =====

  // E1 : sendRestartLevel poste vers /api/Game/restart avec un corps vide.
  it('should POST an empty body to the restart endpoint', () => {
    service.sendRestartLevel().subscribe();

    const req = httpMock.expectOne(restartUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({});
    req.flush({});
  });

  // E2 : le sessionId présent est envoyé dans X-Session-Id.
  it('should send the stored sessionId in the X-Session-Id header on restart', () => {
    sessionStorage.setItem('sessionId', 'sess-999');

    service.sendRestartLevel().subscribe();

    const req = httpMock.expectOne(restartUrl);
    expect(req.request.headers.get('X-Session-Id')).toBe('sess-999');
    req.flush({});
  });

  // E3 : sans sessionId, X-Session-Id vaut ''.
  it('should send an empty X-Session-Id header when no sessionId is stored', () => {
    service.sendRestartLevel().subscribe();

    const req = httpMock.expectOne(restartUrl);
    expect(req.request.headers.get('X-Session-Id')).toBe('');
    req.flush({});
  });

  // ===== Groupe F — Propagation des erreurs =====

  // F1 : une erreur HTTP est propagée au callback error de l'abonné.
  it('should propagate HTTP errors to the subscriber', () => {
    let errored = false;

    service.sendMove().subscribe({
      next: () => {},
      error: () => (errored = true),
    });

    httpMock
      .expectOne(moveUrl)
      .flush('boom', { status: 500, statusText: 'Server Error' });

    expect(errored).toBeTrue();
  });
});
```

## Exécution

```bash
npm test -- --include='**/player-controls.service.spec.ts' --watch=false
```

Tous les cas A1, B1–B3, D1–D3, E1–E3 et F1 doivent passer, sans casser les
tests existants. Le bouton **stealth** reste à couvrir ultérieurement.

## Notes / points ouverts

- **Observables froids** : le service ne s'abonne jamais lui-même. Sans
  `.subscribe()` dans le test, aucune requête n'est émise et `expectOne` échoue.
  C'est voulu : le contrat du service est de **fournir** l'observable, la
  souscription est la responsabilité de l'appelant (le composant).
- **`X-Session-Id` vide, pas `"null"`** : le ternaire `sessionId ? sessionId : ''`
  garantit une chaîne vide quand la clé est absente. D3/E3 le verrouillent — si
  un jour le code envoyait `String(null)` (`"null"`), ces tests le détecteraient.
- **`sessionStorage.clear()` obligatoire** : sans nettoyage, une clé `sessionId`
  résiduelle d'un autre test (ou du `login`) fausserait D3/E3.
- **Pas de validation métier** : le service ne fait aucune logique conditionnelle
  au-delà du header ; il n'y a donc pas de cas « corps invalide » à ce niveau —
  ces vérifications relèvent du serveur.
- Piste ultérieure : si l'API évolue pour ajouter le header de session à
  `sendMove`, il faudra transformer B2 en cas « header présent ».
```