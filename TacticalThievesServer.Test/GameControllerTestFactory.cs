using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TacticalThievesServer.Data;
using TacticalThievesServer.Services;
using TacticalThievesServer.Test.Fakes;

namespace TacticalThievesServer.Test
{
    public class GameControllerTestFactory : WebApplicationFactory<Program>
    {
        public FakeWebSocketHandler FakeWebSocketHandler { get; } = new();
        public FakeHubContext FakeHubContext { get; } = new();

        private readonly string _dbName = "TestDb_" + Guid.NewGuid();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<ApplicationDbContext>();
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase(_dbName));

                services.RemoveAll<IWebSocketHandler>();
                services.AddSingleton<IWebSocketHandler>(FakeWebSocketHandler);

                services.RemoveAll<IHubContext<ClientHub>>();
                services.AddSingleton<IHubContext<ClientHub>>(FakeHubContext);
            });
        }
    }
}
