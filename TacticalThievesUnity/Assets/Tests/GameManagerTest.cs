using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools;
using TacticalThieves;

public class GameManagerTest
{
    private GameObject gameManagerPrefab;
    private GameManager gameManager;

    [SetUp]
    public void Setup()
    {
        gameManagerPrefab = Resources.Load<GameObject>("Prefabs/GameManager");
        gameManager = UnityEngine.Object.Instantiate(gameManagerPrefab).GetComponent<GameManager>();
        gameManager.TestMode = true;
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.Destroy(gameManager);
    }

    [UnityTest]
    public System.Collections.IEnumerator CollectingTreasureAddsGold()
    {
        GameObject treasurePrefab = Resources.Load<GameObject>("Prefabs/Treasure");
        Assert.IsNotNull(treasurePrefab, "Treasure prefab should be loaded successfully.");
        Treasure treasure = UnityEngine.Object.Instantiate(treasurePrefab).GetComponent<Treasure>();
        Assert.IsNotNull(treasure, "Treasure component should be present on the instance.");

        treasure.Gold = 200;
        treasure.Collect(gameManager);

        Assert.AreEqual(200, gameManager.PlayerGold);

        treasure.gameObject.SetActive(true);
        treasure.Gold = 100;
        treasure.Collect(gameManager);

        Assert.AreEqual(300, gameManager.PlayerGold);

        yield return null; // attendre 1 frame
        UnityEngine.Object.Destroy(treasure);

    }

    [UnityTest]
    public System.Collections.IEnumerator GameStateTransitionsToWinOnVictory()
    {
        Assert.AreEqual(GameManager.GameState.IN_GAME, gameManager.GetGameState(), "Initial game state should be IN_GAME.");
        gameManager.OnThiefReachExit();
        Assert.AreEqual(GameManager.GameState.WIN, gameManager.GetGameState(), "Game state should transition to WIN after victory.");
        yield return null; // attendre 1 frame
    }

    [Test]
    public void GameManagerTest_GameStateChangeWhenAllThievesDied()
    {
        Assert.AreEqual(GameManager.GameState.IN_GAME, gameManager.GetGameState(), "Initial game state should be IN_GAME.");

        GameObject thiefPrefab = Resources.Load<GameObject>("Prefabs/Thief");
        Assert.IsNotNull(thiefPrefab, "Thief prefab should be loaded successfully.");
        Thief thief = UnityEngine.Object.Instantiate(thiefPrefab).GetComponent<Thief>();
        Assert.IsNotNull(thief, "Thief component should be present on the instance.");
        Thief thief2 = UnityEngine.Object.Instantiate(thiefPrefab).GetComponent<Thief>();
        Assert.IsNotNull(thief2, "Thief component should be present on the instance.");
        thief.OnThiefStarted();
        thief2.OnThiefStarted();
        List<Character> thieves = new List<Character>();
        thieves.Add(thief);
        thieves.Add(thief2);

        //Case 1 : There's no dead thief
        bool bAllThievesAreDead = gameManager.OnThiefDied(thieves);
        Assert.IsFalse(bAllThievesAreDead);

        //Case 2 : Just one thief is dead 
        thief.OnThiefAttacked();
        bAllThievesAreDead = gameManager.OnThiefDied(thieves);
        Assert.IsFalse(bAllThievesAreDead);

        //Case 3 : All thieves are dead
        thief2.OnThiefAttacked();
        bAllThievesAreDead = gameManager.OnThiefDied(thieves);
        Assert.IsTrue(bAllThievesAreDead);
    }

    [Test]
    public void GameManagerTest_GameManagerShouldKnowCharacters()
    {
        Assert.AreEqual(GameManager.GameState.IN_GAME, gameManager.GetGameState(), "Initial game state should be IN_GAME.");

        GameObject thiefPrefab = Resources.Load<GameObject>("Prefabs/Thief");
        Assert.IsNotNull(thiefPrefab, "Thief prefab should be loaded successfully.");
        Thief thief = UnityEngine.Object.Instantiate(thiefPrefab).GetComponent<Thief>();
        Assert.IsNotNull(thief, "Thief component should be present on the instance.");
        Thief thief2 = UnityEngine.Object.Instantiate(thiefPrefab).GetComponent<Thief>();
        Assert.IsNotNull(thief2, "Thief component should be present on the instance.");

        GameObject monsterPrefab = Resources.Load<GameObject>("Prefabs/Monster");
        Assert.IsNotNull(monsterPrefab, "Monster prefab should be loaded successfully.");
        Monster monster= UnityEngine.Object.Instantiate(monsterPrefab).GetComponent<Monster>();
        Assert.IsNotNull(monster, "Monster component should be present on the instance.");
        Monster monster2 = UnityEngine.Object.Instantiate(monsterPrefab).GetComponent<Monster>();
        Assert.IsNotNull(monster2, "Monster component should be present on the instance.");

        gameManager.OnCharacterStarted(thief);
        gameManager.OnCharacterStarted(thief2);
        gameManager.OnCharacterStarted(monster);
        gameManager.OnCharacterStarted(monster2);

        Assert.AreEqual((Thief)gameManager.Characters[0], thief);
        Assert.AreEqual((Thief)gameManager.Characters[1], thief2);
        Assert.AreEqual((Monster)gameManager.Characters[2], monster);
        Assert.AreEqual((Monster)gameManager.Characters[3], monster2);
        
    }

    [TestCase(true)] 
    [TestCase(false)] 
    public void GameManagerTest_GameManagerSetCharacterTurn(bool expectedValue)
    {
        GameObject thiefPrefab = Resources.Load<GameObject>("Prefabs/Thief");
        Assert.IsNotNull(thiefPrefab, "Thief prefab should be loaded successfully.");
        Thief thief = UnityEngine.Object.Instantiate(thiefPrefab).GetComponent<Thief>();
        Assert.IsNotNull(thief, "Thief component should be present on the instance.");

        GameObject monsterPrefab = Resources.Load<GameObject>("Prefabs/Monster");
        Assert.IsNotNull(monsterPrefab, "Monster prefab should be loaded successfully.");
        Monster monster = UnityEngine.Object.Instantiate(monsterPrefab).GetComponent<Monster>();
        Assert.IsNotNull(monster, "Monster component should be present on the instance.");

        gameManager.SetCharacterTurn(thief, expectedValue);
        Assert.AreEqual(thief.IsYourTurn, expectedValue);

        gameManager.SetCharacterTurn(monster, expectedValue);
        Assert.AreEqual(monster.IsYourTurn, expectedValue);
    }

    [Test]
    public void GameManagerTest_UpdateCharacterTurnIndex()
    {
        Assert.AreEqual(GameManager.GameState.IN_GAME, gameManager.GetGameState(), "Initial game state should be IN_GAME.");

        GameObject thiefPrefab = Resources.Load<GameObject>("Prefabs/Thief");
        Assert.IsNotNull(thiefPrefab, "Thief prefab should be loaded successfully.");
        Thief thief = UnityEngine.Object.Instantiate(thiefPrefab).GetComponent<Thief>();
        Assert.IsNotNull(thief, "Thief component should be present on the instance.");
        Thief thief2 = UnityEngine.Object.Instantiate(thiefPrefab).GetComponent<Thief>();
        Assert.IsNotNull(thief2, "Thief component should be present on the instance.");

        GameObject monsterPrefab = Resources.Load<GameObject>("Prefabs/Monster");
        Assert.IsNotNull(monsterPrefab, "Monster prefab should be loaded successfully.");
        Monster monster = UnityEngine.Object.Instantiate(monsterPrefab).GetComponent<Monster>();
        Assert.IsNotNull(monster, "Monster component should be present on the instance.");
        Monster monster2 = UnityEngine.Object.Instantiate(monsterPrefab).GetComponent<Monster>();
        Assert.IsNotNull(monster2, "Monster component should be present on the instance.");

        GameObject playerControllerPrefab = Resources.Load<GameObject>("Prefabs/PlayerController");
        Assert.IsNotNull(playerControllerPrefab, "playerControllerPrefab prefab should be present on the instance.");
        PlayerController playerController = GameObject.Instantiate(playerControllerPrefab).GetComponent<PlayerController>();
        Assert.IsNotNull(playerController, "playerController component should be present on the instance.");

        GameObject aiControllerPrefab = Resources.Load<GameObject>("Prefabs/AIController");
        Assert.IsNotNull(aiControllerPrefab, "aiControllerPrefab prefab should be present on the instance.");
        AIController aIController = GameObject.Instantiate(aiControllerPrefab).GetComponent<AIController>();
        Assert.IsNotNull(aIController, "aiControllerPrefab component should be present on the instance.");

        GameObject gridPrefab = Resources.Load<GameObject>("Prefabs/GridTest");
        Assert.IsNotNull(gridPrefab, "gridPrefab prefab should be present on the instance.");
        TacticalThieves.Grid grid = GameObject.Instantiate(gridPrefab).GetComponent<TacticalThieves.Grid>();
        Assert.IsNotNull(grid, "grid component should be present on the instance.");

        grid.InitTilesDictionnary();

        playerController.OnGridStarted(grid);

        gameManager.OnPlayerControllerStarted(playerController);
        gameManager.OnAIControllerStarted(aIController);
        gameManager.OnCharacterStarted(thief);
        gameManager.OnCharacterStarted(thief2);
        gameManager.OnCharacterStarted(monster);
        gameManager.OnCharacterStarted(monster2);
        gameManager.InitCharacterTurnIndex();

        Assert.AreEqual(0, gameManager.CharacterTurnIndex);

        gameManager.IncrementCharacterTurnIndex();
        Assert.AreEqual(1, gameManager.CharacterTurnIndex);

        gameManager.IncrementCharacterTurnIndex();
        Assert.AreEqual(2, gameManager.CharacterTurnIndex);

        gameManager.IncrementCharacterTurnIndex();
        Assert.AreEqual(3, gameManager.CharacterTurnIndex);

        gameManager.IncrementCharacterTurnIndex();
        Assert.AreEqual(0, gameManager.CharacterTurnIndex);
    }

    [Test] 
    public void GameManagerTest_OnLevelLoadedTest()
    {
        GameObject levelPrefab = Resources.Load<GameObject>("Prefabs/LevelTest");
        Assert.IsNotNull(levelPrefab, "levelPrefab prefab should be loaded successfully.");

        GameObject level = GameObject.Instantiate(levelPrefab);
        Assert.IsNotNull(level, "level should be loaded successfully.");

        Assert.IsTrue(gameManager.OnLevelLoaded(level));

        Assert.IsFalse(gameManager.OnLevelLoaded(null));
    }


}
