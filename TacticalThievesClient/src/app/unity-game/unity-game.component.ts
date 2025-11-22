import { Component } from '@angular/core';
import { PlayerControlsComponent } from './player-controls/player-controls.component';
import { PlayerUiComponent } from "./player-ui/player-ui.component";

@Component({
  selector: 'app-unity-game',
  imports: [PlayerControlsComponent, PlayerUiComponent],
  templateUrl: './unity-game.component.html',
  styleUrl: './unity-game.component.css'
})
export class UnityGameComponent {

}
