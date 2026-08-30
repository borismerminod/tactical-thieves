import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

/**
 * PlayerControlsService manages all player action interactions with the game server.
 * 
 * This service is responsible for:
 * - Sending player movement commands to the server
 * - Sending stealth action commands to the server
 * - Notifying the server when the player's turn ends
 * - Requesting to restart the current level
 * 
 * All communications with the server are made via HTTP POST requests to the game API endpoints.
 * Some operations require a session ID to be included in the request headers for server-side session validation.
 */
@Injectable({
  providedIn: 'root'
})
export class PlayerControlsService {

  constructor(private http: HttpClient) { }

  /**
   * Sends a movement action command to the server.
   * This notifies the server that the player wants to move to a new position.
   * 
   * @returns An Observable representing the HTTP POST request to the game move endpoint.
   *          The response will contain the server's reaction to the movement command.
   */
  sendMove() : Observable<any>
  {

    const sessionId = sessionStorage.getItem("sessionId");
    const headers = { 'X-Session-Id': sessionId ? sessionId : '' };

    return this.http.post(`${environment.apiURL}/api/Game/move`, {}, { headers });
  }

  /**
   * Sends a stealth action command to the server.
   * This notifies the server that the player wants to perform a stealth action.
   * 
   * @returns An Observable representing the HTTP POST request to the game stealth endpoint.
   *          The response will contain the server's reaction to the stealth command.
   */
  sendStealth() : Observable<any>
  {
    return this.http.post(`${environment.apiURL}/api/Game/stealth`, {});
  }

  /**
   * Notifies the server that the player has ended their turn.
   * This method requires the session ID from sessionStorage to be included in the request headers
   * for proper server-side session validation and state management.
   * 
   * @returns An Observable representing the HTTP POST request to the game end-turn endpoint.
   *          The response will contain updated game state information after the turn has been processed.
   */
  sendEndTurn() : Observable<any>
  {
    const sessionId = sessionStorage.getItem("sessionId");
    const headers = { 'X-Session-Id': sessionId ? sessionId : '' };

    return this.http.post(`${environment.apiURL}/api/Game/end-turn`, {}, { headers });
  }

  /**
   * Requests the server to restart the current level.
   * This method requires the session ID from sessionStorage to be included in the request headers
   * for proper server-side session validation and level state reset.
   * 
   * @returns An Observable representing the HTTP POST request to the game restart endpoint.
   *          The response will contain the reset game state for the restarted level.
   */
  sendRestartLevel() : Observable<any>
  {
    const sessionId = sessionStorage.getItem("sessionId");
    const headers = { 'X-Session-Id': sessionId ? sessionId : '' };
    return this.http.post(`${environment.apiURL}/api/Game/restart`, {}, { headers });
  }

}
