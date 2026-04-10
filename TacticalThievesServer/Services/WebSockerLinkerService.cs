namespace TacticalThievesServer.Services
{
    using System.Collections.Concurrent;

    public class WebSocketLinkerService
    {
        private readonly ConcurrentDictionary<string, WebSocketLinker> _links = new();

        //Ajouter ou mettre à jour une session
        public void AddOrUpdate(string sessionId, string unityGuid = null, string angularGuid = null)
        {
            _links.AddOrUpdate(sessionId,
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
        public bool TryGet(string sessionId, out WebSocketLinker linker)
        {
            return _links.TryGetValue(sessionId, out linker);
        }

        // Trouver via Angular
        public WebSocketLinker GetByAngular(string angularGuid)
        {
            return _links.Values.FirstOrDefault(x => x.AngularGUID == angularGuid);
        }

        // Trouver via Unity
        public WebSocketLinker GetByUnity(string unityGuid)
        {
            return _links.Values.FirstOrDefault(x => x.UnityGUID == unityGuid);
        }

        // Supprimer
        public void Remove(string sessionId)
        {
            _links.TryRemove(sessionId, out _);
        }
    }
}
