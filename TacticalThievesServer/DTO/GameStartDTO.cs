using System.ComponentModel.DataAnnotations;

namespace TacticalThievesServer.DTO
{
    [System.Serializable]
    public class GameStartDTO
    {
        [Required]
        public string? SessionID { get; set; }

        [Required]
        public string? UnityGUID { get; set; }
    }
}
