import { Component, OnInit } from '@angular/core';
import { PlayerControlsService } from '../../../services/player-controls-service/player-controls.service';
import { ServerHubService } from '../../../services/server-hub-service/server-hub.service';


@Component({
  selector: 'app-player-controls',
  imports: [],
  templateUrl: './player-controls.component.html',
  styleUrl: './player-controls.component.css'
})
export class PlayerControlsComponent implements OnInit {
  
  restartLabel: string

  constructor(private playerControlsService : PlayerControlsService, private serverHubService: ServerHubService)
  {
    this.restartLabel = "Restart level"
  }

  ngOnInit()
  {
    this.serverHubService.levelBtnMessage$.subscribe((value) => {
      this.restartLabel = value;
    });
  }
  
  move()
  {
    this.playerControlsService.sendMove().subscribe({
      next: res => console.log('Move command sent', res),
      error: err => console.error('Move command failed', err)
    });
  }

  stealth()
  {
    this.playerControlsService.sendStealth().subscribe({
      next: res => console.log('Stealth command sent', res),
      error: err => console.error('Stealth command failed', err)
    });
  }

  endTurn()
  {
    this.playerControlsService.sendEndTurn().subscribe({
      next: res => console.log('End turn command sent', res),
      error: err => console.error('End turn command failed', err)
    });
  }

  restart()
  {
    this.playerControlsService.sendRestartLevel().subscribe({
      next: res => console.log('Restart command sent', res),
      error: err => console.error('Restart command failed', err)
    });
  }


}
