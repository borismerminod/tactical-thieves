using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using TacticalThieves;

public class ThiefTest
{
    [UnityTest]
    public IEnumerator ThiefTest_ThiefCreation()
    {
        GameObject thiefPrefab = Resources.Load<GameObject>("Prefabs/Thief");
        Assert.IsNotNull(thiefPrefab, "Thief prefab should be loaded successfully.");
        Thief thief = UnityEngine.Object.Instantiate(thiefPrefab).GetComponent<Thief>();
        Assert.IsNotNull(thief, "Thief component should be present on the instance.");

        Assert.AreEqual(thiefPrefab.tag, "Thief", "Thief prefab should have the correct tag.");
        Assert.AreEqual(thief.X, 1);
        Assert.AreEqual(thief.Y, 1);

        yield return null; // Wait for the next frame to ensure the prefab is loaded
        UnityEngine.Object.Destroy(thief);
    }

    [UnityTest]
    public IEnumerator ThiefTest_ThiefMovementEnabled()
    {

        bool[,] AreTilesEnableForMove =
        {
            { false, false, false, false, false, true, false, false, false, false, false, false, false, false, false, false },
            { false, true, false, false, true, true, true, false, false, true, false, false, false, false, false, false },
            { true, true, true, false, true, true, true, true, true, true, true, false, false, true, false, false },
            { true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true }
        };

        int thiefPosX = 2;
        int thiefPosY = 2;

        int[,] tileCoords =
       {
            { 1,  1 },
           {1, 2 },
            { 1,  3 },
            { 1,  4 },
             { 2, 1 },
            { 2, 2 },
             {  2, 3 },
            {  2, 4 },
             {  3, 1 },
             {  3, 2 },
             {  3,  3 },
            { 3,  4 },
             {  4,  1 },
             {  4, 2 },
            {  4, 3 },
            {  4,  4 }
        };

        for (int i = 0; i < AreTilesEnableForMove.GetLength(0); i++)
        {
            int thiefMoveRange = i;
            GameObject thiefPrefab = Resources.Load<GameObject>("Prefabs/Thief");
            Assert.IsNotNull(thiefPrefab, "Thief prefab should be loaded successfully.");
            Thief thief = UnityEngine.Object.Instantiate(thiefPrefab).GetComponent<Thief>();
            Assert.IsNotNull(thief, "Thief component should be present on the instance.");

            thief.MoveRange = thiefMoveRange;
            Assert.AreEqual(thief.MoveRange, thiefMoveRange);

            GameObject gridPrefab = Resources.Load<GameObject>("Prefabs/GridTest");
            Assert.IsNotNull(gridPrefab, "Grid prefab should be loaded successfully.");
            TacticalThieves.Grid grid = UnityEngine.Object.Instantiate(gridPrefab).GetComponent<TacticalThieves.Grid>();

            thief.X = thiefPosX;
            thief.Y = thiefPosY;

            grid.InitTilesDictionnary();
            thief.EnableMove(true, grid);

            Assert.IsTrue(thief.MovementEnable);
            Dictionary<string, Tile> tiles = grid.Tiles;
            for (int j = 0; j < tileCoords.GetLength(0); j++)
            {
                Assert.IsTrue(tiles.ContainsKey($"{tileCoords[j, 0]}_{tileCoords[j, 1]}"), $"Tile with key {tileCoords[j, 0]}_{tileCoords[j, 1]} should exist in the grid's tiles dictionary.");

                string tileKey = $"{tileCoords[j, 0]}_{tileCoords[j, 1]}";
                Assert.IsTrue(tiles.ContainsKey(tileKey), $"Tile with key {tileKey} should exist in the grid's tiles dictionary.");

                Tile tile = tiles[tileKey];
                Assert.IsNotNull(tile, $"Tile at {tileKey} should not be null.");

                //Debug.Log("Adding tile at position: " + tileKey + " " + j + " " + i + " " + tile + " " + thief);
                Assert.AreEqual(AreTilesEnableForMove[i, j], tile.EnableForMove);
            }

            thief.EnableMove(false, grid);
            for (int j = 0; j < tileCoords.GetLength(0); j++)
            {
                Assert.IsTrue(tiles.ContainsKey($"{tileCoords[j, 0]}_{tileCoords[j, 1]}"), $"Tile with key {tileCoords[j, 0]}_{tileCoords[j, 1]} should exist in the grid's tiles dictionary.");

                string tileKey = $"{tileCoords[j, 0]}_{tileCoords[j, 1]}";
                Assert.IsTrue(tiles.ContainsKey(tileKey), $"Tile with key {tileKey} should exist in the grid's tiles dictionary.");

                Tile tile = tiles[tileKey];
                Assert.IsNotNull(tile, $"Tile at {tileKey} should not be null.");

                //Debug.Log("Adding tile at position: " + tileKey + " " + j + " " + i + " " + tile + " " + thief);
                Assert.IsFalse(tile.EnableForMove);
            }


            yield return null; // Wait for the next frame to ensure the prefab is loaded
            UnityEngine.Object.Destroy(thief);
            UnityEngine.Object.Destroy(grid);
        }
    }

}
