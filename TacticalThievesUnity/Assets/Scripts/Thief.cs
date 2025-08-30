using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TacticalThieves
{
    public class Thief : Object
    {

        public enum eThiefStatus
        {
            Wait = 0,
            MovementEnable = 1,
            isMoving = 2 
        }

        [SerializeField] private int moveRange;
        [SerializeField] private eThiefStatus status;
        
       

        public int MoveRange { get => moveRange; set => moveRange = value; }
        public eThiefStatus Status { get => status; private set => status = value; }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        private void OnMouseUp()
        {

            GameObject playerControllerGO = GameObject.FindGameObjectWithTag("PlayerController");
            if(playerControllerGO == null)
                return;
            PlayerController playerController = playerControllerGO.GetComponent<PlayerController>();
            playerController.OnThiefSelected(this);

            
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
            }       
        }

        public void ProceedMovement(bool bCanMove)
        {
            if(bCanMove)
            {
                status = eThiefStatus.isMoving;
            }
            else
            {
                status = eThiefStatus.Wait;
            }
        }
    }
}

