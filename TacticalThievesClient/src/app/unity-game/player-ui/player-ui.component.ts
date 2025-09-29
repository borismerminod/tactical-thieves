import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ServerHubService } from '../../../services/server-hub-service/server-hub.service';

@Component({
  selector: 'app-player-ui',
  imports: [CommonModule],
  templateUrl: './player-ui.component.html',
  styleUrl: './player-ui.component.css'
})
export class PlayerUiComponent implements OnInit {
  
    playerGold : number = 0
    gameOverMessage : string  = ""


   constructor(private serverHubService: ServerHubService) {}

   ngOnInit(): void {
    this.serverHubService.playerGold$.subscribe((value) => {
      this.playerGold = value;
    });

    this.serverHubService.gameOverMessage$.subscribe( (value) => {
      this.gameOverMessage = value
    }) 

  }

}
