using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using TacticalThieves;

public class ExitTest
{
    [UnityTest]
    public IEnumerator ExitReachedByThief_ShouldTriggerVictory()
    {
        bool bSuccess;

        GameObject exitPrefab = Resources.Load<GameObject>("Prefabs/Exit");
        Assert.IsNotNull(exitPrefab, "Exit prefab should be loaded successfully.");
        Exit exit = GameObject.Instantiate(exitPrefab).GetComponent<Exit>();
        Assert.IsNotNull(exit, "Exit component should be present on the instance.");

        GameObject gameManagerPrefab = Resources.Load<GameObject>("Prefabs/GameManager");
        Assert.IsNotNull(gameManagerPrefab, "GameManager prefab should be loaded successfully.");
        GameManager gameManager = GameObject.Instantiate(gameManagerPrefab).GetComponent<GameManager>();
        Assert.IsNotNull(gameManager, "GameManager component should be present on the instance.");

        bSuccess = exit.OnThiefReachExit(null);
        Assert.IsFalse(bSuccess, "Exit should not process null GameManager.");

        bSuccess =  exit.OnThiefReachExit(gameManager);
        Assert.IsTrue(bSuccess, "Exit should successfully process the thief reaching it.");

        yield return null;
        GameObject.Destroy(exit.gameObject);
        GameObject.Destroy(gameManager.gameObject);
    }
}
