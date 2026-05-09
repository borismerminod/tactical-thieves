using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace TacticalThievesServer.Services
{
    public class ClientHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string> bindings = new();
        public async Task SendPlayerGoldUpdate(int playerGold)
        {
            await Clients.All.SendAsync("ReceivePlayerGoldUpdate", playerGold);
        }

        public async Task Register(string clientId)
        {
            // On associe le client à un groupe
            await Groups.AddToGroupAsync(Context.ConnectionId, clientId);
        }

        public async Task ClaimUnity(string unityId)
        {
            var connectionId = Context.ConnectionId;

            // Vérifie si déjà pris
            if (bindings.ContainsKey(unityId))
            {
                await Clients.Caller.SendAsync("UnityAlreadyTaken");
                return;
            }

            //Association
            bindings[unityId] = connectionId;

            Console.WriteLine($"Unity {unityId} claim par {connectionId}");

            //confirmation au client Angular
            await Clients.Caller.SendAsync("UnityAssigned", unityId);
        }
    }
}
