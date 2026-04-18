import { Component, DoCheck } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth-service/auth.service';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-navbar',
  imports: [RouterModule, CommonModule],
  standalone: true,
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.css'
})
export class NavbarComponent {

  isLoggedIn$: Observable<boolean>
  username$: Observable<string>

  constructor(private router: Router, private authService : AuthService) {
    this.isLoggedIn$ = this.authService.isLoggedIn$
    this.username$ = this.authService.username$
  }

  logout() {
    this.authService.logout()
     this.router.navigate(['/home']);
  }

}
