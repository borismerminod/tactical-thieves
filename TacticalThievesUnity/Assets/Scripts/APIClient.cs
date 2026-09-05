using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
//using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Networking;
using static System.Net.WebRequestMethods;

namespace TacticalThieves
{

    /// <summary>
    /// Responsible for communicating with the game's remote API.
    /// </summary>
    /// <remarks>
    /// This MonoBehaviour centralizes HTTP request logic (GET and POST) and exposes a set of
    /// coroutine-based methods used by the GameManager and game systems to report events
    /// (game start, treasure collection, level save/load, etc.). It also provides awaitable
    /// Task-based wrappers for save/load level operations.
    /// </remarks>
    public class APIClient : MonoBehaviour
    {

        [SerializeField] private bool apiClientStarted;

        /// <summary>
        /// Delegate that actually performs an HTTP request. Defaults to <see cref="DefaultSendRequest"/>
        /// (real UnityWebRequest). Unit tests can assign a fake implementation to intercept requests
        /// without hitting the network, since UnityWebRequest cannot complete synchronously in an EditMode [Test].
        /// </summary>
        private System.Func<string, string, string, System.Action<string>, System.Action<string>, IEnumerator> requestSender;

        /// <summary>
        /// The request sender used by every API call. Production code keeps the default; tests may override it.
        /// </summary>
        public System.Func<string, string, string, System.Action<string>, System.Action<string>, IEnumerator> RequestSender
        {
            get => requestSender ?? DefaultSendRequest;
            set => requestSender = value;
        }

        /// <summary>
        /// Starts a coroutine. Defaults to <see cref="MonoBehaviour.StartCoroutine(IEnumerator)"/>.
        /// Unit tests can assign a synchronous runner so that the Task-based wrappers
        /// (<see cref="SaveLevelAsync"/>, <see cref="LoadLevelAsync"/>) complete inside an EditMode [Test],
        /// where StartCoroutine does not run.
        /// </summary>
        private System.Action<IEnumerator> coroutineStarter;

        /// <summary>
        /// The coroutine starter used by the Task-based wrappers. Production code keeps the default; tests may override it.
        /// </summary>
        public System.Action<IEnumerator> CoroutineStarter
        {
            get => coroutineStarter ?? RunWithStartCoroutine;
            set => coroutineStarter = value;
        }

        /// <summary>
        /// Default coroutine starter: delegates to the MonoBehaviour coroutine loop.
        /// </summary>
        private void RunWithStartCoroutine(IEnumerator routine)
        {
            StartCoroutine(routine);
        }


        /// <summary>
        /// This method centralizes the logic for sending API requests, including setting headers and handling responses.
        /// It takes care of both success and error cases, allowing the caller to simply provide callbacks for each scenario. 
        /// This helps reduce code duplication across different API calls and makes it easier to maintain the request logic in one place.
        /// </summary>
        /// <param name="url">The endpoint to reach</param>
        /// <param name="method">POST or GET</param>
        /// <param name="jsonBody">The json body to send</param>
        /// <param name="onSuccess">The call back function on success case</param>
        /// <param name="onError">The call back function on failure case</param>
        /// <returns>IEnumerator for use with StartCoroutine</returns>
        private IEnumerator SendRequest(string url, string method, string jsonBody, System.Action<string> onSuccess, System.Action<string> onError = null)
        {
            var sender = RequestSender;
            return sender(url, method, jsonBody, onSuccess, onError);
        }

        /// <summary>
        /// Default request sender: performs the real HTTP call through UnityWebRequest.
        /// </summary>
        /// <returns>IEnumerator for use with StartCoroutine</returns>
        private IEnumerator DefaultSendRequest(string url, string method, string jsonBody, System.Action<string> onSuccess, System.Action<string> onError = null)
        {
            if(method != "POST" && method != "GET")
            {
                onError?.Invoke($"Unsupported HTTP method: {method}");
                yield break;
            }

            var request = new UnityWebRequest(url, method);

            if (!string.IsNullOrEmpty(jsonBody))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            }

            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-Session-Id", GameManager.Instance.SessionID);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                onSuccess?.Invoke(request.downloadHandler.text);
            else
                onError?.Invoke(request.error);
        }

        /// <summary>
        /// Sends a request to collect treasure on the server for the current session.
        /// </summary>
        /// <param name="amount">Number of treasure units to collect</param>
        /// <returns>IEnumerator that completes when the request finishes</returns>
        public IEnumerator CollectTreasure(int amount)
        {
            string endpoint = $"{GameManager.Instance.Config.serverUrl}/api/Game/collect-treasure";

            yield return SendRequest(
                    endpoint, 
                    "POST", 
                    JsonUtility.ToJson(new TreasureCollectDto { Amount = amount }),
                    onSuccess: response => Debug.Log("Response: " + response),
                    onError: error => Debug.LogError("Error: " + error)
            );
        }
           


        /// <summary>
        /// Notifies the server that a thief has reached the exit and optionally save the next level number.
        /// </summary>
        /// <param name="nextLevel">The next level index to save on the server</param>
        /// <returns>IEnumerator that completes when the request finishes</returns>
        public IEnumerator ThiefReachedExit(int nextLevel)
        {
            string endpoint = $"{GameManager.Instance.Config.serverUrl}/api/Game/exit-reached";

            yield return SendRequest(
                    endpoint,
                    "POST",
                    JsonUtility.ToJson(new SaveLevelDto {Pseudo="userTest", CurrentLevel = nextLevel }),
                    onSuccess: response => Debug.Log("Response: " + response),
                    onError: error => Debug.LogError("Error: " + error)
            );

        }

        /// <summary>
        /// Notifies the server that all thieves died in the current game.
        /// </summary>
        /// <returns>IEnumerator that completes when the request finishes</returns>
        public IEnumerator AllThievesDied()
        {
            string endpoint = $"{GameManager.Instance.Config.serverUrl}/api/Game/thieves-died";
            yield return SendRequest(
                    endpoint,
                    "POST",
                    jsonBody: "",
                    onSuccess: response => Debug.Log("Response: " + response),
                    onError: error => Debug.LogError("Error: " + error)
            );

        }

        /// <summary>
        /// Sends the game-start event to the server including session and client identifiers.
        /// </summary>
        /// <returns>IEnumerator that completes when the request finishes</returns>
        public IEnumerator GameStart()
        {
            string endpoint = $"{GameManager.Instance.Config.serverUrl}/api/Game/game-start";

            yield return SendRequest(
                    endpoint,
                    "POST",
                    JsonUtility.ToJson(new GameStartDto { SessionID = GameManager.Instance.SessionID, UnityGUID = GameManager.Instance.UnityGUID }),
                    onSuccess: response => Debug.Log("Response: " + response),
                    onError: error => Debug.LogError("Error: " + error)
            );

        }

        // Version awaitable pour SaveLevel : Task (l�ve une exception en cas d'erreur)
        /// <summary>
        /// Asynchronously saves the player's current level on the remote service.
        /// </summary>
        /// <param name="pseudo">Player pseudo to save</param>
        /// <param name="nextLevel">Level index to save</param>
        /// <returns>A Task that completes when the save operation finishes. Throws on error.</returns>
        public Task SaveLevelAsync(string pseudo, int nextLevel)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            CoroutineStarter(SaveLevel(pseudo, nextLevel, tcs));
            return tcs.Task;
        }

        /// <summary>
        /// Coroutine implementation for SaveLevelAsync. Completes or faults the provided TaskCompletionSource.
        /// </summary>
        /// <param name="pseudo">Player pseudo to save</param>
        /// <param name="nextLevel">Level index to save</param>
        /// <param name="tcs">TaskCompletionSource to signal completion or failure</param>
        /// <returns>IEnumerator for use with StartCoroutine</returns>
        private IEnumerator SaveLevel(string pseudo, int nextLevel, TaskCompletionSource<bool> tcs)
        {
            string endpoint = $"{GameManager.Instance.Config.serverUrl}/api/Game/save-level";

            yield return SendRequest(
                    endpoint,
                    "POST",
                    JsonUtility.ToJson(new SaveLevelDto { Pseudo = pseudo, CurrentLevel = nextLevel }),
                    onSuccess: response => {
                        Debug.Log("SaveLevelAsync Response: " + response);
                        tcs.SetResult(true);
                    },
                    onError: error => {
                        Debug.LogError("SaveLevelAsync Error: " + error);
                        tcs.SetException(new System.Exception(error));
                    }
            );

        }

        // Expose une API awaitable : Task<int>
        /// <summary>
        /// Asynchronously loads the saved level for a given player pseudo.
        /// </summary>
        /// <param name="pseudo">Player pseudo to load</param>
        /// <returns>A Task that evaluates to the loaded level index. Throws on error.</returns>
        public Task<int> LoadLevelAsync(string pseudo)
        {
            var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            CoroutineStarter(LoadLevel(pseudo, tcs));
            return tcs.Task;
        }

        /// <summary>
        /// Coroutine implementation for LoadLevelAsync. Parses the server response and completes the TaskCompletionSource.
        /// </summary>
        /// <param name="pseudo">Player pseudo to load</param>
        /// <param name="tcs">TaskCompletionSource to signal completion or failure</param>
        /// <returns>IEnumerator for use with StartCoroutine</returns>
        private IEnumerator LoadLevel(string pseudo, TaskCompletionSource<int> tcs)
        {
            string escaped = UnityWebRequest.EscapeURL(pseudo);
            string endpoint = $"{GameManager.Instance.Config.serverUrl}/api/Game/load-level/{escaped}";

            yield return SendRequest(
                    endpoint,
                    "GET",
                    jsonBody: "",
                    onSuccess: response => {
                        Debug.Log("LoadLevelAsync Response: " + response);
                        try
                        {
                            LoadLevelResponseDto resp = JsonUtility.FromJson<LoadLevelResponseDto>(response);
                            if (resp.success)
                                tcs.SetResult(resp.level);
                            else
                                tcs.SetException(new System.Exception($"LoadLevel failed, response: {response}"));
                        }
                        catch (System.Exception ex)
                        {
                            tcs.SetException(new System.Exception($"LoadLevel parse error, response: {response}", ex));
                        }
                    },
                    onError: error => {
                        Debug.LogError("LoadLevelAsync Error: " + error);
                        tcs.SetException(new System.Exception(error));
                    }
            );

        }

        class TreasureCollectDto
        {
            public int Amount;
        }

        // DTO pour SaveLevel
        /// <summary>
        /// DTO used to save the current level for a player.
        /// </summary>
        class SaveLevelDto
        {
            public int CurrentLevel;
            public string Pseudo;
        }

        // DTO pour la r�ponse LoadLevel { success, id, pseudo, level }
        /// <summary>
        /// DTO representing the server response when loading a player's saved level.
        /// </summary>
        class LoadLevelResponseDto
        {
            public bool success;
            public int id;
            public string pseudo;
            public int level;
        }


        /// <summary>
        /// DTO used when notifying the server that the game has started.
        /// </summary>
        public class GameStartDto
        {
            public string SessionID;
            public string UnityGUID;
        }

    }
}

