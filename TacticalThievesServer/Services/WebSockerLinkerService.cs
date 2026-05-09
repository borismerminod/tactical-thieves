namespace TacticalThievesServer.Services
{
    using System.Collections.Concurrent;

    public class WebSocketLinkerService
    {
        private readonly ConcurrentDictionary<string, WebSocketLinker> links = new();

        //Ajouter ou mettre à jour une session
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

        // Récupérer
        public bool TryGet(string sessionId, out WebSocketLinker? linker)
        {
            return links.TryGetValue(sessionId, out linker);
        }

        // Trouver via Angular
        public WebSocketLinker? GetByAngular(string angularGuid)
        {
            return links.Values.FirstOrDefault(x => x.AngularGUID == angularGuid);
        }

        // Trouver via Unity
        public WebSocketLinker? GetByUnity(string unityGuid)
        {
            return links.Values.FirstOrDefault(x => x.UnityGUID == unityGuid);
        }

        // Supprimer
        public void Remove(string sessionId)
        {
            links.TryRemove(sessionId, out _);
        }
    }
}
