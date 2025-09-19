import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';

import { PlayerControlsComponent } from './player-controls.component';
import { PlayerControlsService } from '../../../services/player-controls-service/player-controls.service';



describe('PlayerControlsComponent', () => {
  let component: PlayerControlsComponent;
  let fixture: ComponentFixture<PlayerControlsComponent>;

  beforeEach(async () => {

    const playerControlsServiceMock = {
      sendMove: jasmine.createSpy('sendMove').and.returnValue({ subscribe: () => {} }),
      sendStealth: jasmine.createSpy('sendStealth').and.returnValue({ subscribe: () => {} })
    };

    await TestBed.configureTestingModule({
      imports: [PlayerControlsComponent],
      providers: [
        { provide: PlayerControlsService, useValue: playerControlsServiceMock }
      ]
    }).compileComponents();

    /*await TestBed.configureTestingModule({
      imports: [PlayerControlsComponent]
    })
    .compileComponents();*/

    fixture = TestBed.createComponent(PlayerControlsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have a Move button', () => {
    const button = fixture.debugElement.query(By.css('#move-btn'));
    expect(button).toBeTruthy();
    expect(button.nativeElement.textContent).toContain('Move');
  });

  it('should call sendMove() when Move button is clicked', () => {
    spyOn(component, 'move'); // espionne la fonction
    const button = fixture.debugElement.query(By.css('#move-btn'));
    button.triggerEventHandler('click'); // simule un clic
    expect(component.move).toHaveBeenCalled();
  });

  it('should have a Stealth button', () => {
    const button = fixture.debugElement.query(By.css('#stealth-btn'));
    expect(button).toBeTruthy();
    expect(button.nativeElement.textContent).toContain('Stealth');
  });

});
