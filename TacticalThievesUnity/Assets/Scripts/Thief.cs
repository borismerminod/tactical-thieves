using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TacticalThieves
{
    public class Thief : Character
    {

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

        [SerializeField] private bool moveTest; //A supprimé quand la phase de développement sera terminée

        public bool Stealth { get => stealth; private set => stealth = value; }
        public bool MoveTest { get => moveTest; set => moveTest = value; }
        
       
        public eThiefStatus Status { get => status; private set => status = value; }

        // Start is called before the first frame update
        void Start()
        {
            OnThiefStarted();
            GameManager.Instance?.OnCharacterStarted(this);
        }

        // Update is called once per frame
        void Update()
        {
            if(status == eThiefStatus.isMoving)
                Move();
        }

        private void Move()
        {

            if (currentRouteIndex < 0 || currentRouteIndex >= currentMoveRoute.Count)
                return;

            Tile nextTileDestination = GameManager.Instance?.CurrentGrid.GetNextTileMove(currentMoveRoute[currentRouteIndex]);

            Vector3 direction = (nextTileDestination.transform.position - transform.position).normalized;
            direction = new Vector3(direction.x, 0.0f, direction.z);
            transform.rotation = Quaternion.LookRotation(direction);
            transform.Translate(Vector3.forward * 1 * Time.deltaTime);

        }

        private void OnMouseUp()
        {
            OnThiefSelected();
        }

        public void OnThiefSelected()
        {
            GameObject playerControllerGO = GameObject.FindGameObjectWithTag("PlayerController");
            if (playerControllerGO == null)
                return;
            PlayerController playerController = playerControllerGO.GetComponent<PlayerController>();


            playerController.OnThiefSelected(this, moveTest);
        }

        public void EnableMove(bool bCanMove, Grid grid)
        {
            if (bCanMove)
            {
                status = eThiefStatus.MovementEnable;
                grid.OnThiefMoveEnable(this);
            }
            else
            {
                status = eThiefStatus.Wait;
                grid.OnThiefMoveDisable();
                Debug.Log("TEST");
                GameManager.Instance?.IncrementCharacterTurnIndex();
            }       
        }

        public void ProceedMovement(bool bCanMove)
        {
            if(bCanMove)
            {
                status = eThiefStatus.isMoving;
                currentRouteIndex = 0;
            }
            else
            {
                status = eThiefStatus.Wait;
            }
        }

        public void SetMoveRoute(List<Vector2> moveRoute)
        {
            currentMoveRoute = moveRoute;
            ProceedMovement(true);
        }

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

                    EnableMove(false, GameManager.Instance?.CurrentGrid);
                }

            }
            
        }

        public void EnableStealth(bool enable)
        {
            stealth = enable;
            if(enable)
            {
                thiefBody.GetComponent<Renderer>().material = stealthMaterial;
            }
            else
            {
                thiefBody.GetComponent<Renderer>().material = defaultMaterial;
            }
        }

        public void OnThiefAttacked()
        {
            status = eThiefStatus.Dead;
            GameManager.Instance?.OnThiefDied();
        }

        public void OnThiefStarted()
        {
            status = eThiefStatus.Wait;
        }
    }
}

