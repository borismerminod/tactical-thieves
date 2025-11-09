using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace TacticalThieves
{
    public class Monster : Character
    {

        public enum eActionPhase
        {
            WAIT,
            PHASE1_FIRST_ATTACK,
            PHASE2_MOVE
        }

        [SerializeField] bool testMode;
        public bool TestMode {get => testMode; set => testMode = value; }

        [SerializeField] eActionPhase actionPhase;
        public eActionPhase ActionPhase { get => actionPhase; set => actionPhase = value; }

        [SerializeField] int attackRange;
        public int AttackRange
        {
            get => attackRange;
            set
            {
                attackRange = value;
                if(attackRange < 0)
                    attackRange = 0;
            }
        }
    

        // Start is called before the first frame update
        void Start()
        {
            Init();
        }

        public void Init()
        {
            ActionPhase = eActionPhase.WAIT;
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        public void TryFirstAttack()
        {
            ActionPhase = eActionPhase.PHASE1_FIRST_ATTACK;
        }

        public void OnFirstAttackFailed()
        {
            ActionPhase = eActionPhase.PHASE2_MOVE;
        }
    }

}
