using Azure.Core;
using DotNetEnv;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using TacticalThievesServer.Data;
using TacticalThievesServer.DTO;
using TacticalThievesServer.Models;


namespace TacticalThievesServer.Controllers
{
    /// <summary>
    /// AuthController handles authentication flows using FIDO2 (WebAuthn) and JWT generation.
    /// Responsibilities:
    ///  - Start and finish FIDO2 registration (RegisterStart, RegisterFinish).
    ///  - Start and finish FIDO2 login/assertion (LoginStart, LoginFinish).
    ///  - Generate JWT tokens after successful authentication.
    /// The controller persists users and stored credentials to the database and uses session
    /// to keep temporary FIDO2 options between start and finish calls.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    public class AuthController : Controller
    {
        private readonly Fido2 fido2;
        private readonly ApplicationDbContext db;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ILogger<AuthController> logger;

        public AuthController(Fido2 fido2, ApplicationDbContext db, IHttpContextAccessor httpContextAccessor, ILogger<AuthController> logger)
        {
            this.fido2 = fido2;
            this.db = db;
            this.httpContextAccessor = httpContextAccessor;
            this.logger = logger;
        }



        /// <summary>
        /// Initiates FIDO2 registration for a given username.
        ///  - Creates the user if it does not exist.
        ///  - Builds FIDO2 credential creation options and stores them in session
        ///    for the subsequent <see cref="RegisterFinish"/> call.
        /// Returns the options to be consumed by the client (WebAuthn API).
        /// </summary>
        [HttpPost("RegisterStart")]
        public async Task<IActionResult> RegisterStart([FromBody] AuthRequestDTO request)
        {
            try
            {
                string? username = request.Username?.ToLower().Trim();

                if (string.IsNullOrEmpty(username))
                    return BadRequest("Username missing");

                // get or create user
                var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);

                if (user == null)
                {
                    var progress = new PlayerProgress
                    {
                        CurrentLevel = 1
                    };

                    user = new User
                    {
                        Id = Guid.NewGuid(),
                        Username = username,
                        CurrentLevel = progress
                    };

                    db.Users.Add(user);
                    await db.SaveChangesAsync();
                }

                // Create Fido2User object required for options generation
                var fidoUser = new Fido2User
                {
                    DisplayName = user.Username,
                    Name = user.Username,
                    Id = Encoding.UTF8.GetBytes(user.Id.ToString())
                };

                // get existing credentials for the user to exclude them from registration options
                var credentials = await db.StoredCredentials
                    .Where(c => c.UserId == user.Id)
                    .ToListAsync();

                var existingKeys = new List<PublicKeyCredentialDescriptor>();

                foreach (var credential in credentials)
                {
                    if (credential.DescriptorId != null)
                        existingKeys.Add(new PublicKeyCredentialDescriptor(credential.DescriptorId));
                }

                // configure authenticator selection criteria (e.g. resident key, user verification)
                var authenticatorSelection = new AuthenticatorSelection
                {
                    ResidentKey = ResidentKeyRequirement.Discouraged,
                    UserVerification = UserVerificationRequirement.Preferred
                };

                // Adding extensions
                var extensions = new AuthenticationExtensionsClientInputs
                {
                    Extensions = true,
                    CredProps = true,
                    UserVerificationMethod = true
                };

                //generate credential creation param options
                var requestParams = new RequestNewCredentialParams
                {
                    User = fidoUser,
                    ExcludeCredentials = existingKeys,
                    AuthenticatorSelection = authenticatorSelection,
                    AttestationPreference = AttestationConveyancePreference.None,
                    Extensions = extensions
                };

                var options = fido2.RequestNewCredential(requestParams);

                // store options in session for the RegisterFinish step
                HttpContext.Session.SetString("fido2.attestationOptions", options.ToJson());

                return Ok(options);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in RegisterStart");
                return BadRequest(new
                {
                    status = "error",
                    message = "An error occurred"
                });
            }
        }

        /// <summary>
        /// Completes FIDO2 registration by validating the attestation response from the client.
        ///  - Reads the previously stored credential creation options from session.
        ///  - Validates the attestation and persists the resulting credential.
        /// Returns the FIDO2 result to the client or an error if validation fails.
        /// </summary>
        [HttpPost("RegisterFinish")]
        public async Task<IActionResult> RegisterFinish([FromBody] AuthenticatorAttestationRawResponse clientResponse)
        {
            try
            {
                // get options from session (set in RegisterStart) and remove immediately for security (one-time use)
                var jsonOptions = HttpContext.Session.GetString("fido2.attestationOptions");
                HttpContext.Session.Remove("fido2.attestationOptions");

                if (jsonOptions == null)
                    return BadRequest("Registration session expired");

                var options = CredentialCreateOptions.FromJson(jsonOptions);

                // we need to check that the credential ID is unique across all users (not already registered)
                async Task<bool> IsCredentialIdUniqueToUser(IsCredentialIdUniqueToUserParams args, CancellationToken ct)
                {
                    return !await db.StoredCredentials
                        .AnyAsync(c => c.DescriptorId.SequenceEqual(args.CredentialId), ct);
                }

                // create parameters for MakeNewCredentialAsync
                var makeCredentialParams = new MakeNewCredentialParams
                {
                    AttestationResponse = clientResponse,
                    OriginalOptions = options,
                    IsCredentialIdUniqueToUserCallback = IsCredentialIdUniqueToUser
                };

                // get credential validation result (this will throw if validation fails)
                var result = await fido2.MakeNewCredentialAsync(makeCredentialParams);

                // retrieve the user
                var userIdString = Encoding.UTF8.GetString(result.User.Id);
                var userId = Guid.Parse(userIdString);

                var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return BadRequest("User not found");

                // save the credential information in the database
                var storedCredential = new StoredCredential
                {
                    DescriptorId = result.Id,
                    PublicKey = result.PublicKey,
                    Counter = result.SignCount,
                    UserId = userId
                };

                db.StoredCredentials.Add(storedCredential);

                await db.SaveChangesAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in RegisterFinish");
                return BadRequest(new
                {
                    status = "error",
                    message = "An error occurred"
                });
            }
        }

        /// <summary>
        /// Initiates FIDO2 login (assertion) for a username.
        ///  - Loads stored credentials for the user and generates assertion options.
        ///  - Stores assertion options and username in session for the subsequent <see cref="LoginFinish"/> call.
        /// Returns assertion options to the client to perform the WebAuthn assertion.
        /// </summary>
        [HttpPost("LoginStart")]
        public async Task<IActionResult> LoginStart([FromBody] AuthRequestDTO request)
        {
            try
            {
                string? username = request.Username?.ToLower().Trim();

                if (string.IsNullOrEmpty(username))
                    return BadRequest("Username is required");

                //get user
                var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);

                if (user == null)
                    return BadRequest("User not found");

                //get stored credentials for the user
                var credentials = await db.StoredCredentials
                    .Where(c => c.UserId == user.Id)
                    .ToListAsync();

                if (!credentials.Any())
                    return BadRequest("No credentials registered");

                //convert stored credentials to PublicKeyCredentialDescriptor list for options generation
                var allowedCredentials = credentials
                    .Select(c => new PublicKeyCredentialDescriptor(c.DescriptorId))
                    .ToList();

                //Create assertion options with the allowed credentials and user verification preference
                var options = fido2.GetAssertionOptions(
                    allowedCredentials,
                    UserVerificationRequirement.Preferred
                );

                //Store options and username in session for the LoginFinish step (one-time use)
                HttpContext.Session.SetString("fido2.assertionOptions", options.ToJson());
                HttpContext.Session.SetString("fido2.username", username);

                return Ok(options);
            }
            catch (Exception ex)
            {  
                logger.LogError(ex, "Error in LoginStart");
                return BadRequest(new
                {
                    status = "error",
                    message = "An error occurred"
                });
            }
        }


        /// <summary>
        /// Completes FIDO2 login by validating the assertion response from the client.
        ///  - Reads assertion options and username from session, validates the assertion,
        ///    updates the stored credential counter, and issues a JWT on success.
        /// Returns a JSON object containing the JWT and username.
        /// </summary>
        [HttpPost("LoginFinish")]
        public async Task<IActionResult> LoginFinish([FromBody] AuthenticatorAssertionRawResponse assertionResponse)
        {
            try
            {
                //get assertion options and username from session (set in LoginStart) and remove immediately for security (one-time use)
                var optionsJson = HttpContext.Session.GetString("fido2.assertionOptions");
                var username = HttpContext.Session.GetString("fido2.username");

                //Immediate session cleanup (security: one-time use of the challenge)
                HttpContext.Session.Remove("fido2.attestationOptions");
                HttpContext.Session.Remove("fido2.username");

                if (optionsJson == null || username == null)
                    return BadRequest("Login session expired");

                //rebuild options object from JSON stored in session
                var options = AssertionOptions.FromJson(optionsJson);

                var user = await db.Users.FirstAsync(u => u.Username == username);
                var storedCredentials = await db.StoredCredentials
                    .Where(c => c.UserId == user.Id)
                    .ToListAsync();

                //Get credential that matches the assertion response's raw ID (credential ID)
                var cred = storedCredentials.FirstOrDefault(c =>
                    c.DescriptorId.SequenceEqual(assertionResponse.RawId));

                if (cred == null)
                    return BadRequest("Credential not found");

                //Build parameters for MakeAssertionAsync, including the callback to verify credential ownership
                var makeAssertionParams = new MakeAssertionParams
                {
                    // Raw response sent by the browser (WebAuthn)
                    AssertionResponse = assertionResponse,

                    // original options generated in LoginStart (contains challenge, allowed credentials, etc.)
                    OriginalOptions = options,

                    // public key stored in the database for this credential (used for signature verification)
                    StoredPublicKey = cred.PublicKey,

                    // signature counter
                    StoredSignatureCounter = cred.Counter,

                    //Security callback to verify that the credential ID in the assertion response belongs to the user (defense in depth)
                    IsUserHandleOwnerOfCredentialIdCallback = async (args, ct) =>
                    {
                        var userCredentials = await db.StoredCredentials
                            .Where(c => c.UserId == user.Id)
                            .ToListAsync(ct);

                        return userCredentials.Any(c =>
                            c.DescriptorId.SequenceEqual(args.CredentialId));
                    }
                };

                //Check the assertion response against the stored credential and options (this will throw if validation fails)
                var result = await fido2.MakeAssertionAsync(makeAssertionParams);

                // Update the signature counter in the database to prevent replay attacks (the authenticator should increment this counter on each use)
                cred.Counter = result.SignCount;
                db.Update(cred);
                await db.SaveChangesAsync();

                //Generate a JWT token for the authenticated user (you can include claims as needed, e.g. username, roles, etc.)
                var token = GenerateJwtToken(username);

                return Ok(new
                {
                    token = token,
                    username = username
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in LoginFinish");
                return BadRequest(new
                {
                    status = "error",
                    message = "An error occurred"
                });
            }
        }

        /// <summary>
        /// Generates a signed JWT containing the provided username claim.
        /// The secret key is read from the environment variable `JWT_KEY` (via .env loader).
        /// </summary>
        /// <param name="username">The username to include in the token claims.</param>
        /// <returns>A signed JWT as a string.</returns>
        private string GenerateJwtToken(string username)
        {

            Env.Load(); // Read environment variables from .env file

            var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY");

            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new Exception("JWT_KEY n'est pas défini dans le .env");
            }
 
            //Secret key used to sign the token, must be at least 32 characters (256 bits) for HS256
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            );

            //Create signing credentials using the secret key and specifying the algorithm (HMAC SHA256)
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            //Define the claims contained in the token
            //These data will be accessible on both client and server sides
            var claims = new[]
            {
                //The username claim can be used to identify the user in subsequent requests when the token is sent back to the server
                new System.Security.Claims.Claim("username", username)
            };

            //Create the JWT token with the specified claims, expiration time, and signing credentials
            var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                claims: claims,

                //Expiration time of the token (here set to 2 hours, adjust as needed)
                expires: DateTime.UtcNow.AddHours(2),

                //Token signing credentials (the secret key and algorithm used to sign the token)
                signingCredentials: creds
            );

            //Convert the token object to a string format that can be sent to the client (this will be a compact JWT string)
            return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler()
                .WriteToken(token);
        }
    }

}
