using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools;
using TacticalThieves;


public class TileTest
{
    private GameObject tilePrefab;
    private Tile tile;

    [SetUp]
    public void Setup()
    {
        tilePrefab = Resources.Load<GameObject>("Prefabs/Tile");
        tile = UnityEngine.Object.Instantiate(tilePrefab).GetComponent<Tile>();
    }

    [TearDown]
    public void Teardown()
    {
        UnityEngine.Object.Destroy(tile);
    }

    [Test] 
    public void TileTest_TileShouldBeCreated()
    {
        Assert.IsNotNull(tilePrefab, "Tile prefab should be instantiated");
        Assert.IsNotNull(tile, "Tile instance should be instantiated");
    }

    [TestCase(true, true)]
    [TestCase(false, false)]
    public void TileTest_TileCouldBeEnableForAttack(bool enableForAttack, bool expectedEnableForAttack)
    {
        tile.SetEnableForAttack(enableForAttack);
        Assert.AreEqual(tile.EnableForAttack, expectedEnableForAttack);
    }
}
