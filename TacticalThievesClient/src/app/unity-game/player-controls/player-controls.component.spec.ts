import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BehaviorSubject, of, throwError } from 'rxjs';

import { PlayerControlsComponent } from './player-controls.component';
import { PlayerControlsService } from '../../../services/player-controls-service/player-controls.service';
import { ServerHubService } from '../../../services/server-hub-service/server-hub.service';

describe('PlayerControlsComponent', () => {
  let component: PlayerControlsComponent;
  let fixture: ComponentFixture<PlayerControlsComponent>;

  // Fake PlayerControlsService: each method returns a "neutral" Observable
  // (of({})) so the component's .subscribe(...) runs without an HTTP call.
  let playerControlsServiceMock: jasmine.SpyObj<PlayerControlsService>;

  // Fake ServerHubService: we only expose the observable used by ngOnInit.
  // We drive it via .next(...) to simulate the server messages.
  let mockServerHubService: jasmine.SpyObj<ServerHubService>;
  let levelBtnSubject: BehaviorSubject<string>;

  // DOM helpers.
  function getButton(id: string): HTMLButtonElement {
    return fixture.nativeElement.querySelector(id);
  }

  beforeEach(async () => {
    playerControlsServiceMock = jasmine.createSpyObj<PlayerControlsService>(
      'PlayerControlsService',
      ['sendMove', 'sendStealth', 'sendEndTurn', 'sendRestartLevel'],
    );
    // By default, every command "succeeds" (Observable that emits then completes).
    playerControlsServiceMock.sendMove.and.returnValue(of({}));
    playerControlsServiceMock.sendStealth.and.returnValue(of({}));
    playerControlsServiceMock.sendEndTurn.and.returnValue(of({}));
    playerControlsServiceMock.sendRestartLevel.and.returnValue(of({}));

    // Initial value identical to the real service.
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
    fixture.detectChanges(); // triggers ngOnInit
  });

  // ===== Group A — Creation =====

  // A1: the component instantiates correctly.
  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // ===== Group B — restartLabel: initialization & change on victory =====

  // B1: after construction + ngOnInit, restartLabel equals "Restart level".
  it('should initialize restartLabel to "Restart level"', () => {
    expect(component.restartLabel).toBe('Restart level');
  });

  // B2: on victory, the service emits "Next level" → restartLabel is updated.
  it('should update restartLabel to "Next level" when the player wins', () => {
    levelBtnSubject.next('Next level'); // emitted by onExitReached (ExitReached)
    fixture.detectChanges();
    expect(component.restartLabel).toBe('Next level');
  });

  // B3: the restart button displays restartLabel and follows its changes.
  it('should display restartLabel on the restart button', () => {
    expect(getButton('#restart-btn').textContent).toContain('Restart level');

    levelBtnSubject.next('Next level');
    fixture.detectChanges();
    expect(getButton('#restart-btn').textContent).toContain('Next level');
  });

  // B4: a new GameStart resets the label to "Restart level".
  it('should reset restartLabel to "Restart level" on a new game start', () => {
    levelBtnSubject.next('Next level');
    fixture.detectChanges();
    expect(component.restartLabel).toBe('Next level');

    levelBtnSubject.next('Restart level'); // emitted by onGameStart (GameStart)
    fixture.detectChanges();
    expect(component.restartLabel).toBe('Restart level');
  });

  // ===== Group C — move / endTurn / restart buttons =====

  // C1: clicking #move-btn triggers sendMove.
  it('should call sendMove when the move button is clicked', () => {
    getButton('#move-btn').click();
    expect(playerControlsServiceMock.sendMove).toHaveBeenCalledTimes(1);
  });

  // C2: clicking #end-btn triggers sendEndTurn.
  it('should call sendEndTurn when the end turn button is clicked', () => {
    getButton('#end-btn').click();
    expect(playerControlsServiceMock.sendEndTurn).toHaveBeenCalledTimes(1);
  });

  // C3: clicking #restart-btn triggers sendRestartLevel.
  it('should call sendRestartLevel when the restart button is clicked', () => {
    getButton('#restart-btn').click();
    expect(playerControlsServiceMock.sendRestartLevel).toHaveBeenCalledTimes(1);
  });

  // C4: a click on move calls ONLY sendMove (not the other commands).
  it('should only call sendMove when the move button is clicked', () => {
    getButton('#move-btn').click();
    expect(playerControlsServiceMock.sendMove).toHaveBeenCalledTimes(1);
    expect(playerControlsServiceMock.sendEndTurn).not.toHaveBeenCalled();
    expect(playerControlsServiceMock.sendRestartLevel).not.toHaveBeenCalled();
    expect(playerControlsServiceMock.sendStealth).not.toHaveBeenCalled();
  });

  // ===== Group D — Extras (robustness) =====

  // D1: on success, the next callback runs (console.log), without an exception.
  it('should log a success when the command succeeds', () => {
    const logSpy = spyOn(console, 'log');
    expect(() => component.move()).not.toThrow();
    expect(logSpy).toHaveBeenCalled();
  });

  // D2: on a command error, the error callback runs (console.error),
  //     without a propagated exception.
  it('should log an error when the command fails', () => {
    const errorSpy = spyOn(console, 'error');
    playerControlsServiceMock.sendEndTurn.and.returnValue(
      throwError(() => new Error('server down')),
    );

    expect(() => component.endTurn()).not.toThrow();
    expect(errorSpy).toHaveBeenCalled();
  });
});
