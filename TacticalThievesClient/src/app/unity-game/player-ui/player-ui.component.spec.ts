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

  beforeEach(async () => {

    goldSubject = new BehaviorSubject<number>(0);

    mockServerHubService = jasmine.createSpyObj('ServerHubService', [], {
      playerGold$: goldSubject.asObservable()
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
});
