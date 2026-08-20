# Plan de test — `UnityGameComponent`

Ce document décrit les cas de test unitaires du composant
[`UnityGameComponent`](../src/app/unity-game/unity-game.component.ts).
Les tests s'inspirent du style de
[`register.component.spec.ts`](../src/app/register/register.component.spec.ts)
(commentaires en français, tests regroupés, dépendances mockées).

## Ce que fait le composant

- **Constructeur** : initialise `unityUrl` avec `sanitizer.bypassSecurityTrustResourceUrl("")`.
- **`ngOnInit`** :
  1. génère un `sessionId` unique via `crypto.randomUUID()` ;
  2. construit `rawUrl = ${environment.apiURL}/unity/index.html?sessionId=${sessionId}` ;
  3. la sanitise et l'affecte à `unityUrl` (liée à `[src]` de l'`<iframe>`) ;
  4. **stocke le `sessionId` dans `sessionStorage`** (clé `"sessionId"`).
- **Template** : un conteneur avec `<app-player-ui>`, une `<iframe>` (le jeu Unity)
  et `<app-player-controls>`.

## Contrainte de test

`UnityGameComponent` importe `PlayerControlsComponent` et `PlayerUiComponent`.
Ces enfants injectent `ServerHubService` (et `PlayerControlsService` pour les
contrôles) et s'abonnent, dans leur `ngOnInit`, aux observables
`levelBtnMessage$`, `playerGold$`, `gameOverMessage$`.

→ On fournit donc des **mocks** de `ServerHubService` (des `BehaviorSubject`
exposés en observables) et de `PlayerControlsService`, exactement comme
[`player-ui.component.spec.ts`](../src/app/unity-game/player-ui/player-ui.component.spec.ts).
Sans ces mocks, la DI échoue et les enfants ne peuvent pas se rendre.

`sessionStorage` étant global, on le **nettoie** avant/après chaque test pour
éviter toute pollution inter-tests.

## Cas de test

### Groupe A — Création & état initial

| Id | Cas | Vérification |
|----|-----|--------------|
| A1 | `should create` | le composant s'instancie (`toBeTruthy`). |
| A2 | `unityUrl` défini | le constructeur a bien initialisé `unityUrl`. |

### Groupe B — `ngOnInit` & `sessionId` (cœur de la demande)

| Id | Cas | Vérification |
|----|-----|--------------|
| B1 | `sessionId` stocké en session | `sessionStorage.getItem('sessionId')` non vide après `ngOnInit`. |
| B2 | valeur exacte du `sessionId` | avec `crypto.randomUUID` espionné, `sessionStorage` contient précisément l'UUID généré. |
| B3 | URL de l'iframe | `src` contient `environment.apiURL`, `/unity/index.html` et `sessionId=<valeur>`. |

### Groupe C — Structure HTML

| Id | Cas | Vérification |
|----|-----|--------------|
| C1 | composant UI | `<app-player-ui>` présent dans le DOM. |
| C2 | iframe Unity | `<iframe>` présent (src lié à `unityUrl`). |
| C3 | contrôles | `<app-player-controls>` présent dans le DOM. |

### Groupe D — Complément

| Id | Cas | Vérification |
|----|-----|--------------|
| D1 | unicité du `sessionId` | deux instances produisent deux `sessionId` différents. |

## Code des tests

```ts
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { BehaviorSubject } from 'rxjs';

import { UnityGameComponent } from './unity-game.component';
import { ServerHubService } from '../../services/server-hub-service/server-hub.service';
import { PlayerControlsService } from '../../services/player-controls-service/player-controls.service';
import { environment } from '../../environments/environment';

describe('UnityGameComponent', () => {
  let component: UnityGameComponent;
  let fixture: ComponentFixture<UnityGameComponent>;

  // Mock ServerHubService : les enfants (player-ui / player-controls) s'abonnent
  // à ces observables dans leur ngOnInit → on fournit des BehaviorSubject.
  let mockServerHubService: jasmine.SpyObj<ServerHubService>;
  // Mock PlayerControlsService : injecté par player-controls, non sollicité ici.
  let mockPlayerControlsService: jasmine.SpyObj<PlayerControlsService>;

  beforeEach(async () => {
    sessionStorage.clear(); // éviter la pollution entre tests

    mockServerHubService = jasmine.createSpyObj('ServerHubService', [], {
      levelBtnMessage$: new BehaviorSubject<string>('Restart level').asObservable(),
      playerGold$: new BehaviorSubject<number>(0).asObservable(),
      gameOverMessage$: new BehaviorSubject<string>('').asObservable(),
    });
    mockPlayerControlsService = jasmine.createSpyObj('PlayerControlsService', [
      'sendMove', 'sendStealth', 'sendEndTurn', 'sendRestartLevel',
    ]);

    await TestBed.configureTestingModule({
      imports: [UnityGameComponent], // standalone → importe aussi les enfants
      providers: [
        { provide: ServerHubService, useValue: mockServerHubService },
        { provide: PlayerControlsService, useValue: mockPlayerControlsService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(UnityGameComponent);
    component = fixture.componentInstance;
    fixture.detectChanges(); // déclenche ngOnInit
  });

  afterEach(() => sessionStorage.clear());

  // Utilitaire : lit l'attribut src (string sanitisée) de l'iframe.
  function getIframeSrc(): string {
    return fixture.debugElement.query(By.css('iframe')).nativeElement.getAttribute('src');
  }

  // ===== Groupe A — Création & état initial =====

  // A1 : le composant s'instancie correctement.
  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // A2 : le constructeur initialise unityUrl (SafeResourceUrl).
  it('should initialize a defined unityUrl', () => {
    expect(component.unityUrl).toBeDefined();
  });

  // ===== Groupe B — ngOnInit & sessionId =====

  // B1 : après ngOnInit, un sessionId est présent en sessionStorage.
  it('should store a sessionId in sessionStorage after ngOnInit', () => {
    const stored = sessionStorage.getItem('sessionId');
    expect(stored).toBeTruthy();
  });

  // B2 : le sessionId stocké est exactement celui renvoyé par crypto.randomUUID.
  it('should store the exact generated sessionId', () => {
    const fakeId = '11111111-1111-1111-1111-111111111111';
    spyOn(crypto, 'randomUUID').and.returnValue(fakeId);

    const localFixture = TestBed.createComponent(UnityGameComponent);
    localFixture.detectChanges(); // ngOnInit avec le crypto espionné

    expect(sessionStorage.getItem('sessionId')).toBe(fakeId);
  });

  // B3 : l'URL de l'iframe est construite à partir de apiURL + sessionId.
  it('should build the iframe URL with apiURL and sessionId', () => {
    const sessionId = sessionStorage.getItem('sessionId')!;
    const src = getIframeSrc();
    expect(src).toContain(environment.apiURL);
    expect(src).toContain('/unity/index.html');
    expect(src).toContain(`sessionId=${sessionId}`);
  });

  // ===== Groupe C — Structure HTML =====

  // C1 : le composant d'UI est rendu.
  it('should render the player UI component', () => {
    expect(fixture.debugElement.query(By.css('app-player-ui'))).toBeTruthy();
  });

  // C2 : l'iframe du jeu Unity est rendue.
  it('should render the Unity game iframe', () => {
    expect(fixture.debugElement.query(By.css('iframe'))).toBeTruthy();
  });

  // C3 : le composant des contrôles est rendu.
  it('should render the player controls component', () => {
    expect(fixture.debugElement.query(By.css('app-player-controls'))).toBeTruthy();
  });

  // ===== Groupe D — Complément =====

  // D1 : chaque instance génère un sessionId différent (pas de réutilisation).
  it('should generate a different sessionId for each instance', () => {
    const firstId = sessionStorage.getItem('sessionId');
    sessionStorage.clear();

    const second = TestBed.createComponent(UnityGameComponent);
    second.detectChanges();
    const secondId = sessionStorage.getItem('sessionId');

    expect(secondId).toBeTruthy();
    expect(secondId).not.toBe(firstId);
  });
});
```

## Exécution

```bash
npm test -- --include='**/unity-game.component.spec.ts' --watch=false
```

Tous les cas A1–A2, B1–B3, C1–C3 et D1 doivent passer, sans casser les tests
existants.
