using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using TacticalThievesServer.Data;
using TacticalThievesServer.Models;
using TacticalThievesServer.Services;
using TacticalThievesServer.Test.Fakes;
using Xunit;

namespace TacticalThievesServer.Test
{
    public class GameControllerTest : IClassFixture<GameControllerTestFactory>
    {
        private readonly HttpClient _client;
        private readonly FakeWebSocketHandler _fakeHandler;
        private readonly FakeHubContext _fakeHub;
        private readonly WebSocketLinkerService _linker;
        private readonly GameControllerTestFactory _factory;

        public GameControllerTest(GameControllerTestFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
            _fakeHandler = factory.FakeWebSocketHandler;
            _fakeHub = factory.FakeHubContext;
            _linker = factory.Services.GetRequiredService<WebSocketLinkerService>();
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        private HttpRequestMessage BuildRequest(string endpoint, string? sessionId)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(new { })
            };
            if (sessionId != null)
                request.Headers.Add("X-Session-Id", sessionId);
            return request;
        }

        private HttpRequestMessage BuildLoadRandomLevelRequest(string? connectionId, string? sessionId)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/Game/load-random-level")
            {
                Content = JsonContent.Create(new { })
            };
            if (connectionId != null)
                request.Headers.Add("X-Connection-Id", connectionId);
            if (sessionId != null)
                request.Headers.Add("X-Session-Id", sessionId);
            return request;
        }

        private HttpRequestMessage BuildGameStartRequest(object body)
        {
            return new HttpRequestMessage(HttpMethod.Post, "/api/Game/game-start")
            {
                Content = JsonContent.Create(body)
            };
        }

        private HttpRequestMessage BuildSaveLevelRequest(string? token, object body)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/Game/save-level")
            {
                Content = JsonContent.Create(body)
            };
            if (token != null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return request;
        }

        private async Task<User> SeedUserAsync(string username, uint? currentLevel = null)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var user = new User { Id = Guid.NewGuid(), Username = username };
            db.Users.Add(user);

            if (currentLevel.HasValue)
            {
                db.PlayerProgresses.Add(new PlayerProgress
                {
                    UserId = user.Id,
                    CurrentLevel = currentLevel.Value
                });
            }

            await db.SaveChangesAsync();
            return user;
        }

        private HttpRequestMessage BuildLoadLevelRequest(string? token, string? connectionId, string? sessionId)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/Game/load-level")
            {
                Content = JsonContent.Create(new { })
            };
            if (token != null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (connectionId != null)
                request.Headers.Add("X-Connection-Id", connectionId);
            if (sessionId != null)
                request.Headers.Add("X-Session-Id", sessionId);
            return request;
        }

        // ── HandleClientEventAsync (CollectTreasure / ExitReached) ──────────────

        private HttpRequestMessage BuildSignalRRequest<T>(string endpoint, string? sessionId, T body)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(body)
            };
            if (sessionId != null)
                request.Headers.Add("X-Session-Id", sessionId);
            return request;
        }

        // body valide pour les deux endpoints (les champs supplémentaires sont ignorés)
        private static object ValidSignalRBody() => new { Amount = 5, CurrentLevel = 2, Pseudo = "test" };

        // ── SendGameMessageAsync — cas : header X-Session-Id absent → 400 ──────

        [Theory]
        [InlineData("/api/Game/move")]
        [InlineData("/api/Game/stealth")]
        [InlineData("/api/Game/end-turn")]
        [InlineData("/api/Game/restart")]
        public async Task SendGameMessage_MissingSessionId_ReturnsBadRequest(string endpoint)
        {
            var request = BuildRequest(endpoint, sessionId: null);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // ── SendGameMessageAsync — cas : sessionId inconnu du linker → 404 ─────

        [Theory]
        [InlineData("/api/Game/move")]
        [InlineData("/api/Game/stealth")]
        [InlineData("/api/Game/end-turn")]
        [InlineData("/api/Game/restart")]
        public async Task SendGameMessage_SessionIdNotFound_ReturnsNotFound(string endpoint)
        {
            var request = BuildRequest(endpoint, sessionId: "unknown-" + Guid.NewGuid());
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // ── SendGameMessageAsync — cas : link trouvé mais UnityGUID absent → 500 ─

        [Theory]
        [InlineData("/api/Game/move")]
        [InlineData("/api/Game/stealth")]
        [InlineData("/api/Game/end-turn")]
        [InlineData("/api/Game/restart")]
        public async Task SendGameMessage_LinkFoundButNoUnityGuid_ReturnsInternalServerError(string endpoint)
        {
            var sessionId = Guid.NewGuid().ToString();
            _linker.AddOrUpdate(sessionId, angularGuid: "angular-only-no-unity");
            _fakeHandler.SendToClientResult = false;

            var request = BuildRequest(endpoint, sessionId);
            var response = await _client.SendAsync(request);

            _fakeHandler.SendToClientResult = true;
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        // ── SendGameMessageAsync — cas : WebSocket indisponible → 500 ───────────

        [Theory]
        [InlineData("/api/Game/move")]
        [InlineData("/api/Game/stealth")]
        [InlineData("/api/Game/end-turn")]
        [InlineData("/api/Game/restart")]
        public async Task SendGameMessage_SendToClientFails_ReturnsInternalServerError(string endpoint)
        {
            var sessionId = Guid.NewGuid().ToString();
            _linker.AddOrUpdate(sessionId, unityGuid: "unity-guid-disconnected");
            _fakeHandler.SendToClientResult = false;

            var request = BuildRequest(endpoint, sessionId);
            var response = await _client.SendAsync(request);

            _fakeHandler.SendToClientResult = true;
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        // ── SendGameMessageAsync — cas nominal → 200 { success: true } ──────────

        [Theory]
        [InlineData("/api/Game/move")]
        [InlineData("/api/Game/stealth")]
        [InlineData("/api/Game/end-turn")]
        [InlineData("/api/Game/restart")]
        public async Task SendGameMessage_ValidRequest_ReturnsSuccess(string endpoint)
        {
            var sessionId = Guid.NewGuid().ToString();
            _linker.AddOrUpdate(sessionId, unityGuid: "unity-guid-ok");
            _fakeHandler.SendToClientResult = true;

            var request = BuildRequest(endpoint, sessionId);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<GameControllerResponse>();
            Assert.NotNull(result);
            Assert.True(result.Success);
        }

        // ── SendGameMessageAsync — vérification du message envoyé ─────────────

        [Theory]
        [InlineData("/api/Game/move",     "move")]
        [InlineData("/api/Game/stealth",  "stealth")]
        [InlineData("/api/Game/end-turn", "end-turn")]
        [InlineData("/api/Game/restart",  "restart")]
        public async Task SendGameMessage_ValidRequest_SendsCorrectMessageType(string endpoint, string expectedType)
        {
            var sessionId = Guid.NewGuid().ToString();
            var unityGuid = "unity-" + Guid.NewGuid();
            _linker.AddOrUpdate(sessionId, unityGuid: unityGuid);
            _fakeHandler.SendToClientResult = true;

            var request = BuildRequest(endpoint, sessionId);
            await _client.SendAsync(request);

            Assert.Equal(unityGuid, _fakeHandler.LastClientId);
            Assert.Contains($"\"Type\":\"{expectedType}\"", _fakeHandler.LastMessage, StringComparison.OrdinalIgnoreCase);
        }

        // ── LoadRandomLevel — cas : X-Connection-Id absent → 400 ────────────────

        [Fact]
        public async Task LoadRandomLevel_MissingConnectionId_ReturnsBadRequest()
        {
            var request = BuildLoadRandomLevelRequest(connectionId: null, sessionId: Guid.NewGuid().ToString());
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // ── LoadRandomLevel — cas : X-Session-Id absent → 400 ───────────────────

        [Fact]
        public async Task LoadRandomLevel_MissingSessionId_ReturnsBadRequest()
        {
            var request = BuildLoadRandomLevelRequest(connectionId: "angular-conn-id", sessionId: null);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // ── LoadRandomLevel — cas : link trouvé mais UnityGUID absent → 500 ─────

        [Fact]
        public async Task LoadRandomLevel_LinkFoundButNoUnityGuid_ReturnsInternalServerError()
        {
            var sessionId = Guid.NewGuid().ToString();
            _fakeHandler.SendToClientResult = false;

            var request = BuildLoadRandomLevelRequest(connectionId: "angular-only", sessionId: sessionId);
            var response = await _client.SendAsync(request);

            _fakeHandler.SendToClientResult = true;
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        // ── LoadRandomLevel — cas : Unity déconnecté, SendToClient échoue → 500 ─

        [Fact]
        public async Task LoadRandomLevel_SendToClientFails_ReturnsInternalServerError()
        {
            var sessionId = Guid.NewGuid().ToString();
            _linker.AddOrUpdate(sessionId, unityGuid: "unity-disconnected");
            _fakeHandler.SendToClientResult = false;

            var request = BuildLoadRandomLevelRequest(connectionId: "angular-conn-id", sessionId: sessionId);
            var response = await _client.SendAsync(request);

            _fakeHandler.SendToClientResult = true;
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        // ── LoadRandomLevel — cas nominal → 200 { success: true } ───────────────

        [Fact]
        public async Task LoadRandomLevel_ValidRequest_ReturnsSuccess()
        {
            var sessionId = Guid.NewGuid().ToString();
            _linker.AddOrUpdate(sessionId, unityGuid: "unity-ok");
            _fakeHandler.SendToClientResult = true;

            var request = BuildLoadRandomLevelRequest(connectionId: "angular-conn-id", sessionId: sessionId);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<GameControllerResponse>();
            Assert.NotNull(result);
            Assert.True(result.Success);
        }

        // ── LoadRandomLevel — vérification du message et de l'angularGuid enregistré

        [Fact]
        public async Task LoadRandomLevel_ValidRequest_SendsLoadRandomLevelMessageType()
        {
            var sessionId = Guid.NewGuid().ToString();
            var unityGuid = "unity-" + Guid.NewGuid();
            _linker.AddOrUpdate(sessionId, unityGuid: unityGuid);
            _fakeHandler.SendToClientResult = true;

            var request = BuildLoadRandomLevelRequest(connectionId: "angular-conn-id", sessionId: sessionId);
            await _client.SendAsync(request);

            Assert.Equal(unityGuid, _fakeHandler.LastClientId);
            Assert.Contains("\"Type\":\"load-random-level\"", _fakeHandler.LastMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task LoadRandomLevel_ValidRequest_RegistersAngularGuidInLinker()
        {
            var sessionId = Guid.NewGuid().ToString();
            var connectionId = "angular-" + Guid.NewGuid();
            _linker.AddOrUpdate(sessionId, unityGuid: "unity-ok");
            _fakeHandler.SendToClientResult = true;

            var request = BuildLoadRandomLevelRequest(connectionId: connectionId, sessionId: sessionId);
            await _client.SendAsync(request);

            _linker.TryGet(sessionId, out var link);
            Assert.Equal(connectionId, link?.AngularGUID);
        }

        // cas : X-Session-Id absent → 400

        [Theory]
        [InlineData("/api/Game/collect-treasure")]
        [InlineData("/api/Game/exit-reached")]
        public async Task HandleClientEvent_MissingSessionId_ReturnsBadRequest(string endpoint)
        {
            var request = BuildSignalRRequest(endpoint, sessionId: null, ValidSignalRBody());
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // cas : sessionId inconnu → 404

        [Theory]
        [InlineData("/api/Game/collect-treasure")]
        [InlineData("/api/Game/exit-reached")]
        public async Task HandleClientEvent_SessionIdNotFound_ReturnsNotFound(string endpoint)
        {
            var request = BuildSignalRRequest(endpoint, sessionId: "unknown-" + Guid.NewGuid(), ValidSignalRBody());
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // cas : lien sans AngularGUID → 404

        [Theory]
        [InlineData("/api/Game/collect-treasure")]
        [InlineData("/api/Game/exit-reached")]
        public async Task HandleClientEvent_LinkFoundButNoAngularGuid_ReturnsNotFound(string endpoint)
        {
            var sessionId = Guid.NewGuid().ToString();
            _linker.AddOrUpdate(sessionId, unityGuid: "unity-only-no-angular");

            var request = BuildSignalRRequest(endpoint, sessionId, ValidSignalRBody());
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // cas nominal → 200

        [Theory]
        [InlineData("/api/Game/collect-treasure")]
        [InlineData("/api/Game/exit-reached")]
        public async Task HandleClientEvent_ValidRequest_ReturnsSuccess(string endpoint)
        {
            var sessionId = Guid.NewGuid().ToString();
            _linker.AddOrUpdate(sessionId, angularGuid: "angular-ok");

            var request = BuildSignalRRequest(endpoint, sessionId, ValidSignalRBody());
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<GameControllerResponse>();
            Assert.NotNull(result);
            Assert.True(result.Success);
        }

        // vérification de l'événement SignalR envoyé

        [Theory]
        [InlineData("/api/Game/collect-treasure", "ScoreUpdated")]
        [InlineData("/api/Game/exit-reached",     "ExitReached")]
        public async Task HandleClientEvent_ValidRequest_SendsCorrectSignalREvent(string endpoint, string expectedEvent)
        {
            var sessionId = Guid.NewGuid().ToString();
            var angularGuid = "angular-" + Guid.NewGuid();
            _linker.AddOrUpdate(sessionId, angularGuid: angularGuid);

            var request = BuildSignalRRequest(endpoint, sessionId, ValidSignalRBody());
            await _client.SendAsync(request);

            Assert.Equal(angularGuid, _fakeHub.LastClientId);
            Assert.Equal(expectedEvent, _fakeHub.LastEventName);
        }

        // ── ThievesDied ──────────────────────────────────────────────────────────

        [Fact]
        public async Task ThievesDied_MissingSessionId_ReturnsBadRequest()
        {
            var request = BuildRequest("/api/Game/thieves-died", sessionId: null);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ThievesDied_SessionIdNotFound_ReturnsNotFound()
        {
            var request = BuildRequest("/api/Game/thieves-died", sessionId: "unknown-" + Guid.NewGuid());
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ThievesDied_LinkFoundButNoAngularGuid_ReturnsNotFound()
        {
            var sessionId = Guid.NewGuid().ToString();
            _linker.AddOrUpdate(sessionId, unityGuid: "unity-only-no-angular");

            var request = BuildRequest("/api/Game/thieves-died", sessionId);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ThievesDied_ValidRequest_ReturnsSuccessAndSendsCorrectSignalREvent()
        {
            var sessionId = Guid.NewGuid().ToString();
            var angularGuid = "angular-" + Guid.NewGuid();
            _linker.AddOrUpdate(sessionId, angularGuid: angularGuid);

            var request = BuildRequest("/api/Game/thieves-died", sessionId);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<GameControllerResponse>();
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(angularGuid, _fakeHub.LastClientId);
            Assert.Equal("ThievesDied", _fakeHub.LastEventName);
        }

        // ── GameStart ────────────────────────────────────────────────────────────

        

        [Fact]
        public async Task GameStart_NullDto_ReturnsBadRequest()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/Game/game-start")
            {
                Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json")
            };
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GameStart_MissingSessionId_ReturnsBadRequest()
        {
            var request = BuildGameStartRequest(new { UnityGUID = "unity-guid" });
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GameStart_MissingUnityGuid_ReturnsBadRequest()
        {
            var request = BuildGameStartRequest(new { SessionID = Guid.NewGuid().ToString() });
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GameStart_ValidRequest_ReturnsSuccess()
        {
            var sessionId = Guid.NewGuid().ToString();
            var request = BuildGameStartRequest(new { SessionID = sessionId, UnityGUID = "unity-guid-ok" });
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<GameControllerResponse>();
            Assert.NotNull(result);
            Assert.True(result.Success);
        }

        [Fact]
        public async Task GameStart_ValidRequest_RegistersUnityGuidInLinker()
        {
            var sessionId = Guid.NewGuid().ToString();
            var unityGuid = "unity-" + Guid.NewGuid();
            var request = BuildGameStartRequest(new { SessionID = sessionId, UnityGUID = unityGuid });
            await _client.SendAsync(request);

            _linker.TryGet(sessionId, out var link);
            Assert.Equal(unityGuid, link?.UnityGUID);
        }

        [Fact]
        public async Task GameStart_ValidRequest_BroadcastsGameStartEvent()
        {
            var sessionId = Guid.NewGuid().ToString();
            var request = BuildGameStartRequest(new { SessionID = sessionId, UnityGUID = "unity-guid-ok" });
            await _client.SendAsync(request);

            Assert.Equal("GameStart", _fakeHub.LastBroadcastEventName);
            Assert.Equal(sessionId, _fakeHub.LastBroadcastPayload?.ToString());
        }

        // ── SaveLevel ────────────────────────────────────────────────────────────

        [Fact]
        public async Task SaveLevel_NoJwt_ReturnsUnauthorized()
        {
            var request = BuildSaveLevelRequest(token: null, new { Pseudo = "test", CurrentLevel = 1 });
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task SaveLevel_ExpiredJwt_ReturnsUnauthorized()
        {
            var token = JwtTestHelper.GenerateToken("alice", expired: true);
            var request = BuildSaveLevelRequest(token, new { Pseudo = "alice", CurrentLevel = 1 });
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task SaveLevel_UserNotFound_ReturnsNotFound()
        {
            var token = JwtTestHelper.GenerateToken("unknown-user");
            var request = BuildSaveLevelRequest(token, new { Pseudo = "unknown-user", CurrentLevel = 1 });
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task SaveLevel_UserWithoutProgress_CreatesProgressAndReturnsSuccess()
        {
            await SeedUserAsync("newplayer");
            var token = JwtTestHelper.GenerateToken("newplayer");
            var request = BuildSaveLevelRequest(token, new { Pseudo = "newplayer", CurrentLevel = 3u });
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<SaveLevelResponse>();
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(3u, result.Level);
        }

        [Fact]
        public async Task SaveLevel_UserWithExistingProgress_UpdatesProgressAndReturnsSuccess()
        {
            await SeedUserAsync("veteran", currentLevel: 2);
            var token = JwtTestHelper.GenerateToken("veteran");
            var request = BuildSaveLevelRequest(token, new { Pseudo = "veteran", CurrentLevel = 5u });
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<SaveLevelResponse>();
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(5u, result.Level);
        }

        // ── LoadLevel ────────────────────────────────────────────────────────────


        [Fact]
        public async Task LoadLevel_NoJwt_ReturnsUnauthorized()
        {
            var request = BuildLoadLevelRequest(token: null, connectionId: "conn", sessionId: Guid.NewGuid().ToString());
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task LoadLevel_ExpiredJwt_ReturnsUnauthorized()
        {
            var token = JwtTestHelper.GenerateToken("alice", expired: true);
            var request = BuildLoadLevelRequest(token, connectionId: "conn", sessionId: Guid.NewGuid().ToString());
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task LoadLevel_PlayerNotFound_ReturnsNotFound()
        {
            var token = JwtTestHelper.GenerateToken("ghost");
            var request = BuildLoadLevelRequest(token, connectionId: "conn", sessionId: Guid.NewGuid().ToString());
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task LoadLevel_MissingConnectionId_ReturnsBadRequest()
        {
            await SeedUserAsync("loader", currentLevel: 1);
            var token = JwtTestHelper.GenerateToken("loader");
            var request = BuildLoadLevelRequest(token, connectionId: null, sessionId: Guid.NewGuid().ToString());
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task LoadLevel_MissingSessionId_ReturnsBadRequest()
        {
            await SeedUserAsync("loader2", currentLevel: 1);
            var token = JwtTestHelper.GenerateToken("loader2");
            var request = BuildLoadLevelRequest(token, connectionId: "conn", sessionId: null);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task LoadLevel_SendToClientFails_ReturnsInternalServerError()
        {
            await SeedUserAsync("loader3", currentLevel: 2);
            var token = JwtTestHelper.GenerateToken("loader3");
            _fakeHandler.SendToClientResult = false;

            var request = BuildLoadLevelRequest(token, connectionId: "conn", sessionId: Guid.NewGuid().ToString());
            var response = await _client.SendAsync(request);

            _fakeHandler.SendToClientResult = true;
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task LoadLevel_ValidRequest_ReturnsSuccessWithCorrectLevel()
        {
            await SeedUserAsync("loader4", currentLevel: 7);
            var token = JwtTestHelper.GenerateToken("loader4");
            var sessionId = Guid.NewGuid().ToString();
            _linker.AddOrUpdate(sessionId, unityGuid: "unity-ok");
            _fakeHandler.SendToClientResult = true;

            var request = BuildLoadLevelRequest(token, connectionId: "conn-angular", sessionId: sessionId);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<LoadLevelResponse>();
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(7u, result.Level);
        }

        [Fact]
        public async Task LoadLevel_ValidRequest_SendsCorrectWebSocketMessage()
        {
            await SeedUserAsync("loader5", currentLevel: 4);
            var token = JwtTestHelper.GenerateToken("loader5");
            var sessionId = Guid.NewGuid().ToString();
            var unityGuid = "unity-" + Guid.NewGuid();
            _linker.AddOrUpdate(sessionId, unityGuid: unityGuid);
            _fakeHandler.SendToClientResult = true;

            var request = BuildLoadLevelRequest(token, connectionId: "conn-angular", sessionId: sessionId);
            await _client.SendAsync(request);

            Assert.Equal(unityGuid, _fakeHandler.LastClientId);
            Assert.Contains("\"Type\":\"load-level\"", _fakeHandler.LastMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task LoadLevel_ValidRequest_RegistersAngularGuidInLinker()
        {
            await SeedUserAsync("loader6", currentLevel: 1);
            var token = JwtTestHelper.GenerateToken("loader6");
            var sessionId = Guid.NewGuid().ToString();
            var connectionId = "angular-" + Guid.NewGuid();
            _linker.AddOrUpdate(sessionId, unityGuid: "unity-ok");
            _fakeHandler.SendToClientResult = true;

            var request = BuildLoadLevelRequest(token, connectionId: connectionId, sessionId: sessionId);
            await _client.SendAsync(request);

            _linker.TryGet(sessionId, out var link);
            Assert.Equal(connectionId, link?.AngularGUID);
        }
    }

    public class GameControllerResponse
    {
        public bool Success { get; set; }
    }

    public class SaveLevelResponse
    {
        public bool Success { get; set; }
        public string? Pseudo { get; set; }
        public uint Level { get; set; }
    }

    public class LoadLevelResponse
    {
        public bool Success { get; set; }
        public uint Level { get; set; }
    }
}
