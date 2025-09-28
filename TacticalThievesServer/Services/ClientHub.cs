using Microsoft.AspNetCore.SignalR;

namespace TacticalThievesServer.Services
{
    public class ClientHub : Hub
    {
        public async Task SendPlayerGoldUpdate(int playerGold)
        {
            await Clients.All.SendAsync("ReceivePlayerGoldUpdate", playerGold);
        }
    }
}
