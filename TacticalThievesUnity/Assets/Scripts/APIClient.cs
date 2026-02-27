using System.Collections;
using System.Collections.Generic;
//using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Networking;
using static System.Net.WebRequestMethods;
using System.Threading.Tasks;

namespace TacticalThieves
{

    public class APIClient : MonoBehaviour
    {
        //[SerializeField] private string serverUrl = "http://localhost:5140/api";
        [SerializeField] private string serverUrl = "https://localhost:7186/api";


        // Start is called before the first frame update
        void Start()
        {
            //GameManager.Instance.OnAPIClientStarted(this);        
            StartCoroutine(InitWebGL());
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

        public IEnumerator ThiefReachedExit()
        {
            string endpoint = $"{serverUrl}/Game/exit-reached";

            var request = new UnityWebRequest(endpoint, "POST");
            request.uploadHandler = new UploadHandlerRaw(new byte[0]);
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

        public IEnumerator AllThievesDied()
        {
            string endpoint = $"{serverUrl}/Game/thieves-died";

            var request = new UnityWebRequest(endpoint, "POST");
            request.uploadHandler = new UploadHandlerRaw(new byte[0]);
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

        public IEnumerator GameStart()
        {
            string endpoint = $"{serverUrl}/Game/game-start";

            var request = new UnityWebRequest(endpoint, "POST");
            request.uploadHandler = new UploadHandlerRaw(new byte[0]);
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

        // POST /Game/save-level
        // Envoie un JSON { Pseudo, NextLevel }
        public IEnumerator SaveLevel(string pseudo, int nextLevel, System.Action onComplete = null, System.Action<string> onError = null)
        {
            string endpoint = $"{serverUrl}/Game/save-level";

            var dto = new SaveLevelDto { Pseudo = pseudo, Level = nextLevel };
            var json = JsonUtility.ToJson(dto);
            var request = new UnityWebRequest(endpoint, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("SaveLevel Response: " + request.downloadHandler.text);
                onComplete?.Invoke();
            }
            else
            {
                Debug.LogError("SaveLevel Error: " + request.error);
                onError?.Invoke(request.error);
            }
        }

        // Expose une API awaitable : Task<int>
        public Task<int> LoadLevelAsync(string pseudo)
        {
            var tcs = new TaskCompletionSource<int>();
            StartCoroutine(LoadLevelCoroutine(pseudo, tcs));
            return tcs.Task;
        }

        private IEnumerator LoadLevelCoroutine(string pseudo, TaskCompletionSource<int> tcs)
        {
            string escaped = UnityWebRequest.EscapeURL(pseudo);
            string endpoint = $"{serverUrl}/Game/load-level/{escaped}";

            var request = UnityWebRequest.Get(endpoint);
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string text = request.downloadHandler.text;
                // Essayer de parser d'abord en JSON { NextLevel: n }
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
            public int Level;
            public string Pseudo;
        }

        // DTO pour la réponse LoadLevel { NextLevel: n }
        class LoadLevelResponseDto
        {
            public bool success;
            public int id;
            public string pseudo;
            public int level;
        }

    }
}

