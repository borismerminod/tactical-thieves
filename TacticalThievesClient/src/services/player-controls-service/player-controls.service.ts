import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class PlayerControlsService {

  //private apiUrl = "https://localhost:7186/api/Game" 
  private apiUrl = "http://localhost:5140/api/Game" 

  constructor(private http: HttpClient) { }

  sendMove() : Observable<any>
  {
    return this.http.post(`${this.apiUrl}/move`, {});
  }

  sendStealth() : Observable<any>
  {
    return this.http.post(`${this.apiUrl}/stealth`, {});
  }

  sendEndTurn() : Observable<any>
  {
    return this.http.post(`${this.apiUrl}/end-turn`, {});
  }

}
