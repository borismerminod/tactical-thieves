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
      next: res => console.log('Mouvement envoyé', res),
      error: err => console.error('Erreur mouvement', err)
    });
  }

  stealth()
  {
    this.playerControlsService.sendStealth().subscribe({
      next: res => console.log('Mouvement envoyé', res),
      error: err => console.error('Erreur mouvement', err)
    });
  }

  endTurn()
  {
    this.playerControlsService.sendEndTurn().subscribe({
      next: res => console.log('Fin de tour envoyée', res),
      error: err => console.error('Erreur fin de tour', err)
    });
  }

  restart()
  {
    this.playerControlsService.sendRestartLevel().subscribe({
      next: res => console.log('restart command sent', res),
      error: err => console.error('restart command error', err)
    });
  }

}
