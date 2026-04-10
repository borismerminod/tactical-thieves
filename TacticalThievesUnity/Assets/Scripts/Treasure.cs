using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using DG.Tweening;

namespace TacticalThieves
{
    public class Treasure : MonoBehaviour
    {
        [SerializeField] private int gold;
        [SerializeField] private GameObject model;
        [SerializeField] private GameObject shineEffect;
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

        // Start is called before the first frame update
        void Start()
        {
            shineEffect?.SetActive(false);
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        private void OnTriggerEnter(Collider other)
        {
            Thief thief = other.gameObject.GetComponent<Thief>();
            if (thief != null)
            {
                //TODO Log en cas d'erreur
                Collect(GameManager.Instance);
            }
        }

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
