using System.ComponentModel.DataAnnotations;

namespace TacticalThievesServer.DTO
{
    /// <summary>
    /// Generic DTO used to send simple game commands or payloads between
    /// the server and game clients. `Type` indicates the action and `Level`
    /// can carry an associated numeric value when relevant.
    /// </summary>
    [System.Serializable]
    public class GameMessage
    {
        [Required]
        public string? Type { get; set; }

        public uint Level { get; set; }
    }
}
