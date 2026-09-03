import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { BehaviorSubject } from 'rxjs';

import { PlayerUiComponent } from './player-ui.component';
import { ServerHubService } from '../../../services/server-hub-service/server-hub.service';

describe('PlayerUiComponent', () => {
  let component: PlayerUiComponent;
  let fixture: ComponentFixture<PlayerUiComponent>;

  // Mock ServerHubService: the component subscribes to these observables in ngOnInit.
  // We drive them via .next(...) to simulate the server messages.
  let mockServerHubService: jasmine.SpyObj<ServerHubService>;
  let goldSubject: BehaviorSubject<number>;
  let gameOverSubject: BehaviorSubject<string>;

  // DOM helpers.
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
    fixture.detectChanges(); // triggers ngOnInit
  });

  // ===================================================================
  // Group A — Creation
  // ===================================================================

  // A1: the component instantiates correctly.
  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // ===================================================================
  // Group B — ngOnInit & playerGold
  // ===================================================================

  // B1: after ngOnInit, playerGold is defined (initial value 0 from the observable).
  it('should have a defined playerGold (0) after ngOnInit', () => {
    expect(component.playerGold).toBeDefined();
    expect(component.playerGold).toBe(0);
  });

  // B2: a new emission from the service updates playerGold.
  it('should update playerGold when the service emits a new value', () => {
    goldSubject.next(150);
    fixture.detectChanges();
    expect(component.playerGold).toBe(150);
  });

  // ===================================================================
  // Group C — ngOnInit & gameOverMessage
  // ===================================================================

  // C1: after ngOnInit, gameOverMessage is defined and empty (no game over).
  it('should have a defined empty gameOverMessage after ngOnInit', () => {
    expect(component.gameOverMessage).toBeDefined();
    expect(component.gameOverMessage).toBe('');
  });

  // C2: an emission from the service updates gameOverMessage.
  it('should update gameOverMessage when the service emits a value', () => {
    gameOverSubject.next('You win !!!');
    fixture.detectChanges();
    expect(component.gameOverMessage).toBe('You win !!!');
  });

  // ===================================================================
  // Group D — HTML structure
  // ===================================================================

  // D1: the gold display area is present and reflects the value.
  it('should display the player gold value in the DOM', () => {
    expect(goldValueEl()).toBeTruthy();
    expect(goldValueEl().nativeElement.textContent).toContain('0');

    goldSubject.next(150);
    fixture.detectChanges();
    expect(goldValueEl().nativeElement.textContent).toContain('150');
  });

  // D2: the game over message is hidden while empty (*ngIf).
  it('should not render the game over message when it is empty', () => {
    expect(gameOverEl()).toBeNull();
  });

  // D3: the game over message shows when it is non-empty.
  it('should render the game over message when it is not empty', () => {
    gameOverSubject.next('You win !!!');
    fixture.detectChanges();
    expect(gameOverEl()).toBeTruthy();
    expect(gameOverEl().nativeElement.textContent).toContain('You win !!!');
  });

  // ===================================================================
  // Group E — Conditional victory / defeat styling
  // ===================================================================

  // E1: a winning message (contains "win") applies the "victory" class.
  it('should apply the "victory" class on a winning message', () => {
    gameOverSubject.next('You win !!!');
    fixture.detectChanges();
    expect(gameOverEl().nativeElement.classList).toContain('victory');
    expect(gameOverEl().nativeElement.classList).not.toContain('defeat');
  });

  // E2: a losing message applies the "defeat" class.
  it('should apply the "defeat" class on a losing message', () => {
    gameOverSubject.next('Try again !!!');
    fixture.detectChanges();
    expect(gameOverEl().nativeElement.classList).toContain('defeat');
    expect(gameOverEl().nativeElement.classList).not.toContain('victory');
  });

  // E3: victory detection is case-insensitive (toLowerCase).
  it('should detect victory regardless of case', () => {
    gameOverSubject.next('VICTORY');
    fixture.detectChanges();
    expect(gameOverEl().nativeElement.classList).toContain('victory');
  });

  // ===================================================================
  // Group F — Extra
  // ===================================================================

  // F1: going back to an empty message hides the block again (game reset).
  it('should hide the game over message again when reset to empty', () => {
    gameOverSubject.next('You win !!!');
    fixture.detectChanges();
    expect(gameOverEl()).toBeTruthy();

    gameOverSubject.next('');
    fixture.detectChanges();
    expect(gameOverEl()).toBeNull();
  });
});
