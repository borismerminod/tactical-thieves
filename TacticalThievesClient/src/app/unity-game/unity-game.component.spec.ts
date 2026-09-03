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

  // Mock ServerHubService: the children (player-ui / player-controls) subscribe
  // to these observables in their ngOnInit → we provide BehaviorSubjects.
  let mockServerHubService: jasmine.SpyObj<ServerHubService>;
  // Mock PlayerControlsService: injected by player-controls, not exercised here.
  let mockPlayerControlsService: jasmine.SpyObj<PlayerControlsService>;

  beforeEach(async () => {
    sessionStorage.clear(); // avoid pollution between tests

    mockServerHubService = jasmine.createSpyObj('ServerHubService', [], {
      levelBtnMessage$: new BehaviorSubject<string>('Restart level').asObservable(),
      playerGold$: new BehaviorSubject<number>(0).asObservable(),
      gameOverMessage$: new BehaviorSubject<string>('').asObservable(),
    });
    mockPlayerControlsService = jasmine.createSpyObj('PlayerControlsService', [
      'sendMove', 'sendStealth', 'sendEndTurn', 'sendRestartLevel',
    ]);

    await TestBed.configureTestingModule({
      imports: [UnityGameComponent], // standalone → also imports the children
      providers: [
        { provide: ServerHubService, useValue: mockServerHubService },
        { provide: PlayerControlsService, useValue: mockPlayerControlsService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(UnityGameComponent);
    component = fixture.componentInstance;
    fixture.detectChanges(); // triggers ngOnInit
  });

  afterEach(() => sessionStorage.clear());

  // Helper: reads the iframe's src attribute (sanitized string).
  function getIframeSrc(): string {
    return fixture.debugElement.query(By.css('iframe')).nativeElement.getAttribute('src');
  }

  // ===================================================================
  // Group A — Creation & initial state
  // ===================================================================

  // A1: the component instantiates correctly.
  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // A2: the constructor initializes unityUrl (SafeResourceUrl).
  it('should initialize a defined unityUrl', () => {
    expect(component.unityUrl).toBeDefined();
  });

  // ===================================================================
  // Group B — ngOnInit & sessionId (core of the requirement)
  // ===================================================================

  // B1: after ngOnInit, a sessionId is present in sessionStorage.
  it('should store a sessionId in sessionStorage after ngOnInit', () => {
    const stored = sessionStorage.getItem('sessionId');
    expect(stored).toBeTruthy();
  });

  // B2: the stored sessionId is exactly the one returned by crypto.randomUUID.
  it('should store the exact generated sessionId', () => {
    const fakeId = '11111111-1111-1111-1111-111111111111';
    spyOn(crypto, 'randomUUID').and.returnValue(fakeId);

    const localFixture = TestBed.createComponent(UnityGameComponent);
    localFixture.detectChanges(); // ngOnInit with crypto spied on

    expect(sessionStorage.getItem('sessionId')).toBe(fakeId);
  });

  // B3: the iframe URL is built from apiURL + sessionId.
  it('should build the iframe URL with apiURL and sessionId', () => {
    const sessionId = sessionStorage.getItem('sessionId')!;
    const src = getIframeSrc();
    expect(src).toContain(environment.apiURL);
    expect(src).toContain('/unity/index.html');
    expect(src).toContain(`sessionId=${sessionId}`);
  });

  // ===================================================================
  // Group C — HTML structure
  // ===================================================================

  // C1: the UI component is rendered.
  it('should render the player UI component', () => {
    expect(fixture.debugElement.query(By.css('app-player-ui'))).toBeTruthy();
  });

  // C2: the Unity game iframe is rendered.
  it('should render the Unity game iframe', () => {
    expect(fixture.debugElement.query(By.css('iframe'))).toBeTruthy();
  });

  // C3: the controls component is rendered.
  it('should render the player controls component', () => {
    expect(fixture.debugElement.query(By.css('app-player-controls'))).toBeTruthy();
  });

  // ===================================================================
  // Group D — Extra
  // ===================================================================

  // D1: each instance generates a different sessionId (no reuse).
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
