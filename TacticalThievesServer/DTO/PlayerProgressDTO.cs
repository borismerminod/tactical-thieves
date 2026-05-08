using System.ComponentModel.DataAnnotations;

namespace TacticalThievesServer.DTO
{
    [System.Serializable]
    public class PlayerProgressDTO
    {
        [Required]
        public string? Pseudo { get; set; }
        public uint CurrentLevel { get; set; }
    }
}
