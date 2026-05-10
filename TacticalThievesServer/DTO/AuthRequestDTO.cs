using System.ComponentModel.DataAnnotations;

namespace TacticalThievesServer.DTO
{
    /// <summary>
    /// DTO used when the client initiates an authentication-related request that
    /// requires a username (for registration or login flow start).
    /// </summary>
    public class AuthRequestDTO
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Username must have between 3 and 20 characters")]
        [RegularExpression(@"^[a-zA-Z]+[a-zA-Z0-9]+$", ErrorMessage = "Username is not compliant")]
        public string? Username { get; set; }
    }
}
