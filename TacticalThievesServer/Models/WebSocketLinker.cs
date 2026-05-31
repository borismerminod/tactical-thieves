namespace TacticalThievesServer.Models
{
    /// <summary>
    /// Lightweight model used by <see cref="WebSocketLinkerService"/> to associate
    /// a session identifier with the connected Unity and Angular client identifiers.
    /// </summary>
    public class WebSocketLinker
    {
        /// <summary>
        /// The application session identifier used to correlate clients.
        /// </summary>
        public required string SessionID { get; set; }

        /// <summary>
        /// Optional identifier of the Unity client (WebSocket side).
        /// </summary>
        public string? UnityGUID { get; set; }

        /// <summary>
        /// Optional identifier of the Angular client (SignalR side).
        /// </summary>
        public string? AngularGUID { get; set; }
    }
}
