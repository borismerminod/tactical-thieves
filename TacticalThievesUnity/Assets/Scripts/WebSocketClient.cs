//using Codice.Client.BaseCommands;
using NativeWebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TacticalThieves;
using UnityEngine;

public class WebSocketClient : MonoBehaviour
{
    private WebSocket _websocket;
    [SerializeField] PlayerController playerController;
    [SerializeField] Dictionary<string, Action<Utils.ServerMessage>> websocketActions;
    [SerializeField] string finalURI;

    private void Start()
    {
        try
        {
            finalURI = GameManager.Instance?.Config.websocketURL.Replace("{$clientId}", GameManager.Instance.UnityGUID);
            playerController = GameManager.Instance?.CurrentPlayerController;
            websocketActions = new Dictionary<string, Action<Utils.ServerMessage>>
            {
                {"move", _ => playerController.HandleThiefMove()},
                {"stealth", _ => playerController.HandleThiefStealth()},
                {"end-turn",  msg => GameManager.Instance.LoadLevel(msg.Level)},
                {"load-level", _ => playerController.HandleThiefEndTurn()},
                { "load-random-level", _ => GameManager.Instance.LoadRandomLevel() },
                { "restart", _ => GameManager.Instance.RestartLevel() }
            };
        }
        catch(Exception e)
        {
            Debug.LogError("WebSocketClient Error " + e.Message);
        }

    }

    public async Task ConnectWebSocket()
    {
        GameObject debugText = GameObject.FindGameObjectWithTag("DebugText");

        try
        {
            Debug.Log("Connecting to WebSocket server...");
            // Création de l'instance NativeWebSocket

            _websocket = new WebSocket(finalURI);

            // Événements
            _websocket.OnOpen += () =>
            {
                Debug.Log("Connected!");
                GameManager.Instance.OnGameStart();
            };

            _websocket.OnError += (e) =>
            {
                Debug.LogError("WebSocket error: " + e);
            };

            _websocket.OnClose += (code) =>
            {
                Debug.Log("WebSocket closed: " + code);
            };

            _websocket.OnMessage += (bytes) =>
            {
                var msg = Encoding.UTF8.GetString(bytes);
                Debug.Log("Received: " + msg);
                HandleServerMessage(msg);
            };

            await _websocket.Connect();

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

        await _websocket.SendText(message);
        Debug.Log("Message sent: " + message);
    }

    private void HandleServerMessage(string msg)
    {
        Debug.Log("Server message handled: " + msg);

        Utils.ServerMessage serverMessage;

        try
        {
            serverMessage = JsonUtility.FromJson<Utils.ServerMessage>(msg);
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


        if (playerController == null)
            return;

        if (websocketActions.TryGetValue(serverMessage.Type, out var handler))
            handler(serverMessage);
        else
            Debug.LogWarning($"Unknown command: {serverMessage.Type}");

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


