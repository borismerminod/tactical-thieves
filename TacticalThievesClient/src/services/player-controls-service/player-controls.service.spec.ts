import { TestBed } from '@angular/core/testing';

import { PlayerControlsService } from './player-controls.service';
import { provideHttpClient } from '@angular/common/http';
import {provideHttpClientTesting, HttpTestingController} from '@angular/common/http/testing';

describe('PlayerControlsService', () => {
  let service: PlayerControlsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        PlayerControlsService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(PlayerControlsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify(); // Vérifie qu'il n’y a aucune requête HTTP non traitée
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

   it('send POST http for move skill', () => {
    service.sendMove().subscribe(response => {
      expect(response).toEqual({ success: true });
    });

    const req = httpMock.expectOne(`${service['apiUrl']}/move`);
    expect(req.request.method).toBe('POST');

    req.flush({ success: true });
  });

  it('send POST http for stealth skill', () => {
    service.sendStealth().subscribe(response => {
      expect(response).toEqual({ success: true });
    });

    const req = httpMock.expectOne(`${service['apiUrl']}/stealth`);
    expect(req.request.method).toBe('POST');

    req.flush({ success: true });
  });

});
