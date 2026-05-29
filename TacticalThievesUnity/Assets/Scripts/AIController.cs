using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.TestTools;

namespace TacticalThieves
{
    public class AIController : MonoBehaviour
    {
        // The monster currently controlled by the AI
        [SerializeField] private Monster currentMonster;
        public Monster CurrentMonster {  get =>  currentMonster; set => currentMonster = value;}

        // List of tile positions where the monster can attack or move
        [SerializeField] private List<Vector2> tilesEnabledPos;


        // Dictionary mapping monster action phases to their corresponding processing methods
        [SerializeField] private Dictionary<Monster.eActionPhase, System.Action> monsterActions;


        /// <summary>
        /// Starts the AI controller and schedules periodic processing of monster actions.
        /// </summary>
        /// <remarks>This method initializes the AI controller by setting up recurring execution of
        /// monster action logic. It also notifies the game manager that the AI controller has started. Call this method
        /// to begin AI processing for the associated entity.</remarks>
        void Start()
        {
            try
            {
                monsterActions = new Dictionary<Monster.eActionPhase, System.Action>
                {
                    { Monster.eActionPhase.WAIT, AttackSelect },
                    { Monster.eActionPhase.PHASE1_FIRST_ATTACK, Attack },
                    { Monster.eActionPhase.PHASE2_MOVE_SELECT, MoveSelect },
                    { Monster.eActionPhase.PHASE4_ATTACK_SELECT, AttackSelect },
                    { Monster.eActionPhase.PHASE5_ATTACK, Attack }
                };

                InvokeRepeating("ProcessMonsterActions", 0.1f, 0.5f);
                //GameManager.Instance?.OnAIControllerStarted(this);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in Start: {ex.Message}");
            }
        }

        /// <summary>
        /// The method is called into a regular interval. It checks the current action phase of the monster and executes 
        /// the corresponding logic for attack selection, attack execution, or move selection. 
        /// This method serves as the main loop for processing the monster's behavior based on its current state and action phase.
        /// </summary>
        private void ProcessMonsterActions()
        {
            try
            {
                if (currentMonster == null)
                    return;

                if (monsterActions.TryGetValue(currentMonster.ActionPhase, out var action))
                {
                    action?.Invoke();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in ProcessMonsterActions: {ex.Message}");
            }
        }

        /// <summary>
        /// The method is called when a monster is selected for the AI to control. It initializes the current monster and prepares it for action processing.
        /// </summary>
        /// <param name="monster">the monster currently selected</param>
        /// <returns>False if the monster instance is null</returns>
        public bool OnMonsterSelected(Monster monster)
        {
            
            if (monster == null) 
                return false;  

            CurrentMonster = monster;
            CurrentMonster.Init();

            return true;
            
        }


        /// <summary>
        /// Determines and returns the set of grid tiles which can be selected by the monster for an attack.
        /// </summary>
        /// <param name="monster">The monster whose attack area is to be calculated. Cannot be <see langword="null"/>.</param>
        /// <param name="gridActionHandler">The grid action handler to manage monster attack action onto the grid</param>
        /// <returns>A list of <see cref="Vector2"/> objects representing the grid tiles affected by the monster's attack.
        /// Returns an empty list if <paramref name="monster"/> or <paramref name="grid"/> is <see langword="null"/>, or
        /// if an error occurs.</returns>
        public List<Vector2> SetMonsterAttack(Monster monster, GridActionHandler gridActionHandler)
        {
            
            List<Vector2> tiles = new List<Vector2>();
            if(monster != null && gridActionHandler != null)
            {
                tiles = gridActionHandler.OnMonsterAttackEnable(monster);
            }

            return tiles;
            
        }


        /// <summary>
        /// Set the monster phase into the right state for the attack phase and enables the tiles on which the monster can attack.
        /// </summary>
        private void AttackSelect()
        {
            tilesEnabledPos = SetMonsterAttack(CurrentMonster, GameManager.Instance?.GridActionHandler);

            if(tilesEnabledPos != null && tilesEnabledPos.Count > 0)
            {
                if (CurrentMonster.ActionPhase == Monster.eActionPhase.WAIT)
                    CurrentMonster.TryFirstAttack();
                else if (CurrentMonster.ActionPhase == Monster.eActionPhase.PHASE4_ATTACK_SELECT)
                    CurrentMonster.TrySecondAttack();
            }
        }

        /// <summary>
        /// From the list of tiles enabled for the attack, checks if the thief is on one of those tiles and applies the attack if so.
        /// If thief is attacked, it is instantanly killed.
        /// </summary>
        /// <param name="tiles">The tiles to check</param>
        /// <param name="thief">The thief and its location</param>
        /// <param name="grid">The grid to evaluate if thief is on the target of an attack</param>
        /// <returns>True if thief is attacked. False otherwise</returns>
        public bool Attack(List<Vector2> tiles, Thief thief, Grid grid)
        {
            
            bool thiefIsAttacked = false;
            thiefIsAttacked = grid.IsTargetOnEnabledTiles(tiles, thief);

            if (thiefIsAttacked)
            {
                currentMonster.OnMonsterAttack(thief);
                thief.OnThiefAttacked();
            }

            return thiefIsAttacked;
            
        }

        /// <summary>
        /// Executes the monster's attack phase, attempting to attack any eligible thief character on the grid : 
        /// If an attack is successful, the monster's turn ends and the character turn index is incremented.
        /// </summary>
        /// <remarks>This method iterates through all characters in the game and attempts to perform an
        /// attack on each thief character found.  If an attack is successful, the monster's turn ends and the character
        /// turn index is incremented.  If no attack is successful and the monster is in the first attack phase, the
        /// monster's failed attack handler is invoked.</remarks>
        private void Attack()
        {
            bool thiefAttacked = false;
            foreach(Character character in GameManager.Instance.CharactersManager.Characters)
            {
                Thief thief = character as Thief;
                if(thief == null)
                    continue;
                thiefAttacked = Attack(tilesEnabledPos, thief, GameManager.Instance.CurrentGrid);
                if(thiefAttacked)
                {
                    break;
                }
            }

            GameManager.Instance?.GridActionHandler.OnMonsterAttackDisable();
            if (thiefAttacked || currentMonster.ActionPhase == Monster.eActionPhase.PHASE5_ATTACK)
            {
                currentMonster.EndTurn();
                GameManager.Instance?.IncrementCharacterTurnIndex();
            }
            else if(currentMonster.ActionPhase == Monster.eActionPhase.PHASE1_FIRST_ATTACK)
            {
                currentMonster.OnFirstAttackFailed();
            }


        }

        /// <summary>
        /// Determines the valid movement tiles for the specified monster on the given grid.
        /// </summary>
        /// <remarks>This method does not move the monster; it only determines which tiles are available
        /// for movement.</remarks>
        /// <param name="monster">The monster for which to calculate available movement tiles. Cannot be <c>null</c>.</param>
        /// <param name="grid">The grid action handler to manage monster move action on the grid.</param>
        /// <returns>A list of <see cref="Vector2"/> positions representing the tiles the monster can move to. Returns an empty
        /// list if no valid moves are available or if either parameter is <c>null</c>.</returns>
        public List<Vector2> SetMonsterMove(Monster monster, GridActionHandler gridActionHandler)
        {
            
            List<Vector2> enabledTiles = new List<Vector2>();

            if(gridActionHandler != null && monster != null)
            {
                enabledTiles = gridActionHandler.OnMonsterMoveEnable(monster);
            }

            return enabledTiles;

        }


        /// <summary>
        /// Selects the shortest available movement route for the specified monster to reach any of the given thieves on
        /// the provided grid.
        /// </summary>
        /// <remarks>The method evaluates all provided thieves and determines the shortest path from the
        /// monster's current position to each thief using the grid's movement logic.  If multiple thieves are present,
        /// the route to the closest one (in terms of path length) is selected.  If no valid route exists to any thief,
        /// the method returns an empty list.</remarks>
        /// <param name="monster">The monster for which to calculate the movement route.</param>
        /// <param name="grid">The grid representing the environment in which the monster and thieves are located.</param>
        /// <param name="thieves">A list of thieves that the monster may pursue.</param>
        /// <returns>A list of <see cref="Vector2"/> points representing the shortest movement route from the monster to the
        /// nearest thief.  Returns an empty list if no valid route is found or if an error occurs.</returns>
        public List<Vector2> SelectShortestMoveRoute(Monster monster, Grid grid, List<Thief> thieves)
        {
            
            bool bSelectedRouteInit = true;
            List<Vector2> selectedMoveRoute = new List<Vector2>();

            foreach(Thief thief in thieves)
            {
                Vector2 thiefLoc = new Vector2(thief.X, thief.Y);
                List<Vector2> moveRoute = PathFinder.ComputeMoveRoute(monster, GameManager.Instance?.CurrentGrid, thiefLoc, grid.Height, true);

                if (moveRoute != null)
                {
                    if(bSelectedRouteInit == true)
                    {
                        selectedMoveRoute = moveRoute;
                        bSelectedRouteInit = false;
                    }
                    else if( moveRoute.Count < selectedMoveRoute.Count)
                    {
                        selectedMoveRoute = moveRoute;
                    }
                }
            }

            return selectedMoveRoute;
            
        }

        /// <summary>
        /// Adjusts the provided movement route to fit within the specified movement range.
        /// </summary>
        /// <remarks>Use this method to ensure that a movement route does not exceed a character's allowed
        /// movement range.  The returned list will contain the earliest positions up to the specified range
        /// limit.</remarks>
        /// <param name="moveRoute">A list of <see cref="Vector2"/> positions representing the original movement route. Cannot be <c>null</c>.</param>
        /// <param name="moveRange">The maximum number of positions to include in the adjusted route. Must be non-negative.</param>
        /// <returns>A new list of <see cref="Vector2"/> positions containing at most <paramref name="moveRange"/> elements from
        /// the start of <paramref name="moveRoute"/>.  If <paramref name="moveRoute"/> is <c>null</c> or contains fewer
        /// elements than <paramref name="moveRange"/>, the original list is returned.</returns>
        public List<Vector2> AdjustRouteFromMoveRange(List<Vector2> moveRoute, int moveRange)
        {
            
            if(moveRoute == null || moveRoute.Count <= moveRange)
            {
                return moveRoute;
            }

            List<Vector2> adjustedMoveRoute = new List<Vector2>();

            foreach(Vector2 tileLoc in moveRoute)
            {
                adjustedMoveRoute.Add(tileLoc);
                if(adjustedMoveRoute.Count >= moveRange)
                {
                    break;
                }
            }

            return adjustedMoveRoute;
        }

        /// <summary>
        /// The method is called when the monster is in the move selection phase. 
        /// It determines the valid movement tiles for the monster, selects the shortest route to reach any of the thieves on the grid, 
        /// and instructs the monster to move along that route. If no valid route exists, a random movement route is selected instead.  
        /// Additionally, if the final tile in the selected route coincides with a thief's location, that tile is removed from the route to prevent moving onto the thief's position.
        /// </summary>
        private void MoveSelect()
        {
            tilesEnabledPos = SetMonsterMove(currentMonster, GameManager.Instance?.GridActionHandler);
            List<Thief> thieves = new List<Thief>();
            foreach(Character character in GameManager.Instance?.CharactersManager.Characters)
            {
                Thief thief = character as Thief;
                if (thief != null)
                    thieves.Add(thief);
            }
            List<Vector2> moveRoute = SelectShortestMoveRoute(currentMonster, GameManager.Instance?.CurrentGrid, thieves);

            if(moveRoute.Count ==0)
            {
                moveRoute = PathFinder.GetRandomMoveRoute(currentMonster, GameManager.Instance?.CurrentGrid);
            }
            else
            {
                moveRoute = AdjustRouteFromMoveRange(moveRoute, currentMonster.MoveRange);
            }

       

            foreach(Thief thief1 in thieves)
            {
                Vector2 thiefLoc = new Vector2(thief1.X, thief1.Y);
                if(moveRoute[moveRoute.Count - 1] == thiefLoc)
                {
                     moveRoute.RemoveAt(moveRoute.Count - 1);
                    break;
                }
            }

            currentMonster.OnMonsterMoveRouteSelected(moveRoute);

        }



    }
}


