# Plan de test — `AuthService`

Ce document décrit les cas de test unitaires du service
[`AuthService`](../src/services/auth-service/auth.service.ts).
Les tests s'inspirent du style des specs existantes
([`login.component.spec.ts`](../src/app/login/login.component.spec.ts) :
commentaires en français, dépendances mockées, tests regroupés).

On avance **pas à pas** : ce document sera enrichi au fur et à mesure, une
section par méthode publique testée. Une case cochée = méthode couverte.

## Avancement

- [ ] `register` (👈 étape en cours)
- [ ] `registerStart`
- [ ] `registerFinish`
- [ ] `formatRegisterStartOptions`
- [ ] `requestAttestation`
- [ ] `login`
- [ ] `logout`
- [ ] `getErrorDetailForUser`

---

# Étape 1 — `register`

## Ce que fait la méthode

```ts
public async register(username: string): Promise<boolean> {
  const startOptions        = await this.registerStart(username)
  const attestationResponse = await this.requestAttestation(startOptions)
  const finishResult        = await this.registerFinish(attestationResponse)

  return !!finishResult &&
         finishResult.type === 'public-key' &&
         !!finishResult.id &&
         !!finishResult.publicKey
}
```

`register` est un **orchestrateur** en 3 étapes séquentielles :

1. `registerStart(username)` → `PublicKeyCredentialCreationOptions` (appel HTTP `RegisterStart`).
2. `requestAttestation(startOptions)` → attestation (WebAuthn `navigator.credentials.create`).
3. `registerFinish(attestationResponse)` → `TacticalThievesRegisteredPasskey` (appel HTTP `RegisterFinish`).

Puis elle renvoie `true` **uniquement si** le résultat final est truthy, de
`type === 'public-key'`, et possède un `id` et une `publicKey` non vides.

## Stratégie de test

Pour tester `register` **en isolation**, on **espionne les trois méthodes
internes** (`registerStart`, `requestAttestation`, `registerFinish`) avec
`spyOn(...)`. On ne déclenche donc :

- **aucun appel HTTP réel** (pas besoin de `HttpTestingController` ici) ;
- **aucun appel WebAuthn** (`navigator.credentials.create` n'est pas invoqué).

Ces méthodes internes (HTTP + WebAuthn) seront testées séparément dans les
étapes suivantes. Ici, on vérifie **deux choses seulement** :

1. **L'orchestration** : les 3 étapes sont appelées, dans l'ordre, et la sortie
   de chaque étape est bien passée en entrée de la suivante.
2. **La logique du booléen de retour** selon la forme du `finishResult`.

`AuthService` injecte `HttpClient` : on fournit quand même
`provideHttpClient()` + `provideHttpClientTesting()` pour que la DI se résolve
(aucun appel réseau ne partira puisque les méthodes HTTP sont espionnées).

## Cas de test

### Groupe A — Orchestration des 3 étapes

| Id | Cas | Vérification |
|----|-----|--------------|
| A1 | `should create` | le service s'instancie (`toBeTruthy`). |
| A2 | enchaînement complet | `registerStart` → `requestAttestation` → `registerFinish` sont tous appelés une fois. |
| A3 | `registerStart` reçoit le username | appelé avec l'argument `username` fourni. |
| A4 | passage des sorties en entrées | `requestAttestation` reçoit la sortie de `registerStart` ; `registerFinish` reçoit la sortie de `requestAttestation`. |

### Groupe B — Valeur de retour selon le `finishResult`

| Id | Cas | `finishResult` | Attendu |
|----|-----|----------------|---------|
| B1 | passkey valide | `{ type:'public-key', id:'x', publicKey:'y', ... }` | `true` |
| B2 | résultat nul | `null` | `false` |
| B3 | mauvais `type` | `{ type:'public-key-WRONG', id:'x', publicKey:'y' }` | `false` |
| B4 | `id` manquant | `{ type:'public-key', id:'', publicKey:'y' }` | `false` |
| B5 | `publicKey` manquante | `{ type:'public-key', id:'x', publicKey:'' }` | `false` |

### Groupe C — Propagation des erreurs

| Id | Cas | Vérification |
|----|-----|--------------|
| C1 | échec de `registerStart` | `register` rejette ; `requestAttestation` et `registerFinish` ne sont pas appelés. |
| C2 | échec de `requestAttestation` | `register` rejette (ex. l'utilisateur annule le WebAuthn) ; `registerFinish` n'est pas appelé. |
| C3 | échec de `registerFinish` | `register` rejette. |

## Code des tests

```ts
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

import { AuthService } from './auth.service';
import {
  TacticalThievesAuthenticatorAttestationResponse,
  TacticalThievesRegisteredPasskey,
} from '../../models/webauthn/webauthn.types';

describe('AuthService', () => {
  let service: AuthService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        AuthService,
        provideHttpClient(),          // résout l'injection de HttpClient
        provideHttpClientTesting(),   // aucun vrai appel réseau
      ],
    });
    service = TestBed.inject(AuthService);
  });

  // --- Fabriques de données de test (valeurs factices) -----------------

  // Options de création factices renvoyées par registerStart.
  const fakeStartOptions = { challenge: new ArrayBuffer(0) } as unknown as
    PublicKeyCredentialCreationOptions;

  // Réponse d'attestation factice renvoyée par requestAttestation.
  const fakeAttestation = { id: 'att-id' } as unknown as
    TacticalThievesAuthenticatorAttestationResponse;

  // Passkey enregistrée « valide » par défaut ; on surcharge au besoin.
  function makePasskey(
    over: Partial<TacticalThievesRegisteredPasskey> = {},
  ): TacticalThievesRegisteredPasskey {
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

  // Espionne les 3 étapes internes ; renvoie les spies pour les assertions.
  // finishResult pilote la valeur de retour de register.
  function stubSteps(finishResult: TacticalThievesRegisteredPasskey | null) {
    const startSpy = spyOn<any>(service, 'registerStart')
      .and.resolveTo(fakeStartOptions);
    const attestSpy = spyOn<any>(service, 'requestAttestation')
      .and.resolveTo(fakeAttestation);
    const finishSpy = spyOn<any>(service, 'registerFinish')
      .and.resolveTo(finishResult);
    return { startSpy, attestSpy, finishSpy };
  }

  // ===== Groupe A — Orchestration des 3 étapes =====

  // A1 : le service s'instancie correctement.
  it('should create', () => {
    expect(service).toBeTruthy();
  });

  // A2 : les 3 étapes sont bien enchaînées (une fois chacune).
  it('should call the three registration steps once', async () => {
    const { startSpy, attestSpy, finishSpy } = stubSteps(makePasskey());

    await service.register('Alice');

    expect(startSpy).toHaveBeenCalledTimes(1);
    expect(attestSpy).toHaveBeenCalledTimes(1);
    expect(finishSpy).toHaveBeenCalledTimes(1);
  });

  // A3 : registerStart est appelé avec le username fourni.
  it('should call registerStart with the given username', async () => {
    const { startSpy } = stubSteps(makePasskey());

    await service.register('Alice');

    expect(startSpy).toHaveBeenCalledWith('Alice');
  });

  // A4 : la sortie de chaque étape est passée en entrée de la suivante.
  it('should pipe each step output into the next step input', async () => {
    const { attestSpy, finishSpy } = stubSteps(makePasskey());

    await service.register('Alice');

    // requestAttestation reçoit les options renvoyées par registerStart.
    expect(attestSpy).toHaveBeenCalledWith(fakeStartOptions);
    // registerFinish reçoit l'attestation renvoyée par requestAttestation.
    expect(finishSpy).toHaveBeenCalledWith(fakeAttestation);
  });

  // ===== Groupe B — Valeur de retour selon le finishResult =====

  // B1 : passkey complète et valide → true.
  it('should return true for a valid registered passkey', async () => {
    stubSteps(makePasskey());
    expect(await service.register('Alice')).toBeTrue();
  });

  // B2 : aucun résultat → false.
  it('should return false when registerFinish returns null', async () => {
    stubSteps(null);
    expect(await service.register('Alice')).toBeFalse();
  });

  // B3 : type incorrect → false.
  it('should return false when type is not "public-key"', async () => {
    stubSteps(makePasskey({ type: 'public-key-WRONG' as any }));
    expect(await service.register('Alice')).toBeFalse();
  });

  // B4 : id manquant → false.
  it('should return false when the id is empty', async () => {
    stubSteps(makePasskey({ id: '' }));
    expect(await service.register('Alice')).toBeFalse();
  });

  // B5 : publicKey manquante → false.
  it('should return false when the publicKey is empty', async () => {
    stubSteps(makePasskey({ publicKey: '' }));
    expect(await service.register('Alice')).toBeFalse();
  });

  // ===== Groupe C — Propagation des erreurs =====

  // C1 : échec de registerStart → rejet, étapes suivantes non appelées.
  it('should reject and skip next steps when registerStart fails', async () => {
    const startSpy = spyOn<any>(service, 'registerStart')
      .and.rejectWith(new Error('start failed'));
    const attestSpy = spyOn<any>(service, 'requestAttestation');
    const finishSpy = spyOn<any>(service, 'registerFinish');

    await expectAsync(service.register('Alice')).toBeRejected();
    expect(startSpy).toHaveBeenCalled();
    expect(attestSpy).not.toHaveBeenCalled();
    expect(finishSpy).not.toHaveBeenCalled();
  });

  // C2 : échec de requestAttestation (ex. annulation WebAuthn) → rejet,
  // registerFinish non appelé.
  it('should reject and skip finish when requestAttestation fails', async () => {
    spyOn<any>(service, 'registerStart').and.resolveTo(fakeStartOptions);
    const attestSpy = spyOn<any>(service, 'requestAttestation')
      .and.rejectWith(new Error('user cancelled'));
    const finishSpy = spyOn<any>(service, 'registerFinish');

    await expectAsync(service.register('Alice')).toBeRejected();
    expect(attestSpy).toHaveBeenCalled();
    expect(finishSpy).not.toHaveBeenCalled();
  });

  // C3 : échec de registerFinish → rejet.
  it('should reject when registerFinish fails', async () => {
    spyOn<any>(service, 'registerStart').and.resolveTo(fakeStartOptions);
    spyOn<any>(service, 'requestAttestation').and.resolveTo(fakeAttestation);
    spyOn<any>(service, 'registerFinish')
      .and.rejectWith(new Error('finish failed'));

    await expectAsync(service.register('Alice')).toBeRejected();
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

- `register` **ne fait aucune vérification du username** (ni vide, ni null) :
  elle appelle directement `registerStart`. Un éventuel cas « username vide »
  relève donc de `registerStart` / du composant appelant, pas de `register`.
- La logique `!!finishResult && type === 'public-key' && !!id && !!publicKey`
  n'exige **pas** que `publicKey` soit une vraie clé valide — juste une chaîne
  non vide. Les tests B4/B5 le reflètent (chaîne vide = `false`).
- Prochaine étape suggérée : `registerStart` avec `HttpTestingController` pour
  vérifier l'URL `.../api/auth/RegisterStart`, le corps `{ username }`,
  l'option `withCredentials: true` et le formatage des options.
