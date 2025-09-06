using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

namespace TacticalThieves
{
    public class Grid : MonoBehaviour
    {
        [SerializeField] Dictionary<string, Tile> tiles;
        [SerializeField] int width;
        [SerializeField] int height;
        [SerializeField] private Vector2 minTileCoords;
        [SerializeField] private Vector2 maxTileCoords;

        public Vector2 MinTileCoords { get => minTileCoords; private set => minTileCoords = value; }
        public Vector2 MaxTileCoords { get => maxTileCoords; private set => maxTileCoords = value; }

        public Dictionary<string, Tile> Tiles
        {
            get { return tiles; }
            private set { tiles = value; }
        }

        public int Width
        {
            get { return width; }
            private set { width = value; }
        }

        public int Height
        {
            get { return height; }
            private set { height = value; }
        }


        // Start is called before the first frame update
        void Start()
        {
            InitTilesDictionnary();
            SendGridToPlayerController();
        }

        private void SendGridToPlayerController()
        {
            GameObject playerControllerGO = GameObject.FindGameObjectWithTag("PlayerController");
            if (playerControllerGO == null)
                return;

            PlayerController playerController = playerControllerGO.GetComponent<PlayerController>();
            if (playerController == null)
                return;

            playerController.OnGridStarted(this);
        }

        public bool InitTilesDictionnary()
        {
            width = 0;
            height = 0;
            tiles = new Dictionary<string, Tile>();
            tiles.Clear();
            GameObject[] tilesGO = GameObject.FindGameObjectsWithTag("Tile");
            //Debug.Log("Debut >>");
            foreach (GameObject tileGO in tilesGO)
            {
                Tile tile = tileGO.GetComponent<Tile>();
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

        public void OnThiefMoveEnable(Thief thief)
        {
            if (thief == null || tiles == null || tiles.Count == 0)
            {
                return;
            }

            ComputeMinTileCoords(thief);
            ComputeMaxTileCoords(thief);
            EnableTilesForThiefMove(thief);

        }

        public void OnThiefMoveDisable()
        {

            DisableTileForThiefMove();
            minTileCoords = Vector2.zero;
            maxTileCoords = Vector2.zero;
        }

        private void DisableTileForThiefMove()
        {
            for (int x = (int)minTileCoords.x; x <= (int)maxTileCoords.x; x++)
            {
                for (int y = (int)minTileCoords.y; y <= (int)maxTileCoords.y; y++)
                {
                    string tileKey = x + "_" + y;
                    if (tiles.ContainsKey(tileKey))
                    {
                        tiles[tileKey].SetEnableForMove(false);
                    }
                }
            }
        }

        private void EnableTilesForThiefMove(Thief thief)
        {
            int squareMoveRange = thief.MoveRange * thief.MoveRange;
            for(int x =(int)minTileCoords.x; x <= (int) maxTileCoords.x; x++)
            {
                for(int y = (int)minTileCoords.y; y <= (int)maxTileCoords.y; y++)
                {
                    string tileKey = x + "_" + y;
                    if(tiles.ContainsKey(tileKey))
                    {
                        Tile tile = tiles[tileKey];
                        if(tile != null)
                        {
                            int distanceX = Mathf.Abs(tile.X - thief.X);
                            int distanceY = Mathf.Abs(tile.Y - thief.Y);

                            int squareDistance = distanceX * distanceX + distanceY * distanceY;
                            //Debug.Log($"Tile {tileKey} - Position: ({tile.X}, {tile.Y}), Distance: {squareDistance}, Move Range: {squareMoveRange}");

                            if (squareDistance <= squareMoveRange)
                            {
                                tile.SetEnableForMove(true);
                            }
                            else
                            {
                                tile.SetEnableForMove(false);
                            }
                        }
                    }
                }
            }
        }

        private void ComputeMinTileCoords(Thief thief)
        {
            int posX = Mathf.Max(Mathf.Abs(thief.X - thief.MoveRange), 1);
            int posY = Mathf.Max(Mathf.Abs(thief.Y - thief.MoveRange), 1);

            minTileCoords = new Vector2(posX, posY);
            Debug.Log("MIN " + minTileCoords + " " + thief);
        }

        private void ComputeMaxTileCoords(Thief thief)
        {
            int posX = Mathf.Min(thief.X + thief.MoveRange, width);
            int posY = Mathf.Min(thief.Y + thief.MoveRange, height);
            maxTileCoords = new Vector2(posX, posY);
            Debug.Log("MAX " + maxTileCoords + " " + thief);
        }

        public List<Vector2> ComputeMoveRoute(Thief thief, Tile targetedTile)
        {
            List<Vector2> moveRoute = new List<Vector2>();
            Vector2 currentLocation = new Vector2(thief.X, thief.Y);
            Vector2 targetLocation = new Vector2(targetedTile.X, targetedTile.Y);
            while(currentLocation != targetLocation)
            {
                Vector2[] possibleMoves = new Vector2[4];
                possibleMoves[0] = new Vector2(Mathf.Max(currentLocation.x -1, 1),  currentLocation.y); // Left
                possibleMoves[1] = new Vector2(currentLocation.x, Mathf.Max(currentLocation.y - 1, 1)); // Down
                possibleMoves[2] = new Vector2(Mathf.Min(currentLocation.x + 1, width), currentLocation.y); // Right
                possibleMoves[3] = new Vector2(currentLocation.x, Mathf.Min(currentLocation.y + 1, height)); // Up

                Vector2 nextMove = possibleMoves[0];
                float minDistance = Vector2.Distance(nextMove, targetLocation);

                foreach (Vector2 move in possibleMoves)
                {
                    if(move == currentLocation)
                    {
                        continue; // Skip the current location
                    }

                    float distance = Vector2.Distance(move, targetLocation);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nextMove = move;
                    }
                }

                moveRoute.Add(nextMove);
                currentLocation = nextMove;
            }
            

            foreach (Vector2 move in moveRoute)
            {
                Debug.Log($"Move to: {move}");
            }

            return moveRoute;
        }

        public Tile GetNextTileMove(Vector2 moveDestination)
        {
            string tileKey = moveDestination.x + "_" + moveDestination.y;

            if (tiles.ContainsKey(tileKey) == false)
                return null;

            return tiles[tileKey];

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}

