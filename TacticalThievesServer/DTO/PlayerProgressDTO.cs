using System.ComponentModel.DataAnnotations;

namespace TacticalThievesServer.DTO
{
    /// <summary>
    /// DTO carrying player's progress information (pseudo and current level).
    /// Used when saving progress or notifying the server about level-related events.
    /// </summary>
    [System.Serializable]
    public class PlayerProgressDTO
    {
        [Required]
        public string? Pseudo { get; set; }
        public uint CurrentLevel { get; set; }
    }
}
