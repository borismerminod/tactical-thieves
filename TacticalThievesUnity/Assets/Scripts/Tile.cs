using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TacticalThieves
{
    public class Tile : Object
    {
        [SerializeField] bool enableForMove;
        [SerializeField] Material defaultMaterial;
        [SerializeField] Material moveMaterial;
        [SerializeField] Material attackMaterial;
        [SerializeField] bool walkable = true;

        [SerializeField] bool enableForAttack;


        public bool EnableForMove
        {
            get { return enableForMove; }
            set { enableForMove = value; }
        }

        public bool EnableForAttack
        {
            get { return enableForAttack; }
            set { enableForAttack = value; }
        }

        public bool Walkable
        {
            get => walkable;
            set => walkable = value; 
        }

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
            if(EnableForMove && Walkable)
            {
                GameObject playerControllerGO = GameObject.FindGameObjectWithTag("PlayerController");
                if (playerControllerGO == null)
                    return;

                PlayerController playerController = playerControllerGO.GetComponent<PlayerController>();
                if (playerController == null)
                    return;

                playerController.OnTileSelected(this);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            Thief thief = other.gameObject.GetComponent<Thief>();
            if (thief != null)
            {
                thief.CheckCurrentTileLocation(this);
                return;
            }

            Monster monster = other.gameObject.GetComponent<Monster>();
            if (monster != null)
            {
                monster.CheckCurrentTileLocation(this);
            }


        }

        public void SetEnableForMove(bool enable)
        {
            EnableForMove = enable;
            if (enable)
            {
                GetComponent<Renderer>().material = moveMaterial;
            }
            else
            {
                GetComponent<Renderer>().material = defaultMaterial;
            }
        }

        public void SetEnableForAttack(bool enable)
        {
            EnableForAttack = enable;
            if (enable)
            {
                GetComponent<Renderer>().material = attackMaterial;
            }
            else
            {
                GetComponent<Renderer>().material = defaultMaterial;
            }
        }
    }
}
