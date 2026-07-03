using Microsoft.AspNetCore.Http;
using TacticalThievesServer.Services;

namespace TacticalThievesServer.Test.Fakes
{
    public class FakeWebSocketHandler : IWebSocketHandler
    {
        public bool SendToClientResult { get; set; } = true;
        public string? LastClientId { get; private set; }
        public string? LastMessage { get; private set; }

        public Task HandleAsync(HttpContext context) => Task.CompletedTask;

        public Task<bool> SendToClient(string? clientId, string message)
        {
            LastClientId = clientId;
            LastMessage = message;
            return Task.FromResult(SendToClientResult);
        }

        public Task Broadcast(string message) => Task.CompletedTask;
        public Task DisconnectClient(string clientId) => Task.CompletedTask;
        public int GetConnectedClientsCount() => 0;
    }
}
