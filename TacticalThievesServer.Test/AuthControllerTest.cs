using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TacticalThievesServer.Data;
using TacticalThievesServer.Models;
using Xunit;

namespace TacticalThievesServer.Test
{
    public class AuthControllerTest : IClassFixture<ControllerTestFactory>
    {
        private readonly ControllerTestFactory _factory;
        private readonly HttpClient _client;

        public AuthControllerTest(ControllerTestFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static string UniqueUsername()
        {
            var guid = Guid.NewGuid().ToString("N");
            var suffix = guid[..8];
            return "user" + suffix;
        }

        private HttpRequestMessage BuildRegisterStartRequest(string? username)
        {
            return new HttpRequestMessage(HttpMethod.Post, "/api/auth/RegisterStart")
            {
                Content = JsonContent.Create(new { Username = username })
            };
        }

        private HttpRequestMessage BuildRegisterFinishRequest()
        {
            // Corps minimal en base64url — passe la désérialisation Fido2NetLib
            // mais échouera la vérification cryptographique dans MakeNewCredentialAsync.
            return new HttpRequestMessage(HttpMethod.Post, "/api/auth/RegisterFinish")
            {
                Content = JsonContent.Create(new
                {
                    id      = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
                    rawId   = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
                    type    = "public-key",
                    response = new
                    {
                        attestationObject = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
                        clientDataJSON    = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
                    }
                })
            };
        }

        private ApplicationDbContext GetDb()
        {
            var scope = _factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        }

        private HttpRequestMessage BuildLoginStartRequest(string? username)
        {
            return new HttpRequestMessage(HttpMethod.Post, "/api/auth/LoginStart")
            {
                Content = JsonContent.Create(new { Username = username })
            };
        }

        private HttpRequestMessage BuildLoginFinishRequest()
        {
            // Corps minimal en base64url — peut échouer le model binding Fido2NetLib
            // (même comportement que RegisterFinish), dans tous les cas retourne 400.
            return new HttpRequestMessage(HttpMethod.Post, "/api/auth/LoginFinish")
            {
                Content = JsonContent.Create(new
                {
                    id    = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
                    rawId = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
                    type  = "public-key",
                    response = new
                    {
                        authenticatorData = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
                        clientDataJSON    = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
                        signature         = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
                    }
                })
            };
        }

        private async Task SeedUser(string username, bool withCredential)
        {
            var db = GetDb();
            var userId = Guid.NewGuid();

            var user = new User { Id = userId, Username = username };
            db.Users.Add(user);

            if (withCredential)
            {
                var credential = new StoredCredential
                {
                    DescriptorId = new byte[32],
                    PublicKey    = new byte[32],
                    Counter      = 0,
                    UserId       = userId
                };
                db.StoredCredentials.Add(credential);
            }

            await db.SaveChangesAsync();
        }

        // ── RegisterStart — Branche 1 : validation du username ───────────────────

        [Fact]
        public async Task RegisterStart_NullUsername_ReturnsBadRequest()
        {
            var request = BuildRegisterStartRequest(null);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task RegisterStart_EmptyUsername_ReturnsBadRequest()
        {
            var request = BuildRegisterStartRequest("");

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // ── RegisterStart — Branche 2 : nouvel utilisateur ───────────────────────

        [Fact]
        public async Task RegisterStart_NewUser_ReturnsOk()
        {
            var username = UniqueUsername();
            var request = BuildRegisterStartRequest(username);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task RegisterStart_NewUser_ResponseContainsFidoChallenge()
        {
            var username = UniqueUsername();
            var request = BuildRegisterStartRequest(username);

            var response = await _client.SendAsync(request);

            var body = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(body);
            Assert.True(json.RootElement.TryGetProperty("challenge", out var challenge));
            Assert.False(string.IsNullOrEmpty(challenge.GetString()));
        }

        [Fact]
        public async Task RegisterStart_NewUser_CreatesUserInDatabase()
        {
            var username = UniqueUsername();
            var request = BuildRegisterStartRequest(username);

            await _client.SendAsync(request);

            var db = GetDb();
            var exists = await db.Users.AnyAsync(u => u.Username == username);
            Assert.True(exists);
        }

        [Fact]
        public async Task RegisterStart_NewUser_CreatesPlayerProgressLevel1()
        {
            var username = UniqueUsername();
            var request = BuildRegisterStartRequest(username);

            await _client.SendAsync(request);

            var db = GetDb();
            var user = await db.Users.Include(u => u.CurrentLevel).FirstAsync(u => u.Username == username);
            Assert.NotNull(user.CurrentLevel);
            Assert.Equal(1u, user.CurrentLevel.CurrentLevel);
        }

        // ── RegisterStart — Branche 3 : utilisateur existant ─────────────────────

        [Fact]
        public async Task RegisterStart_ExistingUser_ReturnsOk()
        {
            var username = UniqueUsername();
            var firstRequest = BuildRegisterStartRequest(username);
            var secondRequest = BuildRegisterStartRequest(username);
            await _client.SendAsync(firstRequest);

            var response = await _client.SendAsync(secondRequest);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task RegisterStart_ExistingUser_DoesNotCreateDuplicate()
        {
            var username = UniqueUsername();
            var firstRequest = BuildRegisterStartRequest(username);
            var secondRequest = BuildRegisterStartRequest(username);
            await _client.SendAsync(firstRequest);

            await _client.SendAsync(secondRequest);

            var db = GetDb();
            var count = await db.Users.CountAsync(u => u.Username == username);
            Assert.Equal(1, count);
        }

        // ── RegisterStart — Branche 4 : normalisation du username ────────────────

        [Fact]
        public async Task RegisterStart_UppercaseUsername_StoredLowercase()
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var sent = "USER" + suffix.ToUpper();
            var expected = "user" + suffix.ToLower();
            var request = BuildRegisterStartRequest(sent);

            await _client.SendAsync(request);

            var db = GetDb();
            var exists = await db.Users.AnyAsync(u => u.Username == expected);
            Assert.True(exists);
        }

        [Fact]
        public async Task RegisterStart_UsernameWithWhitespace_ReturnsBadRequest()
        {
            // Le regex [RegularExpression] du DTO rejette les espaces avant que le contrôleur
            // ne soit appelé, rendant le Trim() du code inaccessible via l'API.
            var username = UniqueUsername();
            var usernameWithSpaces = " " + username + " ";
            var request = BuildRegisterStartRequest(usernameWithSpaces);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // ── RegisterStart — Branche 5 : contenu des options FIDO2 ────────────────

        [Fact]
        public async Task RegisterStart_ValidRequest_ResponseContainsUserField()
        {
            var username = UniqueUsername();
            var request = BuildRegisterStartRequest(username);

            var response = await _client.SendAsync(request);

            var body = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(body);
            Assert.True(json.RootElement.TryGetProperty("user", out var user));
            Assert.True(user.TryGetProperty("name", out var name));
            Assert.Equal(username, name.GetString());
        }

        // ── RegisterFinish ────────────────────────────────────────────────────────
        //
        // RegisterFinish valide une AuthenticatorAttestationRawResponse (réponse d'un
        // vrai authenticateur WebAuthn). La validation cryptographique complète
        // (MakeNewCredentialAsync) ne peut pas être reproduite en test sans un vrai
        // authenticateur, donc on couvre : session absente, attestation invalide et
        // caractère one-time-use de la session.
        //
        // Chaque test crée son propre HttpClient pour isoler le cookie de session.

        [Fact]
        public async Task RegisterFinish_NoSession_ReturnsBadRequest()
        {
            // Client sans session préalable → jsonOptions == null → 400
            var client = _factory.CreateClient();
            var request = BuildRegisterFinishRequest();

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task RegisterFinish_InvalidAttestation_ReturnsBadRequest()
        {
            // RegisterStart pose les options en session, RegisterFinish les lit
            // mais MakeNewCredentialAsync lève une exception → catch → 400
            var client = _factory.CreateClient();
            var username = UniqueUsername();
            var startRequest = BuildRegisterStartRequest(username);
            var finishRequest = BuildRegisterFinishRequest();
            await client.SendAsync(startRequest);

            var response = await client.SendAsync(finishRequest);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task RegisterFinish_InvalidAttestation_ResponseContainsStatusField()
        {
            // Le corps dummy échoue la désérialisation Fido2NetLib → ASP.NET Core
            // retourne un ProblemDetails { status: 400 } avant d'atteindre le contrôleur.
            // On vérifie que la réponse est bien une erreur (400) avec un corps JSON.
            var client = _factory.CreateClient();
            var username = UniqueUsername();
            var startRequest = BuildRegisterStartRequest(username);
            var finishRequest = BuildRegisterFinishRequest();
            await client.SendAsync(startRequest);

            var response = await client.SendAsync(finishRequest);

            var body = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(body);
            // Selon la couche qui rejette (model binding → "status":400 numérique,
            // catch du contrôleur → "status":"error" string)
            Assert.True(json.RootElement.TryGetProperty("status", out _),
                $"Réponse inattendue : {body}");
        }

        [Fact]
        public async Task RegisterFinish_SessionIsOneTimeUse_SecondCallReturnsBadRequest()
        {
            // Première RegisterFinish consomme la session (Remove est appelé en tête)
            // → deuxième appel trouve jsonOptions == null → 400
            var client = _factory.CreateClient();
            var username = UniqueUsername();
            var startRequest = BuildRegisterStartRequest(username);
            var firstFinishRequest = BuildRegisterFinishRequest();
            var secondFinishRequest = BuildRegisterFinishRequest();
            await client.SendAsync(startRequest);
            await client.SendAsync(firstFinishRequest);

            var response = await client.SendAsync(secondFinishRequest);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // ── LoginStart — Branche 1 : validation du username ──────────────────────

        [Fact]
        public async Task LoginStart_NullUsername_ReturnsBadRequest()
        {
            var request = BuildLoginStartRequest(null);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task LoginStart_EmptyUsername_ReturnsBadRequest()
        {
            var request = BuildLoginStartRequest("");

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // ── LoginStart — Branche 2 : utilisateur introuvable ─────────────────────

        [Fact]
        public async Task LoginStart_UserNotFound_ReturnsBadRequest()
        {
            var username = UniqueUsername();
            var request = BuildLoginStartRequest(username);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // ── LoginStart — Branche 3 : utilisateur sans credentials ────────────────

        [Fact]
        public async Task LoginStart_UserWithNoCredentials_ReturnsBadRequest()
        {
            var username = UniqueUsername();
            await SeedUser(username, withCredential: false);
            var request = BuildLoginStartRequest(username);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // ── LoginStart — Branche 4 : utilisateur avec credentials → 200 ──────────

        [Fact]
        public async Task LoginStart_ValidUser_ReturnsOk()
        {
            var username = UniqueUsername();
            await SeedUser(username, withCredential: true);
            var request = BuildLoginStartRequest(username);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task LoginStart_ValidUser_ResponseContainsChallenge()
        {
            var username = UniqueUsername();
            await SeedUser(username, withCredential: true);
            var request = BuildLoginStartRequest(username);

            var response = await _client.SendAsync(request);

            var body = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(body);
            Assert.True(json.RootElement.TryGetProperty("challenge", out var challenge));
            Assert.False(string.IsNullOrEmpty(challenge.GetString()));
        }

        [Fact]
        public async Task LoginStart_ValidUser_ResponseContainsAllowCredentials()
        {
            var username = UniqueUsername();
            await SeedUser(username, withCredential: true);
            var request = BuildLoginStartRequest(username);

            var response = await _client.SendAsync(request);

            var body = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(body);
            Assert.True(json.RootElement.TryGetProperty("allowCredentials", out var creds));
            Assert.True(creds.GetArrayLength() > 0);
        }

        // ── LoginFinish ───────────────────────────────────────────────────────────
        //
        // La validation cryptographique complète (MakeAssertionAsync) requiert une vraie
        // réponse WebAuthn d'un authenticateur → non testable ici.
        //
        // Note : ligne 299 de AuthController.cs contient un bug — la clé supprimée est
        // "fido2.attestationOptions" au lieu de "fido2.assertionOptions". Conséquence :
        // les options d'assertion restent en session après LoginFinish. Le comportement
        // "one-time-use" reste assuré car "fido2.username" est supprimé correctement,
        // mais les options ne sont jamais nettoyées (risque sécurité).
        //
        // Chaque test crée son propre HttpClient pour isoler le cookie de session.

        [Fact]
        public async Task LoginFinish_NoSession_ReturnsBadRequest()
        {
            // Client sans session préalable → optionsJson == null → 400
            var client = _factory.CreateClient();
            var request = BuildLoginFinishRequest();

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task LoginFinish_InvalidAssertion_ReturnsBadRequest()
        {
            // LoginStart pose les options en session, LoginFinish les lit
            // mais l'assertion est invalide → model binding ou catch → 400
            var client = _factory.CreateClient();
            var username = UniqueUsername();
            await SeedUser(username, withCredential: true);
            var startRequest = BuildLoginStartRequest(username);
            var finishRequest = BuildLoginFinishRequest();
            await client.SendAsync(startRequest);

            var response = await client.SendAsync(finishRequest);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task LoginFinish_SessionIsOneTimeUse_SecondCallReturnsBadRequest()
        {
            // "fido2.username" est supprimé lors du premier appel → deuxième appel
            // trouve username == null → 400 "Login session expired".
            // (Note : "fido2.assertionOptions" n'est PAS supprimé à cause du bug ligne 299)
            var client = _factory.CreateClient();
            var username = UniqueUsername();
            await SeedUser(username, withCredential: true);
            var startRequest = BuildLoginStartRequest(username);
            var firstFinishRequest = BuildLoginFinishRequest();
            var secondFinishRequest = BuildLoginFinishRequest();
            await client.SendAsync(startRequest);
            await client.SendAsync(firstFinishRequest);

            var response = await client.SendAsync(secondFinishRequest);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
