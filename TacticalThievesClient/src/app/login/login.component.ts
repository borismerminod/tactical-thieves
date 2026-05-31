// login.component.ts
import { Component } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { base64urlToBuffer, bufferToBase64url } from '../../app/utils/webauthn.utils';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth-service/auth.service';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css'],
  imports: [CommonModule, FormsModule]
})
export class LoginComponent {
  username: string = '';
  message: string = '';

  constructor(private http: HttpClient, private router: Router, private authService: AuthService) {}

  async onLogin() 
  {
    try 
    {
      this.message = 'Starting login...';
  
      if(this.username === '') {
        this.message = 'Please enter a username';
        return;
      }
      
      const success : boolean = await this.authService.login(this.username)

      if(success)
      {
        this.router.navigate(['/home']);
      }
      else
      {
        this.message = 'Error: Missing token'
      }

    } 
    catch (err: any) 
    {

      if(environment.logEnabled)
        console.error("FULL ERROR:", err)

      this.message = this.authService.getErrorDetailForUser(err)
      this.message = "Login failed \n"+ this.message
    }
  }


}