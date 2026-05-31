import { Component } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { base64urlToBuffer, bufferToBase64url } from '../../app/utils/webauthn.utils';
import { firstValueFrom, from } from 'rxjs';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {Router} from '@angular/router';
import { AuthService } from '../../services/auth-service/auth.service';
import { environment } from '../../environments/environment';


@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css'],
  imports: [CommonModule, FormsModule],
})
export class RegisterComponent {
  username: string = '';
  displayName: string = '';
  message: string = '';

  //serverURL: string = 'https://localhost:7186';
  serverURL: string = 'https://mozell-fortifiable-moshe.ngrok-free.dev';
  //serverURL: string = 'https://tactical-thieves.loca.lt';

  constructor(private http: HttpClient, private router: Router, private authService : AuthService) {}

  async onRegister() 
  {
    try 
    {
      this.message = 'Registration started...';

      if(this.username.length < 3) 
      {
        this.message = 'Username must be at least 3 characters long';
        return;
      }

      const bSuccess : boolean = await this.authService.register(this.username)

      if(bSuccess)
      {
        this.message = 'Registration successful !';
        this.router.navigate(['/login']);
      }
      else
      {
         this.message = "Registration failed"
      }


    } catch (err: any) {

      if(environment.logEnabled)
      {
        console.error("FULL ERROR:", err);
        console.error("VALIDATION:", err.error);
      }
      
      this.message = this.authService.getErrorDetailForUser(err)
      this.message = "Registration failed \n"+ this.message

      if(environment.logEnabled)
        console.log("Displayed message:", this.message);

    }
  }
}