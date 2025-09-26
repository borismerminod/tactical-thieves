using GluonGui.WorkspaceWindow.Views.WorkspaceExplorer.Explorer;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    public static GameManager Instance { get; private set; }

    [SerializeField] private int playerGold;
    [SerializeField] private WebSocketClient webSocketClient;
    [SerializeField] private APIClient apiClient;

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
        StartCoroutine(apiClient.CollectTreasure(PlayerGold));
    }


}
