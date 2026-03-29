using DG.Tweening;
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
        [SerializeField] Material selectMaterial;
        [SerializeField] Material previousMaterial;
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
            defaultMaterial = GetComponent<Renderer>().material;
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        private void OnMouseUp()
        {
            if(EnableForMove && Walkable)
            {
                GameManager.Instance?.CurrentPlayerController.OnTileSelected(this);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            //Debug.Log(this);
            Thief thief = other.gameObject.GetComponent<Thief>();
            if (thief != null)
            {
                thief.CheckCurrentTileLocation(this);
                walkable = false;
                return;
            }

            Monster monster = other.gameObject.GetComponent<Monster>();
            if (monster != null)
            {
                monster.CheckCurrentTileLocation(this);
                walkable = false;
            }


        }

        private void OnTriggerExit(Collider other)
        {
            Thief thief = other.gameObject.GetComponent<Thief>();
            if (thief != null)
            {
                walkable = true;
                return;
            }
            Monster monster = other.gameObject.GetComponent<Monster>();
            if (monster != null)
            {
                walkable = true;
            }
        }

        private void OnMouseEnter()
        {
            previousMaterial = GetComponent<Renderer>().material;
            GetComponent<Renderer>().material = selectMaterial;

            GameManager.Instance?.CurrentAudioManager.OnTileSelected();
        }

        private void OnMouseExit()
        {
            GetComponent<Renderer>().material = previousMaterial;
        }


        public void SetEnableForMove(bool enable)
        {
            if(walkable == false)
            {
                return;
            }

            EnableForMove = enable;
            if (enable)
            {
                GetComponent<Renderer>().material = moveMaterial;
            }
            else
            {
                GetComponent<Renderer>().material = defaultMaterial;
            }

            previousMaterial = GetComponent<Renderer>().material;
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

            if(walkable)
                previousMaterial = GetComponent<Renderer>().material;
        }
    }
}
