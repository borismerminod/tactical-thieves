using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
//using UnityEditor.Animations;
using UnityEngine;

namespace TacticalThieves
{
    /// <summary>
    /// Represents a thief character controlled by the player. The <see cref="Thief"/> class
    /// manages movement routes, stealth state and reactions to being attacked or reaching the exit.
    /// </summary>
    public class Thief : Character
    {

        /// <summary>
        /// Indicates the current high-level status of the thief.
        /// </summary>
        public enum eThiefStatus
        {
            Dead = 0,
            Wait = 1,
            MovementEnable = 2,
            isMoving = 3 
        }

        [SerializeField] private eThiefStatus status;
        [SerializeField] private List<Vector2> currentMoveRoute;
        [SerializeField] private int currentRouteIndex;
        [SerializeField] private bool stealth;
        [SerializeField] private Material defaultMaterial;
        [SerializeField] private Material stealthMaterial;
        [SerializeField] private GameObject thiefBody;
        [SerializeField] private GameObject model;
        [SerializeField] private GameObject ragdollModel;
        [SerializeField] private GameObject impactEffect;
        [SerializeField] private PlayerController playerController;
        [SerializeField] Animator animator;

        [SerializeField] private bool moveTest; //A supprimé quand la phase de développement sera terminée

        /// <summary>
        /// Gets whether the thief is currently in stealth mode.
        /// </summary>
        public bool Stealth { get => stealth; private set => stealth = value; }

        /// <summary>
        /// Development-only flag used to simulate input during testing. Can be toggled in the inspector.
        /// </summary>
        public bool MoveTest { get => moveTest; set => moveTest = value; }
        
       
        /// <summary>
        /// Gets the current status of the thief (dead, waiting, movement enabled or moving).
        /// </summary>
        public eThiefStatus Status { get => status; private set => status = value; }

        /// <summary>
        /// Unity start callback. Initializes the thief and retrieves references such as the
        /// <see cref="PlayerController"/> and <see cref="Animator"/>. Any setup failures are logged.
        /// </summary>
        void Start()
        {
            try
            {
                OnThiefStarted();
                GameManager.Instance?.OnCharacterStarted(this);
                playerController = GameManager.Instance?.CurrentPlayerController; 
                animator = model?.GetComponent<Animator>();
                impactEffect?.SetActive(false);
            }
            catch(System.Exception ex)
            {
                Debug.LogError($"Error in Thief Start: {ex.Message}");
            }
        }

      
 
        /// <summary>
        /// Unity update callback. Advances movement if the thief is currently in the moving state.
        /// Exceptions are caught and logged to avoid breaking the update loop.
        /// </summary>
        void Update()
        {
            try
            {
                if(status == eThiefStatus.isMoving)
                    Move();
            }
            catch(System.Exception ex)
            {
                Debug.LogError($"Error in Thief Update: {ex.Message}");
            }
        }

        /// <summary>
        /// Moves the thief towards the next tile in the current route by rotating and translating
        /// the transform. Movement is frame-rate independent.
        /// </summary>
        private void Move()
        {

            if (currentRouteIndex < 0 || currentRouteIndex >= currentMoveRoute.Count)
                return;

            Tile nextTileDestination = GameManager.Instance?.CurrentGrid.GetTile(currentMoveRoute[currentRouteIndex]);

            Vector3 direction = (nextTileDestination.transform.position - transform.position).normalized;
            direction = new Vector3(direction.x, 0.0f, direction.z);
            transform.rotation = Quaternion.LookRotation(direction);
            transform.Translate(Vector3.forward * 1 * Time.deltaTime);

        }

        private void OnMouseUp()
        {
            try
            {
                OnThiefSelected();
            }
            catch(System.Exception ex)
            {
                Debug.LogError($"Error in Thief OnMouseUp: {ex.Message}");
            }   
        }

        /// <summary>
        /// Invoked when the thief is clicked by the player. Delegates selection handling to the
        /// current <see cref="PlayerController"/>.
        /// </summary>
        public void OnThiefSelected()
        {
            playerController?.OnThiefSelected(this, moveTest);
        }

        /// <summary>
        /// Enables or disables movement for this thief. When enabling, the grid action handler
        /// is instructed to highlight reachable tiles. When disabling, highlights are removed and
        /// the running animation is cleared.
        /// </summary>
        /// <param name="bCanMove">If <c>true</c>, movement is enabled; otherwise movement is disabled.</param>
        /// <param name="gridActionHandler">The grid action handler used to enable/disable tile highlights.</param>
        public void EnableMove(bool bCanMove, GridActionHandler gridActionHandler)
        {
            if (bCanMove)
            {
                status = eThiefStatus.MovementEnable;
                gridActionHandler.EnableTilesForMove(this);
            }
            else
            {
                status = eThiefStatus.Wait;
                gridActionHandler.DisableTilesForMove();
                animator?.SetBool(Utils.AnimatorParam.Run, false);
            }       
        }

        /// <summary>
        /// Starts or stops the thief's movement along the previously assigned route. When
        /// stopping, the thief signals the game manager to advance the turn index.
        /// </summary>
        /// <param name="bCanMove">If <c>true</c>, movement begins; if <c>false</c>, movement ends.</param>
        public void ProceedMovement(bool bCanMove)
        {
            if(bCanMove)
            {
                status = eThiefStatus.isMoving;
                currentRouteIndex = 0;
                animator?.SetBool(Utils.AnimatorParam.Run, true);
            }
            else
            {
                status = eThiefStatus.Wait;
                GameManager.Instance?.IncrementCharacterTurnIndex();
            }
        }

        /// <summary>
        /// Assigns a movement route and immediately begins movement along it.
        /// </summary>
        /// <param name="moveRoute">List of tile coordinates representing the route.</param>
        public void SetMoveRoute(List<Vector2> moveRoute)
        {
            currentMoveRoute = moveRoute;
            ProceedMovement(true);
        }

        /// <summary>
        /// Checks whether the thief has reached a new tile while moving. When the route end is
        /// reached, movement is stopped and tile highlights are disabled.
        /// </summary>
        /// <param name="tile">The tile currently occupied by the thief.</param>
        public void CheckCurrentTileLocation(Tile tile)
        {

            if (status != eThiefStatus.isMoving || tile == null)
                return;
            
            if (tile.X != X || tile.Y != Y)
            {
                X = tile.X;
                Y = tile.Y;
                currentRouteIndex++;

                if (currentRouteIndex >= currentMoveRoute.Count)
                {
                    ProceedMovement(false);

                    EnableMove(false, GameManager.Instance?.GridActionHandler);
                }

            }
            
        }

        /// <summary>
        /// Enables or disables stealth mode for the thief and updates the visible material.
        /// </summary>
        /// <param name="enable">If <c>true</c> stealth is enabled; otherwise it is disabled.</param>
        public void EnableStealth(bool enable)
        {
            stealth = enable;
            SetStealthRendering(enable);
        }

        private void SetStealthRendering(bool enable)
        {
            if (enable)
            {
                thiefBody.GetComponent<Renderer>().material = stealthMaterial;
            }
            else
            {
                thiefBody.GetComponent<Renderer>().material = defaultMaterial;
            }
        }

        /// <summary>
        /// Handles the thief being attacked: sets status to dead, plays the impact effect,
        /// notifies the game manager and swaps the visible model to a ragdoll representation.
        /// </summary>
        public void OnThiefAttacked()
        {
            status = eThiefStatus.Dead;
            impactEffect?.SetActive(true);

            GameManager.Instance?.OnThiefDied();
            GameManager.Instance?.CurrentAudioManager?.OnMonsterAttack();
            model.SetActive(false);
                ragdollModel.SetActive(true);
 
        }

        /// <summary>
        /// Initializes the thief state when the character is started.
        /// </summary>
        public void OnThiefStarted()
        {
            status = eThiefStatus.Wait;
        }

        /// <summary>
        /// Called when the thief reaches the exit: triggers the win animation.
        /// </summary>
        public void OnThiefReachedExit()
        {
            animator?.SetBool(Utils.AnimatorParam.Win, true);
        }
    }
}

