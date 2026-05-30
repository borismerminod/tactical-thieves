using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TacticalThieves.Thief;
using DG.Tweening;


namespace TacticalThieves
{
    /// <summary>
    /// Represents a monster character that can select actions (attack/move) during its turn.
    /// The <see cref="Monster"/> class extends <see cref="Character"/> and provides state
    /// management for action phases, movement along computed routes and animation control.
    /// </summary>
    public class Monster : Character
    {

        /// <summary>
        /// Defines the discrete action phases a monster can be in during AI processing.
        /// </summary>
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

        /// <summary>
        /// When set, alters initialization behavior for testing scenarios (for example
        /// forcing tiles to be walkable).
        /// </summary>
        [SerializeField] bool testMode;
        public bool TestMode {get => testMode; set => testMode = value; }

        /// <summary>
        /// Current action phase of the monster. Controls which AI logic will be executed
        /// by the <see cref="AIController"/>.
        /// </summary>
        [SerializeField] eActionPhase actionPhase;
        public eActionPhase ActionPhase { get => actionPhase; set => actionPhase = value; }

        /// <summary>
        /// Range (in tiles) at which the monster can perform an attack.
        /// </summary>
        [SerializeField] int attackRange;

        /// <summary>
        /// Reference to the monster model GameObject which is expected to contain an Animator.
        /// </summary>
        [SerializeField] GameObject model;

        /// <summary>
        /// Animator component used to control the monster's animations. It is retrieved from
        /// the <see cref="model"/> on Start if not assigned explicitly.
        /// </summary>
        [SerializeField] Animator animator;

        /// <summary>
        /// Gets or sets the attack range. The setter clamps negative values to zero.
        /// </summary>
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

        /// <summary>
        /// The currently selected movement route expressed as a list of grid coordinates.
        /// </summary>
        [SerializeField] List<Vector2> currentMoveRoute;

        /// <summary>
        /// Index into <see cref="currentMoveRoute"/> representing the next destination tile to
        /// reach while moving.
        /// </summary>
        [SerializeField] int currentRouteIndex;
        
    
 
        /// <summary>
        /// Unity start callback. Initializes the monster state, notifies the <see cref="GameManager"/>
        /// that the character has started and obtains the animator from the model GameObject.
        /// </summary>
        void Start()
        {
            Init();
            GameManager.Instance?.OnCharacterStarted(this);

            animator = model?.GetComponent<Animator>();
        }

        /// <summary>
        /// Initializes the monster's state, putting it in the <see cref="eActionPhase.WAIT"/>
        /// phase and ensuring the running animation flag is cleared.
        /// </summary>
        public void Init()
        {
            ActionPhase = eActionPhase.WAIT;
            animator?.SetBool(Utils.AnimatorParam.Run, false);
        }

        /// <summary>
        /// Unity update callback. Updates end-of-game animations and advances movement when
        /// the monster is in the move phase.
        /// </summary>
        void Update()
        {
            SetEndGameAnimation();
            if (ActionPhase == eActionPhase.PHASE3_MOVE)
                Move();
        }

        /// <summary>
        /// Sets the appropriate defeat/win animation flags based on the current game state
        /// if they are not already set.
        /// </summary>
        private void SetEndGameAnimation()
        {
            if (animator?.GetBool(Utils.AnimatorParam.Defeat) == true || animator?.GetBool(Utils.AnimatorParam.Win) == true)
                return;

            switch (GameManager.Instance?.GetGameState())
            {
                case GameManager.GameState.WIN:
                    animator?.SetBool(Utils.AnimatorParam.Defeat, true);
                    break;
                case GameManager.GameState.LOSE:
                    animator?.SetBool(Utils.AnimatorParam.Win, true);
                    break;
            }
        }

        /// <summary>
        /// Transition the monster into the first attack phase.
        /// </summary>
        public void TryFirstAttack()
        {
            ActionPhase = eActionPhase.PHASE1_FIRST_ATTACK;
        }

        /// <summary>
        /// Transition the monster into the final attack phase.
        /// </summary>
        public void TrySecondAttack()
        {
            ActionPhase = eActionPhase.PHASE5_ATTACK;
        }

        /// <summary>
        /// Ends the monster's turn, switching to the <see cref="eActionPhase.END_TURN"/> state
        /// and disabling the running animation.
        /// </summary>
        public void EndTurn()
        {
            ActionPhase = eActionPhase.END_TURN;
            animator?.SetBool(Utils.AnimatorParam.Run, false);
        }

        /// <summary>
        /// Called when the monster's first attack attempt failed. Moves the monster into the
        /// move selection phase and clears the running animation.
        /// </summary>
        public void OnFirstAttackFailed()
        {
            ActionPhase = eActionPhase.PHASE2_MOVE_SELECT;
            animator?.SetBool(Utils.AnimatorParam.Run, false);
        }

        /// <summary>
        /// Called when a movement route has been selected for this monster. Stores the route
        /// and starts the movement phase and the running animation.
        /// </summary>
        /// <param name="moveRoute">The list of grid coordinates representing the movement route.</param>
        public void OnMonsterMoveRouteSelected(List<Vector2> moveRoute)
        {
            ActionPhase = eActionPhase.PHASE3_MOVE;
            currentMoveRoute = moveRoute;

            animator?.SetBool(Utils.AnimatorParam.Run, true);
        }

        /// <summary>
        /// Advances the monster towards the next tile in <see cref="currentMoveRoute"/>. The
        /// method rotates the monster to face the destination and translates it forward each frame.
        /// </summary>
        private void Move()
        {
            Grid grid = GameManager.Instance?.CurrentGrid;

            if (grid == null)
                return;

            if (currentRouteIndex < 0 || currentRouteIndex >= currentMoveRoute.Count)
                return;

            Tile nextTileDestination = grid.GetTile(currentMoveRoute[currentRouteIndex]);

            Vector3 direction = (nextTileDestination.transform.position - transform.position).normalized;
            direction = new Vector3(direction.x, 0.0f, direction.z);
            transform.rotation = Quaternion.LookRotation(direction);
            transform.Translate(Vector3.forward * 1 * Time.deltaTime);
        }

        /// <summary>
        /// Checks whether the provided tile corresponds to a new tile the monster has reached
        /// while moving. When the end of the route is reached the monster enters the attack
        /// selection phase and movement-related tiles are disabled.
        /// </summary>
        /// <param name="tile">The tile currently occupied by the monster.</param>
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
                    animator?.SetBool(Utils.AnimatorParam.Run, false);


                    GridActionHandler gridActionHandler = GameManager.Instance?.GridActionHandler;
                    if (gridActionHandler == null)
                        return;


                    gridActionHandler.DisableTilesForMove();
                }

            }

        }

        /// <summary>
        /// Performs the visual attack sequence against the provided <see cref="Thief"/> by
        /// orienting the monster, triggering the attack animation and resetting the animation
        /// flag after a short delay.
        /// </summary>
        /// <param name="thief">The thief target being attacked.</param>
        public void OnMonsterAttack(Thief thief)
        {
            Vector3 direction = (thief.transform.position - transform.position).normalized;
            direction = new Vector3(direction.x, 0.0f, direction.z);
            transform.rotation = Quaternion.LookRotation(direction);
            animator?.SetBool(Utils.AnimatorParam.Attack, true);

            DOVirtual.DelayedCall(0.5f, () =>
            {
                animator?.SetBool(Utils.AnimatorParam.Attack, false);
            });
        }

    }

}
