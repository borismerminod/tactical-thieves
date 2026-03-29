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

  private serverURL: string = 'https://localhost:7186';

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
      .withUrl(this.hubUrl, {
        transport: signalR.HttpTransportType.WebSockets, // force WebSocket
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection
      .start()
      .then(() => console.log('Connected to SignalR Hub'))
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

  public async sendLoadLevelCommand() : Promise<void>
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
  
  }

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

  public onGameStart() : void 
  {
    this.hubConnection.on('GameStart', () => {
      console.log("Game start")
      this.gameOverMessage.next("")
      this.playerGoldSource.next(0)
      this.levelBtnMessage.next("Restart level")
      this.sendLoadLevelCommand()
    })
  }


  public onThievesDied() : void 
  {
    this.hubConnection.on("ThievesDied", () => {
      console.log("All thieves died")
      this.gameOverMessage.next("Try again !!!")
    })
  }

}
