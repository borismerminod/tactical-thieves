using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TacticalThieves
{
    public class Thief : Object
    {
        [SerializeField] private int moveRange;
        [SerializeField] private bool movementEnable;
        
       

        public int MoveRange { get => moveRange; set => moveRange = value; }
        public bool MovementEnable { get => movementEnable; private set => movementEnable = value; }

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
            GameObject gridGO = GameObject.FindGameObjectWithTag("Grid");
            if(gridGO == null)
                return;

            Grid grid = gridGO.GetComponent<Grid>();
            if (grid==null)
                return;
            EnableMove(!movementEnable, grid);
        }

        public void EnableMove(bool bCanMove, Grid grid)
        {
            movementEnable = bCanMove;
            if (movementEnable)
            {
                grid.OnThiefMoveEnable(this);
            }
            else
            {
                grid.OnThiefMoveDisable();
            }       
        }
    }
}

