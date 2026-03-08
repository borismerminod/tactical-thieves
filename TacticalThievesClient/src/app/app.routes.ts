import { Routes } from '@angular/router';
import { UnityGameComponent } from './unity-game/unity-game.component';
import { GameTitleComponent } from './game-title/game-title.component';

export const routes: Routes = [
    { path: '', pathMatch: 'full', redirectTo: 'home' },
    { path: 'home', component: GameTitleComponent },
    { path: 'unity-game', component: UnityGameComponent },

];
