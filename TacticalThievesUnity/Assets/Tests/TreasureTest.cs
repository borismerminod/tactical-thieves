using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using TacticalThieves;
using UnityEngine;
using UnityEngine.TestTools;

public class TreasureTest
{
    [UnityTest]
    public IEnumerator CollectTreasure_ShouldIncreasePlayerGold()
    {
        GameObject treasurePrefab = Resources.Load<GameObject>("Prefabs/Treasure");
        Assert.IsNotNull(treasurePrefab, "Treasure prefab should be loaded successfully.");
        Treasure treasure = UnityEngine.Object.Instantiate(treasurePrefab).GetComponent<Treasure>();
        Assert.IsNotNull(treasure, "Treasure component should be present on the instance.");

        GameObject gameManagerPrefab = Resources.Load<GameObject>("Prefabs/GameManager");
        Assert.IsNotNull(gameManagerPrefab, "GameManager prefab should be loaded successfully.");
        GameManager gameManager = UnityEngine.Object.Instantiate(gameManagerPrefab).GetComponent<GameManager>();
        Assert.IsNotNull(gameManager, "Treasure component should be present on the instance.");

        treasure.Gold = 100;
        Assert.AreEqual(100, treasure.Gold);

        treasure.Gold = -1;
        Assert.AreEqual(0, treasure.Gold);

        treasure.Gold = 200;
        Assert.AreEqual(200, treasure.Gold);

        treasure.Collect();
        Assert.AreEqual(treasure.gameObject.active, false);



        yield return null;
    }
}
