import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject, firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';


@Injectable({
  providedIn: 'root'
})
export class ServerHubService {

  private hubConnection!: signalR.HubConnection;
  private readonly hubURL= `${environment.apiURL}/scorehub`;

  private playerGoldSource = new BehaviorSubject<number>(0)
  playerGold$ = this.playerGoldSource.asObservable()

  private gameOverMessage = new BehaviorSubject<string>("")
  gameOverMessage$ = this.gameOverMessage.asObservable()

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

  // S'abonner aux mises à jour du score
  public onScoreUpdated(): void {
    this.hubConnection.on('ScoreUpdated', (gold: number) => {   
      if(environment.logEnabled)
        console.log('Score reçu du serveur:', gold);
      this.playerGoldSource.next(gold)
    });
  }

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


  public onUnityAlreadyTaken(): void
  {
    this.hubConnection.on("UnityAlreadyTaken", () => {
      if(environment.logEnabled)
        console.log("Unity déjà prise par un autre client");
    });
  }

  public onThievesDied() : void 
  {
    this.hubConnection.on("ThievesDied", () => {
      if(environment.logEnabled)
        console.log("All thieves died")
      this.gameOverMessage.next("Try again !!!")
    })
  }

}
