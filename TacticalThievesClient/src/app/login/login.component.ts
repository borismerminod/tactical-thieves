// login.component.ts
import { Component } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { base64urlToBuffer, bufferToBase64url } from '../../app/utils/webauthn.utils';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css'],
  imports: [CommonModule, FormsModule]
})
export class LoginComponent {
  username: string = '';
  message: string = '';
  serverURL: string = 'https://localhost:7186';

  constructor(private http: HttpClient, private router: Router) {}

  async onLogin() {
    this.message = 'Starting login...';

    if(this.username === '') {
      this.message = 'Please enter a username';
      return;
    }

    try {
      // LoginStart : envoyer un objet { username }
      const startOptions: any = await firstValueFrom(
        this.http.post(`${this.serverURL}/api/auth/LoginStart`, 
          { username: this.username },
          { withCredentials: true })
      );

      //Convertir challenge et excludeCredentials en ArrayBuffer
      startOptions.challenge = base64urlToBuffer(startOptions.challenge);

      console.log('StartOptions:', startOptions);

      //Convertir chaque allowCredentials.id en ArrayBuffer
      if (startOptions.allowCredentials) {
        startOptions.allowCredentials = startOptions.allowCredentials.map((cred: any) => ({
          ...cred,
          id: base64urlToBuffer(cred.id)
        }));
      }

      // Convertir excludeCredentials id en ArrayBuffer
      if (startOptions.excludeCredentials) {
        startOptions.excludeCredentials = startOptions.excludeCredentials.map((cred: any) => ({
          ...cred,
          id: base64urlToBuffer(cred.id)
        }));
      }

      // Récupérer WebAuthn assertion
      const assertion: PublicKeyCredential = await navigator.credentials.get({
        publicKey: startOptions
      }) as PublicKeyCredential;

      // Préparer le AuthenticatorAssertionRawResponse
      const assertionResponse: any = {
      id: assertion.id,
      rawId: bufferToBase64url(assertion.rawId),
      type: assertion.type,
      response: {
        authenticatorData: bufferToBase64url((assertion.response as any).authenticatorData),
        clientDataJSON: bufferToBase64url((assertion.response as any).clientDataJSON),
        signature: bufferToBase64url((assertion.response as any).signature),
        userHandle: (assertion.response as any).userHandle
          ? bufferToBase64url((assertion.response as any).userHandle)
          : null
      },
      clientExtensionResults: assertion.getClientExtensionResults() || {}
    };

      // Appel LoginFinish
      const result: any = await firstValueFrom(
        this.http.post(`${this.serverURL}/api/auth/LoginFinish`, assertionResponse, { withCredentials: true })
      );

      if (result.token) {
        sessionStorage.setItem('authToken', result.token);
        sessionStorage.setItem('username', result.username); 
        this.message = 'Login successful!';
        console.log('JWT:', result.token, result.username);

        this.router.navigate(['/home']);

      } else {
        this.message = 'Error: Missing token';
      }

    } catch (err: any) {
      console.error("FULL ERROR:", err);
      //this.message = 'Error during login: ' + (err.error?.message || err.message || err.statusText);

       this.message = '';
      if(Object.hasOwn(err, 'error') && Object.hasOwn(err.error, 'errors'))
      {
        for(let validationError of Object.entries(err.error.errors))
        {
          let errDetail: string = validationError[1] instanceof Array ? validationError[1].join("\n") : String(validationError[1]);
          this.message += `Validation error on ${validationError[0]}: ${errDetail}`;
          this.message += "\n";
        }
      }

      //this.message = 'Error : ' + JSON.stringify(err.error) + ' '+(err.error?.message || err.message || err.statusText);
      this.message = "Registration failed \n"+ this.message
      console.log("Displayed message:", this.message);

    }
  }
}