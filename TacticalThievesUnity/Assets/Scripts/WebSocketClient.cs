//using Codice.Client.BaseCommands;
using NativeWebSocket;
using System;
using System.Text;
using System.Threading.Tasks;
using TacticalThieves;
using UnityEngine;

public class WebSocketClient : MonoBehaviour
{
    private WebSocket _websocket;

    // URL du serveur WebSocket
    //public string serverUri = "ws://localhost:5140/ws";
    public string serverUri = "wss://localhost:7186/ws?clientId={$clientId}"; 
    //private string serverUri = "wss://mozell-fortifiable-moshe.ngrok-free.dev/ws?clientId={$clientId}"; 
    //private string serverUri = "wss://tactical-thieves.loca.lt/ws?clientId={$clientId}"; 
    public bool webSocketClientStarted;

    private void Start()
    {
        webSocketClientStarted = false;
    }

    private async void Update()
    {
        // Dispatcher messages queued (non‑WebGL implementation uses DispatchMessageQueue)
//#if !UNITY_WEBGL || UNITY_EDITOR
//        _websocket?.DispatchMessageQueue();
//#endif

        if (webSocketClientStarted == true || GameManager.Instance == null)
        {
            return;
        }

        webSocketClientStarted = true;

        GameObject debugText = GameObject.FindGameObjectWithTag("DebugText");
        GameManager.Instance.OnWebSocketClientStarted(this);

        /*try
        {
            Debug.Log("Connecting to WebSocket server...");
            if (debugText != null) debugText.GetComponent<UnityEngine.UI.Text>().text = serverUri;

            // Création de l'instance NativeWebSocket
            _websocket = new WebSocket(serverUri);

            // Événements
            _websocket.OnOpen += () =>
            {
                Debug.Log("Connected!");
                if (debugText != null) debugText.GetComponent<UnityEngine.UI.Text>().text = "Connected!";
            };

            _websocket.OnError += (e) =>
            {
                Debug.LogError("WebSocket error: " + e);
                if (debugText != null) debugText.GetComponent<UnityEngine.UI.Text>().text = "WebSocket error: " + e;
            };

            _websocket.OnClose += (code) =>
            {
                Debug.Log("WebSocket closed: " + code);
                if (debugText != null) debugText.GetComponent<UnityEngine.UI.Text>().text = "WebSocket closed: " + code;
            };

            _websocket.OnMessage += (bytes) =>
            {
                var msg = Encoding.UTF8.GetString(bytes);
                Debug.Log("Received: " + msg);
                HandleServerMessage(msg);
            };

            // Connexion (NativeWebSocket.Connect)
            await _websocket.Connect();

            // Exemple : envoyer un message au serveur
            await SendMessageAsync("Hello from Unity!");
        }
        catch (Exception ex)
        {
            string err = "WebSocket error: " + ex.Message;
            Debug.LogError(err);
            if (debugText != null) debugText.GetComponent<UnityEngine.UI.Text>().text = err;
        }*/
    }

    public async Task ConnectWebSocket()
    {
        GameObject debugText = GameObject.FindGameObjectWithTag("DebugText");
        //GameManager.Instance.OnWebSocketClientStarted(this);

        try
        {
            Debug.Log("Connecting to WebSocket server...");
            if (debugText != null) debugText.GetComponent<UnityEngine.UI.Text>().text = serverUri;

            // Création de l'instance NativeWebSocket


            string finallUri = serverUri.Replace("{$clientId}", GameManager.Instance.UnityGUID);

            _websocket = new WebSocket(finallUri);

            // Événements
            _websocket.OnOpen += () =>
            {
                Debug.Log("Connected!");
                if (debugText != null) debugText.GetComponent<UnityEngine.UI.Text>().text = "Connected!";

                GameManager.Instance.OnGameStart();
            };

            _websocket.OnError += (e) =>
            {
                Debug.LogError("WebSocket error: " + e);
                if (debugText != null) debugText.GetComponent<UnityEngine.UI.Text>().text = "WebSocket error: " + e;
            };

            _websocket.OnClose += (code) =>
            {
                Debug.Log("WebSocket closed: " + code);
                if (debugText != null) debugText.GetComponent<UnityEngine.UI.Text>().text = "WebSocket closed: " + code;
            };

            _websocket.OnMessage += (bytes) =>
            {
                var msg = Encoding.UTF8.GetString(bytes);
                Debug.Log("Received: " + msg);
                HandleServerMessage(msg);
            };

            // Connexion (NativeWebSocket.Connect)
            await _websocket.Connect();

            // Exemple : envoyer un message au serveur
            await SendMessageAsync("Hello from Unity!");
        }
        catch (Exception ex)
        {
            string err = "WebSocket error: " + ex.Message;
            Debug.LogError(err);
            if (debugText != null) debugText.GetComponent<UnityEngine.UI.Text>().text = err;
        }
    }
    public async Task SendMessageAsync(string message)
    {
        if (_websocket == null) return;

//#if UNITY_WEBGL && !UNITY_EDITOR
//        await _websocket.SendText(message);
//#else
        // la classe fournie a également SendText pour desktop
        await _websocket.SendText(message);
//#endif
        Debug.Log("Message sent: " + message);
    }

    private void HandleServerMessage(string msg)
    {
        Debug.Log("Server message handled: " + msg);

        ServerMessage serverMessage;

        try
        {
            serverMessage = JsonUtility.FromJson<ServerMessage>(msg);
        }
        catch (Exception e)
        {
            Debug.LogError("JSON parse error: " + e.Message);
            return;
        }

        if (serverMessage == null || string.IsNullOrEmpty(serverMessage.Type))
        {
            Debug.LogWarning("Invalid message format");
            return;
        }


        GameObject playerControllerGO = GameObject.FindGameObjectWithTag("PlayerController");
        if (playerControllerGO == null)
            return;

        PlayerController playerController = playerControllerGO.GetComponent<PlayerController>();
        if (playerController == null)
            return;

        switch (serverMessage.Type)
        {
            case "move":
                Debug.Log("Triggering move action");
                playerController.HandleThiefMove();
                break;
            case "stealth":
                Debug.Log("Triggering stealth action");
                playerController.HandleThiefStealth();
                break;
            case "end-turn":
                Debug.Log("Triggering end turn action");
                playerController.HandleThiefEndTurn();
                break;
            case "load-level":
                Debug.Log("Triggering load level action");
                GameManager.Instance.LoadLevel(serverMessage.Level);
                break;
            case "load-random-level":
                Debug.Log("Triggering load random level action");
                GameManager.Instance.LoadRandomLevel();
                break;
            case "restart":
                Debug.Log("Triggering restart action");
                GameManager.Instance.RestartLevel();
                break;
            default:
                Debug.Log("Unknown command");
                break;
        }
    }

    private void OnDestroy()
    {
        if (_websocket != null)
        {
            // Ne pas await dans OnDestroy, démarrer la fermeture en tâche de fond
            _ = _websocket.Close();
            _websocket = null;
        }
    }
}

[Serializable]
public class ServerMessage
{
    public string Type;
    public int Level;
}
