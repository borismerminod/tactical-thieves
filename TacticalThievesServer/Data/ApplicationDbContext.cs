using Microsoft.EntityFrameworkCore;
using TacticalThievesServer.Models;

namespace TacticalThievesServer.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<PlayerProgress> PlayerProgresses { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;

        public DbSet<StoredCredential> StoredCredentials { get; set; } = null!;



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PlayerProgress>()
                .HasOne(c => c.User)
                .WithOne(u => u.CurrentLevel)
                .HasForeignKey<PlayerProgress>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<StoredCredential>()
                .HasOne(c => c.User)
                .WithMany(u => u.Credentials)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}