//using GluonGui.WorkspaceWindow.Views.WorkspaceExplorer.Explorer;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

//using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.UI.CanvasScaler;

namespace TacticalThieves
{
    public class GameManager : MonoBehaviour
    {
        public enum GameState
        {
            LOADING,
            IN_GAME,
            WIN,
            LOSE
        }

        public static GameManager Instance { get; private set; }

        [SerializeField] private int playerGold;
        [SerializeField] private WebSocketClient webSocketClient;
        [SerializeField] private APIClient apiClient;
        [SerializeField] private GameState gameState;
        [SerializeField] private Grid currentGrid;
        [SerializeField] private bool testMode;
        [SerializeField] private List<Character> characters;
        [SerializeField] private int characterTurnIndex;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private AIController aiController;
        [SerializeField] private LevelManager levelManager;
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private bool bInit;
        [SerializeField] private bool bGameStarted;


        public Grid CurrentGrid { get => currentGrid; set => currentGrid = value; }

        public bool TestMode { get => testMode; set => testMode = value; }

        public List<Character> Characters { get => characters; private set => characters = value; }

        public AudioManager CurrentAudioManager { get => audioManager; private set => audioManager = value; }


        public GameState GetGameState() => gameState;

        public int PlayerGold
        {
            get => playerGold;
            private set
            {
                playerGold = value;
                if (playerGold < 0)
                    playerGold = 0;
            }
        }

        public int CharacterTurnIndex { get => characterTurnIndex; set => characterTurnIndex = value; }

        public PlayerController CurrentPlayerController { get => playerController; private set => playerController = value; }

        private void Awake()
        {
            Instance = this;
        }

        // Start is called before the first frame update
        void Start()
        {
            gameState = GameState.LOADING;
            bInit = false;
            bGameStarted = false;

            Invoke("InitCharacterTurnIndex", 1.0f);
        }

        public void OnWebSocketClientStarted(WebSocketClient client)
        {
            webSocketClient = client;
        }

        public void OnPlayerControllerStarted(PlayerController controller)
        {
            playerController = controller;
        }

        public void OnAIControllerStarted(AIController controller)
        {
            aiController = controller;
        }

        public void OnAPIClientStarted(APIClient client)
        {
            apiClient = client;
            //OnGameStart();
        }

        public void OnGridStarted(Grid grid)
        {
            currentGrid = grid;
        }


        public void OnCharacterStarted(Character character)
        {
            characters.Add(character);
        }

        // Update is called once per frame
        async void Update()
        {
            if (bGameStarted == false && webSocketClient != null && apiClient != null)
            {
                await webSocketClient.ConnectWebSocket();
                bGameStarted = true;
            }

            if (bInit == false && gameState == GameState.IN_GAME)
            {
                InitCharacterTurnIndex();
                bInit = true;
            }
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

            if (apiClient != null && TestMode == false)
            {
                int nextLevel = levelManager.SaveLevel();
                Debug.Log("OnThiefReachExit " + nextLevel);

                StartCoroutine(apiClient.ThiefReachedExit(nextLevel));
                
                //Invoke("RestartLevel", 3.0f);
            }
        }


        public bool OnThiefDied(List<Character> characterList)
        {
            bool AllThievesAreDead = true;

            foreach (Character character in characterList)
            {
                Thief thief = character as Thief;
                if (thief == null) continue;
                if (thief.Status != Thief.eThiefStatus.Dead)
                {
                    AllThievesAreDead = false;
                    break;
                }
            }

            return AllThievesAreDead;

        }

        public void OnThiefDied()
        {
            if (OnThiefDied(characters))
            {
                gameState = GameState.LOSE;

                if (apiClient != null)
                {
                    StartCoroutine(apiClient.AllThievesDied());
                    Invoke("RestartLevel", 3.0f);
                }
            }
        }

        public void SetCharacterTurn(Character character, bool isYourTurn)
        {
            character.IsYourTurn = isYourTurn;
        }

        public void OnGameStart()
        {
            Debug.Log("OnGameStart Start");

            StartCoroutine(apiClient.GameStart());
            Debug.Log("OnGameStart End");
        }

        public void RestartLevel()
        {
            levelManager.RestartLevel();
        }

        public void InitCharacterTurnIndex()
        {
            if (gameState != GameState.IN_GAME)
                return;

            characterTurnIndex = 0;

            Character character = characters[characterTurnIndex];
            Thief thief = character as Thief;
            if (thief != null)
            {
                playerController.OnThiefSelected(thief, true);
                return;
            }

            Monster monster = character as Monster;
            if (monster != null)
            {
                aiController.OnMonsterSelected(monster);
            }

        }

        public void IncrementCharacterTurnIndex()
        {
            if (gameState != GameState.IN_GAME)
                return;
            characterTurnIndex++;
            if (characterTurnIndex >= characters.Count)
                characterTurnIndex = 0;
            playerController.OnThiefSelected(null, true);
            aiController.OnMonsterSelected(null);
            //Debug.Log(characterTurnIndex + " "+ characters.Count);
            Character character = characters[characterTurnIndex];
            Thief thief = character as Thief;
            if (thief != null)
            {
                playerController.OnThiefSelected(thief, true);
                return;
            }

            Monster monster = character as Monster;
            if (monster != null)
            {
                aiController.OnMonsterSelected(monster);
            }
        }

        public bool OnLevelLoaded(GameObject level)
        {
            if (level == null)
                return false;

            level.transform.SetParent(gameObject.transform);
            return true;
        }

        public void OnLevelManagerStarted(LevelManager levelManager)
        {
            this.levelManager = levelManager;
        }

        public void OnAudioManagerStarted(AudioManager audioManager)
        {
            CurrentAudioManager = audioManager;
        }

        // Remplace l'ancienne version incorrecte : renvoie Task<int> et await LoadLevelAsync
        public async Task<int> GetCurrentLevelAsync()
        {
            if (apiClient == null)
                return -1;

            try
            {
                int level = await apiClient.LoadLevelAsync("userTest");
                Debug.Log($"LoadLevel async -> niveau: {level}");
                return level;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"LoadLevel erreur: {ex.Message}");
                return -1;
            }
        }

        public async Task SaveNextLevelAsync(int nextLevelIndex)
        {
            if (apiClient == null)
                return;
            try
            {
                await apiClient.SaveLevelAsync("userTest", nextLevelIndex);
                Debug.Log($"SaveLevel async -> niveau sauvegardé: {nextLevelIndex}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"SaveLevel erreur: {ex.Message}");
            }


        }

        public bool IsAPIClientStarted()
        {
            return apiClient != null;
        }

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
                            gameState = GameState.IN_GAME;
                        });
            }
        }

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
                            gameState = GameState.IN_GAME;
                        });
            }
        }

    }

}
