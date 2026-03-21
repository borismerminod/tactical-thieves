import { Component } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { base64urlToBuffer, bufferToBase64url } from '../../app/utils/webauthn.utils';
import { firstValueFrom, from } from 'rxjs';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {Router} from '@angular/router';


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

  serverURL: string = 'https://localhost:7186';

  constructor(private http: HttpClient, private router: Router) {}

  async onRegister() {
    this.message = 'Registration started...';

    try {

      /*if(this.username.length < 3) {
        this.message = 'Username must be at least 3 characters long';
        return;
      }*/

      // Appel RegisterStart pour obtenir les options de création de credential
      const startOptions: any = await firstValueFrom(
        this.http.post(
          `${this.serverURL}/api/auth/RegisterStart`,
          { username: this.username },
          { withCredentials: true }
        )
      );

      // Convertir challenge et id utilisateur en ArrayBuffer
      startOptions.challenge = base64urlToBuffer(startOptions.challenge);
      startOptions.user.id = base64urlToBuffer(startOptions.user.id);

      // Convertir excludeCredentials id en ArrayBuffer
      if (startOptions.excludeCredentials) {
        startOptions.excludeCredentials = startOptions.excludeCredentials.map((cred: any) => ({
          ...cred,
          id: base64urlToBuffer(cred.id)
        }));
      }

      // Créer la credential via le navigateur
      const credential: PublicKeyCredential = await navigator.credentials.create({
        publicKey: startOptions
      }) as PublicKeyCredential;

      console.log('Credential created:', credential);

      // Préparer les données pour RegisterFinish
      const credentialResponse = credential.response as AuthenticatorAttestationResponse;

      const attestationResponse: any = {
        id: credential.id,
        rawId: bufferToBase64url(credential.rawId),
        type: credential.type,

        clientExtensionResults: credential.getClientExtensionResults(),

        response: {
          clientDataJSON: bufferToBase64url(credentialResponse.clientDataJSON),
          attestationObject: bufferToBase64url(credentialResponse.attestationObject),
          transports: credentialResponse.getTransports ? credentialResponse.getTransports() : []
        }
      };

      // Envoyer à RegisterFinish pour créer l'utilisateur côté serveur
      
      const finishResult = await firstValueFrom(
        this.http.post(
          `${this.serverURL}/api/auth/RegisterFinish`,
          attestationResponse,
          { withCredentials: true }
        )
      );

      this.message = 'Registration successful !';
      this.router.navigate(['/login']);

    } catch (err: any) {

      console.error("FULL ERROR:", err);
      console.error("VALIDATION:", err.error);
      
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