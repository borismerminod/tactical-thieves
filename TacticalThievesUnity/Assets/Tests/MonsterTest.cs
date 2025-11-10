using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TacticalThieves;
using UnityEngine;
using UnityEngine.TestTools;


public class MonsterTest
{
    GameObject monsterPrefab;
    Monster monster;

   [SetUp]
    public void Setup()
    {
        monsterPrefab = Resources.Load<GameObject>("Prefabs/Monster");
        monster = UnityEngine.Object.Instantiate(monsterPrefab).GetComponent<Monster>();
        monster.TestMode = true;
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.Destroy(monster);
    }

    [UnityTest]
    public IEnumerator MonsterTest_MonsterCreation()
    {
        Assert.IsNotNull(monsterPrefab, "Monster prefab should be loaded successfully.");
        Assert.IsNotNull(monster, "Monster component should be present on the instance.");
        Assert.IsTrue(monster.TestMode);

        yield return null;
    }

    [TestCase(1,1,1,1)]
    [TestCase(4,2,4,2)]
    [TestCase(3,4,3,4)]
    [TestCase(0,0,1,1)]
    [TestCase(-5,-5,1,1)]
    public void MonsterTest_MonsterShouldUseGridCoords(int x, int y, int expectedX, int expectedY)
    {
        monster.X = x;
        monster.Y = y;

        Assert.AreEqual(expectedX, monster.X);
        Assert.AreEqual(expectedY, monster.Y);

    }

    [TestCase(1, 1)]
    [TestCase(2, 2)]
    [TestCase(100, 100)]
    [TestCase(0, 0)]
    [TestCase(-1, 0)]
    public void MonsterTest_MonsterShouldHaveAnAttackRange(int attackRange, int expectedAttackRange)
    {
        monster.AttackRange = attackRange;
        Assert.AreEqual(monster.AttackRange, expectedAttackRange);
    }

    [UnityTest]
    public IEnumerator MonsterTest_MonsterFirstActionShouldBeAttack()
    {

        monster.Init();
        Assert.AreEqual(monster.ActionPhase, Monster.eActionPhase.WAIT);

        monster.TryFirstAttack();
        Assert.AreEqual(Monster.eActionPhase.PHASE1_FIRST_ATTACK, monster.ActionPhase);

        yield return null;
    }

    [UnityTest]
    public IEnumerator MonsterTest_MonsterTryingToMove()
    {

        monster.Init();
        Assert.AreEqual(monster.ActionPhase, Monster.eActionPhase.WAIT);

        monster.OnFirstAttackFailed();
        Assert.AreEqual(Monster.eActionPhase.PHASE2_MOVE, monster.ActionPhase);

        yield return null;
    }

}
