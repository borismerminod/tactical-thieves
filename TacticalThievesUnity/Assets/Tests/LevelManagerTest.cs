using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using TacticalThieves;
using UnityEngine;
using UnityEngine.TestTools;

public class LevelManagerTest
{
    private GameObject levelManagerPrefab;
    private LevelManager levelManager;

    [SetUp]
    public void SetUp()
    {
        levelManagerPrefab = Resources.Load<GameObject>("Prefabs/LevelManager");
        levelManager = UnityEngine.Object.Instantiate(levelManagerPrefab).GetComponent<LevelManager>();
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.Destroy(levelManager);
    }

    [Test]
    public void LevelManagerTest_LevelManagerShouldBeCreated()
    {
        Assert.IsNotNull(levelManagerPrefab, "level manager prefab should be created");
        Assert.IsNotNull(levelManager, "level manager instance should be created");
    }

    [Test]
    public void LevelManagerTest_LevelManagerCanInstantiateLevel()
    {
        GameObject levelPrefab = Resources.Load<GameObject>("Prefabs/LevelTest");
        Assert.IsNotNull(levelPrefab, "level1 prefab should be loaded successfully.");

        GameObject level1 = UnityEngine.Object.Instantiate(levelPrefab);
        Assert.IsNotNull(level1, "level1 prefab should be loaded successfully.");

        GameObject level2 = UnityEngine.Object.Instantiate(levelPrefab);
        Assert.IsNotNull(level2, "level2 prefab should be loaded successfully.");

        GameObject gameManagerPrefab = Resources.Load<GameObject>("Prefabs/GameManager");
        Assert.IsNotNull(gameManagerPrefab, "GameObject prefab should be loaded successfully.");

        GameManager gameManager = UnityEngine.Object.Instantiate(gameManagerPrefab).GetComponent<GameManager>();
        gameManager.TestMode = true;

        levelManager.Levels = new GameObject[] { level1, level2 };


        Assert.IsFalse(levelManager.LoadLevel(1, null));

        int levelIndex = 50;
        Assert.IsFalse(levelManager.LoadLevel(levelIndex, gameManager));

        levelIndex = 2;
        Assert.IsFalse(levelManager.LoadLevel(levelIndex, gameManager));

        levelIndex = 0;
        Assert.IsTrue(levelManager.LoadLevel(levelIndex, gameManager));

        levelIndex = 1;
        Assert.IsTrue(levelManager.LoadLevel(levelIndex, gameManager));

    }


}
