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

        GameObject gridPrefab = Resources.Load<GameObject>("Prefabs/GridTest");
        TacticalThieves.Grid grid = UnityEngine.Object.Instantiate(gridPrefab).GetComponent<TacticalThieves.Grid>();
        grid.InitTilesDictionnary();

        //Case 1 : Monster is "selected"
        bool result = aiController.OnMonsterSelected(monster);
        Assert.AreEqual(aiController.CurrentMonster.ActionPhase, Monster.eActionPhase.WAIT);
        Assert.AreEqual(aiController.CurrentMonster, monster);
        Assert.IsTrue(result);

        //Case 2 AI controller drive monster onto its first attack
        result = aiController.SetMonsterFirstAttack(monster, grid);
        Assert.AreEqual(aiController.CurrentMonster.ActionPhase, Monster.eActionPhase.PHASE1_FIRST_ATTACK);
        Assert.IsTrue(result);

        //Case 3 grid is null
        result = aiController.SetMonsterFirstAttack(monster, null);
        Assert.IsFalse(result);

        //Case 4 monster is null
        result = aiController.OnMonsterSelected(null);
        Assert.IsFalse(result);

        result = aiController.SetMonsterFirstAttack(null, grid);
        Assert.IsFalse(result);

    }


}
