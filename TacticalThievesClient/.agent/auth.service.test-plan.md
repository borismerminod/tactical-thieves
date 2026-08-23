# Plan de test — `AuthService`

Ce document décrit les cas de test unitaires du service
[`AuthService`](../src/services/auth-service/auth.service.ts).
Les tests s'inspirent du style des specs existantes
([`login.component.spec.ts`](../src/app/login/login.component.spec.ts) :
commentaires en français, dépendances mockées, tests regroupés).

On avance **pas à pas** : ce document sera enrichi au fur et à mesure, une
section par méthode publique testée. Une case cochée = méthode couverte.

## Avancement

- [x] `register` — couvre *indirectement* `registerStart`, `registerFinish`,
      `formatRegisterStartOptions` et `requestAttestation` (toutes **privées**)
- [x] `login` — couvre *indirectement* `loginStart`, `loginFinish`,
      `formatLoginStartOptions` et `requestAssertion` (privées)
- [x] `logout`
- [ ] `getErrorDetailForUser`

> **Note d'API** : `registerStart`, `registerFinish`,
> `formatRegisterStartOptions` et `requestAttestation` sont maintenant
> `private`. Ce sont des détails d'implémentation : on ne les teste plus
> directement, on les exerce *à travers* les méthodes publiques `register` /
> `login` (approche **boîte noire**). La seule surface publique du service est :
> `register`, `login`, `logout`, `getErrorDetailForUser` (+ les observables
> `isLoggedIn$` / `username$`).

---

# Étape 1 — `register`

## Ce que fait la méthode

```ts
public async register(username: string): Promise<boolean> {
  const startOptions        = await this.registerStart(username)        // privé → HTTP RegisterStart + formatage
  const attestationResponse = await this.requestAttestation(startOptions) // WebAuthn navigator.credentials.create
  const finishResult        = await this.registerFinish(attestationResponse) // privé → HTTP RegisterFinish

  return !!finishResult &&
         finishResult.type === 'public-key' &&
         !!finishResult.id &&
         !!finishResult.publicKey
}
```

`register` orchestre 3 étapes séquentielles, mais **on ne regarde plus les
méthodes internes** : on ne voit que ce qui **sort du service** :

1. un **POST HTTP** vers `.../api/auth/RegisterStart` (corps `{ username }`) ;
2. un appel **WebAuthn** `navigator.credentials.create({ publicKey })` ;
3. un **POST HTTP** vers `.../api/auth/RegisterFinish` (corps = attestation).

Puis un booléen selon la forme de la réponse de `RegisterFinish`.

## Stratégie de test — boîte noire

On teste `register` **uniquement via son API publique**, en mockant les **deux
seules vraies frontières** du service :

| Frontière | Outil de mock |
|-----------|---------------|
| `HttpClient` (POST RegisterStart / RegisterFinish) | `HttpTestingController` (`provideHttpClientTesting`) |
| `navigator.credentials.create` (WebAuthn) | `spyOn(navigator.credentials, 'create')` |

**On ne touche jamais aux méthodes privées** (`registerStart`,
`registerFinish`, `formatRegisterStartOptions`) : elles s'exécutent réellement
pendant le test. Bénéfice : on respecte l'encapsulation et les tests ne cassent
pas si on renomme/réorganise l'interne, tant que le comportement observable
(requêtes HTTP + appel WebAuthn + booléen) reste identique.

**Conséquence pratique** : comme `formatRegisterStartOptions` s'exécute pour de
vrai, la **réponse simulée de `RegisterStart`** doit contenir un `challenge` et
un `user.id` en **base64url valide** (ils passent par `base64urlToBuffer`).

## Pilotage de l'asynchrone

`register` enchaîne HTTP → WebAuthn → HTTP. Le flux se déroule en microtâches.
On utilise un utilitaire `tick()` (`setTimeout(0)`) pour **vider la file de
microtâches** entre le flush de `RegisterStart` et l'apparition de la requête
`RegisterFinish` :

```ts
const tick = () => new Promise<void>(res => setTimeout(res, 0));
```

Séquence type du chemin nominal :

1. appeler `service.register('Alice')` (garder la *Promise*, ne pas `await`) ;
2. `httpMock.expectOne(RegisterStart)` → `flush(startResponse)` ;
3. `await tick()` (laisse tourner formatage + WebAuthn + POST RegisterFinish) ;
4. `httpMock.expectOne(RegisterFinish)` → `flush(passkey)` ;
5. `await` la *Promise* et vérifier le booléen.

## Contrainte d'environnement

`navigator.credentials.create` n'existe que dans un vrai navigateur.
Les tests Angular tournent sous **Karma + ChromeHeadless**, où l'API WebAuthn
est présente → `spyOn(navigator.credentials, 'create')` fonctionne. (Jasmine
restaure automatiquement le spy après chaque test.)

## Cas de test

### Groupe A — Contrat observable (HTTP + WebAuthn)

| Id | Cas | Vérification |
|----|-----|--------------|
| A1 | `should create` | le service s'instancie (`toBeTruthy`). |
| A2 | POST RegisterStart | requête `POST .../api/auth/RegisterStart`, corps `{ username: 'Alice' }`, `withCredentials: true`. |
| A3 | appel WebAuthn + formatage | `navigator.credentials.create` est appelé ; les options reçues ont `challenge` **et** `user.id` convertis en `ArrayBuffer` (preuve que `formatRegisterStartOptions` a tourné). |
| A4 | POST RegisterFinish | après le WebAuthn, requête `POST .../api/auth/RegisterFinish`, `withCredentials: true`. |

### Groupe B — Valeur de retour selon la réponse de RegisterFinish

| Id | Cas | Corps `RegisterFinish` | Attendu |
|----|-----|------------------------|---------|
| B1 | passkey valide | `{ type:'public-key', id:'x', publicKey:'y', ... }` | `true` |
| B2 | réponse nulle | `null` | `false` |
| B3 | mauvais `type` | `{ type:'public-key-WRONG', id:'x', publicKey:'y' }` | `false` |
| B4 | `id` manquant | `{ type:'public-key', id:'', publicKey:'y' }` | `false` |
| B5 | `publicKey` manquante | `{ type:'public-key', id:'x', publicKey:'' }` | `false` |

### Groupe C — Propagation des erreurs

| Id | Cas | Vérification |
|----|-----|--------------|
| C1 | erreur HTTP RegisterStart (500) | `register` rejette ; `navigator.credentials.create` **non** appelé ; **aucune** requête RegisterFinish. |
| C2 | WebAuthn rejeté (annulation utilisateur) | `register` rejette ; **aucune** requête RegisterFinish. |
| C3 | erreur HTTP RegisterFinish (500) | `register` rejette. |

## Code des tests

```ts
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';

import { AuthService } from './auth.service';
import { environment } from '../../environments/environment';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  const startUrl  = `${environment.apiURL}/api/auth/RegisterStart`;
  const finishUrl = `${environment.apiURL}/api/auth/RegisterFinish`;

  // Vide la file de microtâches (HTTP resolve → WebAuthn → POST suivant).
  const tick = () => new Promise<void>((res) => setTimeout(res, 0));

  beforeEach(() => {
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

  afterEach(() => httpMock.verify()); // aucune requête en attente non traitée

  // --- Fabriques de données factices -----------------------------------

  // Réponse simulée de RegisterStart. challenge et user.id DOIVENT être en
  // base64url valide : formatRegisterStartOptions les passe à base64urlToBuffer.
  function makeStartResponse() {
    return {
      challenge: 'AAAA',                                  // base64url valide
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
  it('should POST to RegisterStart with the username and credentials', async () => {
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
  it('should call WebAuthn with formatted options (challenge & user.id as ArrayBuffer)', async () => {
    const createSpy = spyOn(navigator.credentials, 'create')
      .and.resolveTo(makeFakeCredential());
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
  it('should POST to RegisterFinish after the WebAuthn step', async () => {
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
  it('should return true for a valid registered passkey', async () => {
    expect(await runRegister(makePasskey())).toBeTrue();
  });

  // B2 : réponse nulle → false.
  it('should return false when RegisterFinish returns null', async () => {
    expect(await runRegister(null)).toBeFalse();
  });

  // B3 : type incorrect → false.
  it('should return false when type is not "public-key"', async () => {
    expect(await runRegister(makePasskey({ type: 'public-key-WRONG' }))).toBeFalse();
  });

  // B4 : id manquant → false.
  it('should return false when the id is empty', async () => {
    expect(await runRegister(makePasskey({ id: '' }))).toBeFalse();
  });

  // B5 : publicKey manquante → false.
  it('should return false when the publicKey is empty', async () => {
    expect(await runRegister(makePasskey({ publicKey: '' }))).toBeFalse();
  });

  // ===== Groupe C — Propagation des erreurs =====

  // C1 : erreur HTTP sur RegisterStart → rejet, pas de WebAuthn ni RegisterFinish.
  it('should reject on RegisterStart HTTP error and skip the rest', async () => {
    const createSpy = spyOn(navigator.credentials, 'create');
    const promise = service.register('Alice');

    httpMock.expectOne(startUrl)
      .flush('boom', { status: 500, statusText: 'Server Error' });

    await expectAsync(promise).toBeRejected();
    expect(createSpy).not.toHaveBeenCalled();
    httpMock.expectNone(finishUrl);
  });

  // C2 : WebAuthn rejeté (annulation) → rejet, pas de RegisterFinish.
  it('should reject when WebAuthn is cancelled and skip RegisterFinish', async () => {
    spyOn(navigator.credentials, 'create')
      .and.rejectWith(new DOMException('cancelled', 'NotAllowedError'));
    const promise = service.register('Alice');

    httpMock.expectOne(startUrl).flush(makeStartResponse());

    await expectAsync(promise).toBeRejected();
    httpMock.expectNone(finishUrl);
  });

  // C3 : erreur HTTP sur RegisterFinish → rejet.
  it('should reject on RegisterFinish HTTP error', async () => {
    spyOn(navigator.credentials, 'create').and.resolveTo(makeFakeCredential());
    const promise = service.register('Alice');

    httpMock.expectOne(startUrl).flush(makeStartResponse());
    await tick();
    httpMock.expectOne(finishUrl)
      .flush('boom', { status: 500, statusText: 'Server Error' });

    await expectAsync(promise).toBeRejected();
  });
});
```

## Exécution

```bash
npm test -- --include='**/auth.service.spec.ts' --watch=false
```

Tous les cas A1–A4, B1–B5 et C1–C3 doivent passer, sans casser les tests
existants.

## Notes / points ouverts

- **Encapsulation respectée** : aucune méthode privée n'est référencée dans le
  spec. Si l'interne change (renommage, fusion d'étapes), les tests tiennent
  tant que le contrat HTTP + WebAuthn + booléen est conservé.
- `register` **ne valide pas le username** (ni vide, ni null) : il part
  directement en POST RegisterStart. Pas de cas « username vide » à ce niveau.
- La logique de retour n'exige qu'une `publicKey` **non vide** (pas une clé
  cryptographiquement valide) — B4/B5 le reflètent.
- **Dépendance environnement** : les tests supposent que
  `navigator.credentials.create` existe (ChromeHeadless). Si le runner change
  et que l'API manque, il faudra stubber `navigator.credentials` en amont.
- La construction de l'objet attestation par `requestAttestation` (encodage
  base64url de `rawId` / `clientDataJSON` / `attestationObject`, gestion de
  `getTransports` absent) est **désormais privée** : elle est exercée à travers
  `register`. Si on veut la vérifier finement, on ajoutera des assertions sur le
  **corps de la requête `RegisterFinish`** (ce que le service a réellement
  envoyé), sans jamais appeler `requestAttestation` directement.
- Prochaine étape suggérée : `login` (même approche boîte noire —
  `HttpTestingController` pour LoginStart/LoginFinish + `navigator.credentials.get`,
  puis vérifier l'écriture du token/username en `sessionStorage` et l'émission
  des observables).

---

# Étape 2 — `login`

## Ce que fait la méthode

```ts
public async login(username: string): Promise<boolean> {
  let success = false
  const startOptions      = await this.loginStart(username)         // privé → HTTP LoginStart + formatage
  const assertionResponse = await this.requestAssertion(startOptions) // WebAuthn navigator.credentials.get
  const result            = await this.loginFinish(assertionResponse) // privé → HTTP LoginFinish → { token, username }

  if (result.token) {
    sessionStorage.setItem('authToken', result.token);   // ← token du serveur
    sessionStorage.setItem('username', result.username); // ← username du SERVEUR
    this.loggedIn.next(true);
    this.username.next(username);                         // ← username ARGUMENT (pas result.username !)
    success = true
  }
  return success
}
```

Même squelette que `register`, mais avec **deux différences majeures** :

1. la frontière WebAuthn est `navigator.credentials.**get**` (assertion), pas
   `create` (attestation) ;
2. en cas de succès (présence d'un `token`), `login` produit des **effets de
   bord observables** : écriture dans `sessionStorage` **et** émission sur les
   `BehaviorSubject` `loggedIn` / `username`.

### ⚠️ Subtilité à tester explicitement

- `sessionStorage['username']` reçoit **`result.username`** (renvoyé par le serveur) ;
- l'observable `username$` émet **`username`** (l'**argument** de la fonction).

Ces deux valeurs peuvent différer → on le vérifie avec un cas dédié (E3).

## Stratégie de test — boîte noire (identique à `register`)

| Frontière | Outil de mock |
|-----------|---------------|
| `HttpClient` (POST LoginStart / LoginFinish) | `HttpTestingController` |
| `navigator.credentials.get` (WebAuthn) | `spyOn(navigator.credentials, 'get')` |

Les privées (`loginStart`, `loginFinish`, `formatLoginStartOptions`,
`requestAssertion`) s'exécutent réellement. La réponse simulée de `LoginStart`
doit donc avoir un `challenge` en **base64url valide** (passe par
`base64urlToBuffer`).

### Nettoyage de `sessionStorage`

Le **constructeur** d'`AuthService` lit `sessionStorage` (`hasToken`,
`getStoredUsername`) pour l'état initial des observables. Pour des tests
déterministes, on **vide `sessionStorage` AVANT de créer le service** (donc en
tout début de `beforeEach`, avant `TestBed.inject`) et on le re-vide en
`afterEach`.

## Cas de test

### Groupe D — Contrat observable (HTTP + WebAuthn)

| Id | Cas | Vérification |
|----|-----|--------------|
| D1 | POST LoginStart | `POST .../api/auth/LoginStart`, corps `{ username }`, `withCredentials: true`. |
| D2 | appel WebAuthn + formatage | `navigator.credentials.get` appelé ; options reçues avec `challenge` en `ArrayBuffer`. |
| D3 | POST LoginFinish | après l'assertion, `POST .../api/auth/LoginFinish`, `withCredentials: true`. |

### Groupe E — Succès / échec selon le `token`

| Id | Cas | Réponse LoginFinish | Attendu |
|----|-----|---------------------|---------|
| E1 | login réussi | `{ token:'jwt', username:'srvName' }` | `true` + `sessionStorage.authToken='jwt'` + `loggedIn$=true` |
| E2 | pas de token | `{ token:'', username:'srvName' }` | `false` + **rien** écrit + `loggedIn$` reste `false` + pas d'émission |
| E3 | username stocké vs émis | `{ token:'jwt', username:'srvName' }`, argument `'Alice'` | `sessionStorage.username==='srvName'` **mais** `username$==='Alice'`. |

### Groupe F — Propagation des erreurs

| Id | Cas | Vérification |
|----|-----|--------------|
| F1 | erreur HTTP LoginStart (500) | rejet ; `navigator.credentials.get` non appelé ; aucune requête LoginFinish. |
| F2 | WebAuthn `get` rejeté (annulation) | rejet ; aucune requête LoginFinish. |
| F3 | erreur HTTP LoginFinish (500) | rejet. |

## Code des tests (à ajouter dans le même `describe('AuthService')`)

> ⚠️ Ajouter `sessionStorage.clear()` **au tout début** du `beforeEach`
> existant (avant `TestBed.inject`) et un `afterEach(() => sessionStorage.clear())`.

```ts
import { Observable } from 'rxjs';

// URLs login
const loginStartUrl  = `${environment.apiURL}/api/auth/LoginStart`;
const loginFinishUrl = `${environment.apiURL}/api/auth/LoginFinish`;

// Capte la dernière valeur émise par un observable (BehaviorSubject → valeur immédiate).
function latest<T>(obs: Observable<T>): { value: T } {
  const box = { value: undefined as unknown as T };
  obs.subscribe((v) => (box.value = v));
  return box;
}

// Réponse simulée de LoginStart. challenge en base64url valide.
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
it('should POST to LoginStart with the username and credentials', async () => {
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
it('should call WebAuthn get with a formatted challenge (ArrayBuffer)', async () => {
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
it('should POST to LoginFinish after the WebAuthn step', async () => {
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
it('should store token and flag logged-in on success', async () => {
  const loggedIn = latest(service.isLoggedIn$);

  const ok = await runLogin('Alice', { token: 'jwt-123', username: 'srvName' });

  expect(ok).toBeTrue();
  expect(sessionStorage.getItem('authToken')).toBe('jwt-123');
  expect(loggedIn.value).toBeTrue();
});

// E2 : token absent → false + aucun effet de bord.
it('should return false and write nothing when the token is missing', async () => {
  const loggedIn = latest(service.isLoggedIn$);

  const ok = await runLogin('Alice', { token: '', username: 'srvName' });

  expect(ok).toBeFalse();
  expect(sessionStorage.getItem('authToken')).toBeNull();
  expect(sessionStorage.getItem('username')).toBeNull();
  expect(loggedIn.value).toBeFalse();
});

// E3 : sessionStorage stocke result.username, mais username$ émet l'argument.
it('should store server username but emit the argument username', async () => {
  const usernameObs = latest(service.username$);

  await runLogin('Alice', { token: 'jwt', username: 'srvName' });

  expect(sessionStorage.getItem('username')).toBe('srvName'); // serveur
  expect(usernameObs.value).toBe('Alice'); // argument
});

// ===== Groupe F — Propagation des erreurs (login) =====

// F1 : erreur HTTP LoginStart → rejet, pas de WebAuthn ni LoginFinish.
it('should reject on LoginStart HTTP error and skip the rest', async () => {
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
it('should reject when WebAuthn get is cancelled and skip LoginFinish', async () => {
  spyOn(navigator.credentials, 'get').and.rejectWith(
    new DOMException('cancelled', 'NotAllowedError'),
  );
  const promise = service.login('Alice');

  httpMock.expectOne(loginStartUrl).flush(makeLoginStartResponse());

  await expectAsync(promise).toBeRejected();
  httpMock.expectNone(loginFinishUrl);
});

// F3 : erreur HTTP LoginFinish → rejet.
it('should reject on LoginFinish HTTP error', async () => {
  spyOn(navigator.credentials, 'get').and.resolveTo(makeFakeAssertion());
  const promise = service.login('Alice');

  httpMock.expectOne(loginStartUrl).flush(makeLoginStartResponse());
  await tick();
  httpMock
    .expectOne(loginFinishUrl)
    .flush('boom', { status: 500, statusText: 'Server Error' });

  await expectAsync(promise).toBeRejected();
});
```

---

# Étape 3 — `logout`

## Ce que fait la méthode

```ts
public logout() {
  sessionStorage.clear()      // efface TOUT le sessionStorage
  this.loggedIn.next(false)   // émet false
  this.username.next('')      // émet ''
}
```

Méthode **synchrone**, sans HTTP ni WebAuthn. Trois effets à vérifier :
`sessionStorage` vidé, `loggedIn$` émet `false`, `username$` émet `''`.

## Stratégie de test

Aucun mock réseau nécessaire. On **pré-remplit** `sessionStorage` et on
**pré-positionne** l'état connecté (via un `login` réussi, ou directement en
écrivant dans `sessionStorage`), puis on appelle `logout()` et on vérifie le
retour à l'état déconnecté. On capte les observables avec `latest()`.

> Comme `logout` est synchrone et ne touche pas au HTTP, `httpMock.verify()`
> reste vert (aucune requête).

## Cas de test

### Groupe G — `logout`

| Id | Cas | Vérification |
|----|-----|--------------|
| G1 | vide `sessionStorage` | après `logout()`, `sessionStorage.length === 0`. |
| G2 | `loggedIn$` → false | l'observable émet `false` après `logout()`. |
| G3 | `username$` → '' | l'observable émet `''` après `logout()`. |

## Code des tests

```ts
// ===== Groupe G — logout =====

// G1 : logout efface entièrement le sessionStorage.
it('should clear sessionStorage on logout', () => {
  sessionStorage.setItem('authToken', 'jwt');
  sessionStorage.setItem('username', 'Alice');

  service.logout();

  expect(sessionStorage.length).toBe(0);
});

// G2 : logout émet false sur isLoggedIn$.
it('should emit false on isLoggedIn$ after logout', () => {
  const loggedIn = latest(service.isLoggedIn$);

  service.logout();

  expect(loggedIn.value).toBeFalse();
});

// G3 : logout émet '' sur username$.
it('should emit an empty username after logout', () => {
  const usernameObs = latest(service.username$);

  service.logout();

  expect(usernameObs.value).toBe('');
});
```

## Exécution (étapes 1 → 3)

```bash
npm test -- --include='**/auth.service.spec.ts' --watch=false
```

Tous les cas A1–A4, B1–B5, C1–C3 (register), D1–D3, E1–E3, F1–F3 (login) et
G1–G3 (logout) doivent passer.

## Notes / points ouverts (login & logout)

- **Nettoyage `sessionStorage` obligatoire** : le constructeur lit
  `sessionStorage` pour l'état initial. Sans `sessionStorage.clear()` en amont,
  un token résiduel ferait démarrer `isLoggedIn$` à `true` et fausserait E2/G2.
- **Bug potentiel repéré (à confirmer avec toi)** : dans `login`, si
  `result.token` est absent, la méthode renvoie `false` **sans lever d'erreur**
  — mais `result` pourrait aussi être `null`/`undefined` selon la réponse
  serveur, auquel cas `result.token` **jetterait** un `TypeError`. Le cas E2
  suppose un objet `{ token:'' }`. À décider : faut-il un cas « `result` nul » ?
- `logout` fait `sessionStorage.clear()` (efface **tout**, pas seulement
  `authToken`/`username`). G1 le teste tel quel ; si un jour d'autres clés
  doivent survivre au logout, ce test devra évoluer.
