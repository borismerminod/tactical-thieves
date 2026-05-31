using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using DG.Tweening;

namespace TacticalThieves
{
    /// <summary>
    /// Represents a collectible treasure chest in the scene. When a <see cref="Thief"/>
    /// enters the chest's trigger the treasure is collected: the game manager is notified,
    /// visuals and audio for opening the chest are played and the chest is removed from the scene.
    /// </summary>
    public class Treasure : MonoBehaviour
    {
        /// <summary>
        /// Amount of gold contained in the treasure chest. The setter clamps negative values to zero.
        /// </summary>
        [SerializeField] private int gold;

        /// <summary>
        /// Reference to the chest model GameObject which contains the opening animation.
        /// </summary>
        [SerializeField] private GameObject model;

        /// <summary>
        /// Optional shine effect GameObject that is activated when the chest is opened.
        /// </summary>
        [SerializeField] private GameObject shineEffect;

        /// <summary>
        /// Gets or sets the amount of gold in the chest. Negative values are clamped to zero.
        /// </summary>
        public int Gold { 
            get 
            { 
               return gold; 
            } 
        
            set 
            { 
                gold = value; 
                if(gold < 0)
                    gold = 0;
            } 
        }

        /// <summary>
        /// Unity start callback. Ensures the shine effect is initially disabled.
        /// </summary>
        void Start()
        {
            shineEffect?.SetActive(false);
        }

        /// <summary>
        /// Unity trigger callback invoked when another collider enters the chest's trigger.
        /// If the collider belongs to a <see cref="Thief"/>, the treasure is collected.
        /// Exceptions are logged to avoid interrupting the physics callbacks.
        /// </summary>
        /// <param name="other">The other collider that entered the trigger.</param>
        private void OnTriggerEnter(Collider other)
        {
            try
            {
                Thief thief = other?.gameObject.GetComponent<Thief>();
                if (thief != null)
                {
                    Collect(GameManager.Instance);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error collecting treasure: {ex.Message}");
            }
        }

        /// <summary>
        /// Performs the collection logic: notifies the provided <see cref="GameManager"/>
        /// of the collected gold, plays the opening animation and shine effect, fades the
        /// chest materials to transparent and deactivates the chest after a short delay.
        /// </summary>
        /// <param name="gameManager">The game manager instance to notify. If <c>null</c> the collection fails.</param>
        /// <returns><c>true</c> if the collection was performed; otherwise <c>false</c>.</returns>
        public bool Collect(GameManager gameManager)
        {

            if (gameManager == null)
                return false;

            gameManager.OnTreasureCollected(Gold);

            MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();

            model?.GetComponent<Animator>().SetBool("Open", true);
            shineEffect?.SetActive(true);
            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                foreach(Material material in meshRenderer.materials)
                {
                    Utils.SetMaterialTransparent(material);
                    material.DOFade(0f, 1.0f).SetEase(Ease.Linear).SetLink(gameObject);
                }
            }

            
            gameManager.CurrentAudioManager?.OnTreasureChestOpenned();


            DOVirtual.DelayedCall(1.0f, () =>
            {
                gameObject.SetActive(false);
            }).SetLink(gameObject);


            return true;
        }

    }
}
