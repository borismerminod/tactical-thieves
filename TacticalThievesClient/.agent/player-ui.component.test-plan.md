# Plan de test — `PlayerUiComponent`

Ce document décrit les cas de test unitaires du composant
[`PlayerUiComponent`](../src/app/unity-game/player-ui/player-ui.component.ts).
Les tests s'inspirent du style de
[`register.component.spec.ts`](../src/app/register/register.component.spec.ts)
(commentaires en français, tests regroupés, dépendances mockées).

## Ce que fait le composant

[`player-ui.component.ts`](../src/app/unity-game/player-ui/player-ui.component.ts) :

- **Propriétés** : `playerGold: number = 0`, `gameOverMessage: string = ""`.
- **`ngOnInit`** s'abonne à deux observables de `ServerHubService` :
  - `playerGold$` → met à jour `playerGold` ;
  - `gameOverMessage$` → met à jour `gameOverMessage`.

[`player-ui.component.html`](../src/app/unity-game/player-ui/player-ui.component.html) :

- Un bloc `#player-ui-gold` : label `Gold :` + valeur `#player-ui-gold-value`
  (`{{playerGold}}`).
- Un bloc `#player-ui-game-over` **conditionnel** (`*ngIf="gameOverMessage !== ''"`)
  dont la classe vaut `victory` si le message (en minuscules) contient `victory`
  ou `win`, sinon `defeat`.

Messages réels émis par le service (cf.
[`server-hub.service.ts`](../src/services/server-hub-service/server-hub.service.ts)) :
`"You win !!!"` sur `ExitReached` (victoire, contient `win`) et `"Try again !!!"`
sur `ThievesDied` (défaite). `GameStart` remet le message à `""`.

## Stratégie de test

`PlayerUiComponent` injecte `ServerHubService` et s'abonne, dans `ngOnInit`, aux
observables `playerGold$` et `gameOverMessage$`.

→ On fournit un **mock** de `ServerHubService` : deux `BehaviorSubject`
(`goldSubject`, `gameOverSubject`) exposés en observables. On pilote les valeurs
via `subject.next(...)` puis `fixture.detectChanges()`, et on lit soit la
propriété du composant, soit le DOM via `By.css`. Sans ce mock, la DI échoue
(le vrai service ouvre une connexion SignalR au constructeur).

## Cas de test

### Groupe A — Création

| Id | Cas | Vérification |
|----|-----|--------------|
| A1 | `should create` | le composant s'instancie (`toBeTruthy`). |

### Groupe B — `ngOnInit` & `playerGold` (cœur de la demande)

| Id | Cas | Vérification |
|----|-----|--------------|
| B1 | gold défini après `ngOnInit` | `playerGold` défini et vaut `0` (valeur initiale de l'observable). |
| B2 | gold mis à jour | après `goldSubject.next(150)`, `component.playerGold === 150`. |

### Groupe C — `ngOnInit` & `gameOverMessage` (cœur de la demande)

| Id | Cas | Vérification |
|----|-----|--------------|
| C1 | message vide après `ngOnInit` | `gameOverMessage` défini et vaut `""`. |
| C2 | message mis à jour | après `gameOverSubject.next("You win !!!")`, `component.gameOverMessage === "You win !!!"`. |

### Groupe D — Structure HTML

| Id | Cas | Vérification |
|----|-----|--------------|
| D1 | espace d'affichage de l'or | `#player-ui-gold-value` présent et contient la valeur (`0` puis `150`). |
| D2 | message game over masqué si vide | `#player-ui-game-over` absent tant que `gameOverMessage === ""` (`*ngIf`). |
| D3 | message game over affiché si non vide | après émission, `#player-ui-game-over` présent et contient le texte. |

### Groupe E — Style conditionnel victoire / défaite

| Id | Cas | Vérification |
|----|-----|--------------|
| E1 | classe `victory` sur victoire | message `"You win !!!"` → l'élément porte `victory` (et pas `defeat`). |
| E2 | classe `defeat` sur défaite | message `"Try again !!!"` → l'élément porte `defeat` (et pas `victory`). |
| E3 | insensibilité à la casse | message `"VICTORY"` → classe `victory` (vérifie `toLowerCase()`). |

### Groupe F — Complément

| Id | Cas | Vérification |
|----|-----|--------------|
| F1 | disparition du message | après affichage puis `gameOverSubject.next("")`, `#player-ui-game-over` redevient absent (reset de partie via `GameStart`). |

## Code des tests

```ts
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { BehaviorSubject } from 'rxjs';

import { PlayerUiComponent } from './player-ui.component';
import { ServerHubService } from '../../../services/server-hub-service/server-hub.service';

describe('PlayerUiComponent', () => {
  let component: PlayerUiComponent;
  let fixture: ComponentFixture<PlayerUiComponent>;

  // Mock ServerHubService : le composant s'abonne à ces observables dans ngOnInit.
  // On les pilote via .next(...) pour simuler les messages du serveur.
  let mockServerHubService: jasmine.SpyObj<ServerHubService>;
  let goldSubject: BehaviorSubject<number>;
  let gameOverSubject: BehaviorSubject<string>;

  // Utilitaires DOM.
  function goldValueEl() {
    return fixture.debugElement.query(By.css('#player-ui-gold-value'));
  }
  function gameOverEl() {
    return fixture.debugElement.query(By.css('#player-ui-game-over'));
  }

  beforeEach(async () => {
    goldSubject = new BehaviorSubject<number>(0);
    gameOverSubject = new BehaviorSubject<string>('');

    mockServerHubService = jasmine.createSpyObj('ServerHubService', [], {
      playerGold$: goldSubject.asObservable(),
      gameOverMessage$: gameOverSubject.asObservable(),
    });

    await TestBed.configureTestingModule({
      imports: [PlayerUiComponent], // standalone
      providers: [
        { provide: ServerHubService, useValue: mockServerHubService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PlayerUiComponent);
    component = fixture.componentInstance;
    fixture.detectChanges(); // déclenche ngOnInit
  });

  // ===== Groupe A — Création =====

  // A1 : le composant s'instancie correctement.
  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // ===== Groupe B — ngOnInit & playerGold =====

  // B1 : après ngOnInit, playerGold est défini (valeur initiale 0 de l'observable).
  it('should have a defined playerGold (0) after ngOnInit', () => {
    expect(component.playerGold).toBeDefined();
    expect(component.playerGold).toBe(0);
  });

  // B2 : une nouvelle émission du service met à jour playerGold.
  it('should update playerGold when the service emits a new value', () => {
    goldSubject.next(150);
    fixture.detectChanges();
    expect(component.playerGold).toBe(150);
  });

  // ===== Groupe C — ngOnInit & gameOverMessage =====

  // C1 : après ngOnInit, gameOverMessage est défini et vide (pas de fin de partie).
  it('should have a defined empty gameOverMessage after ngOnInit', () => {
    expect(component.gameOverMessage).toBeDefined();
    expect(component.gameOverMessage).toBe('');
  });

  // C2 : une émission du service met à jour gameOverMessage.
  it('should update gameOverMessage when the service emits a value', () => {
    gameOverSubject.next('You win !!!');
    fixture.detectChanges();
    expect(component.gameOverMessage).toBe('You win !!!');
  });

  // ===== Groupe D — Structure HTML =====

  // D1 : l'espace d'affichage de l'or est présent et reflète la valeur.
  it('should display the player gold value in the DOM', () => {
    expect(goldValueEl()).toBeTruthy();
    expect(goldValueEl().nativeElement.textContent).toContain('0');

    goldSubject.next(150);
    fixture.detectChanges();
    expect(goldValueEl().nativeElement.textContent).toContain('150');
  });

  // D2 : le message de game over est masqué tant qu'il est vide (*ngIf).
  it('should not render the game over message when it is empty', () => {
    expect(gameOverEl()).toBeNull();
  });

  // D3 : le message de game over s'affiche quand il est non vide.
  it('should render the game over message when it is not empty', () => {
    gameOverSubject.next('You win !!!');
    fixture.detectChanges();
    expect(gameOverEl()).toBeTruthy();
    expect(gameOverEl().nativeElement.textContent).toContain('You win !!!');
  });

  // ===== Groupe E — Style conditionnel victoire / défaite =====

  // E1 : un message de victoire (contient "win") applique la classe "victory".
  it('should apply the "victory" class on a winning message', () => {
    gameOverSubject.next('You win !!!');
    fixture.detectChanges();
    expect(gameOverEl().nativeElement.classList).toContain('victory');
    expect(gameOverEl().nativeElement.classList).not.toContain('defeat');
  });

  // E2 : un message de défaite applique la classe "defeat".
  it('should apply the "defeat" class on a losing message', () => {
    gameOverSubject.next('Try again !!!');
    fixture.detectChanges();
    expect(gameOverEl().nativeElement.classList).toContain('defeat');
    expect(gameOverEl().nativeElement.classList).not.toContain('victory');
  });

  // E3 : la détection de victoire est insensible à la casse (toLowerCase).
  it('should detect victory regardless of case', () => {
    gameOverSubject.next('VICTORY');
    fixture.detectChanges();
    expect(gameOverEl().nativeElement.classList).toContain('victory');
  });

  // ===== Groupe F — Complément =====

  // F1 : repasser à un message vide masque de nouveau le bloc (reset de partie).
  it('should hide the game over message again when reset to empty', () => {
    gameOverSubject.next('You win !!!');
    fixture.detectChanges();
    expect(gameOverEl()).toBeTruthy();

    gameOverSubject.next('');
    fixture.detectChanges();
    expect(gameOverEl()).toBeNull();
  });
});
```

## Exécution

```bash
npm test -- --include='**/player-ui.component.spec.ts' --watch=false
```

Tous les cas A1, B1–B2, C1–C2, D1–D3, E1–E3 et F1 doivent passer, sans casser
les tests existants.
