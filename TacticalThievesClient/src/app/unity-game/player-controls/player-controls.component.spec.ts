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
