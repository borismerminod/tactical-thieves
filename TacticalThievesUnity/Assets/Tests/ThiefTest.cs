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
        Assert.AreEqual(thief.X, 2);
        Assert.AreEqual(thief.Y, 2);

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
            { true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false }
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
            thief.MoveTest = true;
            Assert.AreEqual(thief.MoveRange, thiefMoveRange);

            GameObject gridPrefab = Resources.Load<GameObject>("Prefabs/GridTest");
            Assert.IsNotNull(gridPrefab, "Grid prefab should be loaded successfully.");
            TacticalThieves.Grid grid = UnityEngine.Object.Instantiate(gridPrefab).GetComponent<TacticalThieves.Grid>();

            thief.X = thiefPosX;
            thief.Y = thiefPosY;
            grid.TestMode = true;
            grid.InitTilesDictionnary();
            thief.EnableMove(true, grid);

            Assert.AreEqual(thief.Status, Thief.eThiefStatus.MovementEnable);
            Dictionary<string, Tile> tiles = grid.Tiles;
            for (int j = 0; j < tileCoords.GetLength(0); j++)
            {
                Assert.IsTrue(tiles.ContainsKey($"{tileCoords[j, 0]}_{tileCoords[j, 1]}"), $"Tile with key {tileCoords[j, 0]}_{tileCoords[j, 1]} should exist in the grid's tiles dictionary.");

                string tileKey = $"{tileCoords[j, 0]}_{tileCoords[j, 1]}";
                Assert.IsTrue(tiles.ContainsKey(tileKey), $"Tile with key {tileKey} should exist in the grid's tiles dictionary.");

                Tile tile = tiles[tileKey];
                Assert.IsNotNull(tile, $"Tile at {tileKey} should not be null.");

                Debug.Log("Adding tile at position: " + tileKey + " " + j + " " + i + " " + tile + " " + thief);
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

    [UnityTest]
    public IEnumerator ThiefTest_MovementEnabledWithUnWalkableTiles()
    {
        GameObject thiefPrefab = Resources.Load<GameObject>("Prefabs/Thief");
        Assert.IsNotNull(thiefPrefab, "Thief prefab should be loaded successfully.");
        Thief thief = UnityEngine.Object.Instantiate(thiefPrefab).GetComponent<Thief>();
        Assert.IsNotNull(thief, "Thief component should be present on the instance.");
        thief.MoveRange = 4;
        thief.MoveTest = true;

        GameObject gridPrefab = Resources.Load<GameObject>("Prefabs/GridTest");
        Assert.IsNotNull(gridPrefab, "Grid prefab should be loaded successfully.");
        TacticalThieves.Grid grid = UnityEngine.Object.Instantiate(gridPrefab).GetComponent<TacticalThieves.Grid>();
        thief.X = 2;
        thief.Y = 2;
        grid.TestMode = true;
        grid.InitTilesDictionnary();
        Dictionary<string, Tile> tiles = grid.Tiles;
        string tileKeyNotWalkable = "3_2";
        Assert.IsTrue(tiles.ContainsKey(tileKeyNotWalkable));
        tiles[tileKeyNotWalkable].Walkable = false;

        thief.EnableMove(true, grid);
        Assert.AreEqual(thief.Status, Thief.eThiefStatus.MovementEnable);

        for(int i=1; i<= grid.Width; i++)
        {
            for(int j=1; j<= grid.Height; j++)
            {
                string tileKey = i + "_" + j;
                Assert.IsTrue(tiles.ContainsKey(tileKey));
                if(tileKey.Equals(tileKeyNotWalkable))
                {
                    Assert.IsFalse(tiles[tileKey].EnableForMove);
                }
                else
                {
                    Assert.IsTrue(tiles[tileKey].EnableForMove);
                }
            }
        }

        yield return null;

        UnityEngine.Object.Destroy(thief);
        UnityEngine.Object.Destroy(grid);
    }

    [UnityTest]
    public IEnumerator ThiefTest_OnMovementProceed()
    {
        //string[] targetedTileKeys = { "4_4", "1_4", "4_1", "2_3"};
        string[] targetedTileKeys = {"1_4", "4_1", "2_3"};
        Vector2[][] expectedMove = new Vector2[][]{ 
            //new Vector2[] {new Vector2(2, 1), new Vector2(2, 2), new Vector2(3, 2), new Vector2(3, 3), new Vector2(4, 3), new Vector2(4, 4) },
            new Vector2[] {new Vector2(1, 2), new Vector2(1, 3), new Vector2(1, 4) },
            new Vector2[] {new Vector2(2, 1), new Vector2(3, 1), new Vector2(4, 1) },
            new Vector2[] {new Vector2(1, 2), new Vector2(2, 2) , new Vector2(2, 3) }
        };
        GameObject thiefPrefab = Resources.Load<GameObject>("Prefabs/Thief");
        Assert.IsNotNull(thiefPrefab, "Thief prefab should be loaded successfully.");
        Thief thief = UnityEngine.Object.Instantiate(thiefPrefab).GetComponent<Thief>();
        Assert.IsNotNull(thief, "Thief component should be present on the instance.");

        GameObject gridPrefab = Resources.Load<GameObject>("Prefabs/GridTest");
        Assert.IsNotNull(gridPrefab, "Grid prefab should be loaded successfully.");
        TacticalThieves.Grid grid = UnityEngine.Object.Instantiate(gridPrefab).GetComponent<TacticalThieves.Grid>();
        grid.InitTilesDictionnary();

        for (int i = 0; i < targetedTileKeys.Length; i++)
        {
            string targetedTileKey = targetedTileKeys[i];
            Vector2[] expectedMoveRoute = new Vector2[expectedMove[i].Length];
            for (int j = 0; j < expectedMove[i].Length; j++)
            {
                expectedMoveRoute[j] = expectedMove[i][j];
            }

            OnMovementProceed(targetedTileKey, expectedMoveRoute);
        }


        void OnMovementProceed(string targetedTileKey, Vector2[] expectedMove)
        {
            thief.X = 1;
            thief.Y = 1;
            thief.MoveRange = 3;
            Dictionary<string, Tile> tiles = grid.Tiles;

            Assert.IsTrue(tiles.ContainsKey(targetedTileKey), $"Tile with key {targetedTileKey} should exist in the grid's tiles dictionary.");
            Tile targetedTile = tiles[targetedTileKey];
            Assert.IsNotNull(targetedTile, $"Tile at {targetedTileKey} should not be null.");

            List<Vector2> moveRoute = grid.ComputeMoveRoute(thief, targetedTile, thief.MoveRange);
            Assert.AreEqual(expectedMove.Length, moveRoute.Count, "Move route should have the expected number of steps.");
            for (int i = 0; i < moveRoute.Count; i++)
            {
                Assert.AreEqual(expectedMove[i], moveRoute[i], $"Move route at index {i} should match the expected value.");
            }
        }

        yield return null; // Wait for the next frame to ensure the prefab is loaded
        UnityEngine.Object.Destroy(thief);
        UnityEngine.Object.Destroy(grid);
    }

    [UnityTest]
    public IEnumerator ThiefTest_ThiefStatus()
    {
        GameObject thiefPrefab = Resources.Load<GameObject>("Prefabs/Thief");
        Assert.IsNotNull(thiefPrefab, "Thief prefab should be loaded successfully.");
        Thief thief = UnityEngine.Object.Instantiate(thiefPrefab).GetComponent<Thief>();
        Assert.IsNotNull(thief, "Thief component should be present on the instance.");

        GameObject gridPrefab = Resources.Load<GameObject>("Prefabs/GridTest");
        Assert.IsNotNull(gridPrefab, "Grid prefab should be loaded successfully.");
        TacticalThieves.Grid grid = UnityEngine.Object.Instantiate(gridPrefab).GetComponent<TacticalThieves.Grid>();
        grid.InitTilesDictionnary();
        Assert.AreEqual(thief.Status, Thief.eThiefStatus.Wait);

        thief.EnableMove(true, grid);
        Assert.AreEqual(thief.Status, Thief.eThiefStatus.MovementEnable);

        thief.ProceedMovement(true);
        Assert.AreEqual(thief.Status, Thief.eThiefStatus.isMoving);

        thief.ProceedMovement(false);
        Assert.AreEqual(thief.Status, Thief.eThiefStatus.Wait);

        yield return null;
        UnityEngine.Object.Destroy(thief);
        UnityEngine.Object.Destroy(grid);
    }

    [UnityTest]
    public IEnumerator ThiefTest_UsingStealthSkill()
    {
        GameObject thiefPrefab = Resources.Load<GameObject>("Prefabs/Thief");
        Assert.IsNotNull(thiefPrefab, "Thief prefab should be loaded successfully.");
        Thief thief = UnityEngine.Object.Instantiate(thiefPrefab).GetComponent<Thief>();
        Assert.IsNotNull(thief, "Thief component should be present on the instance.");

        thief.EnableStealth(true);
        Assert.IsTrue(thief.Stealth);

        thief.EnableStealth(false);
        Assert.IsFalse(thief.Stealth);

       yield return null;
        UnityEngine.Object.Destroy(thief);

    }

    [Test] 
    public void ThiefTest_ThiefShouldDieIfAttacked()
    {
        GameObject thiefPrefab = Resources.Load<GameObject>("Prefabs/Thief");
        Assert.IsNotNull(thiefPrefab, "Thief prefab should be loaded successfully.");
        Thief thief = UnityEngine.Object.Instantiate(thiefPrefab).GetComponent<Thief>();
        Assert.IsNotNull(thief, "Thief component should be present on the instance.");

        thief.OnThiefStarted();
        Assert.AreEqual(thief.Status, Thief.eThiefStatus.Wait);

        thief.OnThiefAttacked();
        Assert.AreEqual(thief.Status, Thief.eThiefStatus.Dead);

    }
        

}
