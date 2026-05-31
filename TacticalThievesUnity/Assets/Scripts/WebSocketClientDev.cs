using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace TacticalThieves
{
    public class WebSocketClientDev : MonoBehaviour
    {
        private ClientWebSocket _websocket;
        private CancellationTokenSource _cts;
        [SerializeField] private string finalURI;
        [SerializeField] private PlayerController playerController;
        [SerializeField] Dictionary<string, Action<Utils.ServerMessage>> websocketActions;


        // Start is called before the first frame update
        void Start()
        {
            try
            {
                _cts = new CancellationTokenSource();
                _websocket = new ClientWebSocket();
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
            catch (Exception ex)
            {
                Debug.LogError("WebSocket initialization error: " + ex.Message);
            }
        }

        public IEnumerator Connect()
        {

            Debug.Log("Connecting to WebSocket server...");
            // Convertir l'awaitable en coroutine-friendly
            var task = _websocket.ConnectAsync(new Uri(finalURI), _cts.Token);
            yield return new WaitUntil(() => task.IsCompleted);

            // Lancer la réception des messages
            _ = ReceiveLoop();

            task = SendMessage("Hello from Unity!");
            yield return new WaitUntil(() => task.IsCompleted);

        }

        private async Task SendMessage(string message)
        {
            if (_websocket.State == WebSocketState.Open)
            {
                var bytes = Encoding.UTF8.GetBytes(message);
                await _websocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);
                Debug.Log("Message sent: " + message);
            }
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[1024];
            while (_websocket.State == WebSocketState.Open)
            {
                try
                {
                    var result = await _websocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Debug.Log("Server requested close. Closing connection...");
                        await _websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", _cts.Token);
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

        private void OnDestroy()
        {
            _cts.Cancel();
            _websocket.Dispose();
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

    }

}
