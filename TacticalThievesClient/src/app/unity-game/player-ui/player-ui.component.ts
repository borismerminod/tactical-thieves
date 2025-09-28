import { Component, OnInit } from '@angular/core';
import { ServerHubService } from '../../../services/server-hub-service/server-hub.service';

@Component({
  selector: 'app-player-ui',
  imports: [],
  templateUrl: './player-ui.component.html',
  styleUrl: './player-ui.component.css'
})
export class PlayerUiComponent implements OnInit {
  
    playerGold : number = 0

   constructor(private serverHubService: ServerHubService) {}

   ngOnInit(): void {
    this.serverHubService.playerGold$.subscribe((value) => {
      this.playerGold = value;
    });
  }

}
