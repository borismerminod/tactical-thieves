using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using TacticalThievesServer.Data;
using TacticalThievesServer.Models;
using Microsoft.EntityFrameworkCore;


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
        public async Task<IActionResult> RegisterStart([FromBody] string username)
        {
            try
            {
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

                var requestParams = new RequestNewCredentialParams
                {
                    User = fidoUser,
                    ExcludeCredentials = existingKeys,
                    AuthenticatorSelection = authenticatorSelection,
                    AttestationPreference = AttestationConveyancePreference.None,
                    Extensions = extensions
                };

                var options = _fido2.RequestNewCredential(requestParams);

                // stocker en session
                HttpContext.Session.SetString("fido2.attestationOptions", options.ToJson());

                // retourner au client
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
    }
}
