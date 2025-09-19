using System;
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
        [Fact]
        public async Task EchoHandler_ShouldRespondWithEcho()
        {
            // Arrange: configurer un serveur de test avec WebSocketHandler
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<WebSocketHandler>();
                })
                .Configure(app =>
                {
                    app.UseWebSockets();
                    app.Map("/ws", wsApp =>
                    {
                        wsApp.Run(async context =>
                        {
                            var handler = context.RequestServices.GetRequiredService<WebSocketHandler>();
                            await handler.HandleAsync(context);
                        });
                    });
                });

            using var testServer = new TestServer(builder);
            var wsClient = testServer.CreateWebSocketClient();
            var wsUri = new Uri("ws://localhost/ws"); // le chemin du handler

            // Act: connecter le client WebSocket de test
            using var socket = await wsClient.ConnectAsync(wsUri, CancellationToken.None);

            var message = "HelloTest";
            var sendBytes = Encoding.UTF8.GetBytes(message);
            await socket.SendAsync(new ArraySegment<byte>(sendBytes), WebSocketMessageType.Text, true, CancellationToken.None);

            var recvBuffer = new byte[1024];
            var recvResult = await socket.ReceiveAsync(new ArraySegment<byte>(recvBuffer), CancellationToken.None);
            var recvMessage = Encoding.UTF8.GetString(recvBuffer, 0, recvResult.Count);

            // Assert: le handler renvoie “Echo: HelloTest” ou ce que tu auras codé
            Assert.Equal("Server received: " + message, recvMessage);
        }

    }
}
