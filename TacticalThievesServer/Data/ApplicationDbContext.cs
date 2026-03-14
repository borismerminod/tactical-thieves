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
                .ToTable("PlayerProgress")
                .HasIndex(p => p.Pseudo)
                .IsUnique(false); // true si vous voulez un pseudo unique

            modelBuilder.Entity<StoredCredential>()
                .HasOne(c => c.User)
                .WithMany(u => u.Credentials)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}