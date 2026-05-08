using System.ComponentModel.DataAnnotations;

namespace TacticalThievesServer.DTO
{
    [System.Serializable]
    public class GameMessage
    {
        [Required]
        public string? Type { get; set; }

        public uint Level { get; set; }
    }
}
