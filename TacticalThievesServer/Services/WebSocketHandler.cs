using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;

namespace TacticalThievesServer.Services
{
    public class WebSocketHandler
    {

        // Mapping clientId -> WebSocket
        private readonly ConcurrentDictionary<string, WebSocket> clients = new();

        public async Task HandleAsync(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                return;
            }

            //Récupération de l'identifiant client depuis la query string
            var clientId = context.Request.Query["clientId"].ToString();

            if (string.IsNullOrEmpty(clientId))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("clientId is required");
                return;
            }

            var socket = await context.WebSockets.AcceptWebSocketAsync();

            // Enregistrement du client
            clients[clientId] = socket;

            var buffer = new byte[1024 * 4];

            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        CancellationToken.None
                    );

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }

                    var msg = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    // Ici on peut parser ton message JSON si besoin
                    Console.WriteLine($"[{clientId}] {msg}");

                    // Exemple : echo
                    var response = Encoding.UTF8.GetBytes($"Server received from {clientId}: {msg}");

                    await socket.SendAsync(
                        new ArraySegment<byte>(response),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None
                    );
                }
            }
             catch (Exception ex)
            {
                Console.WriteLine($"Error with client {clientId}: {ex.Message}");
            }
            finally
            {
                // Nettoyage à la déconnexion
                clients.TryRemove(clientId, out _);

                if (socket.State != WebSocketState.Closed)
                {
                    await socket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Connection closed",
                        CancellationToken.None
                    );
                }

                socket.Dispose();
                Console.WriteLine($"Client disconnected: {clientId}");
            }
        }

        //Envoi ciblé
        public async Task<bool> SendToClient(string? clientId, string message)
        {
            bool bSuccess = false;
            if (clientId != null && clients.TryGetValue(clientId, out var socket) && socket.State == WebSocketState.Open)
            {
                var data = Encoding.UTF8.GetBytes(message);

                await socket.SendAsync(
                    new ArraySegment<byte>(data),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );
                bSuccess = true;
            }

            return bSuccess;
        }


        public async Task Broadcast(string message)
        {
            var data = Encoding.UTF8.GetBytes(message);

            var tasks = clients
                .Where(c => c.Value.State == WebSocketState.Open)
                .Select(c => c.Value.SendAsync(
                    new ArraySegment<byte>(data),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                ));

            await Task.WhenAll(tasks);
        }

        public async Task DisconnectClient(string clientId)
        {
            if (clients.TryRemove(clientId, out var socket))
            {
                if (socket.State == WebSocketState.Open)
                {
                    await socket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Disconnected by server",
                        CancellationToken.None
                    );
                }

                socket.Dispose();
            }
        }

        public int GetConnectedClientsCount()
        {
            return clients.Count;
        }
    }
}
