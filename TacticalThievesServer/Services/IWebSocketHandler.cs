using Microsoft.AspNetCore.Http;

namespace TacticalThievesServer.Services
{
    public interface IWebSocketHandler
    {
        Task HandleAsync(HttpContext context);
        Task<bool> SendToClient(string? clientId, string message);
        Task Broadcast(string message);
        Task DisconnectClient(string clientId);
        int GetConnectedClientsCount();
    }
}
