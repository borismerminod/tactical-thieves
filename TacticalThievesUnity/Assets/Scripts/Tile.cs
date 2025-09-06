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

        public bool EnableForMove
        {
            get { return enableForMove; }
            set { enableForMove = value; }
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
            if(EnableForMove)
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
            if (thief == null)
                return;
            thief.CheckCurrentTileLocation(this);
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
    }
}
