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
        treasure.Collect();

        Assert.AreEqual(200, gameManager.PlayerGold);

        treasure.gameObject.SetActive(true);
        treasure.Gold = 100;
        treasure.Collect();

        Assert.AreEqual(300, gameManager.PlayerGold);

        yield return null; // attendre 1 frame
        UnityEngine.Object.Destroy(treasure);

    }
}
