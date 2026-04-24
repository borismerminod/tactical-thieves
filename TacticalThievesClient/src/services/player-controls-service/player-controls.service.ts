import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class PlayerControlsService {

  constructor(private http: HttpClient) { }

  sendMove() : Observable<any>
  {
    return this.http.post(`${environment.apiURL}/api/Game/move`, {});
  }

  sendStealth() : Observable<any>
  {
    return this.http.post(`${environment.apiURL}/api/Game/stealth`, {});
  }

  sendEndTurn() : Observable<any>
  {
    const sessionId = sessionStorage.getItem("sessionId");
    const headers = { 'X-Session-Id': sessionId ? sessionId : '' };

    return this.http.post(`${environment.apiURL}/api/Game/end-turn`, {}, { headers });
  }

  sendRestartLevel() : Observable<any>
  {
    const sessionId = sessionStorage.getItem("sessionId");
    const headers = { 'X-Session-Id': sessionId ? sessionId : '' };
    return this.http.post(`${environment.apiURL}/api/Game/restart`, {}, { headers });
  }

}
