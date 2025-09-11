import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class PlayerControlsService {

  private apiUrl = "localhost/api" //DOTO à Compléter

  constructor(private http: HttpClient) { }

  sendMove() : Observable<any>
  {
    return this.http.post(`${this.apiUrl}/move`, {});
  }

  sendStealth() : Observable<any>
  {
    return this.http.post(`${this.apiUrl}/stealth`, {});
  }

}
