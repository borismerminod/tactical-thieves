using Microsoft.EntityFrameworkCore;
using TacticalThievesServer.Models;

namespace TacticalThievesServer.Data
{
    /// <summary>
    /// Entity Framework Core database context for the application.
    /// Exposes DbSet properties for application entities and configures
    /// relationships and cascade behaviors in <see cref="OnModelCreating"/>.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        /// <summary>
        /// Creates a new instance of <see cref="ApplicationDbContext"/> with the provided options.
        /// </summary>
        /// <param name="options">The options used by a DbContext.</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }


        // Player progress records for each user.



        // Player progress records for each user.
        public DbSet<PlayerProgress> PlayerProgresses { get; set; } = null!;

        // Application users.
        public DbSet<User> Users { get; set; } = null!;

        // Stored credentials for users (e.g., for external authentication).
        public DbSet<StoredCredential> StoredCredentials { get; set; } = null!;



        /// <summary>
        /// Configures the EF Core model relationships and behaviors.
        /// - PlayerProgress has a one-to-one relationship with User (cascade delete).
        /// - StoredCredential has a many-to-one relationship with User (cascade delete).
        /// </summary>
        /// <param name="modelBuilder">Model builder used to configure entity mappings.</param>
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