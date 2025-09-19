using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;

namespace TacticalThievesServer.Services
{
    public class WebSocketHandler
    {
        private readonly ConcurrentBag<WebSocket> _sockets = new();

        public async Task HandleAsync(HttpContext context)
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
        }

        public async void Broadcast(string message)
        {
            var data = Encoding.UTF8.GetBytes(message);
            foreach (var socket in _sockets.Where(s => s.State == WebSocketState.Open))
            {
                await socket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }
}
