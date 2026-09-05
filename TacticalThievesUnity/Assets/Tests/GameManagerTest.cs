using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using TacticalThieves;

/// <summary>
/// Tests des procédures publiques de <see cref="GameManager"/>.
/// </summary>
/// <remarks>
/// La prefab <c>Prefabs/GameManager</c> ne sérialise AUCUN de ses <c>[SerializeField]</c> :
/// sur une instance fraîche, tous les collaborateurs (<c>goldManager</c>, <c>charactersManager</c>,
/// <c>gridActionHandler</c>, <c>apiClient</c>...) sont <c>null</c>. On crée donc le
/// <see cref="GameManager"/> sur un GameObject neuf et on injecte par réflexion, dans chaque test,
/// les seuls collaborateurs nécessaires — même stratégie que <see cref="APIClientTest"/>.
///
/// Deux leviers rendent les tests synchrones (<c>[Test]</c>, jamais <c>[UnityTest]</c>) :
/// <list type="bullet">
/// <item><see cref="GameManager.TestMode"/> à <c>true</c> court-circuite les branches API de
/// <c>OnTreasureCollected</c> et <c>OnThiefReachExit</c>.</item>
/// <item><c>OnThiefDied</c> n'a pas de garde <c>TestMode</c> : on laisse <c>apiClient</c> à <c>null</c>
/// pour neutraliser le <c>StartCoroutine(...)</c> + <c>Invoke("RestartLevel", 3f)</c>.</item>
/// </list>
/// <c>Awake()</c> s'exécute dès l'ajout du composant en EditMode (pose <c>Instance</c> et l'UnityGUID) ;
/// <c>Start()</c> n'est PAS appelé → aucune déréférence du <c>webSocketClient</c> null.
/// </remarks>
public class GameManagerTest
{
    private const string SessionId = "test-session-id";

    private GameManager gameManager;
    private readonly List<GameObject> spawned = new List<GameObject>();

    [SetUp]
    public void Setup()
    {
        GameObject gameManagerObject = new GameObject("GameManagerUnderTest");
        spawned.Add(gameManagerObject);

        // AddComponent déclenche Awake() en EditMode : Instance + unityGUID sont initialisés.
        gameManager = gameManagerObject.AddComponent<GameManager>();
        Assert.IsNotNull(gameManager, "GameManager component should be present on the instance.");

        // TestMode court-circuite les appels réseau des méthodes concernées.
        gameManager.TestMode = true;
    }

    [TearDown]
    public void TearDown()
    {
        // DestroyImmediate car Object.Destroy est différé et peu fiable en EditMode.
        foreach (GameObject gameObject in spawned)
        {
            if (gameObject != null)
                UnityEngine.Object.DestroyImmediate(gameObject);
        }
        spawned.Clear();

        // Réinitialise le singleton statique pour éviter que l'Awake du test suivant ne se détruise.
        PropertyInfo instanceProp = typeof(GameManager).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        MethodInfo instanceSetter = instanceProp.GetSetMethod(true);
        instanceSetter.Invoke(null, new object[] { null });
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    /// <summary>Injecte une valeur dans un champ privé de <see cref="GameManager"/>.</summary>
    private void SetPrivateField(string name, object value)
    {
        FieldInfo field = typeof(GameManager).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, "Le champ privé '" + name + "' de GameManager doit exister.");
        field.SetValue(gameManager, value);
    }

    /// <summary>Pose le champ privé <c>gameState</c> sans passer par le setter privé <c>State</c>.</summary>
    private void SetGameState(GameManager.GameState state)
    {
        SetPrivateField("gameState", state);
    }

    /// <summary>Crée un GameObject portant un composant <typeparamref name="T"/>, suivi pour le nettoyage.</summary>
    private T AttachComponent<T>() where T : Component
    {
        GameObject host = new GameObject(typeof(T).Name + "UnderTest");
        spawned.Add(host);
        return host.AddComponent<T>();
    }

    /// <summary>
    /// Attache un <see cref="CharactersManager"/> et initialise sa liste interne (null via AddComponent,
    /// contrairement à la prefab) pour éviter une NPE dans <c>AddCharacter</c>.
    /// </summary>
    private CharactersManager MakeCharactersManager()
    {
        CharactersManager charactersManager = AttachComponent<CharactersManager>();

        FieldInfo charactersField = typeof(CharactersManager).GetField("characters", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(charactersField, "Le champ privé 'characters' de CharactersManager doit exister.");
        charactersField.SetValue(charactersManager, new List<Character>());

        return charactersManager;
    }

    /// <summary>Instancie un <see cref="Thief"/> depuis la prefab et lui fixe un statut par réflexion.</summary>
    private Thief MakeThief(Thief.eThiefStatus status)
    {
        GameObject thiefPrefab = Resources.Load<GameObject>("Prefabs/Thief");
        Assert.IsNotNull(thiefPrefab, "Thief prefab should be loaded successfully.");

        GameObject thiefInstance = UnityEngine.Object.Instantiate(thiefPrefab);
        spawned.Add(thiefInstance);
        Thief thief = thiefInstance.GetComponent<Thief>();
        Assert.IsNotNull(thief, "Thief component should be present on the instance.");

        FieldInfo statusField = typeof(Thief).GetField("status", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(statusField, "Le champ privé 'status' de Thief doit exister.");
        statusField.SetValue(thief, status);

        return thief;
    }

    // ---------------------------------------------------------------------
    // A. OnLevelLoaded (pur, aucun collaborateur requis)
    // ---------------------------------------------------------------------

    [Test]
    public void OnLevelLoaded_ReturnsFalseForNull()
    {
        bool attached = gameManager.OnLevelLoaded(null);

        Assert.IsFalse(attached);
    }

    [Test]
    public void OnLevelLoaded_ParentsLevelAndReturnsTrue()
    {
        GameObject level = new GameObject("Level");
        spawned.Add(level);

        bool attached = gameManager.OnLevelLoaded(level);

        Assert.IsTrue(attached);
        Assert.AreEqual(gameManager.transform, level.transform.parent);
    }

    // ---------------------------------------------------------------------
    // B. OnTreasureCollected (goldManager injecté, TestMode=true)
    // ---------------------------------------------------------------------

    [Test]
    public void OnTreasureCollected_AddsGold()
    {
        GoldManager goldManager = AttachComponent<GoldManager>();
        SetPrivateField("goldManager", goldManager);

        gameManager.OnTreasureCollected(200);

        Assert.AreEqual(200, gameManager.PlayerGoldManager.PlayerGold);
    }

    [Test]
    public void OnTreasureCollected_AccumulatesGold()
    {
        GoldManager goldManager = AttachComponent<GoldManager>();
        SetPrivateField("goldManager", goldManager);

        gameManager.OnTreasureCollected(200);
        gameManager.OnTreasureCollected(100);

        Assert.AreEqual(300, gameManager.PlayerGoldManager.PlayerGold);
    }

    [Test]
    public void OnTreasureCollected_ZeroLeavesGoldUnchanged()
    {
        GoldManager goldManager = AttachComponent<GoldManager>();
        SetPrivateField("goldManager", goldManager);

        gameManager.OnTreasureCollected(0);

        Assert.AreEqual(0, gameManager.PlayerGoldManager.PlayerGold);
    }

    // ---------------------------------------------------------------------
    // C. OnThiefReachExit (TestMode=true)
    // ---------------------------------------------------------------------

    [Test]
    public void OnThiefReachExit_TransitionsToWin()
    {
        gameManager.OnThiefReachExit();

        Assert.AreEqual(GameManager.GameState.WIN, gameManager.GetGameState());
    }

    // ---------------------------------------------------------------------
    // D. OnThiefDied (charactersManager injecté, apiClient == null)
    // ---------------------------------------------------------------------

    [Test]
    public void OnThiefDied_KeepsStateWhenAThiefIsStillAlive()
    {
        CharactersManager charactersManager = MakeCharactersManager();
        SetPrivateField("charactersManager", charactersManager);

        Thief aliveThief = MakeThief(Thief.eThiefStatus.Wait);
        gameManager.OnCharacterStarted(aliveThief);

        gameManager.OnThiefDied();

        Assert.AreNotEqual(GameManager.GameState.LOSE, gameManager.GetGameState());
    }

    [Test]
    public void OnThiefDied_TransitionsToLoseWhenAllThievesAreDead()
    {
        CharactersManager charactersManager = MakeCharactersManager();
        SetPrivateField("charactersManager", charactersManager);

        gameManager.OnCharacterStarted(MakeThief(Thief.eThiefStatus.Dead));
        gameManager.OnCharacterStarted(MakeThief(Thief.eThiefStatus.Dead));

        gameManager.OnThiefDied();

        Assert.AreEqual(GameManager.GameState.LOSE, gameManager.GetGameState());
    }

    [Test]
    public void OnThiefDied_TransitionsToLoseWhenNoThiefPresent()
    {
        // Liste vide => AreAllThievesDied() renvoie true (vérité vacue).
        CharactersManager charactersManager = MakeCharactersManager();
        SetPrivateField("charactersManager", charactersManager);

        gameManager.OnThiefDied();

        Assert.AreEqual(GameManager.GameState.LOSE, gameManager.GetGameState());
    }

    // ---------------------------------------------------------------------
    // E. OnGridStarted (gridActionHandler injecté)
    // ---------------------------------------------------------------------

    [Test]
    public void OnGridStarted_NullGridLeavesCurrentGridUnchanged()
    {
        GridActionHandler gridActionHandler = AttachComponent<GridActionHandler>();
        SetPrivateField("gridActionHandler", gridActionHandler);

        gameManager.OnGridStarted(null);

        Assert.IsNull(gameManager.CurrentGrid);
    }

    [Test]
    public void OnGridStarted_SetsCurrentGrid()
    {
        GridActionHandler gridActionHandler = AttachComponent<GridActionHandler>();
        SetPrivateField("gridActionHandler", gridActionHandler);

        TacticalThieves.Grid grid = AttachComponent<TacticalThieves.Grid>();

        gameManager.OnGridStarted(grid);

        Assert.AreSame(grid, gameManager.CurrentGrid);
    }

    [Test]
    public void OnGridStarted_ReplacingGridWarnsAndUpdatesReference()
    {
        GridActionHandler gridActionHandler = AttachComponent<GridActionHandler>();
        SetPrivateField("gridActionHandler", gridActionHandler);

        TacticalThieves.Grid firstGrid = AttachComponent<TacticalThieves.Grid>();
        TacticalThieves.Grid secondGrid = AttachComponent<TacticalThieves.Grid>();

        gameManager.OnGridStarted(firstGrid);

        // Le remplacement d'une grille existante logge un warning.
        LogAssert.Expect(LogType.Warning, new Regex("being replaced"));
        gameManager.OnGridStarted(secondGrid);

        Assert.AreSame(secondGrid, gameManager.CurrentGrid);
    }

    // ---------------------------------------------------------------------
    // F. OnCharacterStarted (charactersManager injecté)
    // ---------------------------------------------------------------------

    [Test]
    public void OnCharacterStarted_RegistersCharacter()
    {
        CharactersManager charactersManager = MakeCharactersManager();
        SetPrivateField("charactersManager", charactersManager);

        Thief thief = MakeThief(Thief.eThiefStatus.Wait);

        gameManager.OnCharacterStarted(thief);

        Assert.IsTrue(gameManager.CharactersManager.Characters.Contains(thief));
    }

    [Test]
    public void OnCharacterStarted_IgnoresNull()
    {
        CharactersManager charactersManager = MakeCharactersManager();
        SetPrivateField("charactersManager", charactersManager);

        gameManager.OnCharacterStarted(null);

        Assert.AreEqual(0, gameManager.CharactersManager.Characters.Count);
    }

    // ---------------------------------------------------------------------
    // G. InitCharacterTurnIndex (garde d'état)
    // ---------------------------------------------------------------------

    [Test]
    public void InitCharacterTurnIndex_DoesNothingWhenNotInGame()
    {
        // turnManager reste null : si la garde ne renvoyait pas tôt, on aurait une NPE.
        SetGameState(GameManager.GameState.LOADING);

        Assert.DoesNotThrow(() => gameManager.InitCharacterTurnIndex());
    }

    // ---------------------------------------------------------------------
    // H. IncrementCharacterTurnIndex (garde d'état)
    // ---------------------------------------------------------------------

    [Test]
    public void IncrementCharacterTurnIndex_DoesNothingWhenNotInGame()
    {
        SetGameState(GameManager.GameState.LOADING);

        Assert.DoesNotThrow(() => gameManager.IncrementCharacterTurnIndex());
    }

    // ---------------------------------------------------------------------
    // I. GetGameState
    // ---------------------------------------------------------------------

    [Test]
    public void GetGameState_DefaultsToLoading()
    {
        // Start() n'est pas appelé en EditMode : gameState garde la valeur par défaut de l'enum.
        Assert.AreEqual(GameManager.GameState.LOADING, gameManager.GetGameState());
    }

    // ---------------------------------------------------------------------
    // J. GetQueryParam (statique)
    // ---------------------------------------------------------------------

    [Test]
    public void GetQueryParam_ReturnsNullWhenNoUrl()
    {
        // En EditMode, Application.absoluteURL est vide => retour null.
        string value = GameManager.GetQueryParam("sessionId");

        Assert.IsNull(value);
    }

    // ---------------------------------------------------------------------
    // K. Accesseurs publics
    // ---------------------------------------------------------------------

    [Test]
    public void UnityGUID_IsSetByAwake()
    {
        Assert.IsFalse(string.IsNullOrEmpty(gameManager.UnityGUID));
    }

    [Test]
    public void TestMode_RoundTrips()
    {
        gameManager.TestMode = false;
        Assert.IsFalse(gameManager.TestMode);

        gameManager.TestMode = true;
        Assert.IsTrue(gameManager.TestMode);
    }

    [Test]
    public void CurrentGrid_RoundTrips()
    {
        TacticalThieves.Grid grid = AttachComponent<TacticalThieves.Grid>();

        gameManager.CurrentGrid = grid;

        Assert.AreSame(grid, gameManager.CurrentGrid);
    }

    [Test]
    public void Getters_ReflectInjectedCollaborators()
    {
        GoldManager goldManager = AttachComponent<GoldManager>();
        CharactersManager charactersManager = MakeCharactersManager();
        GridActionHandler gridActionHandler = AttachComponent<GridActionHandler>();
        PlayerController playerController = AttachComponent<PlayerController>();
        AudioManager audioManager = AttachComponent<AudioManager>();
        AppConfig config = ScriptableObject.CreateInstance<AppConfig>();

        SetPrivateField("goldManager", goldManager);
        SetPrivateField("charactersManager", charactersManager);
        SetPrivateField("gridActionHandler", gridActionHandler);
        SetPrivateField("playerController", playerController);
        SetPrivateField("audioManager", audioManager);
        SetPrivateField("config", config);
        SetPrivateField("sessionID", SessionId);

        Assert.AreSame(goldManager, gameManager.PlayerGoldManager);
        Assert.AreSame(charactersManager, gameManager.CharactersManager);
        Assert.AreSame(gridActionHandler, gameManager.GridActionHandler);
        Assert.AreSame(playerController, gameManager.CurrentPlayerController);
        Assert.AreSame(audioManager, gameManager.CurrentAudioManager);
        Assert.AreSame(config, gameManager.Config);
        Assert.AreEqual(SessionId, gameManager.SessionID);
    }

    // ---------------------------------------------------------------------
    // L. OnGameStart (smoke test)
    // ---------------------------------------------------------------------

    [Test]
    public void OnGameStart_DoesNotThrow()
    {
        // GameStart() est un itérateur (corps différé) ; StartCoroutine ne "tourne" pas en EditMode.
        // On vérifie seulement que l'appel ne lève pas d'exception.
        APIClient apiClient = AttachComponent<APIClient>();
        SetPrivateField("apiClient", apiClient);

        Assert.DoesNotThrow(() => gameManager.OnGameStart());
    }
}
