import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PlayerUiComponent } from './player-ui.component';
import { BehaviorSubject } from 'rxjs';
import { ServerHubService } from '../../../services/server-hub-service/server-hub.service';
import { By } from '@angular/platform-browser';

describe('PlayerUiComponent', () => {
  let component: PlayerUiComponent;
  let fixture: ComponentFixture<PlayerUiComponent>;
  let mockServerHubService: jasmine.SpyObj<ServerHubService>;
  let goldSubject: BehaviorSubject<number>;
  let gameOverSubject: BehaviorSubject<string>;

  beforeEach(async () => {

    goldSubject = new BehaviorSubject<number>(0);
    gameOverSubject = new BehaviorSubject<string>("");

    mockServerHubService = jasmine.createSpyObj('ServerHubService', [], {
      playerGold$: goldSubject.asObservable(),
       gameOverMessage$: gameOverSubject.asObservable()
    });

    await TestBed.configureTestingModule({
      imports: [PlayerUiComponent],
      providers: [
        { provide: ServerHubService, useValue: mockServerHubService }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PlayerUiComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display initial gold (0)', () => {
    const goldText = fixture.debugElement.query(By.css('#player-ui-gold')).nativeElement.textContent;
    expect(goldText).toContain('0');
  });

  it('should update gold when service emits new value', () => {
    goldSubject.next(150);
    fixture.detectChanges();

    const goldText = fixture.debugElement.query(By.css('p')).nativeElement.textContent;
    expect(goldText).toContain('150');
  });

  it('should not show win message at game start', () => {
    const msg = fixture.debugElement.query(By.css('#player-ui-game-over'));
    expect(msg).toBeNull();
  });

  it('should show win message when thief reached the exit', () => {
    gameOverSubject.next("You win !!!")
    fixture.detectChanges();
    const msg = fixture.debugElement.query(By.css('#player-ui-game-over')).nativeElement.textContent;
    expect(msg).toContain("You win !!!")
  })


});
