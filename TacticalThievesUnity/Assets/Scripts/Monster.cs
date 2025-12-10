using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TacticalThieves.Thief;
using DG.Tweening;


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

        [SerializeField] GameObject model;
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
            model.GetComponent<Animator>().SetBool("Run", false);
        }

        // Update is called once per frame
        void Update()
        {
            SetEndGameAnimation();
            if (ActionPhase == eActionPhase.PHASE3_MOVE)
                Move();
        }

        private void SetEndGameAnimation()
        {
            if (model.GetComponent<Animator>().GetBool("Defeat") == true || model.GetComponent<Animator>().GetBool("Win") == true)
                return;

            switch (GameManager.Instance?.GetGameState())
            {
                case GameManager.GameState.WIN:
                    model.GetComponent<Animator>().SetBool("Defeat", true);
                    break;
                case GameManager.GameState.LOSE:
                    model.GetComponent<Animator>().SetBool("Win", true);
                    break;
            }
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
            model?.GetComponent<Animator>().SetBool("Run", false);
        }

        public void OnFirstAttackFailed()
        {
            ActionPhase = eActionPhase.PHASE2_MOVE_SELECT;
            model?.GetComponent<Animator>().SetBool("Run", false);
        }

        public void OnMonsterMoveRouteSelected(List<Vector2> moveRoute)
        {
            ActionPhase = eActionPhase.PHASE3_MOVE;
            currentMoveRoute = moveRoute;

            model?.GetComponent<Animator>().SetBool("Run", true);
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

                if (currentRouteIndex >= currentMoveRoute.Count ||  currentMoveRoute[currentRouteIndex] == Vector2.zero)
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

        public void OnMonsterAttack(Thief thief)
        {
            Vector3 direction = (thief.transform.position - transform.position).normalized;
            direction = new Vector3(direction.x, 0.0f, direction.z);
            transform.rotation = Quaternion.LookRotation(direction);
            model?.GetComponent<Animator>().SetBool("Attack", true);

            DOVirtual.DelayedCall(0.5f, () =>
            {
                model?.GetComponent<Animator>().SetBool("Attack", false);
            });
        }

    }

}
