using System.Reflection;
using NUnit.Framework;
using TacticalThieves;
using UnityEngine;

public class ExitTest
{
    private GameManager gameManager;
    private Exit exit;

    [SetUp]
    public void Setup()
    {
        // GameManager depuis la prefab : son Awake initialise le singleton Instance.
        GameObject gameManagerPrefab = Resources.Load<GameObject>("Prefabs/GameManager");
        Assert.IsNotNull(gameManagerPrefab, "GameManager prefab should be loaded successfully.");

        GameObject gameManagerInstance = UnityEngine.Object.Instantiate(gameManagerPrefab);
        gameManager = gameManagerInstance.GetComponent<GameManager>();
        Assert.IsNotNull(gameManager, "GameManager component should be present on the instance.");

        // TestMode neutralise la branche reseau (apiClient) dans GameManager.OnThiefReachExit.
        gameManager.TestMode = true;

        // Exit : le champ model n'est pas lu par OnThiefReachExit, un simple AddComponent suffit.
        GameObject exitObject = new GameObject("ExitUnderTest");
        exit = exitObject.AddComponent<Exit>();
        Assert.IsNotNull(exit, "Exit component should be present on the instance.");
    }

    [TearDown]
    public void TearDown()
    {
        // DestroyImmediate est fiable en EditMode (contrairement a Object.Destroy, differe).
        if (exit != null)
            UnityEngine.Object.DestroyImmediate(exit.gameObject);

        if (gameManager != null)
            UnityEngine.Object.DestroyImmediate(gameManager.gameObject);
    }

    /// <summary>
    /// Force l'etat du GameManager a IN_GAME en ecrivant directement le champ prive serialise
    /// <c>gameState</c>. On evite volontairement le setter public <c>State</c> qui declencherait
    /// InitCharacterTurnIndex (dependances turnManager/personnages absentes en test).
    /// </summary>
    private void ForceInGameState(GameManager manager)
    {
        FieldInfo gameStateField = typeof(GameManager).GetField("gameState",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(gameStateField, "Private field 'gameState' should exist on GameManager.");
        gameStateField.SetValue(manager, GameManager.GameState.IN_GAME);
    }

    [Test]
    public void OnThiefReachExit_ShouldReturnFalse_WhenGuardsFail()
    {
        // A1 : GameManager null -> false.
        bool bNullResult = exit.OnThiefReachExit(null);
        Assert.IsFalse(bNullResult, "Exit should not process a null GameManager.");

        // A4 : etat different de IN_GAME -> false.
        // L'etat initial de la prefab (LOADING) n'est deja pas IN_GAME, on le verifie explicitement.
        Assert.AreNotEqual(GameManager.GameState.IN_GAME, gameManager.GetGameState(), "Precondition: game state should not be IN_GAME.");

        bool bWrongStateResult = exit.OnThiefReachExit(gameManager);
        Assert.IsFalse(bWrongStateResult, "Exit should not process the thief when game state is not IN_GAME.");
    }

    [Test]
    public void OnThiefReachExit_ShouldTriggerVictory_WhenInGame()
    {
        // On force l'etat a IN_GAME (aucun chemin public synchrone ne le permet).
        ForceInGameState(gameManager);
        Assert.AreEqual(GameManager.GameState.IN_GAME, gameManager.GetGameState(), "Precondition: game state should be IN_GAME.");

        // A2 + A5 : chemin nominal, audio absent (CurrentAudioManager null) -> pas d'exception, retourne true.
        bool bSuccess = exit.OnThiefReachExit(gameManager);
        Assert.IsTrue(bSuccess, "Exit should successfully process the thief reaching it while IN_GAME.");

        // A3 : effet observable, l'etat passe a WIN.
        Assert.AreEqual(GameManager.GameState.WIN, gameManager.GetGameState(), "Game state should transition to WIN after a thief reaches the exit.");
    }
}
