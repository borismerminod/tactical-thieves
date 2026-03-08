import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-game-title',
  imports: [],
  templateUrl: './game-title.component.html',
  styleUrl: './game-title.component.css'
})
export class GameTitleComponent {
  private router = inject(Router);

  startGame() {
    this.router.navigate(['/unity-game']);
  }
}
