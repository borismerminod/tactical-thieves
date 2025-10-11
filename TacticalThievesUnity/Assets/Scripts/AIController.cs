using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TacticalThieves
{
    public class AIController : MonoBehaviour
    {
        [SerializeField] private Monster currentMonster;

        public Monster CurrentMonster {  get =>  currentMonster; set => currentMonster = value;}

        // Start is called before the first frame update
        void Start()
        {
            //TODO : Test à supprimer
            GameObject monsterGO = GameObject.FindGameObjectWithTag("Monster");
            CurrentMonster = monsterGO.GetComponent<Monster>();
            OnMonsterSelected(CurrentMonster);
        }

        // Update is called once per frame
        void Update()
        {
            ProcessMonsterActions();
        }

        private void ProcessMonsterActions()
        {
            if(currentMonster == null)
                return;

            switch(currentMonster.ActionPhase)
            {
                case Monster.eActionPhase.WAIT:
                    SetMonsterFirstAttack(currentMonster, GameManager.Instance.CurrentGrid);
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

        public bool SetMonsterFirstAttack(Monster monster, Grid grid)
        {
            if(monster == null) return false;
            if(grid == null) return false;

            monster.TryFirstAttack();
            return grid.OnMonsterAttackEnable(monster);
        }
    }
}


