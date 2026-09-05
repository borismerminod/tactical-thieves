using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TacticalThieves
{
    /// <summary>
    /// Represents a single grid tile. Tiles can be highlighted for movement or attack,
    /// provide selectable visuals and track whether they are currently walkable.
    /// </summary>
    public class Tile : Object
    {
        /// <summary>
        /// True when this tile is currently enabled for movement selection.
        /// </summary>
        [SerializeField] bool enableForMove;

        /// <summary>
        /// Default material used when the tile is in its normal state.
        /// </summary>
        [SerializeField] Material defaultMaterial;

        /// <summary>
        /// Material used to indicate the tile is available for movement.
        /// </summary>
        [SerializeField] Material moveMaterial;

        /// <summary>
        /// Material used to indicate the tile is available for attack.
        /// </summary>
        [SerializeField] Material attackMaterial;

        /// <summary>
        /// Material used when the mouse is hovering over the tile.
        /// </summary>
        [SerializeField] Material selectMaterial;

        /// <summary>
        /// Material that was previously applied to the renderer. Used to restore the visual state
        /// after hover or highlight is cleared.
        /// </summary>
        [SerializeField] Material previousMaterial;

        /// <summary>
        /// Indicates whether the tile is currently walkable (not occupied). When false the tile
        /// will not be enabled for movement.
        /// </summary>
        [SerializeField] bool walkable = true;

        /// <summary>
        /// True when this tile is currently enabled for attack selection.
        /// </summary>
        [SerializeField] bool enableForAttack;

        /// <summary>
        /// Renderer component used to change the tile material. The field uses the C# "new"
        /// modifier to hide a base member name if present.
        /// </summary>
        [SerializeField] new Renderer renderer;


        /// <summary>
        /// Gets or sets whether the tile is enabled for movement.
        /// </summary>
        public bool EnableForMove
        {
            get { return enableForMove; }
            set { enableForMove = value; }
        }

        /// <summary>
        /// Gets or sets whether the tile is enabled for attack.
        /// </summary>
        public bool EnableForAttack
        {
            get { return enableForAttack; }
            set { enableForAttack = value; }
        }

        /// <summary>
        /// Gets or sets whether the tile is walkable. Non-walkable tiles are ignored when
        /// computing movement routes or enabling move highlights.
        /// </summary>
        public bool Walkable
        {
            get => walkable;
            set => walkable = value; 
        }

        

        /// <summary>
        /// Unity start callback. Caches the <see cref="Renderer"/> component and stores the
        /// default material for later restoration.
        /// </summary>
        void Start()
        {
            renderer = GetComponent<Renderer>();
            defaultMaterial = renderer?.material;
        }

        /// <summary>
        /// Called when the user releases the mouse button over this tile. If the tile is
        /// enabled for movement and walkable, the currently active <see cref="PlayerController"/>
        /// is notified of the tile selection.
        /// </summary>
        private void OnMouseUp()
        {
            try
            {
                if(EnableForMove && Walkable)
                {
                    GameManager.Instance?.CurrentPlayerController.OnTileSelected(this);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in OnMouseUp: {ex.Message}");
            }
        }

        

        /// <summary>
        /// Called when another collider enters this tile's trigger. If the collider belongs to
        /// a <see cref="Thief"/> or <see cref="Monster"/>, the corresponding character is
        /// notified that it has reached this tile and the tile is marked non-walkable while
        /// occupied.
        /// </summary>
        /// <param name="other">The other collider that entered the trigger.</param>
        private void OnTriggerEnter(Collider other)
        {
            try
            {
                Thief thief = other?.gameObject.GetComponent<Thief>();
                if (thief != null)
                {
                    thief.CheckCurrentTileLocation(this);
                    walkable = false;
                    return;
                }

                Monster monster = other?.gameObject.GetComponent<Monster>();
                if (monster != null)
                {
                    monster.CheckCurrentTileLocation(this);
                    walkable = false;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in OnTriggerEnter: {ex.Message}");
            }


        }

        /// <summary>
        /// Called when another collider exits this tile's trigger. If the collider belongs to a
        /// <see cref="Thief"/> or <see cref="Monster"/>, the tile is marked walkable again.
        /// </summary>
        /// <param name="other">The other collider that exited the trigger.</param>
        private void OnTriggerExit(Collider other)
        {
            try
            {
                Thief thief = other?.gameObject.GetComponent<Thief>();
                if (thief != null)
                {
                    walkable = true;
                    return;
                }
                Monster monster = other?.gameObject.GetComponent<Monster>();
                if (monster != null)
                {
                    walkable = true;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in OnTriggerExit: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when the mouse cursor enters the tile. The tile material is changed to
        /// the select material and a tile-selected audio event is triggered.
        /// </summary>
        private void OnMouseEnter()
        {
            try
            {
                previousMaterial = renderer?.material;
                renderer.material = selectMaterial;

                GameManager.Instance?.CurrentAudioManager.OnTileSelected();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in OnMouseEnter: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when the mouse cursor exits the tile. Restores the previous material.
        /// </summary>
        private void OnMouseExit()
        {
            try
            {
                renderer.material = previousMaterial;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in OnMouseExit: {ex.Message}");
            }
        }


        /// <summary>
        /// Enables or disables the movement highlighting for this tile. If the tile is not
        /// walkable the method returns without making changes. When enabling, the move material
        /// is applied; otherwise the default material is restored.
        /// </summary>
        /// <param name="enable">If <c>true</c> the tile is enabled for movement; otherwise it is disabled.</param>
        public void SetEnableForMove(bool enable)
        {
            if(walkable == false)
            {
                return;
            }

            EnableForMove = enable;

            if(renderer != null)
            {
                if (enable)
                {
                    renderer.material = moveMaterial;
                }
                else
                {
                    renderer.material = defaultMaterial;
                }
                previousMaterial = renderer?.material;
            }
            else
            {
                Debug.LogWarning("SetEnableForMove renderer is null");
            }
        }

        /// <summary>
        /// Enables or disables the attack highlighting for this tile. When enabling the attack
        /// material is applied; otherwise the default material is restored. The previous material
        /// is updated only when the tile is walkable.
        /// </summary>
        /// <param name="enable">If <c>true</c> the tile is enabled for attack; otherwise it is disabled.</param>
        public void SetEnableForAttack(bool enable)
        {

            EnableForAttack = enable;

            if (renderer != null)
            {

                if (enable)
                {
                    renderer.material = attackMaterial;
                }
                else
                {
                    renderer.material = defaultMaterial;
                }

                if (walkable)
                    previousMaterial = renderer?.material;
            }
            else
                Debug.LogWarning("SetEnableForAttack renderer is null");
        }
    }
}
