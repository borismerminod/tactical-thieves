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

    public class APIClient : MonoBehaviour
    {
        //[SerializeField] private string serverUrl = "http://localhost:5140/api";
        [SerializeField] private string serverUrl = "https://localhost:7186/api";
        [SerializeField] private bool apiClientStarted;


        // Start is called before the first frame update
        void Start()
        {
            //GameManager.Instance.OnAPIClientStarted(this);        
            //StartCoroutine(InitWebGL());
            apiClientStarted = false;
        }

        private void Update()
        {
            if(!apiClientStarted && GameManager.Instance != null)
            {
                apiClientStarted = true;
                GameManager.Instance.OnAPIClientStarted(this);
            }
        }

        IEnumerator InitWebGL()
        {
            // Attendre 1 frame minimum
            //yield return null;

            // Attendre encore un peu (sécurité WebGL)
            //yield return new WaitForSeconds(1.0f);

            GameManager.Instance.OnAPIClientStarted(this);
            yield return null;
        }

        public IEnumerator CollectTreasure(int amount)
        {
            string endpoint = $"{serverUrl}/Game/collect-treasure";

            var json = JsonUtility.ToJson(new TreasureCollectDto { Amount = amount });
            var request = new UnityWebRequest(endpoint, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-Session-Id", GameManager.Instance.SessionID);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Response: " + request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("Error: " + request.error);
            }
        }

        public IEnumerator ThiefReachedExit(int nextLevel)
        {
            string endpoint = $"{serverUrl}/Game/exit-reached";

            var dto = new SaveLevelDto { CurrentLevel = nextLevel };
            var json = JsonUtility.ToJson(dto);

            var request = new UnityWebRequest(endpoint, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-Session-Id", GameManager.Instance.SessionID);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Response: " + request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("Error: " + request.error);
            }
        }

        public IEnumerator AllThievesDied()
        {
            string endpoint = $"{serverUrl}/Game/thieves-died";

            var request = new UnityWebRequest(endpoint, "POST");
            request.uploadHandler = new UploadHandlerRaw(new byte[0]);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-Session-Id", GameManager.Instance.SessionID);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Response: " + request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("Error: " + request.error);
            }
        }

        public IEnumerator GameStart()
        {
            string endpoint = $"{serverUrl}/Game/game-start";

            var payload = new GameStartDto
            {
                SessionID = GameManager.Instance.SessionID,
                UnityGUID = GameManager.Instance.UnityGUID
            };

            string json = JsonUtility.ToJson(payload);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            Debug.Log(json);
            var request = new UnityWebRequest(endpoint, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Response: " + request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("Error: " + request.error);
            }
        }

        // Version awaitable pour SaveLevel : Task (lève une exception en cas d'erreur)
        public Task SaveLevelAsync(string pseudo, int nextLevel)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            StartCoroutine(SaveLevel(pseudo, nextLevel, tcs));
            return tcs.Task;
        }

        private IEnumerator SaveLevel(string pseudo, int nextLevel, TaskCompletionSource<bool> tcs)
        {
            string endpoint = $"{serverUrl}/Game/save-level";

            var dto = new SaveLevelDto { Pseudo = pseudo, CurrentLevel = nextLevel };
            var json = JsonUtility.ToJson(dto);
            var request = new UnityWebRequest(endpoint, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();
            //request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("SaveLevelAsync Response: " + request.downloadHandler.text);
                tcs.SetResult(true);
            }
            else
            {
                Debug.LogError("SaveLevelAsync Error: " + request.error);
                tcs.SetException(new System.Exception(request.error));
            }
        }

        // Expose une API awaitable : Task<int>
        public Task<int> LoadLevelAsync(string pseudo)
        {
            var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            StartCoroutine(LoadLevel(pseudo, tcs));
            return tcs.Task;
        }

        private IEnumerator LoadLevel(string pseudo, TaskCompletionSource<int> tcs)
        {
            string escaped = UnityWebRequest.EscapeURL(pseudo);
            string endpoint = $"{serverUrl}/Game/load-level/{escaped}";

            var request = UnityWebRequest.Get(endpoint);
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string text = request.downloadHandler.text;
                // Essayer de parser d'abord en JSON { level: n } ou formulaire complet
                LoadLevelResponseDto resp = null;
                try
                {
                    resp = JsonUtility.FromJson<LoadLevelResponseDto>(text);
                }
                catch
                {
                    resp = null;
                }

                if (resp != null)
                {
                    tcs.SetResult(resp.level);
                    yield break;
                }

                if (int.TryParse(text, out int level))
                {
                    tcs.SetResult(level);
                }
                else
                {
                    tcs.SetException(new System.Exception($"LoadLevel parse error, response: {text}"));
                }
            }
            else
            {
                tcs.SetException(new System.Exception(request.error));
            }
        }

        class TreasureCollectDto
        {
            public int Amount;
        }

        // DTO pour SaveLevel
        class SaveLevelDto
        {
            public int CurrentLevel;
            public string Pseudo;
        }

        // DTO pour la réponse LoadLevel { success, id, pseudo, level }
        class LoadLevelResponseDto
        {
            public bool success;
            public int id;
            public string pseudo;
            public int level;
        }


        public class GameStartDto
        {
            public string SessionID;
            public string UnityGUID;
        }

    }
}

