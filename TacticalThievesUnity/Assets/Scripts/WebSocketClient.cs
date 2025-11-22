using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using UnityEngine;
using TacticalThieves;

public class WebSocketClient : MonoBehaviour
{
    private ClientWebSocket _webSocket;
    private CancellationTokenSource _cts;

    // URL du serveur WebSocket
    public string serverUri = "ws://localhost:5000/ws";

    private async void Start()
    {
        _cts = new CancellationTokenSource();
        _webSocket = new ClientWebSocket();

        GameManager.Instance.OnWebSocketClientStarted(this);

        try
        {
            Debug.Log("Connecting to WebSocket server...");
            await _webSocket.ConnectAsync(new Uri(serverUri), _cts.Token);
            Debug.Log("Connected!");

            // Lancer la réception des messages
            _ = ReceiveLoop();

            // Exemple : envoyer un message au serveur
            await SendMessage("Hello from Unity!");
        }
        catch (Exception ex)
        {
            Debug.LogError("WebSocket error: " + ex.Message);
        }
    }

    private async Task SendMessage(string message)
    {
        if (_webSocket.State == WebSocketState.Open)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);
            Debug.Log("Message sent: " + message);
        }
    }

    private async Task ReceiveLoop()
    {
        var buffer = new byte[1024];
        while (_webSocket.State == WebSocketState.Open)
        {
            try
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Debug.Log("Server requested close. Closing connection...");
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", _cts.Token);
                }
                else
                {
                    var msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Debug.Log("Received: " + msg);

                    // Ici tu peux déclencher des actions dans ton jeu
                    HandleServerMessage(msg);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Receive loop error: " + ex.Message);
                break;
            }
        }
    }

    private void HandleServerMessage(string msg)
    {
        // TODO: Décoder le message et déclencher les actions (move, stealth, etc.)
        Debug.Log("Server message handled: " + msg);

        GameObject playerControllerGO = GameObject.FindGameObjectWithTag("PlayerController");
        if (playerControllerGO == null)
            return;

        PlayerController playerController = playerControllerGO.GetComponent<PlayerController>();
        if (playerController == null)
            return;

        switch (msg)
        {
            case "move":
                // Appeler la méthode de déplacement dans ton jeu
                Debug.Log("Triggering move action");
                playerController.HandleThiefMove();
                break;
            case "stealth":
                // Appeler la méthode de furtivité dans ton jeu
                Debug.Log("Triggering stealth action");
                playerController.HandleThiefStealth();
                break;
            default:
                Debug.Log("Unknown command");
                break;
        }

    }

    private void OnDestroy()
    {
        if (_webSocket != null)
        {
            _cts.Cancel();
            _webSocket.Dispose();
        }
    }
}
