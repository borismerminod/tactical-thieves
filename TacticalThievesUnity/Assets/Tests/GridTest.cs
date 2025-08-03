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

        GameObject gridInstance = UnityEngine.Object.Instantiate(gridPrefab);
        Assert.IsNotNull(gridInstance, "Prefab instance is null");

        TacticalThieves.Grid grid = gridInstance.GetComponent<TacticalThieves.Grid>();
        Assert.IsNotNull(grid, "Grid component should be attached to the prefab instance.");

        Assert.AreEqual(gridInstance.tag, "Grid");

        Assert.AreEqual(gridInstance.transform.childCount, 16, "Grid should have at four children.");

        yield return null; // Wait for the next frame to ensure the prefab is loaded
        Dictionary<string, Tile> tiles = grid.Tiles;

        for (int i = 0; i < tilesCoords.GetLength(0); i++)
        {
           
           string tileKey = $"{tilesCoords[i, 0]}_{tilesCoords[i, 1]}";
           Assert.IsTrue(tiles.ContainsKey(tileKey), $"Tile with key {tileKey} should exist in the grid's tiles dictionary.");

            Tile tile = tiles[tileKey];

            Assert.IsNotNull(tile, $"Child {i} should have a Tile component.");

            Assert.AreEqual(tilesCoords[i, 0], tile.X, $"Tile {i} X coordinate should match.");
            Assert.AreEqual(tilesCoords[i, 1], tile.Y, $"Tile {i} Y coordinate should match.");
        }

        yield return null; // Wait for the next frame to ensure the prefab is loaded

        UnityEngine.Object.Destroy(gridInstance); // Clean up the instantiated prefab
    }
}
