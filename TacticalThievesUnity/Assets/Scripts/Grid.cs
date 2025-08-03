using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TacticalThieves
{
    public class Grid : MonoBehaviour
    {
        [SerializeField] Dictionary<string, Tile> tiles;
        public Dictionary<string, Tile> Tiles
        {
            get { return tiles; }
            private set { tiles = value; }
        }


        // Start is called before the first frame update
        void Start()
        {
            tiles = new Dictionary<string, Tile>();
            GameObject[] tilesGO = GameObject.FindGameObjectsWithTag("Tile");

            foreach(GameObject tileGO in tilesGO)
            {
                Tile tile = tileGO.GetComponent<Tile>();
                if (tile != null)
                {
                    string tileKey = tile.X + "_" + tile.Y;
                    tiles.Add(tileKey, tile);
                }
            }
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}

