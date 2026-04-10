import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject, firstValueFrom } from 'rxjs';


@Injectable({
  providedIn: 'root'
})
export class ServerHubService {

  private hubConnection!: signalR.HubConnection;
  //private readonly hubUrl = 'http://localhost:5140/scorehub';
  private readonly hubUrl = 'https://localhost:7186/scorehub';
  //private readonly hubUrl = 'https://mozell-fortifiable-moshe.ngrok-free.dev/scorehub';
  //private readonly hubUrl = 'https://tactical-thieves.loca.lt/scorehub';

  private serverURL: string = 'https://localhost:7186';
  //private serverURL: string = 'https://mozell-fortifiable-moshe.ngrok-free.dev';
  //private serverURL: string = 'https://tactical-thieves.loca.lt';

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
    //this. onUnityAssigned()
    //this.onUnityAlreadyTaken()

  }

  public startConnection(): void {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(this.hubUrl, {
        transport: signalR.HttpTransportType.WebSockets, // force WebSocket
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection
      .start()
      .then(() => {
        console.log('Connected to SignalR Hub')
      } )
      .catch((err) => console.error('Error while starting SignalR connection: ' + err));
  }

  // S'abonner aux mises à jour du score
  public onScoreUpdated(): void {
    this.hubConnection.on('ScoreUpdated', (gold: number) => {
      console.log('Score reçu du serveur:', gold);
      this.playerGoldSource.next(gold)
    });
  }

  public onExitReached() : void 
  {  
      this.hubConnection.on('ExitReached', (nextLevel: number) => {
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
      ? `${this.serverURL}/api/Game/load-random-level`
      : `${this.serverURL}/api/Game/load-level`;

    await firstValueFrom(
      this.http.post(endpoint, {}, { headers })
    );
  }

  /*public async sendLoadLevelCommand(connectionId : string) : Promise<void>
  {
    const authToken = sessionStorage.getItem("authToken");

    if(authToken === null)
    {
        await firstValueFrom(
          this.http.post(`${this.serverURL}/api/Game/load-random-level`, {}, {
            headers: {
              Authorization: `Bearer ${authToken}`
            }
        })
      );
    }
    else
    {
        await firstValueFrom(
          this.http.post(`${this.serverURL}/api/Game/load-level`, {}, {
            headers: {
              Authorization: `Bearer ${authToken}`
            }
        })
      );
    }
  
  }*/

   public async sendSaveLevelCommand(nextLevel: number) : Promise<void>
  {
    const authToken = sessionStorage.getItem("authToken");

    const body = {
      Pseudo: "",
      CurrentLevel : nextLevel
    }

      await firstValueFrom(
          this.http.post(`${this.serverURL}/api/Game/save-level`, body, {
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
          this.http.post(`${this.serverURL}/api/Game/claim-unity`, body, {
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

      /*// Claim de cette Unity
      this.hubConnection.invoke("ClaimUnity", unityGUID)
        .then(() => {
          console.log("Unity claim envoyée");
      })
      .catch(err => console.error("Erreur claim:", err));*/

    })
  }

  /*public onUnityAssigned(): void
  {
    this.hubConnection.on("UnityAssigned", (unityId: string) => {
      console.log("Unity assignée:", unityId);
      this.sendLoadLevelCommand();
    });
  }*/

  public onUnityAlreadyTaken(): void
  {
    this.hubConnection.on("UnityAlreadyTaken", () => {
      console.log("Unity déjà prise par un autre client");
    });
  }

  public onThievesDied() : void 
  {
    this.hubConnection.on("ThievesDied", () => {
      console.log("All thieves died")
      this.gameOverMessage.next("Try again !!!")
    })
  }

}
