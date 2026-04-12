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