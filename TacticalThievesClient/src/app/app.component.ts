import { Component } from '@angular/core';
import { UnityGameComponent } from './unity-game/unity-game.component';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, UnityGameComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  title = 'TacticalThievesClient';
}
