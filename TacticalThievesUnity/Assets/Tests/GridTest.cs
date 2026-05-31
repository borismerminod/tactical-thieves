using JetBrains.Annotations;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TacticalThieves;
using UnityEngine;
using UnityEngine.TestTools;

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

        Assert.AreEqual(gridInstance.transform.childCount, 16, "Grid should have at sixteen children.");

        grid.InitTilesDictionnary();

        // yield return null; // Wait for the next frame to ensure the prefab is loaded
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

    [UnityTest]
    public IEnumerator GridTest_GridShouldEnableTilesForMonsterAttack()
    {

        string[] enabledTiles = { "2_3", "4_3", "3_2", "3_3", "3_4" };

        GameObject gridPrefab = Resources.Load<GameObject>("Prefabs/GridTest");
        Assert.IsNotNull(gridPrefab, "Grid prefab should be loaded successfully.");

        GameObject gridInstance = UnityEngine.Object.Instantiate(gridPrefab);
        Assert.IsNotNull(gridInstance, "Prefab instance is null");

        TacticalThieves.Grid grid = gridInstance.GetComponent<TacticalThieves.Grid>();
        Assert.IsNotNull(grid, "Grid component should be attached to the prefab instance.");

        GameObject monsterPrefab = Resources.Load<GameObject>("Prefabs/Monster");
        Assert.IsNotNull(gridPrefab, "monster prefab should be loaded successfully.");

        Monster monster = UnityEngine.Object.Instantiate(monsterPrefab).GetComponent<Monster>();
        Assert.IsNotNull(grid, "monster component should be attached to the prefab instance.");

        monster.AttackRange = 1;

        grid.InitTilesDictionnary();

        monster.X = 3;
        monster.Y = 3;


        //grid.OnMonsterAttackEnable(monster);

        Dictionary<string, Tile> tiles = grid.Tiles;

        for (int i = 1; i < 5; i++)
        {
            for (int j = 1; j < 5; j++)
            {
                string tileKey = i + "_" + j;
                Debug.Log(tiles[tileKey]);
                if (enabledTiles.Contains(tileKey))
                {
                    Assert.IsTrue(tiles[tileKey].EnableForAttack);
                }
                else
                {
                    Assert.IsFalse(tiles[tileKey].EnableForAttack);
                }
            }
        }


        yield return null;
        UnityEngine.Object.Destroy(gridInstance);
        UnityEngine.Object.Destroy(grid);
        UnityEngine.Object.Destroy(monster);
    }

    [UnityTest]
    public IEnumerator GridTest_GridShouldEnableTilesForMove()
    {
        string[] enabledTiles = { "2_3", "4_3", "3_2", "3_3", "3_4" };

        GameObject gridPrefab = Resources.Load<GameObject>("Prefabs/GridTest");
        Assert.IsNotNull(gridPrefab, "Grid prefab should be loaded successfully.");

        GameObject gridInstance = UnityEngine.Object.Instantiate(gridPrefab);
        Assert.IsNotNull(gridInstance, "Prefab instance is null");

        TacticalThieves.Grid grid = gridInstance.GetComponent<TacticalThieves.Grid>();
        Assert.IsNotNull(grid, "Grid component should be attached to the prefab instance.");

        GameObject monsterPrefab = Resources.Load<GameObject>("Prefabs/Monster");
        Assert.IsNotNull(gridPrefab, "monster prefab should be loaded successfully.");

        Monster monster = UnityEngine.Object.Instantiate(monsterPrefab).GetComponent<Monster>();
        Assert.IsNotNull(grid, "monster component should be attached to the prefab instance.");

        monster.MoveRange = 1;

        grid.InitTilesDictionnary();

        monster.X = 3;
        monster.Y = 3;

        //grid.OnMonsterMoveEnable(monster);

        Dictionary<string, Tile> tiles = grid.Tiles;

        for (int i = 1; i < 5; i++)
        {
            for (int j = 1; j < 5; j++)
            {
                string tileKey = i + "_" + j;
                Debug.Log(tiles[tileKey]);
                if (enabledTiles.Contains(tileKey))
                {
                    Assert.IsTrue(tiles[tileKey].EnableForMove);
                }
                else
                {
                    Assert.IsFalse(tiles[tileKey].EnableForMove);
                }
            }
        }

        yield return null;
        UnityEngine.Object.Destroy(gridInstance);
        UnityEngine.Object.Destroy(grid);
        UnityEngine.Object.Destroy(monster);
    }

    [TestCase(1, 1, 4, 4)]
    [TestCase(-2, -2, 4, 4)]
    [TestCase(1, 1, 1, 1)]
    [TestCase(4, 4, 4, 4)]
    public void GridTest_GetARandomTileLocation(int xMin, int yMin, int xMax, int yMax)
    {
        GameObject gridPrefab = Resources.Load<GameObject>("Prefabs/GridTest");
        Assert.IsNotNull(gridPrefab, "Grid prefab should be loaded successfully.");

        GameObject gridInstance = UnityEngine.Object.Instantiate(gridPrefab);
        Assert.IsNotNull(gridInstance, "Prefab instance is null");

        TacticalThieves.Grid grid = gridInstance.GetComponent<TacticalThieves.Grid>();
        Assert.IsNotNull(grid, "Grid component should be attached to the prefab instance.");

        Vector2 randomTileLocation = grid.GetRandomTileLocation(xMin, xMax, yMin, yMax);

        Assert.GreaterOrEqual(randomTileLocation.x, xMin);
        Assert.GreaterOrEqual(randomTileLocation.y, yMin);
        Assert.LessOrEqual(randomTileLocation.x, xMax);
        Assert.LessOrEqual(randomTileLocation.y, yMax);
    }
}
