# Plan de test — `PlayerControlsComponent`

Ce document décrit les cas de test unitaires du composant
[`PlayerControlsComponent`](../src/app/unity-game/player-controls/player-controls.component.ts).
Les tests s'inspirent du style de
[`register.component.spec.ts`](../src/app/register/register.component.spec.ts)
(commentaires en français, tests regroupés, dépendances mockées) et de
[`player-ui.component.spec.ts`](../src/app/unity-game/player-ui/player-ui.component.spec.ts)
pour le pilotage d'un observable de `ServerHubService`.

## Ce que fait le composant

[`player-controls.component.ts`](../src/app/unity-game/player-controls/player-controls.component.ts) :

- **Propriété** : `restartLabel: string`, initialisée à `"Restart level"` dans le
  **constructeur**.
- **`ngOnInit`** s'abonne à `serverHubService.levelBtnMessage$` et met à jour
  `restartLabel` à chaque émission.
- **`move()`** appelle `playerControlsService.sendMove()` et s'abonne au résultat.
- **`endTurn()`** appelle `playerControlsService.sendEndTurn()` et s'abonne.
- **`restart()`** appelle `playerControlsService.sendRestartLevel()` et s'abonne.
- **`stealth()`** appelle `sendStealth()` — **laissé de côté** pour l'instant
  (à la demande).

Chaque handler s'abonne avec `{ next, error }` : le callback `next` logge un
succès, le callback `error` logge une erreur (`console.error`).

[`player-controls.component.html`](../src/app/unity-game/player-controls/player-controls.component.html) :

- `#move-btn` → `(click)="move()"`
- `#stealth-btn` → `(click)="stealth()"` (hors périmètre)
- `#end-btn` → `(click)="endTurn()"`
- `#restart-btn` → `(click)="restart()"`, texte = `{{restartLabel}}`

Côté serveur (cf.
[`server-hub.service.ts`](../src/services/server-hub-service/server-hub.service.ts)) :
`levelBtnMessage$` vaut `"Restart level"` au départ, passe à `"Next level"` sur
l'événement `ExitReached` (**le joueur gagne**, `onExitReached`), et est remis à
`"Restart level"` sur `GameStart` (`onGameStart`). C'est ce qui pilote la
modification de `restartLabel` demandée.

## Stratégie de test

`PlayerControlsComponent` injecte deux services :

- **`PlayerControlsService`** : ses méthodes (`sendMove`, `sendEndTurn`,
  `sendRestartLevel`) renvoient des `Observable`. On fournit un **mock**
  (`jasmine.createSpyObj`) dont chaque méthode renvoie `of({})` par défaut, pour
  que le `.subscribe(...)` du composant s'exécute sans vrai appel HTTP. On vérifie
  ensuite que la bonne méthode a été appelée lors d'un clic.
- **`ServerHubService`** : on expose `levelBtnMessage$` via un `BehaviorSubject`
  (valeur initiale `"Restart level"`, comme le vrai service) que l'on pilote avec
  `.next(...)`. Sans ce mock, la DI échouerait (le vrai service ouvre une
  connexion SignalR dans son constructeur).

On lit soit la propriété `component.restartLabel`, soit le DOM via des sélecteurs
d'id (`#move-btn`, `#end-btn`, `#restart-btn`).

## Cas de test

### Groupe A — Création

| Id | Cas | Vérification |
|----|-----|--------------|
| A1 | `should create` | le composant s'instancie (`toBeTruthy`). |

### Groupe B — `restartLabel` : initialisation & modification à la victoire (cœur de la demande)

| Id | Cas | Vérification |
|----|-----|--------------|
| B1 | valeur initiale | `restartLabel` vaut `"Restart level"` (valeur du constructeur, confirmée par la valeur initiale de l'observable après `ngOnInit`). |
| B2 | victoire → `"Next level"` | après `levelBtnSubject.next("Next level")` (émis quand le joueur gagne), `component.restartLabel === "Next level"`. |
| B3 | libellé reflété dans le DOM | `#restart-btn` affiche `"Restart level"` puis `"Next level"` après émission. |
| B4 | reset de partie | après `"Next level"` puis `levelBtnSubject.next("Restart level")` (nouveau `GameStart`), `restartLabel` redevient `"Restart level"`. |

### Groupe C — Boutons `move` / `endTurn` / `restart` (cœur de la demande)

| Id | Cas | Vérification |
|----|-----|--------------|
| C1 | clic sur `#move-btn` | `playerControlsService.sendMove` appelé une fois. |
| C2 | clic sur `#end-btn` | `playerControlsService.sendEndTurn` appelé une fois. |
| C3 | clic sur `#restart-btn` | `playerControlsService.sendRestartLevel` appelé une fois. |
| C4 | isolation des commandes | un clic sur `#move-btn` n'appelle que `sendMove` (ni `sendEndTurn`, ni `sendRestartLevel`, ni `sendStealth`). |

### Groupe D — Compléments proposés (robustesse)

| Id | Cas | Vérification |
|----|-----|--------------|
| D1 | souscription effective (succès) | quand `sendMove` renvoie `of(...)`, le callback `next` s'exécute : `console.log` appelé, aucune exception. |
| D2 | branche d'erreur | quand `sendEndTurn` renvoie `throwError(...)`, le callback `error` s'exécute : `console.error` appelé, aucune exception propagée. |

> Le bouton **stealth** (`#stealth-btn` / `stealth()`) est volontairement laissé
> de côté pour l'instant ; il pourra être couvert plus tard sur le même modèle
> que `move`.

## Code des tests

```ts
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BehaviorSubject, of, throwError } from 'rxjs';

import { PlayerControlsComponent } from './player-controls.component';
import { PlayerControlsService } from '../../../services/player-controls-service/player-controls.service';
import { ServerHubService } from '../../../services/server-hub-service/server-hub.service';

describe('PlayerControlsComponent', () => {
  let component: PlayerControlsComponent;
  let fixture: ComponentFixture<PlayerControlsComponent>;

  // Faux PlayerControlsService : chaque méthode renvoie un Observable "neutre"
  // (of({})) pour que le .subscribe(...) du composant s'exécute sans appel HTTP.
  let playerControlsServiceMock: jasmine.SpyObj<PlayerControlsService>;

  // Faux ServerHubService : on n'expose que l'observable utilisé par ngOnInit.
  // On le pilote via .next(...) pour simuler les messages du serveur.
  let mockServerHubService: jasmine.SpyObj<ServerHubService>;
  let levelBtnSubject: BehaviorSubject<string>;

  // Utilitaires DOM.
  function getButton(id: string): HTMLButtonElement {
    return fixture.nativeElement.querySelector(id);
  }

  beforeEach(async () => {
    playerControlsServiceMock = jasmine.createSpyObj<PlayerControlsService>(
      'PlayerControlsService',
      ['sendMove', 'sendStealth', 'sendEndTurn', 'sendRestartLevel'],
    );
    // Par défaut, toutes les commandes "réussissent" (Observable qui émet puis complète).
    playerControlsServiceMock.sendMove.and.returnValue(of({}));
    playerControlsServiceMock.sendStealth.and.returnValue(of({}));
    playerControlsServiceMock.sendEndTurn.and.returnValue(of({}));
    playerControlsServiceMock.sendRestartLevel.and.returnValue(of({}));

    // Valeur initiale identique au vrai service.
    levelBtnSubject = new BehaviorSubject<string>('Restart level');
    mockServerHubService = jasmine.createSpyObj('ServerHubService', [], {
      levelBtnMessage$: levelBtnSubject.asObservable(),
    });

    await TestBed.configureTestingModule({
      imports: [PlayerControlsComponent], // standalone
      providers: [
        { provide: PlayerControlsService, useValue: playerControlsServiceMock },
        { provide: ServerHubService, useValue: mockServerHubService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PlayerControlsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges(); // déclenche ngOnInit
  });

  // ===== Groupe A — Création =====

  // A1 : le composant s'instancie correctement.
  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // ===== Groupe B — restartLabel : initialisation & modification à la victoire =====

  // B1 : après construction + ngOnInit, restartLabel vaut "Restart level".
  it('should initialize restartLabel to "Restart level"', () => {
    expect(component.restartLabel).toBe('Restart level');
  });

  // B2 : à la victoire, le service émet "Next level" → restartLabel est mis à jour.
  it('should update restartLabel to "Next level" when the player wins', () => {
    levelBtnSubject.next('Next level'); // émis par onExitReached (ExitReached)
    fixture.detectChanges();
    expect(component.restartLabel).toBe('Next level');
  });

  // B3 : le bouton restart affiche restartLabel et suit ses changements.
  it('should display restartLabel on the restart button', () => {
    expect(getButton('#restart-btn').textContent).toContain('Restart level');

    levelBtnSubject.next('Next level');
    fixture.detectChanges();
    expect(getButton('#restart-btn').textContent).toContain('Next level');
  });

  // B4 : un nouveau GameStart remet le libellé à "Restart level".
  it('should reset restartLabel to "Restart level" on a new game start', () => {
    levelBtnSubject.next('Next level');
    fixture.detectChanges();
    expect(component.restartLabel).toBe('Next level');

    levelBtnSubject.next('Restart level'); // émis par onGameStart (GameStart)
    fixture.detectChanges();
    expect(component.restartLabel).toBe('Restart level');
  });

  // ===== Groupe C — Boutons move / endTurn / restart =====

  // C1 : cliquer sur #move-btn déclenche sendMove.
  it('should call sendMove when the move button is clicked', () => {
    getButton('#move-btn').click();
    expect(playerControlsServiceMock.sendMove).toHaveBeenCalledTimes(1);
  });

  // C2 : cliquer sur #end-btn déclenche sendEndTurn.
  it('should call sendEndTurn when the end turn button is clicked', () => {
    getButton('#end-btn').click();
    expect(playerControlsServiceMock.sendEndTurn).toHaveBeenCalledTimes(1);
  });

  // C3 : cliquer sur #restart-btn déclenche sendRestartLevel.
  it('should call sendRestartLevel when the restart button is clicked', () => {
    getButton('#restart-btn').click();
    expect(playerControlsServiceMock.sendRestartLevel).toHaveBeenCalledTimes(1);
  });

  // C4 : un clic sur move n'appelle QUE sendMove (pas les autres commandes).
  it('should only call sendMove when the move button is clicked', () => {
    getButton('#move-btn').click();
    expect(playerControlsServiceMock.sendMove).toHaveBeenCalledTimes(1);
    expect(playerControlsServiceMock.sendEndTurn).not.toHaveBeenCalled();
    expect(playerControlsServiceMock.sendRestartLevel).not.toHaveBeenCalled();
    expect(playerControlsServiceMock.sendStealth).not.toHaveBeenCalled();
  });

  // ===== Groupe D — Compléments (robustesse) =====

  // D1 : en cas de succès, le callback next s'exécute (console.log), sans exception.
  it('should log a success when the command succeeds', () => {
    const logSpy = spyOn(console, 'log');
    expect(() => component.move()).not.toThrow();
    expect(logSpy).toHaveBeenCalled();
  });

  // D2 : en cas d'erreur de la commande, le callback error s'exécute (console.error),
  //      sans exception propagée.
  it('should log an error when the command fails', () => {
    const errorSpy = spyOn(console, 'error');
    playerControlsServiceMock.sendEndTurn.and.returnValue(
      throwError(() => new Error('server down')),
    );

    expect(() => component.endTurn()).not.toThrow();
    expect(errorSpy).toHaveBeenCalled();
  });
});
```

## Exécution

```bash
npm test -- --include='**/player-controls.component.spec.ts' --watch=false
```

Tous les cas A1, B1–B4, C1–C4 et D1–D2 doivent passer, sans casser les tests
existants. Le bouton **stealth** reste à couvrir ultérieurement.
