namespace TacticalThievesServer.Models
{
    public class WebSocketLinker
    {
        public required string SessionID { get; set; }
        public string? UnityGUID { get; set; }
        public string? AngularGUID { get; set; }
    }
}
