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

        public void OnThiefSelected(Thief thief)
        {
            selectedThief = thief;
            thief.EnableMove(thief.Status != eThiefStatus.MovementEnable, levelGrid);
        }

        public void OnTileSelected(Tile tile)
        {
            List<Vector2> moveRoute = levelGrid.ComputeMoveRoute(selectedThief, tile);
            selectedThief.SetMoveRoute(moveRoute);

        }

    }
}

