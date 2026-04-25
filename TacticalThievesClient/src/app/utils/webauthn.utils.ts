/**
 * WebAuthn Utility Functions
 * 
 * This module provides helper functions for converting between different data formats
 * used in the WebAuthn authentication protocol.
 * 
 * WebAuthn requires:
 * - ArrayBuffer format for cryptographic operations (challenge, user ID, etc.)
 * - Base64URL format for transmission over HTTP and storage in JSON
 * 
 * These utilities handle the bidirectional conversion between these formats,
 * ensuring proper encoding and decoding of sensitive authentication data.
 */

/**
 * Converts a Base64URL-encoded string to an ArrayBuffer.
 * 
 * This conversion is necessary because:
 * - The WebAuthn API requires ArrayBuffer for cryptographic challenges and user IDs
 * - The server sends these values as Base64URL-encoded strings in HTTP responses
 * - Base64URL is URL-safe (uses '-' and '_' instead of '+' and '/')
 * 
 * The conversion process:
 * 1. Replace Base64URL characters with standard Base64 characters
 * 2. Use atob() to decode Base64 to binary string
 * 3. Convert each character code to a byte in a Uint8Array
 * 4. Return the underlying ArrayBuffer
 * 
 * @param base64url The Base64URL-encoded string to convert (e.g., "SGVsbG8gV29ybGQh")
 * @returns An ArrayBuffer containing the decoded binary data ready for WebAuthn operations
 */
export function base64urlToBuffer(base64url: string): ArrayBuffer {
  const base64 = base64url.replace(/-/g, '+').replace(/_/g, '/');
  const binary = atob(base64);
  const buffer = new Uint8Array(binary.length);

  for (let i = 0; i < binary.length; i++) {
    buffer[i] = binary.charCodeAt(i);
  }

  return buffer.buffer;
}

/**
 * Converts an ArrayBuffer to a Base64URL-encoded string.
 * 
 * This conversion is necessary because:
 * - WebAuthn operations produce ArrayBuffer results (rawId, clientDataJSON, attestationObject, etc.)
 * - These results must be sent to the server as JSON over HTTP
 * - Base64URL encoding is URL-safe and can be transmitted in JSON without escaping
 * 
 * The conversion process:
 * 1. Convert the ArrayBuffer to a Uint8Array for byte access
 * 2. Convert each byte to its character representation
 * 3. Use btoa() to encode the binary string to standard Base64
 * 4. Replace standard Base64 characters with Base64URL characters
 * 5. Remove padding characters ('=')
 * 
 * @param buffer The ArrayBuffer to convert (e.g., from navigator.credentials.create() or navigator.credentials.get())
 * @returns A Base64URL-encoded string safe for transmission and storage in JSON
 */
export function bufferToBase64url(buffer: ArrayBuffer): string {
  const bytes = new Uint8Array(buffer);
  let binary = '';

  bytes.forEach(b => binary += String.fromCharCode(b));

  return btoa(binary)
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=/g, '');
}

/**
 * Prepares public key credential options for WebAuthn registration or authentication.
 * 
 * Note: This function is currently unused but preserved for future reference.
 * Individual format methods (formatRegisterStartOptions, formatLoginStartOptions)
 * in AuthService handle specific option formatting based on the operation type.
 * 
 * @param options The raw public key credential options from the server
 * @returns The formatted options ready for WebAuthn API calls
 * 
 * 
 * export function preparePublicKeyOptions(options: any) {
 *   options.challenge = base64urlToBuffer(options.challenge);
 *   options.user.id = base64urlToBuffer(options.user.id);
 *   return options;
 * }
 */