import { ComponentFixture, TestBed } from '@angular/core/testing';

import { UnityGameComponent } from './unity-game.component';
import { PlayerControlsComponent } from './player-controls/player-controls.component';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

describe('UnityGameComponent', () => {
  let component: UnityGameComponent;
  let fixture: ComponentFixture<UnityGameComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        UnityGameComponent,
        PlayerControlsComponent
      ],
      providers: [
        provideHttpClient(),        // ✅ fournit HttpClient
        provideHttpClientTesting()  // ✅ fournit la version test
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(UnityGameComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
