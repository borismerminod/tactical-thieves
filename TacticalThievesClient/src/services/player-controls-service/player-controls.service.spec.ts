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

  // URLs of the game endpoints.
  const moveUrl    = `${environment.apiURL}/api/Game/move`;
  const endTurnUrl = `${environment.apiURL}/api/Game/end-turn`;
  const restartUrl = `${environment.apiURL}/api/Game/restart`;

  beforeEach(() => {
    sessionStorage.clear(); // deterministic session state before each test

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
    httpMock.verify(); // no pending unhandled request
    sessionStorage.clear();
  });

  // ===== Group A — Creation =====

  // A1: the service instantiates correctly.
  it('should create', () => {
    expect(service).toBeTruthy();
  });

  // ===== Group B — sendMove (session header) =====

  // B1: sendMove posts to /api/Game/move with an empty body.
  it('should POST an empty body to the move endpoint', () => {
    service.sendMove().subscribe();

    const req = httpMock.expectOne(moveUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({});
    req.flush({});
  });

  // B2: the sessionId present in sessionStorage is sent in X-Session-Id.
  it('should send the stored sessionId in the X-Session-Id header on move', () => {
    sessionStorage.setItem('sessionId', 'sess-move');

    service.sendMove().subscribe();

    const req = httpMock.expectOne(moveUrl);
    expect(req.request.headers.get('X-Session-Id')).toBe('sess-move');
    req.flush({});
  });

  // B3: with no sessionId, X-Session-Id equals '' (empty string, never "null").
  it('should send an empty X-Session-Id header on move when no sessionId is stored', () => {
    // sessionStorage already cleared by beforeEach.
    service.sendMove().subscribe();

    const req = httpMock.expectOne(moveUrl);
    expect(req.request.headers.get('X-Session-Id')).toBe('');
    req.flush({});
  });

  // B4: the returned observable relays the server response to the subscriber.
  it('should relay the server response to the subscriber', () => {
    const serverResponse = { reaction: 'moved' };
    let received: unknown;

    service.sendMove().subscribe((res) => (received = res));

    httpMock.expectOne(moveUrl).flush(serverResponse);
    expect(received).toEqual(serverResponse);
  });

  // ===== Group D — sendEndTurn (session header) =====

  // D1: sendEndTurn posts to /api/Game/end-turn with an empty body.
  it('should POST an empty body to the end-turn endpoint', () => {
    service.sendEndTurn().subscribe();

    const req = httpMock.expectOne(endTurnUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({});
    req.flush({});
  });

  // D2: the sessionId present in sessionStorage is sent in X-Session-Id.
  it('should send the stored sessionId in the X-Session-Id header on end-turn', () => {
    sessionStorage.setItem('sessionId', 'sess-123');

    service.sendEndTurn().subscribe();

    const req = httpMock.expectOne(endTurnUrl);
    expect(req.request.headers.get('X-Session-Id')).toBe('sess-123');
    req.flush({});
  });

  // D3: with no sessionId, X-Session-Id equals '' (empty string, never "null").
  it('should send an empty X-Session-Id header on end-turn when no sessionId is stored', () => {
    // sessionStorage already cleared by beforeEach.
    service.sendEndTurn().subscribe();

    const req = httpMock.expectOne(endTurnUrl);
    expect(req.request.headers.get('X-Session-Id')).toBe('');
    req.flush({});
  });

  // ===== Group E — sendRestartLevel (session header) =====

  // E1: sendRestartLevel posts to /api/Game/restart with an empty body.
  it('should POST an empty body to the restart endpoint', () => {
    service.sendRestartLevel().subscribe();

    const req = httpMock.expectOne(restartUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({});
    req.flush({});
  });

  // E2: the present sessionId is sent in X-Session-Id.
  it('should send the stored sessionId in the X-Session-Id header on restart', () => {
    sessionStorage.setItem('sessionId', 'sess-999');

    service.sendRestartLevel().subscribe();

    const req = httpMock.expectOne(restartUrl);
    expect(req.request.headers.get('X-Session-Id')).toBe('sess-999');
    req.flush({});
  });

  // E3: with no sessionId, X-Session-Id equals ''.
  it('should send an empty X-Session-Id header on restart when no sessionId is stored', () => {
    service.sendRestartLevel().subscribe();

    const req = httpMock.expectOne(restartUrl);
    expect(req.request.headers.get('X-Session-Id')).toBe('');
    req.flush({});
  });

  // ===== Group F — Error propagation =====

  // F1: an HTTP error is propagated to the subscriber's error callback.
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
