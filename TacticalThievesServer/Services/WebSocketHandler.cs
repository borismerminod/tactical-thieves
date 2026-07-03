using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace TacticalThievesServer.Services
{
    /// <summary>
    /// Simple WebSocket handler (between ASP.NET and Unity Game) that accepts WebSocket connections, keeps an in-memory
    /// mapping of client identifiers to WebSocket instances and provides helpers to
    /// send messages to a single client or broadcast to all connected clients.
    /// </summary>
    public class WebSocketHandler : IWebSocketHandler
    {

        /// <summary>
        /// Mapping from client id to the associated <see cref="WebSocket"/> instance.
        /// </summary>
        private readonly ConcurrentDictionary<string, WebSocket> clients = new();

        /// <summary>
        /// Handles an incoming WebSocket upgrade request and processes messages for the life
        /// of the connection. The query string must contain a `clientId` used to identify the connection.
        /// </summary>
        /// <param name="context">The current HTTP context used to accept the WebSocket.</param>
        public async Task HandleAsync(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                return;
            }

            //Get clientId from query string
            var clientId = context.Request.Query["clientId"].ToString();

            if (string.IsNullOrEmpty(clientId))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("clientId is required");
                return;
            }

            var socket = await context.WebSockets.AcceptWebSocketAsync();

            //Save the new connection in the clients map
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

                    Console.WriteLine($"[{clientId}] {msg}");

                    // Example : echo
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
                // Cleanup on disconnect
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

        /// <summary>
        /// Sends a text message to a specific connected client identified by <paramref name="clientId"/>.
        /// </summary>
        /// <param name="clientId">The target client identifier.</param>
        /// <param name="message">The text message to send.</param>
        /// <returns>True if the message was sent; otherwise false.</returns>
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


        /// <summary>
        /// Broadcasts a text message to all connected clients that are in the open state.
        /// </summary>
        /// <param name="message">The message to broadcast.</param>
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

        /// <summary>
        /// Gracefully disconnects a specific client and removes it from the internal map.
        /// </summary>
        /// <param name="clientId">The client identifier to disconnect.</param>
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

        /// <summary>
        /// Returns the number of currently connected clients.
        /// </summary>
        public int GetConnectedClientsCount()
        {
            return clients.Count;
        }
    }
}
