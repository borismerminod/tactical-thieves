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

  // ===================================================================
  // Groupe A — Création & état initial
  // ===================================================================

  // A1 : le composant s'instancie correctement.
  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // A2 : le constructeur initialise unityUrl (SafeResourceUrl).
  it('should initialize a defined unityUrl', () => {
    expect(component.unityUrl).toBeDefined();
  });

  // ===================================================================
  // Groupe B — ngOnInit & sessionId (cœur de la demande)
  // ===================================================================

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

  // ===================================================================
  // Groupe C — Structure HTML
  // ===================================================================

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

  // ===================================================================
  // Groupe D — Complément
  // ===================================================================

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
