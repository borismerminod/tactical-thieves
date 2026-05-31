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

    


}
