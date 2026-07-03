using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using TacticalThievesServer.Services;
using Xunit;

namespace TacticalThievesServer.Test
{
    public class WebSocketServiceTest
    {
        private static TestServer BuildTestServer(out IWebSocketHandler handler)
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<WebSocketHandler>();
                    services.AddSingleton<IWebSocketHandler>(sp => sp.GetRequiredService<WebSocketHandler>());
                })
                .Configure(app =>
                {
                    app.UseWebSockets();
                    app.Map("/ws", wsApp =>
                    {
                        wsApp.Run(async context =>
                        {
                            var wsHandler = context.RequestServices.GetRequiredService<IWebSocketHandler>();
                            await wsHandler.HandleAsync(context);
                        });
                    });
                });

            var testServer = new TestServer(builder);
            handler = testServer.Services.GetRequiredService<IWebSocketHandler>();
            return testServer;
        }

        private static async Task SendTextAsync(WebSocket socket, string message)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            var segment = new ArraySegment<byte>(bytes);
            await socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private static async Task<string> ReceiveTextAsync(WebSocket socket, int bufferSize = 8192)
        {
            var buffer = new byte[bufferSize];
            var segment = new ArraySegment<byte>(buffer);
            var result = await socket.ReceiveAsync(segment, CancellationToken.None);
            var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
            return text;
        }

        private static async Task<int> WaitForClientCountAsync(IWebSocketHandler handler, int expected, int timeoutMs = 2000)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var count = handler.GetConnectedClientsCount();

            while (count != expected && stopwatch.ElapsedMilliseconds < timeoutMs)
            {
                await Task.Delay(20);
                count = handler.GetConnectedClientsCount();
            }

            return count;
        }

        [Fact]
        public async Task HandleAsync_NotWebSocketRequest_Returns400()
        {
            using var testServer = BuildTestServer(out _);
            using var httpClient = testServer.CreateClient();

            var response = await httpClient.GetAsync("/ws");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task HandleAsync_MissingClientId_Returns400()
        {
            using var testServer = BuildTestServer(out _);
            var wsClient = testServer.CreateWebSocketClient();
            var uri = new Uri("ws://localhost/ws");

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => wsClient.ConnectAsync(uri, CancellationToken.None));
        }

        [Fact]
        public async Task HandleAsync_EmptyClientId_Returns400()
        {
            using var testServer = BuildTestServer(out _);
            var wsClient = testServer.CreateWebSocketClient();
            var uri = new Uri("ws://localhost/ws?clientId=");

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => wsClient.ConnectAsync(uri, CancellationToken.None));
        }


        [Fact]
        public async Task HandleAsync_EmptyMessage_EchoedBack()
        {
            using var testServer = BuildTestServer(out _);
            var wsClient = testServer.CreateWebSocketClient();
            var clientId = "client-" + Guid.NewGuid();
            var uri = new Uri($"ws://localhost/ws?clientId={clientId}");

            using var socket = await wsClient.ConnectAsync(uri, CancellationToken.None);

            await SendTextAsync(socket, string.Empty);
            var recvMessage = await ReceiveTextAsync(socket);

            var expected = $"Server received from {clientId}: ";
            Assert.Equal(expected, recvMessage);
        }

        [Fact]
        public async Task HandleAsync_MultipleMessages_EchoedInOrder()
        {
            using var testServer = BuildTestServer(out _);
            var wsClient = testServer.CreateWebSocketClient();
            var clientId = "client-" + Guid.NewGuid();
            var uri = new Uri($"ws://localhost/ws?clientId={clientId}");

            using var socket = await wsClient.ConnectAsync(uri, CancellationToken.None);

            var messages = new[] { "first", "second", "third" };

            foreach (var message in messages)
            {
                await SendTextAsync(socket, message);
                var recvMessage = await ReceiveTextAsync(socket);
                var expected = $"Server received from {clientId}: {message}";
                Assert.Equal(expected, recvMessage);
            }
        }

        [Fact]
        public async Task HandleAsync_MessageExceedsBufferSize_TruncatesAt4KB()
        {
            using var testServer = BuildTestServer(out _);
            var wsClient = testServer.CreateWebSocketClient();
            var clientId = "client-" + Guid.NewGuid();
            var uri = new Uri($"ws://localhost/ws?clientId={clientId}");

            using var socket = await wsClient.ConnectAsync(uri, CancellationToken.None);

            const int bufferSize = 1024 * 4;
            var largeMessage = new string('A', bufferSize + 500);

            await SendTextAsync(socket, largeMessage);
            var recvMessage = await ReceiveTextAsync(socket, bufferSize + 200);

            var truncatedPortion = largeMessage.Substring(0, bufferSize);
            var expected = $"Server received from {clientId}: {truncatedPortion}";
            Assert.Equal(expected, recvMessage);
        }

        [Fact]
        public async Task HandleAsync_ClientSendsCloseFrame_ClosesGracefully()
        {
            using var testServer = BuildTestServer(out _);
            var wsClient = testServer.CreateWebSocketClient();
            var clientId = "client-" + Guid.NewGuid();
            var uri = new Uri($"ws://localhost/ws?clientId={clientId}");

            using var socket = await wsClient.ConnectAsync(uri, CancellationToken.None);

            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);

            Assert.Equal(WebSocketState.Closed, socket.State);
        }

        [Fact]
        public async Task HandleAsync_OnDisconnect_ClientRemovedFromMap()
        {
            using var testServer = BuildTestServer(out var handler);
            var wsClient = testServer.CreateWebSocketClient();
            var clientId = "client-" + Guid.NewGuid();
            var uri = new Uri($"ws://localhost/ws?clientId={clientId}");

            var socket = await wsClient.ConnectAsync(uri, CancellationToken.None);
            var countWhileConnected = await WaitForClientCountAsync(handler, 1);
            Assert.Equal(1, countWhileConnected);

            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
            socket.Dispose();

            var countAfterDisconnect = await WaitForClientCountAsync(handler, 0);
            Assert.Equal(0, countAfterDisconnect);
        }

        [Fact]
        public async Task HandleAsync_WhileConnected_ClientCountIncremented()
        {
            using var testServer = BuildTestServer(out var handler);
            var wsClient = testServer.CreateWebSocketClient();
            var uri1 = new Uri($"ws://localhost/ws?clientId=client-{Guid.NewGuid()}");
            var uri2 = new Uri($"ws://localhost/ws?clientId=client-{Guid.NewGuid()}");

            using var socket1 = await wsClient.ConnectAsync(uri1, CancellationToken.None);
            var countAfterFirst = await WaitForClientCountAsync(handler, 1);
            Assert.Equal(1, countAfterFirst);

            using var socket2 = await wsClient.ConnectAsync(uri2, CancellationToken.None);
            var countAfterSecond = await WaitForClientCountAsync(handler, 2);
            Assert.Equal(2, countAfterSecond);
        }

        [Fact]
        public async Task HandleAsync_DuplicateClientId_ReplacesOldConnection()
        {
            using var testServer = BuildTestServer(out var handler);
            var wsClient = testServer.CreateWebSocketClient();
            var clientId = "client-" + Guid.NewGuid();
            var uri = new Uri($"ws://localhost/ws?clientId={clientId}");

            using var firstSocket = await wsClient.ConnectAsync(uri, CancellationToken.None);
            var countAfterFirst = await WaitForClientCountAsync(handler, 1);
            Assert.Equal(1, countAfterFirst);

            using var secondSocket = await wsClient.ConnectAsync(uri, CancellationToken.None);
            var countAfterSecond = await WaitForClientCountAsync(handler, 1);
            Assert.Equal(1, countAfterSecond);
        }

        [Fact]
        public async Task HandleAsync_SpecialCharactersInClientId_HandledCorrectly()
        {
            using var testServer = BuildTestServer(out _);
            var wsClient = testServer.CreateWebSocketClient();
            var rawClientId = "client é@ #1";
            var encodedClientId = Uri.EscapeDataString(rawClientId);
            var uri = new Uri($"ws://localhost/ws?clientId={encodedClientId}");

            using var socket = await wsClient.ConnectAsync(uri, CancellationToken.None);

            var message = "ping";
            await SendTextAsync(socket, message);
            var recvMessage = await ReceiveTextAsync(socket);

            var expected = $"Server received from {rawClientId}: {message}";
            Assert.Equal(expected, recvMessage);
        }

        [Fact]
        public async Task SendToClient_ExistingOpenClient_SendsMessageAndReturnsTrue()
        {
            using var testServer = BuildTestServer(out var handler);
            var wsClient = testServer.CreateWebSocketClient();
            var clientId = "client-" + Guid.NewGuid();
            var uri = new Uri($"ws://localhost/ws?clientId={clientId}");

            using var socket = await wsClient.ConnectAsync(uri, CancellationToken.None);

            var message = "hello from server";
            var success = await handler.SendToClient(clientId, message);
            var recvMessage = await ReceiveTextAsync(socket);

            Assert.True(success);
            Assert.Equal(message, recvMessage);
        }

        [Fact]
        public async Task SendToClient_UnknownClientId_ReturnsFalse()
        {
            using var testServer = BuildTestServer(out var handler);

            var success = await handler.SendToClient("never-connected", "hello");

            Assert.False(success);
        }

        [Fact]
        public async Task SendToClient_NullClientId_ReturnsFalse()
        {
            using var testServer = BuildTestServer(out var handler);

            var success = await handler.SendToClient(null, "hello");

            Assert.False(success);
        }

        [Fact]
        public async Task Broadcast_MultipleOpenClients_AllReceiveMessage()
        {
            using var testServer = BuildTestServer(out var handler);
            var wsClient = testServer.CreateWebSocketClient();
            var uri1 = new Uri($"ws://localhost/ws?clientId=client-{Guid.NewGuid()}");
            var uri2 = new Uri($"ws://localhost/ws?clientId=client-{Guid.NewGuid()}");

            using var socket1 = await wsClient.ConnectAsync(uri1, CancellationToken.None);
            using var socket2 = await wsClient.ConnectAsync(uri2, CancellationToken.None);
            await WaitForClientCountAsync(handler, 2);

            var message = "broadcast message";
            await handler.Broadcast(message);

            var recvMessage1 = await ReceiveTextAsync(socket1);
            var recvMessage2 = await ReceiveTextAsync(socket2);

            Assert.Equal(message, recvMessage1);
            Assert.Equal(message, recvMessage2);
        }

        [Fact]
        public async Task Broadcast_NoConnectedClients_CompletesWithoutError()
        {
            using var testServer = BuildTestServer(out var handler);

            var exception = await Record.ExceptionAsync(() => handler.Broadcast("hello"));

            Assert.Null(exception);
        }

        [Fact]
        public async Task Broadcast_AfterClientDisconnects_OnlyRemainingClientReceivesMessage()
        {
            using var testServer = BuildTestServer(out var handler);
            var wsClient = testServer.CreateWebSocketClient();
            var clientId1 = "client-" + Guid.NewGuid();
            var clientId2 = "client-" + Guid.NewGuid();
            var uri1 = new Uri($"ws://localhost/ws?clientId={clientId1}");
            var uri2 = new Uri($"ws://localhost/ws?clientId={clientId2}");

            var socket1 = await wsClient.ConnectAsync(uri1, CancellationToken.None);
            using var socket2 = await wsClient.ConnectAsync(uri2, CancellationToken.None);
            await WaitForClientCountAsync(handler, 2);

            await socket1.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
            socket1.Dispose();
            var countAfterDisconnect = await WaitForClientCountAsync(handler, 1);
            Assert.Equal(1, countAfterDisconnect);

            var message = "still here";
            await handler.Broadcast(message);
            var recvMessage2 = await ReceiveTextAsync(socket2);

            Assert.Equal(message, recvMessage2);
        }

        [Fact]
        public async Task DisconnectClient_ConnectedClient_RemovesFromMapImmediately()
        {
            using var testServer = BuildTestServer(out var handler);
            var wsClient = testServer.CreateWebSocketClient();
            var clientId = "client-" + Guid.NewGuid();
            var uri = new Uri($"ws://localhost/ws?clientId={clientId}");

            using var socket = await wsClient.ConnectAsync(uri, CancellationToken.None);
            var countWhileConnected = await WaitForClientCountAsync(handler, 1);
            Assert.Equal(1, countWhileConnected);

            // clients.TryRemove runs synchronously before the first await inside
            // DisconnectClient, so the map is already updated once the call returns.
            // The close handshake that follows races with HandleAsync's own receive loop
            // on the same socket and can hang under TestServer's fake WebSocket, so it is
            // intentionally left unobserved here.
            _ = handler.DisconnectClient(clientId);

            Assert.Equal(0, handler.GetConnectedClientsCount());
        }

        [Fact]
        public async Task DisconnectClient_UnknownClientId_DoesNothing()
        {
            using var testServer = BuildTestServer(out var handler);

            var exception = await Record.ExceptionAsync(() => handler.DisconnectClient("never-connected"));

            Assert.Null(exception);
            Assert.Equal(0, handler.GetConnectedClientsCount());
        }
    }
}
