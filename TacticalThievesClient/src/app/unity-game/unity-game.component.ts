import { Component } from '@angular/core';
import { PlayerControlsComponent } from './player-controls/player-controls.component';

@Component({
  selector: 'app-unity-game',
  imports: [PlayerControlsComponent],
  templateUrl: './unity-game.component.html',
  styleUrl: './unity-game.component.css'
})
export class UnityGameComponent {

}
