using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.UI.CanvasScaler;

namespace TacticalThieves
{
    /// <summary>
    /// Central manager that coordinates game subsystems and global game flow.
    /// </summary>
    /// <remarks>
    /// This MonoBehaviour acts as a singleton facade providing access to primary systems
    /// (grid, input controllers, managers) and handles high-level events such as level
    /// loading, game start, win/lose conditions and communication with server-side APIs.
    /// Use GameManager.Instance to access the running instance.
    /// </remarks>
    public class GameManager : MonoBehaviour
    {
        /// <summary>
        /// Represents the current state of the game.
        /// </summary>
        public enum GameState
        {
            LOADING,
            IN_GAME,
            WIN,
            LOSE
        }

        public static GameManager Instance { get; private set; }

        
        [SerializeField] private WebSocketClient webSocketClient;
        [SerializeField] private APIClient apiClient;
        [SerializeField] private GameState gameState;
        [SerializeField] private Grid currentGrid;
        [SerializeField] private bool testMode;
        
        
        [SerializeField] private PlayerController playerController;
        [SerializeField] private AIController aiController;
        [SerializeField] private LevelManager levelManager;
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private bool bInit;
        [SerializeField] private string unityGUID;
        [SerializeField] private string sessionID;
        [SerializeField] private AppConfig config;
        [SerializeField] private GoldManager goldManager;
        [SerializeField] private CharactersManager charactersManager;
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private GridActionHandler gridActionHandler;


        /// <summary>
        /// The currently active grid instance in the scene.
        /// </summary>
        public Grid CurrentGrid { get => currentGrid; set => currentGrid = value; }

        /// <summary>
        /// Gets the <see cref="GridActionHandler"/> instance responsible for handling grid-related actions.
        /// </summary>
        public GridActionHandler GridActionHandler { get => gridActionHandler; private set => gridActionHandler = value; }

        public bool TestMode { get => testMode; set => testMode = value; }

        /// <summary>
        /// Provides access to the audio manager responsible for in-game sounds.
        /// </summary>
        public AudioManager CurrentAudioManager { get => audioManager; private set => audioManager = value; }

        /// <summary>
        /// Unique identifier generated for this client runtime.
        /// </summary>
        public string UnityGUID { get => unityGUID; private set => unityGUID = value; }

        /// <summary>
        /// Session identifier obtained from the hosting environment or query string.
        /// </summary>
        public string SessionID { get => sessionID; private set => sessionID = value; }

        /// <summary>
        /// Application configuration scriptable object containing server URLs and other settings.
        /// </summary>
        public AppConfig Config { get => config; private set => config = value; }

        /// <summary>
        /// Manager that tracks and manipulates the player's gold.
        /// </summary>
        public GoldManager PlayerGoldManager { get => goldManager; private set => goldManager = value; }

        /// <summary>
        /// Manager that holds the collection of characters participating in the current level.
        /// </summary>
        public CharactersManager CharactersManager { get => charactersManager; private set => charactersManager = value; }


        /// <summary>
        /// Returns the currently stored game state.
        /// </summary>
        /// <returns>The current <see cref="GameState"/> value.</returns>
        public GameState GetGameState() => gameState;

        /// <summary>
        /// Game state property with side-effects when entering IN_GAME.
        /// </summary>
        public GameState State {
            get => gameState;
            private set
            {
                gameState = value;
                Debug.Log("Game state changed to: " + gameState);
                if (gameState == GameState.IN_GAME && !bInit)
                {
                    InitCharacterTurnIndex();
                    bInit = true;
                }

            }
        }

        public PlayerController CurrentPlayerController { get => playerController; private set => playerController = value; }

        /// <summary>
        /// Called when the script instance is being loaded. Sets up the singleton instance.
        /// </summary>
        private void Awake()
        {
            try
            {
                if (InitGameManagerInstance() == true)
                    InitGameIDs();
            }
            catch (Exception ex)
            {
                Debug.LogError("Error during GameManager Awake: " + ex.Message);
            }
        }

        /// <summary>
        /// Ensures only one GameManager singleton exists and destroys duplicates.
        /// </summary>
        /// <returns>True if this instance should continue initialization; false if destroyed.</returns>
        private bool InitGameManagerInstance()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Another instance of GameManager already exists. Destroying this instance.");
                Destroy(gameObject);
                return false;
            }
            Instance = this;
            return true;
        }   

        /// <summary>
        /// Initializes runtime identifiers such as UnityGUID and reads session id from the URL.
        /// </summary>
        private void InitGameIDs()
        {
            unityGUID = System.Guid.NewGuid().ToString();
            Debug.Log("Unity GUID: " + unityGUID);


            SessionID = GetQueryParam("sessionId");
            Debug.Log("SessionId: " + SessionID);
        }

        /// <summary>
        /// Reads a query parameter value from the application's absolute URL.
        /// </summary>
        /// <param name="key">The query parameter name to extract.</param>
        /// <returns>The unescaped parameter value or null if not present.</returns>
        public static string GetQueryParam(string key)
        {
            string url = Application.absoluteURL;

            if (string.IsNullOrEmpty(url))
                return null;

            var uri = new System.Uri(url);
            var query = uri.Query.TrimStart('?').Split('&');

            foreach (var param in query)
            {
                var parts = param.Split('=');
                if (parts.Length == 2 && parts[0] == key)
                {
                    return Uri.UnescapeDataString(parts[1]);
                }
            }

            return null;
        }

        /// <summary>
        /// Initializes the game loop on Start and connects to remote services.
        /// </summary>
        private void Start()
        {
            try
            {
                State = GameState.LOADING;
                bInit = false;
                StartCoroutine(WaitAndConnect());
            }
            catch (Exception ex)
            {
                Debug.LogError("Error during GameManager Start: " + ex.Message);
            }
        }

        /// <summary>
        /// Waits until required clients are available then connects the websocket client.
        /// </summary>
        /// <returns>Coroutine enumerator</returns>
        private IEnumerator WaitAndConnect()
        {
            yield return new WaitUntil(() => webSocketClient != null && apiClient != null);

            // Convertir l'awaitable en coroutine-friendly
            var task = webSocketClient.ConnectWebSocket();
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.IsFaulted)
            {
                Debug.LogError("WebSocket connection failed: " + task.Exception);
                yield break;
            }
        }

        /// <summary>
        /// Notifies the GameManager that a grid has started and becomes the active grid.
        /// </summary>
        /// <param name="grid">The grid instance that started.</param>
        public void OnGridStarted(Grid grid)
        {
            if (grid == null) return;
            if (currentGrid != null)
                Debug.LogWarning("GameManager: Existing grid is being replaced!");
            currentGrid = grid;
            gridActionHandler.OnGridStarted(grid);
        }


        /// <summary>
        /// Registers a character when it is initialized in the scene.
        /// </summary>
        /// <param name="character">The character instance that started.</param>
        public void OnCharacterStarted(Character character)
        {
            charactersManager.AddCharacter(character);
        }

        /// <summary>
        /// Called when treasure is collected in the level. Updates local gold and optionally notifies the API.
        /// </summary>
        /// <param name="gold">Amount of gold collected.</param>
        public void OnTreasureCollected(int gold)
        {
            goldManager.AddGold(gold);

            if (apiClient != null && TestMode == false)
                StartCoroutine(apiClient.CollectTreasure(goldManager.PlayerGold));
        }

        /// <summary>
        /// Called when a thief reaches the exit and the player wins the level.
        /// </summary>
        public void OnThiefReachExit()
        {
            State = GameState.WIN;

            if (apiClient != null && TestMode == false)
            {
                int nextLevel = levelManager.SaveLevel();
                Debug.Log("OnThiefReachExit " + nextLevel);

                StartCoroutine(apiClient.ThiefReachedExit(nextLevel));                
            }
        }

        /// <summary>
        /// Called when a thief dies. If all thieves are dead, triggers lose state and notifies the API.
        /// </summary>
        public void OnThiefDied()
        {
            if (charactersManager.AreAllThievesDied())
            {
                State = GameState.LOSE;

                if (apiClient != null)
                {
                    StartCoroutine(apiClient.AllThievesDied());
                    Invoke("RestartLevel", 3.0f);
                }
            }
        }


        /// <summary>
        /// Called when the game starts. Sends a game-start event to the server.
        /// </summary>
        public void OnGameStart()
        {
            Debug.Log("OnGameStart Start");

            StartCoroutine(apiClient.GameStart());
            Debug.Log("OnGameStart End");
        }

        /// <summary>
        /// Restarts the current level using the level manager.
        /// </summary>
        public void RestartLevel()
        {
            levelManager.RestartLevel();
        }

        /// <summary>
        /// Initializes the character turn order when entering IN_GAME state.
        /// </summary>
        public void InitCharacterTurnIndex()
        {
            if (gameState != GameState.IN_GAME)
                return;

            turnManager.InitCharacterTurnIndex(charactersManager, playerController, aiController);
        }

        /// <summary>
        /// Advances the character turn index, used to pass control to the next entity.
        /// </summary>
        public void IncrementCharacterTurnIndex()
        {
            if (gameState != GameState.IN_GAME)
                return;

            turnManager.IncrementCharacterTurnIndex(charactersManager, playerController, aiController);
        }

        /// <summary>
        /// Called when a level GameObject has been loaded. Parents the level under the GameManager.
        /// </summary>
        /// <param name="level">Loaded level game object.</param>
        /// <returns>True if the level was attached; false if the argument was null.</returns>
        public bool OnLevelLoaded(GameObject level)
        {
            if (level == null)
                return false;

            level.transform.SetParent(gameObject.transform);
            return true;
        }


        /// <summary>
        /// Loads the specified level index through the level manager and plays an entrance animation.
        /// </summary>
        /// <param name="levelIndex">Index of the level to load.</param>
        public void LoadLevel(int levelIndex)
        {
            GameObject level = levelManager.LoadLevel(levelIndex);
            if(level !=null)
            {
                level.transform.DOMoveY(10, 1.0f)
                        .From()
                        .SetEase(Ease.OutBounce)
                        .SetLink(gameObject)
                        .OnComplete( () =>
                        {
                            OnLevelLoaded(level);
                            State = GameState.IN_GAME;
                        });
            }
        }

        /// <summary>
        /// Loads a random level through the level manager and plays an entrance animation.
        /// </summary>
        public void LoadRandomLevel()
        {
            GameObject level = levelManager.LoadRandomLevel();
            if (level != null)
            {
                level.transform.DOMoveY(10, 1.0f)
                        .From()
                        .SetEase(Ease.OutBounce)
                        .SetLink(gameObject)
                        .OnComplete(() =>
                        {
                            OnLevelLoaded(level);
                            State = GameState.IN_GAME;
                        });
            }
        }

    }

}
