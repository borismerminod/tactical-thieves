import { TestBed } from '@angular/core/testing';

import { ServerHubService } from './server-hub.service';

describe('ServerHubService', () => {
  let service: ServerHubService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ServerHubService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
