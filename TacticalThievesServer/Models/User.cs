using System.ComponentModel.DataAnnotations;

namespace TacticalThievesServer.Models
{
    public class User
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Username is required")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Username must have between 3 and 20 characters")]
        [RegularExpression(@"^[a-zA-Z]+[a-zA-Z0-9]+$", ErrorMessage = "Username is not compliant")]
        public string Username { get; set; }

        public List<StoredCredential> Credentials { get; set; } = new();

        public PlayerProgress CurrentLevel { get; set; } = null!;

    }
}
