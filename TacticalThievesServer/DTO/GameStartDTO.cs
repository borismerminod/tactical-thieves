using System.ComponentModel.DataAnnotations;

namespace TacticalThievesServer.DTO
{
    /// <summary>
    /// DTO sent when a Unity client notifies the server that a game session started.
    /// Contains the session identifier and the Unity instance identifier.
    /// </summary>
    [System.Serializable]
    public class GameStartDTO
    {
        [Required]
        public string? SessionID { get; set; }

        [Required]
        public string? UnityGUID { get; set; }
    }
}
