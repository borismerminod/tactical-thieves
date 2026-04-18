export type TacticalThievesPublicKeyCredentialRequestOptions =
{
    challenge: ArrayBuffer;
    timeout?: number;
    rpId?: string;
    allowCredentials?: PublicKeyCredentialDescriptor[]
    excludeCredentials? : PublicKeyCredentialDescriptor[]
    userVerification?: "required" | "preferred" | "discouraged";
    hints?: string[];
}

export type TacticalThievesAuthenticatorAssertionResponse = {
  id: string;
  rawId: string;
  type: string;
  clientExtensionResults: AuthenticationExtensionsClientOutputs;
  response: {
    authenticatorData: string;
    clientDataJSON: string;
    signature: string;
    userHandle: string | null;
  };
};

export type TacticalThievesLoginResponse = {
    token : string
    username : string
}

export type TacticalThievesAuthenticatorAttestationResponse = 
{
  id: string
  rawId: string
  type: string

  clientExtensionResults: AuthenticationExtensionsClientOutputs

  response: 
  {
    clientDataJSON: string
    attestationObject: string
    transports: string[]
  }
}

export type TacticalThievesRegisteredPasskey = {
  id: string
  type: 'public-key'
  publicKey: string
  signCount: number
  transports: AuthenticatorTransport[]

  aaGuid: string
  attestationFormat: string
  attestationObject: string
  attestationClientDataJson: string

  isBackedUp: boolean
  isBackupEligible: boolean

  user: {
    name: string
    id: string
    displayName: string
  }
}