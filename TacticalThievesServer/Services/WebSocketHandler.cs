using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;

namespace TacticalThievesServer.Services
{
    public class WebSocketHandler
    {
        //Obsolète ? 
        //private readonly ConcurrentBag<WebSocket> _sockets = new();

        // Mapping clientId -> WebSocket
        private readonly ConcurrentDictionary<string, WebSocket> _clients = new();

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
            _clients[clientId] = socket;

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
                _clients.TryRemove(clientId, out _);

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

        /*public async Task HandleAsync(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                using var socket = await context.WebSockets.AcceptWebSocketAsync();
                _sockets.Add(socket);

                var buffer = new byte[1024 * 4];

                while (socket.State == WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by server", CancellationToken.None);
                    }
                    else
                    {
                        var msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        var response = Encoding.UTF8.GetBytes("Server received: " + msg);

                        await socket.SendAsync(new ArraySegment<byte>(response), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
            }
            else
            {
                context.Response.StatusCode = 400; // mauvaise requête si ce n’est pas un WS
            }
        }*/

        //Envoi ciblé
        public async Task SendToClient(string clientId, string message)
        {
            if (_clients.TryGetValue(clientId, out var socket) && socket.State == WebSocketState.Open)
            {
                var data = Encoding.UTF8.GetBytes(message);

                await socket.SendAsync(
                    new ArraySegment<byte>(data),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );
            }
        }

        /*public async void Broadcast(string message)
        {
            var data = Encoding.UTF8.GetBytes(message);
            foreach (var socket in _sockets.Where(s => s.State == WebSocketState.Open))
            {
                await socket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }*/

        public async Task Broadcast(string message)
        {
            var data = Encoding.UTF8.GetBytes(message);

            var tasks = _clients
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
            if (_clients.TryRemove(clientId, out var socket))
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
            return _clients.Count;
        }
    }
}
