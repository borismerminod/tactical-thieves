using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.TestTools;

namespace TacticalThieves
{
    public class AIController : MonoBehaviour
    {
        [SerializeField] private Monster currentMonster;
        [SerializeField] private List<Vector2> tilesEnabledPos;
        [SerializeField] private bool bTestMode = true;

        public Monster CurrentMonster {  get =>  currentMonster; set => currentMonster = value;}

        // Start is called before the first frame update
        void Start()
        {
            if (bTestMode)
            {
                //TODO : Test à supprimer
                GameObject monsterGO = GameObject.FindGameObjectWithTag("Monster");
                CurrentMonster = monsterGO.GetComponent<Monster>();
                OnMonsterSelected(CurrentMonster);
            }

            InvokeRepeating("ProcessMonsterActions", 0.1f, 0.5f);
        }

        // Update is called once per frame
        void Update()
        {
            //ProcessMonsterActions();
        }

        private void ProcessMonsterActions()
        {
            if(currentMonster == null)
                return;

            switch(currentMonster.ActionPhase)
            {
                case Monster.eActionPhase.WAIT:
                    AttackSelect();
                    //tilesEnabledPos = SetMonsterFirstAttack(currentMonster, GameManager.Instance?.CurrentGrid);
                    break;
                case Monster.eActionPhase.PHASE1_FIRST_ATTACK:
                    Attack();
                    break;
                case Monster.eActionPhase.PHASE2_MOVE_SELECT:
                    MoveSelect();
                    break;
                case Monster.eActionPhase.PHASE4_ATTACK_SELECT :
                    AttackSelect();
                    break;
                case Monster.eActionPhase.PHASE5_ATTACK:
                    Attack();
                    break;
            }
        }

        public bool OnMonsterSelected(Monster monster)
        {
            if (monster == null) return false;  
            CurrentMonster = monster;
            CurrentMonster.Init();

            return true;
        }

        public List<Vector2> SetMonsterAttack(Monster monster, Grid grid)
        {
            List<Vector2> tiles = new List<Vector2>();
            if(monster != null && grid != null)
            {
                tiles = grid.OnMonsterAttackEnable(monster);
            }

            return tiles;
        }

        private void AttackSelect()
        {
            tilesEnabledPos = SetMonsterAttack(CurrentMonster, GameManager.Instance?.CurrentGrid);

            if(tilesEnabledPos != null && tilesEnabledPos.Count > 0)
            {
                if (CurrentMonster.ActionPhase == Monster.eActionPhase.WAIT)
                    CurrentMonster.TryFirstAttack();
                else if (CurrentMonster.ActionPhase == Monster.eActionPhase.PHASE4_ATTACK_SELECT)
                    CurrentMonster.TrySecondAttack();
            }
        }

        public bool  Attack(List<Vector2> tiles, Thief thief, Grid grid)
        {
            bool thiefIsAttacked = false;
            thiefIsAttacked = grid.IsTargetOnEnabledTiles(tiles, thief);

            if (thiefIsAttacked)
            {
                thief.OnThiefAttacked();
            }


            return thiefIsAttacked;
        }

        private void Attack()
        {
            bool thiefAttacked = false;
            foreach(Thief thief in GameManager.Instance.Thieves)
            {
                thiefAttacked = Attack(tilesEnabledPos, thief, GameManager.Instance.CurrentGrid);
                if(thiefAttacked) 
                    break;
            }

            if(thiefAttacked || currentMonster.ActionPhase == Monster.eActionPhase.PHASE5_ATTACK)
            {
                currentMonster.Init();
            }
            else if(currentMonster.ActionPhase == Monster.eActionPhase.PHASE1_FIRST_ATTACK)
            {
                currentMonster.OnFirstAttackFailed();
            }

                GameManager.Instance.CurrentGrid.OnMonsterAttackDisable();
        }

        public List<Vector2> SetMonsterMove(Monster monster, Grid grid)
        {
            List<Vector2> enabledTiles = new List<Vector2>();

            if(grid != null && monster != null)
            {
                enabledTiles = grid.OnMonsterMoveEnable(monster);
            }

            return enabledTiles;
        }

        public List<Vector2> SelectShortestMoveRoute(Monster monster, Grid grid, List<Thief> thieves)
        {
            bool bSelectedRouteInit = true;
            List<Vector2> selectedMoveRoute = new List<Vector2>();

            foreach(Thief thief in thieves)
            {
                Vector2 thiefLoc = new Vector2(thief.X, thief.Y);
                List<Vector2> moveRoute = grid.ComputeMoveRoute(monster, thiefLoc, grid.Height);

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

        private void MoveSelect()
        {
            tilesEnabledPos = SetMonsterMove(currentMonster, GameManager.Instance?.CurrentGrid);
            List<Vector2> moveRoute = SelectShortestMoveRoute(currentMonster, GameManager.Instance?.CurrentGrid, GameManager.Instance?.Thieves);
            moveRoute = AdjustRouteFromMoveRange(moveRoute, currentMonster.MoveRange);

            currentMonster.OnMonsterMoveRouteSelected(moveRoute);

        }



    }
}


