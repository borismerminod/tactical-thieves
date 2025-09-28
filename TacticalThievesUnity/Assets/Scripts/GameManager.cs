using GluonGui.WorkspaceWindow.Views.WorkspaceExplorer.Explorer;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        IN_GAME,
        WIN
    }

    public static GameManager Instance { get; private set; }

    [SerializeField] private int playerGold;
    [SerializeField] private WebSocketClient webSocketClient;
    [SerializeField] private APIClient apiClient;
    [SerializeField] private GameState gameState;
    [SerializeField] private bool testMode;

    public bool TestMode { get => testMode; set => testMode = value; }


    public GameState GetGameState() => gameState;

    public int PlayerGold
    {
        get => playerGold;
        private set
        {
            playerGold = value;
            if(playerGold < 0)
                playerGold = 0;
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        gameState = GameState.IN_GAME;
    }

    public void OnWebSocketClientStarted(WebSocketClient client)
    {
        webSocketClient = client;
    }

    public void OnAPIClientStarted(APIClient client)
    {
        apiClient = client;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnTreasureCollected(int gold)
    {
        PlayerGold += gold;

        //TODO Log en cas d'échec de l'appel
        if (apiClient != null && TestMode == false)
            StartCoroutine(apiClient.CollectTreasure(PlayerGold));
    }

    public void OnThiefReachExit()
    {
        gameState = GameState.WIN;
        //StartCoroutine(apiClient.ThiefReachedExit());
    }


}
