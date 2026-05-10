using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TacticalThievesServer.Models
{
    /// <summary>
    /// Represents a player's progress in the game. Mapped to the "PlayerProgress" table.
    /// Stores the current level reached and links back to the owning user.
    /// </summary>
    [Table("PlayerProgress")]
    public class PlayerProgress
    {
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to the owning user.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Navigation property to the owning user.
        /// </summary>
        public User User { get; set; } = null!;

        /// <summary>
        /// The last level reached by the player.
        /// </summary>
        public uint CurrentLevel { get; set; }
    }
}