using System.Collections;
using System.Collections.Generic;
using TacticalThieves;
using UnityEngine;
using static TacticalThieves.Thief;

namespace TacticalThieves
{
    /// <summary>
    /// Handles player interactions with thief characters in the scene. This component
    /// receives input from the player (for example tile or thief selection) and delegates
    /// actions to the currently selected <see cref="Thief"/> such as enabling movement or
    /// toggling stealth.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        /// <summary>
        /// The thief currently selected by the player. Used as target for movement and stealth commands.
        /// </summary>
        [SerializeField] Thief selectedThief;

        /// <summary>
        /// Internal flag used to avoid loading the level multiple times when the space key is held.
        /// </summary>
        [SerializeField] bool levelLoaded;

        /// <summary>
        /// Unity start callback. Initializes internal flags.
        /// </summary>
        void Start()
        {
            levelLoaded = false;
        }

        /// <summary>
        /// Unity update callback. For development convenience this method loads level 1 when
        /// the space key is pressed (only once per session while the flag is false).
        /// </summary>
        void Update()
        {
            if(levelLoaded == false && Input.GetKey(KeyCode.Space))
            {
                GameManager.Instance?.LoadLevel(1);
                levelLoaded = true;
            }
        }


        /// <summary>
        /// Called when a thief is selected by the player. If <paramref name="leftClickUsed"/>
        /// is <c>true</c> toggles the thief's movement UI; otherwise toggles the thief's stealth.
        /// </summary>
        /// <param name="thief">The thief that was selected.</param>
        /// <param name="leftClickUsed">Indicates whether the selection was performed with the left click.</param>
        public void OnThiefSelected(Thief thief, bool leftClickUsed)
        {

            selectedThief = thief;
            
            if(thief != null)
            {
                if (leftClickUsed)
                    thief.EnableMove(thief.Status != eThiefStatus.MovementEnable, GameManager.Instance?.GridActionHandler);
                else
                    thief.EnableStealth(!thief.Stealth);
            }

        }

        /// <summary>
        /// Called when the player selects a tile. If a thief is selected and movement is enabled
        /// this method computes a movement route to the tile and assigns it to the thief.
        /// </summary>
        /// <param name="tile">The tile selected by the player.</param>
        public void OnTileSelected(Tile tile)
        {
            if (selectedThief == null || selectedThief.Status != eThiefStatus.MovementEnable)
                return;

            Vector2 tileLoc = new Vector2(tile.X, tile.Y);
            List<Vector2> moveRoute = PathFinder.ComputeMoveRoute(selectedThief, GameManager.Instance?.CurrentGrid, tileLoc, selectedThief.MoveRange, true ); 
            selectedThief.SetMoveRoute(moveRoute);

        }

        /// <summary>
        /// Enables movement for the currently selected thief, if any, by invoking the thief's
        /// movement enabling API and passing the grid action handler.
        /// </summary>
        public void HandleThiefMove()
        {
            selectedThief?.EnableMove(true, GameManager.Instance?.GridActionHandler);
        }

        /// <summary>
        /// Ends the selected thief's turn. If the thief is in movement-enabled state the method
        /// disables movement and instructs the thief to proceed or finish its movement.
        /// </summary>
        public void HandleThiefEndTurn()
        {

            if(selectedThief?.Status == eThiefStatus.MovementEnable)
            {
                selectedThief?.EnableMove(false, GameManager.Instance?.GridActionHandler);
                selectedThief?.ProceedMovement(false);
            }

        }

        /// <summary>
        /// Toggles stealth for the selected thief.
        /// </summary>
        public void HandleThiefStealth()
        {
            selectedThief?.EnableStealth(!selectedThief.Stealth);
        }

    }
}

