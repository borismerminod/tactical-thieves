using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using TacticalThieves;

/// <summary>
/// Tests des membres publics de <see cref="CharactersManager"/> :
/// <c>AddCharacter</c>, <c>AreAllThievesDied</c> et le getter <c>Characters</c>.
/// </summary>
/// <remarks>
/// <see cref="CharactersManager"/> est un pur registre en mémoire, sans dépendance externe
/// (pas de GameManager, réseau ni Grid). Les tests sont donc synchrones (<c>[Test]</c>) et isolés.
/// Deux détails techniques :
/// <list type="bullet">
/// <item>La prefab restitue une <c>List</c> vide non-nulle ; par sécurité on l'initialise par
/// réflexion si elle était nulle.</item>
/// <item><see cref="Thief.Status"/> a un setter privé : on place le statut voulu par réflexion
/// sur le champ privé <c>status</c>, ce qui évite les effets de bord de <c>OnThiefAttacked()</c>.</item>
/// </list>
/// </remarks>
public class CharactersManagerTest
{
    private CharactersManager charactersManager;
    private readonly List<GameObject> spawned = new List<GameObject>();

    [SetUp]
    public void Setup()
    {
        GameObject managerPrefab = Resources.Load<GameObject>("Prefabs/CharactersManager");
        Assert.IsNotNull(managerPrefab, "CharactersManager prefab should be loaded successfully.");

        GameObject managerInstance = UnityEngine.Object.Instantiate(managerPrefab);
        spawned.Add(managerInstance);
        charactersManager = managerInstance.GetComponent<CharactersManager>();
        Assert.IsNotNull(charactersManager, "CharactersManager component should be present on the instance.");

        // Sécurité : si la liste sérialisée était nulle, on l'initialise pour éviter une NPE
        // dans AddCharacter (la prefab est censée fournir une liste vide non-nulle).
        if (charactersManager.Characters == null)
            SetCharactersList(new List<Character>());
    }

    [TearDown]
    public void TearDown()
    {
        // DestroyImmediate car Object.Destroy est différé et peu fiable en EditMode.
        foreach (GameObject gameObject in spawned)
        {
            if (gameObject != null)
                UnityEngine.Object.DestroyImmediate(gameObject);
        }
        spawned.Clear();
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    /// <summary>Instancie un Thief depuis la prefab et lui fixe un statut donné (par réflexion).</summary>
    private Thief MakeThief(Thief.eThiefStatus status)
    {
        GameObject thiefPrefab = Resources.Load<GameObject>("Prefabs/Thief");
        Assert.IsNotNull(thiefPrefab, "Thief prefab should be loaded successfully.");

        GameObject thiefInstance = UnityEngine.Object.Instantiate(thiefPrefab);
        spawned.Add(thiefInstance);
        Thief thief = thiefInstance.GetComponent<Thief>();
        Assert.IsNotNull(thief, "Thief component should be present on the instance.");

        SetThiefStatus(thief, status);
        return thief;
    }

    /// <summary>Instancie un Monster depuis la prefab (sert à vérifier que les non-Thief sont ignorés).</summary>
    private Monster MakeMonster()
    {
        GameObject monsterPrefab = Resources.Load<GameObject>("Prefabs/Monster");
        Assert.IsNotNull(monsterPrefab, "Monster prefab should be loaded successfully.");

        GameObject monsterInstance = UnityEngine.Object.Instantiate(monsterPrefab);
        spawned.Add(monsterInstance);
        Monster monster = monsterInstance.GetComponent<Monster>();
        Assert.IsNotNull(monster, "Monster component should be present on the instance.");

        return monster;
    }

    private void SetThiefStatus(Thief thief, Thief.eThiefStatus status)
    {
        FieldInfo statusField = typeof(Thief).GetField("status", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(statusField, "Le champ privé 'status' de Thief doit exister.");
        statusField.SetValue(thief, status);
    }

    private void SetCharactersList(List<Character> list)
    {
        FieldInfo charactersField = typeof(CharactersManager).GetField("characters", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(charactersField, "Le champ privé 'characters' de CharactersManager doit exister.");
        charactersField.SetValue(charactersManager, list);
    }

    // ---------------------------------------------------------------------
    // A. AddCharacter
    // ---------------------------------------------------------------------

    [Test]
    public void AddCharacter_AddsCharacterToList()
    {
        Thief thief = MakeThief(Thief.eThiefStatus.Wait);

        charactersManager.AddCharacter(thief);

        Assert.AreEqual(1, charactersManager.Characters.Count);
        Assert.IsTrue(charactersManager.Characters.Contains(thief));
    }

    [Test]
    public void AddCharacter_IgnoresNull()
    {
        charactersManager.AddCharacter(null);

        Assert.AreEqual(0, charactersManager.Characters.Count);
    }

    [Test]
    public void AddCharacter_IgnoresDuplicate()
    {
        Thief thief = MakeThief(Thief.eThiefStatus.Wait);

        charactersManager.AddCharacter(thief);
        charactersManager.AddCharacter(thief);

        Assert.AreEqual(1, charactersManager.Characters.Count);
    }

    [Test]
    public void AddCharacter_AddsMultipleDistinctInInsertionOrder()
    {
        Thief first = MakeThief(Thief.eThiefStatus.Wait);
        Thief second = MakeThief(Thief.eThiefStatus.Wait);
        Thief third = MakeThief(Thief.eThiefStatus.Wait);

        charactersManager.AddCharacter(first);
        charactersManager.AddCharacter(second);
        charactersManager.AddCharacter(third);

        Assert.AreEqual(3, charactersManager.Characters.Count);
        Assert.AreSame(first, charactersManager.Characters[0]);
        Assert.AreSame(second, charactersManager.Characters[1]);
        Assert.AreSame(third, charactersManager.Characters[2]);
    }

    [Test]
    public void AddCharacter_AcceptsThievesAndMonstersWithoutTypeFiltering()
    {
        Thief thief = MakeThief(Thief.eThiefStatus.Wait);
        Monster monster = MakeMonster();

        charactersManager.AddCharacter(thief);
        charactersManager.AddCharacter(monster);

        Assert.AreEqual(2, charactersManager.Characters.Count);
        Assert.IsTrue(charactersManager.Characters.Contains(thief));
        Assert.IsTrue(charactersManager.Characters.Contains(monster));
    }

    // ---------------------------------------------------------------------
    // B. AreAllThievesDied
    // ---------------------------------------------------------------------

    [Test]
    public void AreAllThievesDied_ReturnsTrueWhenListIsEmpty()
    {
        // Vérité vacue : aucun voleur présent => "tous morts" par convention documentée.
        Assert.IsTrue(charactersManager.AreAllThievesDied());
    }

    [TestCase(Thief.eThiefStatus.Wait)]
    [TestCase(Thief.eThiefStatus.MovementEnable)]
    [TestCase(Thief.eThiefStatus.isMoving)]
    public void AreAllThievesDied_ReturnsFalseWhenSingleThiefIsAlive(Thief.eThiefStatus aliveStatus)
    {
        Thief thief = MakeThief(aliveStatus);
        charactersManager.AddCharacter(thief);

        Assert.IsFalse(charactersManager.AreAllThievesDied());
    }

    [Test]
    public void AreAllThievesDied_ReturnsTrueWhenSingleThiefIsDead()
    {
        Thief thief = MakeThief(Thief.eThiefStatus.Dead);
        charactersManager.AddCharacter(thief);

        Assert.IsTrue(charactersManager.AreAllThievesDied());
    }

    [Test]
    public void AreAllThievesDied_ReturnsTrueWhenAllThievesAreDead()
    {
        charactersManager.AddCharacter(MakeThief(Thief.eThiefStatus.Dead));
        charactersManager.AddCharacter(MakeThief(Thief.eThiefStatus.Dead));
        charactersManager.AddCharacter(MakeThief(Thief.eThiefStatus.Dead));

        Assert.IsTrue(charactersManager.AreAllThievesDied());
    }

    [Test]
    public void AreAllThievesDied_ReturnsFalseWhenOneThiefIsStillAliveAmongDeadOnes()
    {
        charactersManager.AddCharacter(MakeThief(Thief.eThiefStatus.Dead));
        charactersManager.AddCharacter(MakeThief(Thief.eThiefStatus.Dead));
        charactersManager.AddCharacter(MakeThief(Thief.eThiefStatus.Wait));

        Assert.IsFalse(charactersManager.AreAllThievesDied());
    }

    [Test]
    public void AreAllThievesDied_IgnoresMonstersWhenAllThievesAreDead()
    {
        charactersManager.AddCharacter(MakeThief(Thief.eThiefStatus.Dead));
        charactersManager.AddCharacter(MakeThief(Thief.eThiefStatus.Dead));
        charactersManager.AddCharacter(MakeMonster());

        Assert.IsTrue(charactersManager.AreAllThievesDied());
    }

    [Test]
    public void AreAllThievesDied_ReturnsTrueWhenNoThiefIsPresent()
    {
        // Que des monstres : aucun Thief => vérité vacue.
        charactersManager.AddCharacter(MakeMonster());
        charactersManager.AddCharacter(MakeMonster());

        Assert.IsTrue(charactersManager.AreAllThievesDied());
    }

    [Test]
    public void AreAllThievesDied_ReturnsFalseWhenMonsterPresentAndThiefAlive()
    {
        charactersManager.AddCharacter(MakeMonster());
        charactersManager.AddCharacter(MakeThief(Thief.eThiefStatus.Wait));

        Assert.IsFalse(charactersManager.AreAllThievesDied());
    }

    [Test]
    public void AreAllThievesDied_HandlesNullEntryWithoutThrowing()
    {
        // AddCharacter refuse null : on injecte donc une liste "anormale" par réflexion.
        Thief deadThief = MakeThief(Thief.eThiefStatus.Dead);
        List<Character> listWithNull = new List<Character> { deadThief, null };
        SetCharactersList(listWithNull);

        Assert.IsTrue(charactersManager.AreAllThievesDied());
    }

    // ---------------------------------------------------------------------
    // C. Characters (getter)
    // ---------------------------------------------------------------------

    [Test]
    public void Characters_ReflectsAddedCharacters()
    {
        Thief thief = MakeThief(Thief.eThiefStatus.Wait);

        charactersManager.AddCharacter(thief);

        Assert.IsTrue(charactersManager.Characters.Contains(thief));
    }

    [Test]
    public void Characters_ReturnsSameUnderlyingInstance()
    {
        List<Character> firstRead = charactersManager.Characters;
        List<Character> secondRead = charactersManager.Characters;

        Assert.AreSame(firstRead, secondRead);
    }
}
