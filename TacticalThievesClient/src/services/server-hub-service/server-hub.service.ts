import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject, firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';

/**
 * ServerHubService manages real-time communication with the game server via SignalR.
 * 
 * This service is responsible for:
 * - Establishing and maintaining a WebSocket connection to the SignalR Hub
 * - Broadcasting game state changes (player gold, game status, level information)
 * - Listening for server events (score updates, level completion, game over conditions)
 * - Sending game commands to the server (load level, save level, claim unity)
 * - Managing Observable streams that other components subscribe to for UI updates
 * 
 * The service uses BehaviorSubjects to maintain the current state of game properties and
 * broadcasts these states to all subscribers whenever the server pushes updates.
 */
@Injectable({
  providedIn: 'root'
})
export class ServerHubService {

  private hubConnection!: signalR.HubConnection;
  private readonly hubURL= `${environment.apiURL}/scorehub`;

  /**
   * Observable stream of the player's current gold/score amount.
   * Emits new values whenever the server updates the player's score.
   * Watched by PlayerUIComponent to display the current gold count.
   */
  private playerGoldSource = new BehaviorSubject<number>(0)
  playerGold$ = this.playerGoldSource.asObservable()

  /**
   * Observable stream of game over messages.
   * Emits messages when the game ends (either victory or defeat).
   * Watched by components to display appropriate end-of-game messages.
   */
  private gameOverMessage = new BehaviorSubject<string>("")
  gameOverMessage$ = this.gameOverMessage.asObservable()

  /**
   * Observable stream of the level button message text.
   * Emits button labels that change based on game state (e.g., "Restart level" vs "Next level").
   * Watched by components to update button text dynamically.
   */
  private levelBtnMessage = new BehaviorSubject<string>("Restart level")
  levelBtnMessage$ = this.levelBtnMessage.asObservable();


  constructor(private http: HttpClient)
  { 
    this.startConnection()
    this.onScoreUpdated()
    this.onExitReached()
    this.onGameStart()
    this.onThievesDied()

  }

  /**
   * Establishes the WebSocket connection to the SignalR Hub.
   * 
   * Configures the hub connection to:
   * - Use WebSocket as the transport protocol for real-time communication
   * - Automatically attempt to reconnect if the connection is lost
   * - Start the connection and log the result (success or error)
   * 
   * This method is called automatically in the constructor.
   */
  public startConnection(): void {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(this.hubURL, {
        transport: signalR.HttpTransportType.WebSockets, // force WebSocket
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection
      .start()
      .then(() => {
          if(environment.logEnabled)
            console.log('Connected to SignalR Hub')
      } )
      .catch((err) => {
        if(environment.logEnabled)
          console.error('Error while starting SignalR connection: ' + err)
      });
  }

  /**
   * Registers a listener for the 'ScoreUpdated' server event.
   * 
   * When the server broadcasts a score update, this method:
   * - Receives the updated gold amount from the server
   * - Emits the new value through the playerGold$ Observable
   * - Triggers UI updates in any component subscribed to this Observable
   */
  public onScoreUpdated(): void {
    this.hubConnection.on('ScoreUpdated', (gold: number) => {   
      if(environment.logEnabled)
        console.log('Score reçu du serveur:', gold);
      this.playerGoldSource.next(gold)
    });
  }

  /**
   * Registers a listener for the 'ExitReached' server event.
   * 
   * When the player reaches the exit and completes the level, this method:
   * - Receives the next level number from the server
   * - Updates the game over message to "You win !!!"
   * - Updates the level button message to "Next level"
   * - Automatically saves the level progress to the server
   */
  public onExitReached() : void 
  {  
      this.hubConnection.on('ExitReached', (nextLevel: number) => {
      
        if(environment.logEnabled)
          console.log("Exit reached by thief")
  
        this.gameOverMessage.next("You win !!!")

      this.levelBtnMessage.next("Next level")

      this.sendSaveLevelCommand(nextLevel)
    })
    
  }

  /**
   * Sends a command to the server to load a game level.
   * 
   * This method:
   * - Uses the provided session ID and connection ID for authentication
   * - Includes the authentication token in the Authorization header if the user is logged in
   * - Determines which endpoint to call based on authentication status:
   *   - Authenticated users: load their saved level
   *   - Unauthenticated users: load a random level
   * 
   * @param sessionId The session ID obtained during game initialization.
   * @param connectionId The SignalR connection ID for server-side session linking.
   * @returns A promise that resolves when the level loading command is sent.
   */
  public async sendLoadLevelCommand(sessionId: string, connectionId: string): Promise<void>
  {
    const authToken = sessionStorage.getItem("authToken");

    const headers: any = {
      "X-Connection-Id": connectionId,
      "X-Session-Id": sessionId
    };

    if (authToken) {
      headers["Authorization"] = `Bearer ${authToken}`;
    }

    const endpoint = authToken === null
      ? `${environment.apiURL}/api/Game/load-random-level`
      : `${environment.apiURL}/api/Game/load-level`;

    await firstValueFrom(
      this.http.post(endpoint, {}, { headers })
    );
  }

  /**
   * Sends a command to the server to save the player's progress after completing a level.
   * 
   * This method:
   * - Saves the current level information to the server
   * - Includes the authentication token in the Authorization header for user identification
   * - Persists player progress so it can be retrieved on subsequent logins
   * 
   * @param nextLevel The level number that has been completed.
   * @returns A promise that resolves when the save command is processed by the server.
   */
  public async sendSaveLevelCommand(nextLevel: number) : Promise<void>
  {
    const authToken = sessionStorage.getItem("authToken");

    const body = {
      Pseudo: "",
      CurrentLevel : nextLevel
    }

      await firstValueFrom(
          this.http.post(`${environment.apiURL}/api/Game/save-level`, body, {
            headers: {
              Authorization: `Bearer ${authToken}`
            }
        })
      );
  
  }

  /**
   * Sends a command to the server to claim the Unity certificate/reward.
   * 
   * This method:
   * - Sends the current SignalR connection ID to the server
   * - The server uses the connection ID to identify and verify the claimer
   * - Completes the claim process for the player
   * 
   * @returns A promise that resolves when the claim command is processed by the server.
   */
  public async sendClaimUnity() : Promise<void>
  {
    const connectionId = this.hubConnection.connectionId;
    const body = {
      connectionId: connectionId,
    }
      
    await firstValueFrom(
          this.http.post(`${environment.apiURL}/api/Game/claim-unity`, body, {
            headers: {
              ContentType: 'application/json',
            }
        })
      );
  }

  /**
   * Registers a listener for the 'GameStart' server event.
   * 
   * When the game starts, this method:
   * - Clears any previous game over message
   * - Resets the player's gold count to 0
   * - Resets the level button message to "Restart level"
   * - Verifies the session ID matches the current session
   * - Automatically loads the level for the starting game
   */
  public onGameStart() : void 
  {
    this.hubConnection.on('GameStart', (sessionID: string) => {
      console.log("Game start")
      this.gameOverMessage.next("")
      this.playerGoldSource.next(0)
      this.levelBtnMessage.next("Restart level")

      if (sessionStorage.getItem("sessionId") === sessionID && this.hubConnection.connectionId !== null)
      {
        this.sendLoadLevelCommand(sessionID, this.hubConnection.connectionId)
      }

    })
  }

  /**
   * Registers a listener for the 'UnityAlreadyTaken' server event.
   * 
   * This event is triggered when a player attempts to claim the Unity certificate
   * but it has already been claimed by another client/player in the current session.
   * Logs this information for debugging purposes.
   */
  public onUnityAlreadyTaken(): void
  {
    this.hubConnection.on("UnityAlreadyTaken", () => {
      if(environment.logEnabled)
        console.log("Unity déjà prise par un autre client");
    });
  }

  /**
   * Registers a listener for the 'ThievesDied' server event.
   * 
   * When all player thieves are killed and the game ends in failure, this method:
   * - Updates the game over message to "Try again !!!"
   * - Triggers UI updates to display the failure state
   * - Allows the player to restart the level
   */
  public onThievesDied() : void 
  {
    this.hubConnection.on("ThievesDied", () => {
      if(environment.logEnabled)
        console.log("All thieves died")
      this.gameOverMessage.next("Try again !!!")
    })
  }

}
