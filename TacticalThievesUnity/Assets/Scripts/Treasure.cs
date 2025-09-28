using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;

namespace TacticalThieves
{
    public class Treasure : MonoBehaviour
    {
        [SerializeField] private int gold;
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
            gameObject.SetActive(false);

            return true;
        }
    }
}
