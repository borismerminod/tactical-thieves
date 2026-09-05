using NUnit.Framework;
using System.Collections.Generic;
using TacticalThieves;
using UnityEngine;

public class AIControllerTest
{
    private GameObject aiControllerPrefab;
    private AIController aiController;

    // Toutes les instances creees pendant un test sont detruites en TearDown.
    // On utilise DestroyImmediate pour que les tuiles (tag "Tile") et le singleton
    // GameManager.Instance soient bien liberes entre chaque test (pas de fuite EditMode).
    private List<GameObject> spawnedObjects;

    [SetUp]
    public void SetUp()
    {
        spawnedObjects = new List<GameObject>();

        aiControllerPrefab = Resources.Load<GameObject>("Prefabs/AIController");
        GameObject aiControllerGO = UnityEngine.Object.Instantiate(aiControllerPrefab);
        spawnedObjects.Add(aiControllerGO);
        aiController = aiControllerGO.GetComponent<AIController>();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject spawnedObject in spawnedObjects)
        {
            if (spawnedObject != null)
                UnityEngine.Object.DestroyImmediate(spawnedObject);
        }
        spawnedObjects.Clear();
    }

    // ----------------------------------------------------------------------
    // Helpers d'instanciation (variables intermediaires, pas d'appels imbriques)
    // ----------------------------------------------------------------------

    private Monster InstantiateMonster(int x, int y, int attackRange, int moveRange)
    {
        GameObject monsterPrefab = Resources.Load<GameObject>("Prefabs/Monster");
        GameObject monsterGO = UnityEngine.Object.Instantiate(monsterPrefab);
        spawnedObjects.Add(monsterGO);

        Monster monster = monsterGO.GetComponent<Monster>();
        monster.TestMode = true;
        monster.AttackRange = attackRange;
        monster.MoveRange = moveRange;
        monster.X = x;
        monster.Y = y;

        return monster;
    }

    private TacticalThieves.Grid InstantiateGrid()
    {
        GameObject gridPrefab = Resources.Load<GameObject>("Prefabs/GridTest");
        GameObject gridGO = UnityEngine.Object.Instantiate(gridPrefab);
        spawnedObjects.Add(gridGO);

        TacticalThieves.Grid grid = gridGO.GetComponent<TacticalThieves.Grid>();
        grid.TestMode = true;              // force toutes les tuiles a Walkable = true
        grid.InitTilesDictionnary();       // Start() ne tourne pas en EditMode
        return grid;
    }

    private GridActionHandler InstantiateGridActionHandler(TacticalThieves.Grid grid)
    {
        GameObject handlerPrefab = Resources.Load<GameObject>("Prefabs/GridActionHandler");
        GameObject handlerGO = UnityEngine.Object.Instantiate(handlerPrefab);
        spawnedObjects.Add(handlerGO);

        GridActionHandler gridActionHandler = handlerGO.GetComponent<GridActionHandler>();
        gridActionHandler.OnGridStarted(grid); // indispensable : renseigne currentGrid
        return gridActionHandler;
    }

    private Thief InstantiateThief(int x, int y)
    {
        GameObject thiefPrefab = Resources.Load<GameObject>("Prefabs/Thief");
        GameObject thiefGO = UnityEngine.Object.Instantiate(thiefPrefab);
        spawnedObjects.Add(thiefGO);

        Thief thief = thiefGO.GetComponent<Thief>();
        thief.X = x;
        thief.Y = y;
        return thief;
    }

    private GameManager InstantiateGameManager(TacticalThieves.Grid grid)
    {
        GameObject gameManagerPrefab = Resources.Load<GameObject>("Prefabs/GameManager");
        GameObject gameManagerGO = UnityEngine.Object.Instantiate(gameManagerPrefab);
        spawnedObjects.Add(gameManagerGO);

        // Awake() est appele par Instantiate en EditMode : GameManager.Instance est renseigne.
        GameManager gameManager = gameManagerGO.GetComponent<GameManager>();
        gameManager.TestMode = true;
        gameManager.CurrentGrid = grid;
        return gameManager;
    }

    // ----------------------------------------------------------------------
    // Sanity check
    // ----------------------------------------------------------------------

    [Test]
    public void AIController_ShouldBeInstantiated()
    {
        Assert.IsNotNull(aiControllerPrefab, "Le prefab AIController doit etre charge.");
        Assert.IsNotNull(aiController, "Le composant AIController doit etre present sur l'instance.");
    }

    // ----------------------------------------------------------------------
    // A. CurrentMonster (propriete)
    // ----------------------------------------------------------------------

    [Test]
    public void CurrentMonster_SetThenGet_ReturnsSameMonster()
    {
        Monster monster = InstantiateMonster(3, 3, 1, 1);

        aiController.CurrentMonster = monster;

        Assert.AreEqual(monster, aiController.CurrentMonster);
    }

    // ----------------------------------------------------------------------
    // B. OnMonsterSelected
    // ----------------------------------------------------------------------

    [Test]
    public void OnMonsterSelected_WithValidMonster_SelectsAndInitializesIt()
    {
        Monster monster = InstantiateMonster(3, 3, 1, 1);

        bool result = aiController.OnMonsterSelected(monster);

        Assert.IsTrue(result);
        Assert.AreEqual(monster, aiController.CurrentMonster);
        Assert.AreEqual(Monster.eActionPhase.WAIT, aiController.CurrentMonster.ActionPhase,
            "Init() doit remettre le monstre en phase WAIT.");
    }

    [Test]
    public void OnMonsterSelected_WithNullMonster_ReturnsFalse()
    {
        bool result = aiController.OnMonsterSelected(null);

        Assert.IsFalse(result);
    }

    // ----------------------------------------------------------------------
    // C. SetMonsterAttack
    // ----------------------------------------------------------------------

    [Test]
    public void SetMonsterAttack_NominalCase_ReturnsReachableAttackTiles()
    {
        Monster monster = InstantiateMonster(3, 3, 1, 1);
        TacticalThieves.Grid grid = InstantiateGrid();
        GridActionHandler gridActionHandler = InstantiateGridActionHandler(grid);

        List<Vector2> tiles = aiController.SetMonsterAttack(monster, gridActionHandler);

        Assert.IsNotNull(tiles);
        // Portee 1 en (3,3) : la tuile centrale + les 4 tuiles orthogonales.
        Assert.AreEqual(5, tiles.Count);
    }

    [Test]
    public void SetMonsterAttack_WithNullMonster_ReturnsEmptyList()
    {
        TacticalThieves.Grid grid = InstantiateGrid();
        GridActionHandler gridActionHandler = InstantiateGridActionHandler(grid);

        List<Vector2> tiles = aiController.SetMonsterAttack(null, gridActionHandler);

        Assert.AreEqual(0, tiles.Count);
    }

    [Test]
    public void SetMonsterAttack_WithNullHandler_ReturnsEmptyList()
    {
        Monster monster = InstantiateMonster(3, 3, 1, 1);

        List<Vector2> tiles = aiController.SetMonsterAttack(monster, null);

        Assert.AreEqual(0, tiles.Count);
    }

    // ----------------------------------------------------------------------
    // D. Attack
    // ----------------------------------------------------------------------

    [Test]
    public void Attack_ThiefOnEnabledTile_ReturnsTrue()
    {
        Monster monster = InstantiateMonster(3, 3, 1, 1);
        TacticalThieves.Grid grid = InstantiateGrid();
        GridActionHandler gridActionHandler = InstantiateGridActionHandler(grid);
        Thief thief = InstantiateThief(2, 3); // tuile orthogonale, activee pour l'attaque

        // currentMonster doit etre renseigne (utilise en cas de touche) : on selectionne d'abord.
        bool selected = aiController.OnMonsterSelected(monster);
        Assert.IsTrue(selected);

        List<Vector2> tiles = aiController.SetMonsterAttack(monster, gridActionHandler);

        bool thiefIsAttacked = aiController.Attack(tiles, thief, grid);

        Assert.IsTrue(thiefIsAttacked);
        Assert.AreEqual(Thief.eThiefStatus.Dead, thief.Status);
    }

    [Test]
    public void Attack_ThiefOutsideEnabledTiles_ReturnsFalse()
    {
        Monster monster = InstantiateMonster(3, 3, 1, 1);
        TacticalThieves.Grid grid = InstantiateGrid();
        GridActionHandler gridActionHandler = InstantiateGridActionHandler(grid);
        Thief thief = InstantiateThief(1, 1); // hors de portee

        List<Vector2> tiles = aiController.SetMonsterAttack(monster, gridActionHandler);

        bool thiefIsAttacked = aiController.Attack(tiles, thief, grid);

        Assert.IsFalse(thiefIsAttacked);
    }

    [Test]
    public void Attack_WithEmptyTileList_ReturnsFalse()
    {
        TacticalThieves.Grid grid = InstantiateGrid();
        Thief thief = InstantiateThief(3, 3);
        List<Vector2> tiles = new List<Vector2>();

        bool thiefIsAttacked = aiController.Attack(tiles, thief, grid);

        Assert.IsFalse(thiefIsAttacked);
    }

    // ----------------------------------------------------------------------
    // E. SetMonsterMove
    // ----------------------------------------------------------------------

    [Test]
    public void SetMonsterMove_NominalCase_ReturnsReachableMoveTiles()
    {
        Monster monster = InstantiateMonster(3, 3, 1, 1);
        TacticalThieves.Grid grid = InstantiateGrid();
        GridActionHandler gridActionHandler = InstantiateGridActionHandler(grid);

        List<Vector2> tiles = aiController.SetMonsterMove(monster, gridActionHandler);

        Assert.IsNotNull(tiles);
        Assert.AreEqual(5, tiles.Count);
    }

    [Test]
    public void SetMonsterMove_WithNullMonster_ReturnsEmptyList()
    {
        TacticalThieves.Grid grid = InstantiateGrid();
        GridActionHandler gridActionHandler = InstantiateGridActionHandler(grid);

        List<Vector2> tiles = aiController.SetMonsterMove(null, gridActionHandler);

        Assert.AreEqual(0, tiles.Count);
    }

    [Test]
    public void SetMonsterMove_WithNullHandler_ReturnsEmptyList()
    {
        Monster monster = InstantiateMonster(3, 3, 1, 1);

        List<Vector2> tiles = aiController.SetMonsterMove(monster, null);

        Assert.AreEqual(0, tiles.Count);
    }

    // ----------------------------------------------------------------------
    // F. SelectShortestMoveRoute (necessite GameManager.Instance.CurrentGrid)
    // ----------------------------------------------------------------------

    [Test]
    public void SelectShortestMoveRoute_SingleThief_ReturnsRouteTowardIt()
    {
        Monster monster = InstantiateMonster(3, 3, 1, 1);
        TacticalThieves.Grid grid = InstantiateGrid();
        InstantiateGameManager(grid); // renseigne GameManager.Instance + CurrentGrid
        Thief thief = InstantiateThief(4, 4);

        List<Thief> thieves = new List<Thief>();
        thieves.Add(thief);

        List<Vector2> route = aiController.SelectShortestMoveRoute(monster, grid, thieves);

        Assert.IsNotNull(route);
        Assert.AreEqual(2, route.Count);
        Assert.AreEqual(new Vector2(4, 3), route[0]);
        Assert.AreEqual(new Vector2(4, 4), route[1]);
    }

    [Test]
    public void SelectShortestMoveRoute_MultipleThieves_ReturnsShortestRoute()
    {
        Monster monster = InstantiateMonster(3, 3, 1, 1);
        TacticalThieves.Grid grid = InstantiateGrid();
        InstantiateGameManager(grid);
        Thief farThief = InstantiateThief(1, 1);
        Thief nearThief = InstantiateThief(4, 4);

        List<Thief> thieves = new List<Thief>();
        thieves.Add(farThief);
        thieves.Add(nearThief);

        List<Vector2> route = aiController.SelectShortestMoveRoute(monster, grid, thieves);

        // La route vers le voleur le plus proche (4,4) est la plus courte.
        Assert.AreEqual(2, route.Count);
        Assert.AreEqual(new Vector2(4, 3), route[0]);
        Assert.AreEqual(new Vector2(4, 4), route[1]);
    }

    [Test]
    public void SelectShortestMoveRoute_NoThief_ReturnsEmptyRoute()
    {
        Monster monster = InstantiateMonster(3, 3, 1, 1);
        TacticalThieves.Grid grid = InstantiateGrid();
        List<Thief> thieves = new List<Thief>();

        List<Vector2> route = aiController.SelectShortestMoveRoute(monster, grid, thieves);

        Assert.AreEqual(0, route.Count);
    }

    // ----------------------------------------------------------------------
    // G. AdjustRouteFromMoveRange (fonction pure)
    // ----------------------------------------------------------------------

    [Test]
    public void AdjustRouteFromMoveRange_RouteLongerThanRange_TruncatesToRange()
    {
        List<Vector2> route = new List<Vector2>
        {
            new Vector2(4, 3), new Vector2(4, 4), new Vector2(4, 5)
        };

        List<Vector2> adjusted = aiController.AdjustRouteFromMoveRange(route, 1);

        Assert.AreEqual(1, adjusted.Count);
        Assert.AreEqual(new Vector2(4, 3), adjusted[0]);
    }

    [Test]
    public void AdjustRouteFromMoveRange_RouteEqualToRange_ReturnsOriginalList()
    {
        List<Vector2> route = new List<Vector2> { new Vector2(4, 3), new Vector2(4, 4) };

        List<Vector2> adjusted = aiController.AdjustRouteFromMoveRange(route, 2);

        Assert.AreEqual(2, adjusted.Count);
        Assert.AreSame(route, adjusted, "La liste d'origine doit etre renvoyee telle quelle.");
    }

    [Test]
    public void AdjustRouteFromMoveRange_RouteShorterThanRange_ReturnsOriginalList()
    {
        List<Vector2> route = new List<Vector2> { new Vector2(4, 3) };

        List<Vector2> adjusted = aiController.AdjustRouteFromMoveRange(route, 3);

        Assert.AreEqual(1, adjusted.Count);
        Assert.AreSame(route, adjusted);
    }

    [Test]
    public void AdjustRouteFromMoveRange_NullRoute_ReturnsNull()
    {
        List<Vector2> adjusted = aiController.AdjustRouteFromMoveRange(null, 2);

        Assert.IsNull(adjusted);
    }

    // Cas limite : range <= 0. Comportement actuel fige (la boucle ajoute une tuile
    // avant de tester la condition d'arret) -> renvoie 1 element.
    [Test]
    public void AdjustRouteFromMoveRange_ZeroRange_ReturnsSingleElement()
    {
        List<Vector2> route = new List<Vector2> { new Vector2(4, 3), new Vector2(4, 4) };

        List<Vector2> adjusted = aiController.AdjustRouteFromMoveRange(route, 0);

        Assert.AreEqual(1, adjusted.Count);
    }

    [Test]
    public void AdjustRouteFromMoveRange_NegativeRange_ReturnsSingleElement()
    {
        List<Vector2> route = new List<Vector2> { new Vector2(4, 3), new Vector2(4, 4) };

        List<Vector2> adjusted = aiController.AdjustRouteFromMoveRange(route, -1);

        Assert.AreEqual(1, adjusted.Count);
    }
}
