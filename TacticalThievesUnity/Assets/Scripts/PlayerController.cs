using System.Collections;
using System.Collections.Generic;
using TacticalThieves;
using UnityEngine;
using static TacticalThieves.Thief;

namespace TacticalThieves
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] Thief selectedThief;
        [SerializeField] Grid levelGrid;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void OnGridStarted(Grid grid)
        {
            levelGrid = grid;
        }

        public void OnThiefSelected(Thief thief, bool leftClickUsed)
        {

            selectedThief = thief;
            if (leftClickUsed)
                thief.EnableMove(thief.Status != eThiefStatus.MovementEnable, levelGrid);
            else
                thief.EnableStealth(!thief.Stealth);
        }

        public void OnTileSelected(Tile tile)
        {
            List<Vector2> moveRoute = levelGrid.ComputeMoveRoute(selectedThief, tile);
            selectedThief.SetMoveRoute(moveRoute);

        }

        public void HandleThiefMove()
        {
            if (selectedThief == null)
            {
                GameObject thiefGO = GameObject.FindGameObjectWithTag("Thief");
                if (thiefGO == null)
                    return;
                Thief thief = thiefGO.GetComponent<Thief>();
                if (thief == null)
                    return;
                selectedThief = thief;
            }

            selectedThief.EnableMove(true, levelGrid);
        }

        public void HandleThiefStealth()
        {
            GameObject thiefGO = GameObject.FindGameObjectWithTag("Thief");
            if (thiefGO == null)
                return;
            Thief thief = thiefGO.GetComponent<Thief>();
            if (thief == null)
                return;
            thief.EnableStealth(!thief.Stealth);
        }

    }
}

