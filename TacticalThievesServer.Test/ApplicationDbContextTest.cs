using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TacticalThievesServer.Data;
using TacticalThievesServer.Models;
using Xunit;

namespace TacticalThievesServer.Test
{
    // Vérifie la configuration des relations dans ApplicationDbContext.OnModelCreating :
    // PlayerProgress et StoredCredential doivent être supprimés en cascade avec leur User.
    public class ApplicationDbContextTest : IClassFixture<ControllerTestFactory>
    {
        private readonly ControllerTestFactory _factory;

        public ApplicationDbContextTest(ControllerTestFactory factory)
        {
            _factory = factory;
        }

        private ApplicationDbContext GetDb()
        {
            var scope = _factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        }

        private async Task<Guid> SeedUserWithProgressAndCredential()
        {
            var db = GetDb();
            var userId = Guid.NewGuid();
            var username = "user" + Guid.NewGuid().ToString("N")[..8];

            var user = new User { Id = userId, Username = username };
            db.Users.Add(user);
            db.PlayerProgresses.Add(new PlayerProgress { UserId = userId, CurrentLevel = 1 });
            db.StoredCredentials.Add(new StoredCredential
            {
                DescriptorId = new byte[32],
                PublicKey = new byte[32],
                Counter = 0,
                UserId = userId
            });

            await db.SaveChangesAsync();
            return userId;
        }

        [Fact]
        public async Task DeletingUser_CascadesDeletePlayerProgress()
        {
            var userId = await SeedUserWithProgressAndCredential();

            var db = GetDb();
            var user = await db.Users
                .Include(u => u.CurrentLevel)
                .Include(u => u.Credentials)
                .FirstAsync(u => u.Id == userId);
            db.Users.Remove(user);
            await db.SaveChangesAsync();

            var verifyDb = GetDb();
            var progressExists = await verifyDb.PlayerProgresses.AnyAsync(p => p.UserId == userId);
            Assert.False(progressExists);
        }

        [Fact]
        public async Task DeletingUser_CascadesDeleteStoredCredentials()
        {
            var userId = await SeedUserWithProgressAndCredential();

            var db = GetDb();
            var user = await db.Users
                .Include(u => u.CurrentLevel)
                .Include(u => u.Credentials)
                .FirstAsync(u => u.Id == userId);
            db.Users.Remove(user);
            await db.SaveChangesAsync();

            var verifyDb = GetDb();
            var credentialExists = await verifyDb.StoredCredentials.AnyAsync(c => c.UserId == userId);
            Assert.False(credentialExists);
        }
    }
}
