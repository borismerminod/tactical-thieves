using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Networking;

namespace TacticalThieves
{

    public class APIClient : MonoBehaviour
    {
        [SerializeField] private string serverUrl = "http://localhost:5140/api";


        // Start is called before the first frame update
        void Start()
        {
            GameManager.Instance.OnAPIClientStarted(this);        
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

        class TreasureCollectDto
        {
            public int Amount;
        }

    }
}

