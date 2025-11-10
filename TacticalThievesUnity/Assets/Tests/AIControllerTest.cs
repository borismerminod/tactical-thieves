using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TacticalThieves;
using UnityEngine;
using UnityEngine.TestTools;

public class AIControllerTest
{
    private GameObject aiControllerPrefab;
    private AIController aiController;

    [SetUp]
    public void SetUp()
    {
        aiControllerPrefab = Resources.Load<GameObject>("Prefabs/AIController");
        aiController = UnityEngine.Object.Instantiate(aiControllerPrefab).GetComponent<AIController>();
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.Destroy(aiController);
    }

    [Test]
    public void AIControllerTest_ShouldBeCreated()
    {
        Assert.IsNotNull(aiControllerPrefab, "AI controller prefab should be created");
        Assert.IsNotNull(aiController, "AI instance should be created");
    }

    [Test]
    public void AIControllerTest_AIControllerCanSelectAMonster()
    {
        GameObject monsterPrefab = Resources.Load<GameObject>("Prefabs/Monster");
        Monster monster = UnityEngine.Object.Instantiate(monsterPrefab).GetComponent<Monster>();
        monster.TestMode = true;
        monster.AttackRange = 1;
        monster.X = 3;
        monster.Y = 3;

        GameObject gridPrefab = Resources.Load<GameObject>("Prefabs/GridTest");
        TacticalThieves.Grid grid = UnityEngine.Object.Instantiate(gridPrefab).GetComponent<TacticalThieves.Grid>();
        grid.InitTilesDictionnary();

        //Case 1 : Monster is "selected"
        bool result = aiController.OnMonsterSelected(monster);
        Assert.AreEqual(aiController.CurrentMonster.ActionPhase, Monster.eActionPhase.WAIT);
        Assert.AreEqual(aiController.CurrentMonster, monster);
        Assert.IsTrue(result);

        //Case 2 AI controller drive monster onto its first attack
        List<Vector2> tiles = new List<Vector2>();
        tiles = aiController.SetMonsterFirstAttack(monster, grid);
        Assert.AreEqual(aiController.CurrentMonster.ActionPhase, Monster.eActionPhase.PHASE1_FIRST_ATTACK);
        Assert.AreEqual(5, tiles.Count);


        //Case 3 grid is null
        tiles = aiController.SetMonsterFirstAttack(monster, null);
        Assert.AreEqual(tiles.Count, 0);

        //Case 4 monster is null
        result = aiController.OnMonsterSelected(null);
        Assert.IsFalse(result);

        tiles = aiController.SetMonsterFirstAttack(null, grid);
        Assert.IsFalse(result);
        Assert.AreEqual(tiles.Count, 0);

    }

    [Test]
    public void AIControllerTest_AIControllerTryFirstAttackWithMonster()
    {
        GameObject monsterPrefab = Resources.Load<GameObject>("Prefabs/Monster");
        Monster monster = UnityEngine.Object.Instantiate(monsterPrefab).GetComponent<Monster>();
        monster.TestMode = true;
        monster.AttackRange = 1;
        monster.X = 3;
        monster.Y = 3;

        GameObject gridPrefab = Resources.Load<GameObject>("Prefabs/GridTest");
        TacticalThieves.Grid grid = UnityEngine.Object.Instantiate(gridPrefab).GetComponent<TacticalThieves.Grid>();
        grid.InitTilesDictionnary();

        GameObject thiefPrefab = Resources.Load<GameObject>("Prefabs/Thief");
        TacticalThieves.Thief thief = UnityEngine.Object.Instantiate(thiefPrefab).GetComponent<TacticalThieves.Thief>();
        thief.X = 2;
        thief.Y = 3;

        //Step 1 : Monster is "selected"
        bool result = aiController.OnMonsterSelected(monster);
        Assert.AreEqual(aiController.CurrentMonster.ActionPhase, Monster.eActionPhase.WAIT);
        Assert.AreEqual(aiController.CurrentMonster, monster);
        Assert.IsTrue(result);

        //Step 2 AI controller drive monster onto its first attack
        List<Vector2> tiles = new List<Vector2>();
        tiles = aiController.SetMonsterFirstAttack(monster, grid);
        Assert.AreEqual(aiController.CurrentMonster.ActionPhase, Monster.eActionPhase.PHASE1_FIRST_ATTACK);
        Assert.AreEqual(5, tiles.Count);

        //Case 1 : Cas nominal  AI controller try to attack the thief on the selected tiles
        result = aiController.Attack(tiles, thief, grid);
        Assert.IsTrue(result);

        //Case 2 : There is no thief on selected tiles
        thief.X = 1;
        thief.Y = 1;
       
        result = aiController.Attack(tiles, thief, grid);
        Assert.IsFalse(result);


    }

    [Test]
    public void AIControllerTest_AIControllerEnableMonsterMove()
    {
        GameObject monsterPrefab = Resources.Load<GameObject>("Prefabs/Monster");
        Monster monster = UnityEngine.Object.Instantiate(monsterPrefab).GetComponent<Monster>();
        monster.TestMode = true;
        monster.MoveRange = 1;
        monster.AttackRange = 1;
        monster.X = 3;
        monster.Y = 3;

        GameObject gridPrefab = Resources.Load<GameObject>("Prefabs/GridTest");
        TacticalThieves.Grid grid = UnityEngine.Object.Instantiate(gridPrefab).GetComponent<TacticalThieves.Grid>();
        grid.InitTilesDictionnary();

        GameObject thiefPrefab = Resources.Load<GameObject>("Prefabs/Thief");
        TacticalThieves.Thief thief = UnityEngine.Object.Instantiate(thiefPrefab).GetComponent<TacticalThieves.Thief>();
        thief.X = 1;
        thief.Y = 1;

        //Step 1 : Monster is "selected"
        bool result = aiController.OnMonsterSelected(monster);
        Assert.AreEqual(aiController.CurrentMonster.ActionPhase, Monster.eActionPhase.WAIT);
        Assert.AreEqual(aiController.CurrentMonster, monster);
        Assert.IsTrue(result);

        //Step 2 AI controller drive monster onto its first attack
        List<Vector2> tiles = new List<Vector2>();
        tiles = aiController.SetMonsterFirstAttack(monster, grid);
        Assert.AreEqual(aiController.CurrentMonster.ActionPhase, Monster.eActionPhase.PHASE1_FIRST_ATTACK);
        Assert.AreEqual(5, tiles.Count);

        //Step 3 : AI controller try to attack the thief on the selected tiles
        result = aiController.Attack(tiles, thief, grid);
        Assert.IsFalse(result);

        //Case 1 : AI Controller try to move the monster 
        tiles = aiController.SetMonsterMove(monster, grid);
        Assert.AreEqual(5, tiles.Count);

        //Case 2 : Monster is null
        tiles = aiController.SetMonsterMove(null, grid);
        Assert.AreEqual(0, tiles.Count);

        //Case 3 : Grid is null
        tiles = aiController.SetMonsterMove(monster, null);
        Assert.AreEqual(0, tiles.Count);
    }


}
