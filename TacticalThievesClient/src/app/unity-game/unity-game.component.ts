import { Component } from '@angular/core';
import { PlayerControlsComponent } from './player-controls/player-controls.component';
import { PlayerUiComponent } from "./player-ui/player-ui.component";
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';

@Component({
  selector: 'app-unity-game',
  imports: [PlayerControlsComponent, PlayerUiComponent],
  templateUrl: './unity-game.component.html',
  styleUrl: './unity-game.component.css'
})
export class UnityGameComponent {

  unityUrl: SafeResourceUrl

  constructor(private sanitizer: DomSanitizer) {
    // Générer un sessionId unique pour chaque instance du composant
    const sessionId = crypto.randomUUID();
    //const rawUrl= `https://localhost:7186/unity/index.html?sessionId=${sessionId}`;
    const rawUrl= `https://mozell-fortifiable-moshe.ngrok-free.dev/unity/index.html?sessionId=${sessionId}`;
    //const rawUrl= `https://tactical-thieves.loca.lt/unity/index.html?sessionId=${sessionId}`;
    console.log(rawUrl);

     this.unityUrl = this.sanitizer.bypassSecurityTrustResourceUrl(rawUrl);

     sessionStorage.setItem("sessionId", sessionId);


  }


}
