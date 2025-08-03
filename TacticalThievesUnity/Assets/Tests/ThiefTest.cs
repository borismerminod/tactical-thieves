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
}
