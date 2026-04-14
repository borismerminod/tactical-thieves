import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { BehaviorSubject, firstValueFrom } from 'rxjs';
import { base64urlToBuffer, bufferToBase64url } from '../../app/utils/webauthn.utils';
import { LoggerService } from '../logger/logger.service';
import { TacticalThievesPublicKeyCredentialRequestOptions, TacticalThievesAuthenticatorAssertionResponse, TacticalThievesLoginResponse} from '../../models/webauthn/webauthn.types';


@Injectable({ providedIn: 'root' })
export class AuthService {

  private loggedIn = new BehaviorSubject<boolean>(this.hasToken())
  isLoggedIn$ = this.loggedIn.asObservable()

  private username = new BehaviorSubject<string>(this.getStoredUsername());
  username$ = this.username.asObservable();

  constructor(private http: HttpClient, private logger : LoggerService) {}

  private hasToken(): boolean 
  {
    return !!sessionStorage.getItem('authToken');
  }

  private getStoredUsername(): string 
  {
    return sessionStorage.getItem('username') || '';
  }


  registerStart(username: string) {
    return this.http.post<any>(
      '/api/auth/RegisterStart',
      username
    );
  }

  registerFinish(data: any) {
    return this.http.post(
      '/api/auth/RegisterFinish',
      data
    );
  }

  public logOut()
  {
    sessionStorage.clear()
    this.loggedIn.next(false)
    this.username.next('')
  }

  public async login(username: string) : Promise<boolean>
  {
    let success : boolean = false
    const startOptions : TacticalThievesPublicKeyCredentialRequestOptions = await this.loginStart(username)
    const assertionResponse : TacticalThievesAuthenticatorAssertionResponse = await this.requestAssertion(startOptions) 
    const result : TacticalThievesLoginResponse = await this.loginFinish(assertionResponse)

    if(result.token)
    {
      sessionStorage.setItem('authToken', result.token);
      sessionStorage.setItem('username', result.username);
      this.loggedIn.next(true);
      this.username.next(username);

      success = true
    }

    return success
  }

  private async loginStart(username: string) : Promise<TacticalThievesPublicKeyCredentialRequestOptions>
  {

    const loginStartURL = `${environment.apiURL}/api/auth/LoginStart`
    const usernameObj =  { username: username }
    const credentialOpt = { withCredentials: true }
    const startOptionsToFormat = await firstValueFrom(this.http.post(loginStartURL, usernameObj, credentialOpt));

    const startOptions : TacticalThievesPublicKeyCredentialRequestOptions = this.formatStartOptions(startOptionsToFormat)

    return startOptions

  }

  private async loginFinish(assertionResponse: TacticalThievesAuthenticatorAssertionResponse) : Promise<TacticalThievesLoginResponse>
  {
    const loginFinishURL : string = `${environment.apiURL}/api/auth/LoginFinish`
    const credentialOpt = { withCredentials: true }
    const result: TacticalThievesLoginResponse = await firstValueFrom( this.http.post(loginFinishURL, assertionResponse, credentialOpt)) as TacticalThievesLoginResponse

    this.logger.log(result)

    return result
  }

  private formatStartOptions(startOptionsToFormat : any) : TacticalThievesPublicKeyCredentialRequestOptions
  {
     this.logger.log(startOptionsToFormat)
      //Convertir challenge en ArrayBuffer
      startOptionsToFormat.challenge = base64urlToBuffer(startOptionsToFormat.challenge)

      this.logger.log(startOptionsToFormat)

      //Convertir chaque allowCredentials.id en ArrayBuffer
      if (startOptionsToFormat.allowCredentials) 
        startOptionsToFormat.allowCredentials = startOptionsToFormat.allowCredentials.map(this.convertCredentialsToArrayBuffer)
      

      // Convertir excludeCredentials id en ArrayBuffer
      if (startOptionsToFormat.excludeCredentials) 
        startOptionsToFormat.excludeCredentials = startOptionsToFormat.excludeCredentials.map(this.convertCredentialsToArrayBuffer)

      const startOptions: TacticalThievesPublicKeyCredentialRequestOptions = startOptionsToFormat

      return startOptions
  }

  private async requestAssertion(startOptions : TacticalThievesPublicKeyCredentialRequestOptions) : Promise<TacticalThievesAuthenticatorAssertionResponse>
  {

    const publicKeyObj = {publicKey: startOptions}
    // Récupérer WebAuthn assertion
    const assertion: PublicKeyCredential = await navigator.credentials.get(publicKeyObj) as PublicKeyCredential;

    // Préparer le AuthenticatorAssertionRawResponse
    const assertionResponse: TacticalThievesAuthenticatorAssertionResponse  = {
      id: assertion.id,
      rawId: bufferToBase64url(assertion.rawId),
      type: assertion.type,
      clientExtensionResults: assertion.getClientExtensionResults() || {},
      response: {
          authenticatorData: bufferToBase64url((assertion.response as any).authenticatorData),
          clientDataJSON: bufferToBase64url((assertion.response as any).clientDataJSON),
          signature: bufferToBase64url((assertion.response as any).signature),
          userHandle: (assertion.response as any).userHandle
            ? bufferToBase64url((assertion.response as any).userHandle)
            : null
        }
    }

    return assertionResponse

  }

  private convertCredentialsToArrayBuffer(cred: any)
  {
    return {...cred, id: base64urlToBuffer(cred.id)}
  }

  public getErrorDetailForUser(err : any)
  {
    let message = ""
    if(Object.hasOwn(err, 'error') && Object.hasOwn(err.error, 'errors'))
    {
      for(let validationError of Object.entries(err.error.errors))
      {
        if(validationError.length >= 2)
        {
          let errDetail: string = validationError[1] instanceof Array ? validationError[1].join("\n") : String(validationError[1]);
          message += `Validation error on ${validationError[0]}: ${errDetail}`;
          message += "\n";
        }
      }
    }

    return message
  }

}