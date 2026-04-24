import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { BehaviorSubject, firstValueFrom } from 'rxjs';
import { base64urlToBuffer, bufferToBase64url } from '../../app/utils/webauthn.utils';
import { TacticalThievesAuthenticatorAssertionResponse, TacticalThievesLoginResponse, TacticalThievesAuthenticatorAttestationResponse, TacticalThievesRegisteredPasskey} from '../../models/webauthn/webauthn.types';


@Injectable({ providedIn: 'root' })
export class AuthService {

  private loggedIn = new BehaviorSubject<boolean>(this.hasToken())
  isLoggedIn$ = this.loggedIn.asObservable()

  private username = new BehaviorSubject<string>(this.getStoredUsername());
  username$ = this.username.asObservable();

  constructor(private http: HttpClient) {}

  private hasToken(): boolean 
  {
    return !!sessionStorage.getItem('authToken');
  }

  private getStoredUsername(): string 
  {
    return sessionStorage.getItem('username') || '';
  }


  public async register(username : string) : Promise<boolean>
  {
    let bSuccess: boolean = false

    const startOptions : PublicKeyCredentialCreationOptions = await this.registerStart(username)
    const attestationResponse : TacticalThievesAuthenticatorAttestationResponse = await this.requestAttestation(startOptions)
    const finishResult : TacticalThievesRegisteredPasskey = await this.registerFinish(attestationResponse)

    bSuccess = !!finishResult && 
                finishResult.type === 'public-key' && 
                !!finishResult.id && 
                !!finishResult.publicKey

    return bSuccess
  }

  async registerStart(username: string) : Promise<PublicKeyCredentialCreationOptions>
  {
    
     // Appel RegisterStart pour obtenir les options de création de credential
    const registerStartURL = `${environment.apiURL}/api/auth/RegisterStart`
    const usernameObj = { username: username}
    const credentialOpt =  { withCredentials: true }

    const startOptionsToFormat = await firstValueFrom(this.http.post(registerStartURL, usernameObj, credentialOpt))


    const startOptions : PublicKeyCredentialCreationOptions = this.formatRegisterStartOptions(startOptionsToFormat)

    return startOptions

  }

  async registerFinish(attestationResponse: TacticalThievesAuthenticatorAttestationResponse) : Promise<TacticalThievesRegisteredPasskey>
  {
    if(environment.logEnabled)
      console.log(attestationResponse)

    let bSuccess: boolean = true
    const registerFinishURL : string = `${environment.apiURL}/api/auth/RegisterFinish`
    const credentialOpt = { withCredentials: true }
    const finishResult : TacticalThievesRegisteredPasskey= await firstValueFrom(this.http.post( registerFinishURL, attestationResponse, credentialOpt)) as TacticalThievesRegisteredPasskey

    if(environment.logEnabled)
      console.log(finishResult)
    
    return finishResult

  }

  formatRegisterStartOptions(startOptionsToFormat : any) : PublicKeyCredentialCreationOptions
  {
    
    if(environment.logEnabled)
      console.log(startOptionsToFormat)
    
    // Convertir challenge et id utilisateur en ArrayBuffer
    startOptionsToFormat.challenge = base64urlToBuffer(startOptionsToFormat.challenge);
    startOptionsToFormat.user.id = base64urlToBuffer(startOptionsToFormat.user.id);
    
    if(startOptionsToFormat.excludeCredentials)
      startOptionsToFormat.excludeCredentials = startOptionsToFormat.excludeCredentials.map(this.convertCredentialsToArrayBuffer)
    
    const startOptions : PublicKeyCredentialCreationOptions = startOptionsToFormat

    return startOptions
  }

  async requestAttestation(startOptions: PublicKeyCredentialCreationOptions) : Promise<TacticalThievesAuthenticatorAttestationResponse>
  {
    // Créer la credential via le navigateur
      const credential: PublicKeyCredential = await navigator.credentials.create({ publicKey: startOptions }) as PublicKeyCredential;

      const credentialResponse = credential.response as AuthenticatorAttestationResponse;

      const attestationResponse: TacticalThievesAuthenticatorAttestationResponse = {
        id: credential.id,
        rawId: bufferToBase64url(credential.rawId),
        type: credential.type,

        clientExtensionResults: credential.getClientExtensionResults(),

        response: {
          clientDataJSON: bufferToBase64url(credentialResponse.clientDataJSON),
          attestationObject: bufferToBase64url(credentialResponse.attestationObject),
          transports: credentialResponse.getTransports ? credentialResponse.getTransports() : []
        }
      }

      if(environment.logEnabled)
        console.log(attestationResponse)

      return attestationResponse
  }

  

  public logout()
  {
    sessionStorage.clear()
    this.loggedIn.next(false)
    this.username.next('')
  }


  public async login(username: string) : Promise<boolean>
  {
    let success : boolean = false
    const startOptions : PublicKeyCredentialCreationOptions = await this.loginStart(username)
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

  private async loginStart(username: string) : Promise<PublicKeyCredentialCreationOptions>
  {

    const loginStartURL = `${environment.apiURL}/api/auth/LoginStart`
    const usernameObj =  { username: username }
    const credentialOpt = { withCredentials: true }
    const startOptionsToFormat = await firstValueFrom(this.http.post(loginStartURL, usernameObj, credentialOpt));

    const startOptions : PublicKeyCredentialCreationOptions = this.formatLoginStartOptions(startOptionsToFormat)

    return startOptions

  }

  private async loginFinish(assertionResponse: TacticalThievesAuthenticatorAssertionResponse) : Promise<TacticalThievesLoginResponse>
  {
    const loginFinishURL : string = `${environment.apiURL}/api/auth/LoginFinish`
    const credentialOpt = { withCredentials: true }
    const result: TacticalThievesLoginResponse = await firstValueFrom( this.http.post(loginFinishURL, assertionResponse, credentialOpt)) as TacticalThievesLoginResponse

    if(environment.logEnabled)
      console.log(result)

    return result
  }

  private formatLoginStartOptions(startOptionsToFormat : any) : PublicKeyCredentialCreationOptions
  {
    if(environment.logEnabled)
      console.log(startOptionsToFormat)
      //Convertir challenge en ArrayBuffer
      startOptionsToFormat.challenge = base64urlToBuffer(startOptionsToFormat.challenge)

      if(environment.logEnabled)
        console.log(startOptionsToFormat)

      //Convertir chaque allowCredentials.id en ArrayBuffer
      if (startOptionsToFormat.allowCredentials) 
        startOptionsToFormat.allowCredentials = startOptionsToFormat.allowCredentials.map(this.convertCredentialsToArrayBuffer)

      const startOptions: PublicKeyCredentialCreationOptions = startOptionsToFormat

      return startOptions
  }

  private async requestAssertion(startOptions : PublicKeyCredentialCreationOptions) : Promise<TacticalThievesAuthenticatorAssertionResponse>
  {

    const publicKeyObj = {publicKey: startOptions}
    // Récupérer WebAuthn assertion
    const assertion: PublicKeyCredential = await navigator.credentials.get(publicKeyObj) as PublicKeyCredential;

    // Préparer le AuthenticatorAssertionRawResponse
    const assertionResponse: any  = {
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

    if(environment.logEnabled)
      console.log(assertionResponse)

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