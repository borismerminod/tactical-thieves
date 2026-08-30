import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';

import { PlayerControlsService } from './player-controls.service';
import { environment } from '../../environments/environment';

describe('PlayerControlsService', () => {
  let service: PlayerControlsService;
  let httpMock: HttpTestingController;

  // URLs des endpoints du jeu.
  const moveUrl    = `${environment.apiURL}/api/Game/move`;
  const endTurnUrl = `${environment.apiURL}/api/Game/end-turn`;
  const restartUrl = `${environment.apiURL}/api/Game/restart`;

  beforeEach(() => {
    sessionStorage.clear(); // état de session déterministe avant chaque test

    TestBed.configureTestingModule({
      providers: [
        PlayerControlsService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(PlayerControlsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify(); // aucune requête en attente non traitée
    sessionStorage.clear();
  });

  // ===== Groupe A — Création =====

  // A1 : le service s'instancie correctement.
  it('should create', () => {
    expect(service).toBeTruthy();
  });

  // ===== Groupe B — sendMove (header de session) =====

  // B1 : sendMove poste vers /api/Game/move avec un corps vide.
  it('should POST an empty body to the move endpoint', () => {
    service.sendMove().subscribe();

    const req = httpMock.expectOne(moveUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({});
    req.flush({});
  });

  // B2 : le sessionId présent dans sessionStorage est envoyé dans X-Session-Id.
  it('should send the stored sessionId in the X-Session-Id header on move', () => {
    sessionStorage.setItem('sessionId', 'sess-move');

    service.sendMove().subscribe();

    const req = httpMock.expectOne(moveUrl);
    expect(req.request.headers.get('X-Session-Id')).toBe('sess-move');
    req.flush({});
  });

  // B3 : sans sessionId, X-Session-Id vaut '' (chaîne vide, jamais "null").
  it('should send an empty X-Session-Id header on move when no sessionId is stored', () => {
    // sessionStorage déjà vidé par beforeEach.
    service.sendMove().subscribe();

    const req = httpMock.expectOne(moveUrl);
    expect(req.request.headers.get('X-Session-Id')).toBe('');
    req.flush({});
  });

  // B4 : l'observable retourné transmet la réponse du serveur à l'abonné.
  it('should relay the server response to the subscriber', () => {
    const serverResponse = { reaction: 'moved' };
    let received: unknown;

    service.sendMove().subscribe((res) => (received = res));

    httpMock.expectOne(moveUrl).flush(serverResponse);
    expect(received).toEqual(serverResponse);
  });

  // ===== Groupe D — sendEndTurn (header de session) =====

  // D1 : sendEndTurn poste vers /api/Game/end-turn avec un corps vide.
  it('should POST an empty body to the end-turn endpoint', () => {
    service.sendEndTurn().subscribe();

    const req = httpMock.expectOne(endTurnUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({});
    req.flush({});
  });

  // D2 : le sessionId présent dans sessionStorage est envoyé dans X-Session-Id.
  it('should send the stored sessionId in the X-Session-Id header on end-turn', () => {
    sessionStorage.setItem('sessionId', 'sess-123');

    service.sendEndTurn().subscribe();

    const req = httpMock.expectOne(endTurnUrl);
    expect(req.request.headers.get('X-Session-Id')).toBe('sess-123');
    req.flush({});
  });

  // D3 : sans sessionId, X-Session-Id vaut '' (chaîne vide, jamais "null").
  it('should send an empty X-Session-Id header on end-turn when no sessionId is stored', () => {
    // sessionStorage déjà vidé par beforeEach.
    service.sendEndTurn().subscribe();

    const req = httpMock.expectOne(endTurnUrl);
    expect(req.request.headers.get('X-Session-Id')).toBe('');
    req.flush({});
  });

  // ===== Groupe E — sendRestartLevel (header de session) =====

  // E1 : sendRestartLevel poste vers /api/Game/restart avec un corps vide.
  it('should POST an empty body to the restart endpoint', () => {
    service.sendRestartLevel().subscribe();

    const req = httpMock.expectOne(restartUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({});
    req.flush({});
  });

  // E2 : le sessionId présent est envoyé dans X-Session-Id.
  it('should send the stored sessionId in the X-Session-Id header on restart', () => {
    sessionStorage.setItem('sessionId', 'sess-999');

    service.sendRestartLevel().subscribe();

    const req = httpMock.expectOne(restartUrl);
    expect(req.request.headers.get('X-Session-Id')).toBe('sess-999');
    req.flush({});
  });

  // E3 : sans sessionId, X-Session-Id vaut ''.
  it('should send an empty X-Session-Id header on restart when no sessionId is stored', () => {
    service.sendRestartLevel().subscribe();

    const req = httpMock.expectOne(restartUrl);
    expect(req.request.headers.get('X-Session-Id')).toBe('');
    req.flush({});
  });

  // ===== Groupe F — Propagation des erreurs =====

  // F1 : une erreur HTTP est propagée au callback error de l'abonné.
  it('should propagate HTTP errors to the subscriber', () => {
    let errored = false;

    service.sendMove().subscribe({
      next: () => {},
      error: () => (errored = true),
    });

    httpMock
      .expectOne(moveUrl)
      .flush('boom', { status: 500, statusText: 'Server Error' });

    expect(errored).toBeTrue();
  });
});
