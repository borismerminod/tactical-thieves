import { Component, OnInit } from '@angular/core';
import { PlayerControlsComponent } from './player-controls/player-controls.component';
import { PlayerUiComponent } from "./player-ui/player-ui.component";
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-unity-game',
  imports: [PlayerControlsComponent, PlayerUiComponent],
  templateUrl: './unity-game.component.html',
  styleUrl: './unity-game.component.css'
})
export class UnityGameComponent implements OnInit {

  unityUrl: SafeResourceUrl

  constructor(private sanitizer: DomSanitizer) 
  {
    this.unityUrl = this.sanitizer.bypassSecurityTrustResourceUrl("");
  }
  
  ngOnInit(): void
  {
    // Générer un sessionId unique pour chaque instance du composant
    const sessionId = crypto.randomUUID();
    const rawUrl= `${environment.apiURL}/unity/index.html?sessionId=${sessionId}`;
    
    if(environment.logEnabled)
      console.log("Raw url: ", rawUrl);
  
     this.unityUrl = this.sanitizer.bypassSecurityTrustResourceUrl(rawUrl);
  
     if(environment.logEnabled)      
        console.log("Sanitized url: ", this.unityUrl);
  
     sessionStorage.setItem("sessionId", sessionId);

  }


}
