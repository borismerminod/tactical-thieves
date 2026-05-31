using System.ComponentModel.DataAnnotations;

namespace TacticalThievesServer.Models
{
    /// <summary>
    /// Represents an application user. Stores username, associated credentials
    /// (for FIDO2/WebAuthn) and a reference to the player's progress.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Primary identifier for the user.
        /// </summary>
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Username is required")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Username must have between 3 and 20 characters")]
        [RegularExpression(@"^[a-zA-Z]+[a-zA-Z0-9]+$", ErrorMessage = "Username is not compliant")]
        public required string Username { get; set; }

        /// <summary>
        /// Collection of stored FIDO2 credentials belonging to the user.
        /// </summary>
        public List<StoredCredential> Credentials { get; set; } = new();

        /// <summary>
        /// Navigation property to the player's progress record.
        /// </summary>
        public PlayerProgress CurrentLevel { get; set; } = null!;

    }
}
