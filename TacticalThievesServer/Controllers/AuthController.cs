using Azure.Core;
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
using TacticalThievesServer.Models;


namespace TacticalThievesServer.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : Controller
    {
        private readonly Fido2 _fido2;
        private readonly ApplicationDbContext _db; // Ton DbContext
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthController(Fido2 fido2, ApplicationDbContext db, IHttpContextAccessor httpContextAccessor)
        {
            _fido2 = fido2;
            _db = db;
            _httpContextAccessor = httpContextAccessor;
        }



        [HttpPost("RegisterStart")]
        public async Task<IActionResult> RegisterStart([FromBody] AuthRequest request)
        {
            try
            {
                string username = request.Username.ToLower().Trim();

                if (string.IsNullOrEmpty(username))
                    return BadRequest("Username missing");

                // récupérer ou créer l'utilisateur
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);

                if (user == null)
                {
                    user = new User
                    {
                        Id = Guid.NewGuid(),
                        Username = username
                    };

                    _db.Users.Add(user);
                    await _db.SaveChangesAsync();
                }

                // créer l'utilisateur FIDO2
                var fidoUser = new Fido2User
                {
                    DisplayName = user.Username,
                    Name = user.Username,
                    Id = Encoding.UTF8.GetBytes(user.Id.ToString())
                };

                // récupérer les credentials existants
                var credentials = await _db.StoredCredentials
                    .Where(c => c.UserId == user.Id)
                    .ToListAsync();

                var existingKeys = new List<PublicKeyCredentialDescriptor>();

                foreach (var credential in credentials)
                {
                    existingKeys.Add(new PublicKeyCredentialDescriptor(credential.DescriptorId));
                }

                // config authenticator
                var authenticatorSelection = new AuthenticatorSelection
                {
                    ResidentKey = ResidentKeyRequirement.Discouraged,
                    UserVerification = UserVerificationRequirement.Preferred
                };

                // extensions
                var extensions = new AuthenticationExtensionsClientInputs
                {
                    Extensions = true,
                    CredProps = true,
                    UserVerificationMethod = true
                };

                //Paramètres pour la génération des credentials
                var requestParams = new RequestNewCredentialParams
                {
                    User = fidoUser,
                    ExcludeCredentials = existingKeys,
                    AuthenticatorSelection = authenticatorSelection,
                    AttestationPreference = AttestationConveyancePreference.None,
                    Extensions = extensions
                };

                var options = _fido2.RequestNewCredential(requestParams);

                // stocker en session ==> temporaire le temps de l'inscription
                HttpContext.Session.SetString("fido2.attestationOptions", options.ToJson());

                return Ok(options);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    status = "error",
                    message = ex.Message
                });
            }
        }

        [HttpPost("RegisterFinish")]
        public async Task<IActionResult> RegisterFinish([FromBody] AuthenticatorAttestationRawResponse clientResponse)
        {
            try
            {
                // récupérer les options stockées dans la session
                var jsonOptions = HttpContext.Session.GetString("fido2.attestationOptions");
                HttpContext.Session.Remove("fido2.attestationOptions");

                if (jsonOptions == null)
                    return BadRequest("Registration session expired");

                var options = CredentialCreateOptions.FromJson(jsonOptions);

                // callback pour vérifier que le credentialId est unique
                async Task<bool> IsCredentialIdUniqueToUser(IsCredentialIdUniqueToUserParams args, CancellationToken ct)
                {
                    return !await _db.StoredCredentials
                        .AnyAsync(c => c.DescriptorId.SequenceEqual(args.CredentialId), ct);
                }

                // construire les paramètres pour MakeNewCredential
                var makeCredentialParams = new MakeNewCredentialParams
                {
                    AttestationResponse = clientResponse,
                    OriginalOptions = options,
                    IsCredentialIdUniqueToUserCallback = IsCredentialIdUniqueToUser
                };

                // validation du credential
                var result = await _fido2.MakeNewCredentialAsync(makeCredentialParams);

                // récupérer l'utilisateur
                var userIdString = Encoding.UTF8.GetString(result.User.Id);
                var userId = Guid.Parse(userIdString);

                var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return BadRequest("User not found");

                // sauvegarder le credential
                var storedCredential = new StoredCredential
                {
                    DescriptorId = result.Id,
                    PublicKey = result.PublicKey,
                    Counter = result.SignCount,
                    UserId = userId
                };

                _db.StoredCredentials.Add(storedCredential);

                await _db.SaveChangesAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    status = "error",
                    message = ex.Message
                });
            }
        }

        [HttpPost("LoginStart")]
        public async Task<IActionResult> LoginStart([FromBody] AuthRequest request)
        {
            try
            {
                string username = request.Username.ToLower().Trim();

                if (string.IsNullOrEmpty(username))
                    return BadRequest("Username is required");

                //Récupérer l'utilisateur
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);

                if (user == null)
                    return BadRequest("User not found");

                //Récupérer les credentials enregistrés
                var credentials = await _db.StoredCredentials
                    .Where(c => c.UserId == user.Id)
                    .ToListAsync();

                if (!credentials.Any())
                    return BadRequest("No credentials registered");

                //Convertir en descriptors FIDO2
                var allowedCredentials = credentials
                    .Select(c => new PublicKeyCredentialDescriptor(c.DescriptorId))
                    .ToList();

                //Générer les options d'assertion
                var options = _fido2.GetAssertionOptions(
                    allowedCredentials,
                    UserVerificationRequirement.Preferred
                );

                //Stocker en session (OBLIGATOIRE pour LoginFinish)
                HttpContext.Session.SetString("fido2.assertionOptions", options.ToJson());
                HttpContext.Session.SetString("fido2.username", username);

                // Retourner au client
                return Ok(options);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    status = "error",
                    message = ex.Message
                });
            }
        }


        [HttpPost("LoginFinish")]
        public async Task<IActionResult> LoginFinish([FromBody] AuthenticatorAssertionRawResponse assertionResponse)
        {
            try
            {
                //Récupération des données stockées en session lors du LoginStart
                var optionsJson = HttpContext.Session.GetString("fido2.assertionOptions");
                var username = HttpContext.Session.GetString("fido2.username");

                //Nettoyage immédiat de la session (sécurité : usage unique du challenge)
                HttpContext.Session.Remove("fido2.attestationOptions");
                HttpContext.Session.Remove("fido2.username");

                if (optionsJson == null || username == null)
                    return BadRequest("Login session expired");

                //Reconstruction des options FIDO2 envoyées initialement au client
                var options = AssertionOptions.FromJson(optionsJson);

                var user = await _db.Users.FirstAsync(u => u.Username == username);
                var storedCredentials = await _db.StoredCredentials
                    .Where(c => c.UserId == user.Id)
                    .ToListAsync();

                //On retrouve le credential utilisé par le client (via RawId)
                var cred = storedCredentials.FirstOrDefault(c =>
                    c.DescriptorId.SequenceEqual(assertionResponse.RawId));

                if (cred == null)
                    return BadRequest("Credential not found");

                //Construction des paramètres nécessaires à la validation FIDO2
                var makeAssertionParams = new MakeAssertionParams
                {
                    // Réponse brute envoyée par le navigateur (WebAuthn)
                    AssertionResponse = assertionResponse,

                    // Options originales (challenge, rpId, etc.)
                    OriginalOptions = options,

                    // Clé publique stockée en base pour ce credential
                    StoredPublicKey = cred.PublicKey,

                    // Compteur de signature (protection contre replay attack)
                    StoredSignatureCounter = cred.Counter,

                    //Callback de sécurité : vérifie que le credential appartient bien à l'utilisateur
                    IsUserHandleOwnerOfCredentialIdCallback = async (args, ct) =>
                    {
                        var userCredentials = await _db.StoredCredentials
                            .Where(c => c.UserId == user.Id)
                            .ToListAsync(ct);

                        return userCredentials.Any(c =>
                            c.DescriptorId.SequenceEqual(args.CredentialId));
                    }
                };

                //Vérification cryptographique de l'assertion FIDO2
                var result = await _fido2.MakeAssertionAsync(makeAssertionParams);

                // Mise à jour du compteur (important pour sécurité future)
                cred.Counter = result.SignCount;
                _db.Update(cred);
                await _db.SaveChangesAsync();

                //Génération d’un JWT pour gérer la session côté client
                var token = GenerateJwtToken(username);

                return Ok(new
                {
                    token = token,
                    username = username
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    status = "error",
                    message = ex.Message
                });
            }
        }

        private string GenerateJwtToken(string username)
        {
            /*Clé secrète utilisée pour signer le token
            Doit faire au moins 32 caractères (256 bits) pour HS256
            DOTO En production ==> stocker dans appsettings.json ou variable d'environnement*/
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("SUPER_SECRET_KEY_1234567890123456")
            );

            //Création des credentials de signature avec l'algorithme HmacSha256
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            //Définition des informations (claims) contenues dans le token
            //Ces données seront accessibles côté client et serveur
            var claims = new[]
            {
                //Identité de l'utilisateur
                new System.Security.Claims.Claim("username", username)

                // Possiblement on peut en ajouter d'autre ici en fonction des attributs liés à l'utilisateur:
                // new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                // new Claim(ClaimTypes.Role, "Player")
            };

            //Création du token JWT
            var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                claims: claims,

                //Date d'expiration du token (ici 2 heures)
                expires: DateTime.UtcNow.AddHours(2),

                //Signature du token avec la clé secrète
                signingCredentials: creds
            );

            //Conversion du token en string (format compact JWT)
            return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler()
                .WriteToken(token);
        }
    }

    public class AuthRequest
    {
        [Required(ErrorMessage="Username is required")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Username must have between 3 and 20 characters")]
        [RegularExpression(@"^[a-zA-Z]+[a-zA-Z0-9]+$", ErrorMessage = "Username is not compliant") ]
        public string Username { get; set; }
    }

}
