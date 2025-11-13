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
        List<Thief> thieves = new List<Thief>();
        thieves.Add(thief);
        thieves.Add(thief2);

        //Case 1 : There's no dead thief
        bool bAllThievesAreDead = gameManager.OnThiefDied(thieves);
        Assert.IsFalse(bAllThievesAreDead);

        //Case 2 : Just one thief is dead 
        thieves[0].OnThiefAttacked();
        bAllThievesAreDead = gameManager.OnThiefDied(thieves);
        Assert.IsFalse(bAllThievesAreDead);

        //Case 3 : All thieves are dead
        thieves[1].OnThiefAttacked();
        bAllThievesAreDead = gameManager.OnThiefDied(thieves);
        Assert.IsTrue(bAllThievesAreDead);


    }
}
