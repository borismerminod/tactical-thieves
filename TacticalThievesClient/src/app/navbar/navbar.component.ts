import { Component, DoCheck } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-navbar',
  imports: [RouterModule, CommonModule],
  standalone: true,
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.css'
})
export class NavbarComponent implements DoCheck {

  isLoggedIn: boolean
  username: string 

  constructor() {
    this.isLoggedIn = false;
    this.username = ""
  }

  isUserLoggedIn(): boolean {
    return !!sessionStorage.getItem('authToken');
  }

  getUsername(): string {
    return sessionStorage.getItem('username') || '';
  }

  logout() {
    sessionStorage.removeItem('authToken');
    sessionStorage.removeItem('username');
    this.isLoggedIn = false;
    this.username = '';
  }

  ngDoCheck() {
    this.isLoggedIn = this.isUserLoggedIn();
    this.username = this.getUsername();
  }
}
