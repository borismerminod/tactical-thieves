using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TacticalThieves.Thief;


namespace TacticalThieves
{
    public class Monster : Character
    {

        public enum eActionPhase
        {
            WAIT,
            PHASE1_FIRST_ATTACK,
            PHASE2_MOVE_SELECT, 
            PHASE3_MOVE,
            PHASE4_ATTACK_SELECT,
            PHASE5_ATTACK,
            END_TURN
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

        [SerializeField] List<Vector2> currentMoveRoute;
        [SerializeField] int currentRouteIndex;
    

        // Start is called before the first frame update
        void Start()
        {
            Init();
            GameManager.Instance?.OnCharacterStarted(this);
        }

        public void Init()
        {
            ActionPhase = eActionPhase.WAIT;
        }

        // Update is called once per frame
        void Update()
        {
            if (ActionPhase == eActionPhase.PHASE3_MOVE)
                Move();
        }

        public void TryFirstAttack()
        {
            ActionPhase = eActionPhase.PHASE1_FIRST_ATTACK;
        }

        //TODO : A tester
        public void TrySecondAttack()
        {
            ActionPhase = eActionPhase.PHASE5_ATTACK;
        }

        public void EndTurn()
        {
            ActionPhase = eActionPhase.END_TURN;
        }

        public void OnFirstAttackFailed()
        {
            ActionPhase = eActionPhase.PHASE2_MOVE_SELECT;
        }

        public void OnMonsterMoveRouteSelected(List<Vector2> moveRoute)
        {
            ActionPhase = eActionPhase.PHASE3_MOVE;
            currentMoveRoute = moveRoute;
        }

        private void Move()
        {
            Grid grid = GameManager.Instance?.CurrentGrid;

            if (grid == null)
                return;

            if (currentRouteIndex < 0 || currentRouteIndex >= currentMoveRoute.Count)
                return;

            Tile nextTileDestination = grid.GetNextTileMove(currentMoveRoute[currentRouteIndex]);

            Vector3 direction = (nextTileDestination.transform.position - transform.position).normalized;
            direction = new Vector3(direction.x, 0.0f, direction.z);
            transform.rotation = Quaternion.LookRotation(direction);
            transform.Translate(Vector3.forward * 1 * Time.deltaTime);
        }

        //TODO : A tester
        public void CheckCurrentTileLocation(Tile tile)
        {

            if (ActionPhase != eActionPhase.PHASE3_MOVE || tile == null)
                return;

            if (tile.X != X || tile.Y != Y)
            {
                X = tile.X;
                Y = tile.Y;
                currentRouteIndex++;

                if (currentRouteIndex >= currentMoveRoute.Count)
                {
                    ActionPhase = eActionPhase.PHASE4_ATTACK_SELECT;
                    currentRouteIndex = 0;


                    Grid grid = GameManager.Instance?.CurrentGrid;
                    if (grid == null)
                        return;

                    grid.OnMonsterMoveDisable();
                }

            }

        }

    }

}
