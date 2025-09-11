import { Component } from '@angular/core';
import { PlayerControlsService } from '../../../services/player-controls-service/player-controls.service';


@Component({
  selector: 'app-player-controls',
  imports: [],
  templateUrl: './player-controls.component.html',
  styleUrl: './player-controls.component.css'
})
export class PlayerControlsComponent {
  
  constructor(private playerControlsService : PlayerControlsService)
  {

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

}
