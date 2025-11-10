using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace TacticalThieves
{
    public class AIController : MonoBehaviour
    {
        [SerializeField] private Monster currentMonster;
        [SerializeField] private List<Vector2> tilesEnabledPos;

        public Monster CurrentMonster {  get =>  currentMonster; set => currentMonster = value;}

        // Start is called before the first frame update
        void Start()
        {
            //TODO : Test à supprimer
            GameObject monsterGO = GameObject.FindGameObjectWithTag("Monster");
            CurrentMonster = monsterGO.GetComponent<Monster>();
            OnMonsterSelected(CurrentMonster);

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
                    tilesEnabledPos = SetMonsterFirstAttack(currentMonster, GameManager.Instance?.CurrentGrid);
                    break;
                case Monster.eActionPhase.PHASE1_FIRST_ATTACK:
                    Attack();
                    break;
                case Monster.eActionPhase.PHASE2_MOVE:
                    tilesEnabledPos = SetMonsterMove(currentMonster, GameManager.Instance?.CurrentGrid);
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

        public List<Vector2> SetMonsterFirstAttack(Monster monster, Grid grid)
        {
            List<Vector2> tiles = new List<Vector2>();
            if(monster != null && grid != null)
            {
                monster.TryFirstAttack();
                tiles = grid.OnMonsterAttackEnable(monster);
            }

            return tiles;
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

            if(thiefAttacked)
            {
                currentMonster.Init();
            }
            else
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



    }
}


