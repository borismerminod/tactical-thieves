namespace TacticalThievesServer.Services
{
    using System.Collections.Concurrent;
    using System.Linq;
    using TacticalThievesServer.Models;

    /// <summary>
    /// Service that keeps an in-memory mapping between session identifiers and
    /// their corresponding WebSocket/SignalR identifiers.
    /// It is used to link Unity clients and Angular clients by a session id.
    /// </summary>
    public class WebSocketLinkerService
    {
        /// Internal storage for session links.
        /// Key: sessionId, Value: WebSocketLinker containing unity/angular ids.
        private readonly ConcurrentDictionary<string, WebSocketLinker> links = new();

        /// <summary>
        /// Adds a new session link or updates an existing one.
        /// If a non-null guid is provided it will overwrite the corresponding value.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="unityGuid">Optional Unity client identifier.</param>
        /// <param name="angularGuid">Optional Angular client identifier.</param>
        public void AddOrUpdate(string sessionId, string? unityGuid = null, string? angularGuid = null)
        {
            links.AddOrUpdate(sessionId,
                new WebSocketLinker
                {
                    SessionID = sessionId,
                    UnityGUID = unityGuid,
                    AngularGUID = angularGuid
                },
                (key, existing) =>
                {
                    if (!string.IsNullOrEmpty(unityGuid))
                        existing.UnityGUID = unityGuid;

                    if (!string.IsNullOrEmpty(angularGuid))
                        existing.AngularGUID = angularGuid;

                    return existing;
                });
        }

        /// <summary>
        /// Tries to retrieve a session link by session id.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="linker">Out parameter containing the found linker or null.</param>
        /// <returns>True if a link was found; otherwise false.</returns>
        public bool TryGet(string sessionId, out WebSocketLinker? linker)
        {
            return links.TryGetValue(sessionId, out linker);
        }

        /// <summary>
        /// Finds a session link by the Angular connection id.
        /// </summary>
        /// <param name="angularGuid">Angular connection identifier.</param>
        /// <returns>The matching <see cref="WebSocketLinker"/> or null if not found.</returns>
        public WebSocketLinker? GetByAngular(string angularGuid)
        {
            return links.Values.FirstOrDefault(x => x.AngularGUID == angularGuid);
        }

        /// <summary>
        /// Finds a session link by the Unity connection id.
        /// </summary>
        /// <param name="unityGuid">Unity connection identifier.</param>
        /// <returns>The matching <see cref="WebSocketLinker"/> or null if not found.</returns>
        public WebSocketLinker? GetByUnity(string unityGuid)
        {
            return links.Values.FirstOrDefault(x => x.UnityGUID == unityGuid);
        }

        /// <summary>
        /// Removes a session link from the internal store.
        /// </summary>
        /// <param name="sessionId">The session identifier to remove.</param>
        public void Remove(string sessionId)
        {
            links.TryRemove(sessionId, out _);
        }
    }
}
