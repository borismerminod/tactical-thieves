using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TextCore.Text;

namespace TacticalThieves
{
    /// <summary>
    /// Represents the game grid composed of <see cref="Tile"/> instances. This component locates
    /// tiles in the scene, exposes accessors to query tiles by position and provides utility
    /// functions used by game systems to select random tiles or check attack targets.
    /// </summary>
    public class Grid : MonoBehaviour
    {
        /// <summary>
        /// Internal dictionary mapping a string key of the form "x_y" to the corresponding
        /// <see cref="Tile"/> instance present on the scene. This field is populated by
        /// <see cref="InitTilesDictionnary"/> on Start.
        /// </summary>
        [SerializeField] Dictionary<string, Tile> tiles;

        /// <summary>
        /// Maximum X value discovered among the tiles. Populated during initialization.
        /// </summary>
        [SerializeField] int width;

        /// <summary>
        /// Maximum Y value discovered among the tiles. Populated during initialization.
        /// </summary>
        [SerializeField] int height;
        [SerializeField] private bool testMode;

        public bool TestMode { get => testMode; set => testMode = value; }

        /// <summary>
        /// Gets the dictionary of tiles keyed by their string coordinates ("x_y"). The setter is
        /// private because the dictionary is managed by the Grid component itself.
        /// </summary>
        public Dictionary<string, Tile> Tiles
        {
            get { return tiles; }
            private set { tiles = value; }
        }

        /// <summary>
        /// Gets the detected grid width (maximum X coordinate among discovered tiles).
        /// </summary>
        public int Width
        {
            get { return width; }
            private set { width = value; }
        }

        /// <summary>
        /// Gets the detected grid height (maximum Y coordinate among discovered tiles).
        /// </summary>
        public int Height
        {
            get { return height; }
            private set { height = value; }
        }


        /// <summary>
        /// Unity start callback. Notifies the <see cref="GameManager"/> that the grid has started
        /// and initializes the internal tiles dictionary by scanning scene objects tagged as
        /// "Tile".
        /// </summary>
        void Start()
        {
            GameManager.Instance?.OnGridStarted(this);
            InitTilesDictionnary();
            
        }


        /// <summary>
        /// Scans the scene for GameObjects tagged as "Tile" and populates the internal
        /// dictionary mapping their coordinates to <see cref="Tile"/> instances. The method
        /// also computes the grid's width and height (maximum discovered coordinates).
        /// </summary>
        /// <returns><c>true</c> if at least one tile was discovered (width and height &gt; 0);
        /// otherwise <c>false</c>.</returns>
        public bool InitTilesDictionnary()
        {
            width = 0;
            height = 0;
            tiles = new Dictionary<string, Tile>();
            tiles.Clear();
            GameObject[] tilesGO = GameObject.FindGameObjectsWithTag("Tile");
            
            foreach (GameObject tileGO in tilesGO)
            {
                Tile tile = tileGO.GetComponent<Tile>();
                if(TestMode)
                    tile.Walkable = true;

                if (tile != null)
                {
                    if(tile.X > width)
                    {
                        width = tile.X;
                    }
                    if(tile.Y > height)
                    {
                        height = tile.Y;
                    }

                    string tileKey = tile.X + "_" + tile.Y;
                   
                    if(tiles.ContainsKey(tileKey) == false)
                    {
                        tiles.Add(tileKey, tile);
                    }
                }
            }

            return width > 0 && height > 0;
        }


        /// <summary>
        /// Retrieves the <see cref="Tile"/> located at the specified grid coordinates.
        /// </summary>
        /// <param name="moveDestination">The grid coordinates (x,y) of the desired tile.</param>
        /// <returns>The <see cref="Tile"/> at the given coordinates, or <c>null</c> if no tile is
        /// present at that location.</returns>
        public Tile GetTile(Vector2 moveDestination)
        {
            string tileKey = moveDestination.x + "_" + moveDestination.y;

            if (tiles.ContainsKey(tileKey) == false)
                return null;

            return tiles[tileKey];

        }

        /// <summary>
        /// Determines whether the provided character is standing on any of the tiles that are
        /// currently enabled for attack.
        /// </summary>
        /// <param name="enabledTilesPos">A list of positions representing tiles that have been
        /// enabled for attack.</param>
        /// <param name="character">The character to test against the enabled tiles.</param>
        /// <returns><c>true</c> if the character's coordinates match one of the enabled tiles
        /// that also has its <see cref="Tile.EnableForAttack"/> flag set; otherwise <c>false</c>.</returns>
        public bool IsTargetOnEnabledTiles(List<Vector2> enabledTilesPos, Character character)
        {
            bool result = false;
            foreach(Vector2 tilePos in enabledTilesPos)
            {
                string key = tilePos.x+ "_" + tilePos.y;
                if (tiles.ContainsKey(key) == true)
                {
                    Tile tile = tiles[key];
                    if(tile.EnableForAttack && tile.X == character.X && tile.Y == character.Y)
                    {
                        result = true;
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Returns a random tile location within the specified inclusive bounds. If the provided
        /// minimum value is greater than the maximum, the values are swapped to ensure a valid
        /// range.
        /// </summary>
        /// <param name="xMin">Minimum X coordinate (inclusive).</param>
        /// <param name="yMin">Minimum Y coordinate (inclusive).</param>
        /// <param name="xMax">Maximum X coordinate (inclusive).</param>
        /// <param name="yMax">Maximum Y coordinate (inclusive).</param>
        /// <returns>A <see cref="Vector2"/> containing the randomly chosen tile coordinates.</returns>
        public Vector2 GetRandomTileLocation(int xMin, int yMin, int xMax, int yMax)
        {
            if(xMin > xMax)
            {
                int temp = xMin;
                xMin = xMax;
                xMax = temp;
            }

            if(yMin > yMax)
            {
                int temp = yMin;
                yMin = yMax;
                yMax = temp;
            }

            float x = Mathf.Round(Random.Range((float)xMin, (float)xMax));
            float y = Mathf.Round(Random.Range((float)yMin, (float)yMax));
            return new Vector2(x, y);
        }

    }
}

