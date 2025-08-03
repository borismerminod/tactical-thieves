using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using TacticalThieves;
using JetBrains.Annotations;

public class GridTest
{
    [UnityTest]
    public IEnumerator GridTest_GridCreation()
    {

        int[,] tilesCoords = {
            { 1, 1 },
            { 1, 2 },
            { 2, 1 },
            { 2, 2 }
        };

        GameObject gridPrefab = Resources.Load<GameObject>("Prefabs/GridTest");
        Assert.IsNotNull(gridPrefab, "Grid prefab should be loaded successfully.");

        GameObject gridInstance = Object.Instantiate(gridPrefab);
        Assert.IsNotNull(gridInstance, "Prefab instance is null");

        Assert.AreEqual(gridInstance.tag, "Grid");

        Assert.AreEqual(gridInstance.transform.childCount, 4, "Grid should have at four children.");

        for (int i = 0; i < gridInstance.transform.childCount; i++)
        {
            Tile tile = gridInstance.transform.GetChild(i).gameObject.GetComponent<TacticalThieves.Tile>();

            Assert.IsNotNull(tile, $"Child {i} should have a Tile component.");

            Assert.AreEqual(tilesCoords[i, 0], tile.X, $"Tile {i} X coordinate should match.");
            Assert.AreEqual(tilesCoords[i, 1], tile.Y, $"Tile {i} Y coordinate should match.");
        }

        yield return null; // Wait for the next frame to ensure the prefab is loaded

        Object.Destroy(gridInstance); // Clean up the instantiated prefab
    }
}
