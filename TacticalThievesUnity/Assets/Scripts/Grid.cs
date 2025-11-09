using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TextCore.Text;

namespace TacticalThieves
{
    public class Grid : MonoBehaviour
    {
        [SerializeField] Dictionary<string, Tile> tiles;
        [SerializeField] int width;
        [SerializeField] int height;
        [SerializeField] private Vector2 minTileCoords;
        [SerializeField] private Vector2 maxTileCoords;
        [SerializeField] private bool testMode;

        public bool TestMode { get => testMode; set => testMode = value; }
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
            GameManager.Instance.OnGridStarted(this);
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

        public void OnThiefMoveEnable(Thief thief)
        {
            if (thief == null || tiles == null || tiles.Count == 0)
            {
                return;
            }

            ComputeMinTileCoords(thief, thief.MoveRange);
            ComputeMaxTileCoords(thief, thief.MoveRange);
            EnableTilesForCharacterAction(thief, false);

        }

        public void OnThiefMoveDisable()
        {

            DisableTileForCharacterAction(false);
            minTileCoords = Vector2.zero;
            maxTileCoords = Vector2.zero;
        }

        public List<Vector2> OnMonsterAttackEnable(Monster monster)
        {
            List<Vector2> enablesTiles = new List<Vector2>();
            if (monster == null || tiles == null || tiles.Count == 0)
            {
                return enablesTiles;
            }

            ComputeMinTileCoords(monster, monster.AttackRange);
            ComputeMaxTileCoords(monster, monster.AttackRange);
            //Debug.Log("TEST "+ minTileCoords + " "+ maxTileCoords + " "+ monster);
            enablesTiles = EnableTilesForCharacterAction(monster, true);

            return enablesTiles;

        }

        public void OnMonsterAttackDisable()
        {
            DisableTileForCharacterAction(true);
            minTileCoords = Vector2.zero;
            maxTileCoords = Vector2.zero;
        }

        private void DisableTileForCharacterAction(bool actionIsAttack)
        {
            for (int x = (int)minTileCoords.x; x <= (int)maxTileCoords.x; x++)
            {
                for (int y = (int)minTileCoords.y; y <= (int)maxTileCoords.y; y++)
                {
                    string tileKey = x + "_" + y;
                    if (tiles.ContainsKey(tileKey))
                    {
                        if(actionIsAttack)
                            tiles[tileKey].SetEnableForAttack(false);
                        else
                            tiles[tileKey].SetEnableForMove(false);
                    }
                }
            }
        }

        private List<Vector2> EnableTilesForCharacterAction(Character character, bool actionIsAttack)
        {
            List<Vector2> enabledTiles = new List<Vector2>();
            int squareMoveRange = character.MoveRange * character.MoveRange;
            for(int x =(int)minTileCoords.x; x <= (int) maxTileCoords.x; x++)
            {
                for(int y = (int)minTileCoords.y; y <= (int)maxTileCoords.y; y++)
                {
                    if (actionIsAttack)
                    {
                        Monster monster = (Monster)character;
                        Tile enabledTile = HandleTileAttackToggle(monster, x, y);
                        if(enabledTile != null)
                        {
                            Vector2 enabledTilePos = new Vector2(enabledTile.X, enabledTile.Y);
                            enabledTiles.Add(enabledTilePos);
                        }
                    }
                    else
                    {
                        HandleTileMoveToggle(character, x,y);
                    }
                }
            }

            Debug.Log(enabledTiles.Count);

            return enabledTiles;
        }

        private void HandleTileMoveToggle(Character character, int x, int y)
        {
            string tileKey = x + "_" + y;
            if (tiles.ContainsKey(tileKey))
            {
                Tile tile = tiles[tileKey];
                List<Vector2> routes = ComputeMoveRoute(character, tile, character.MoveRange);
                if (routes.Count <= character.MoveRange)
                {
                    tile.SetEnableForMove(true);
                }
                else
                {
                    tile.SetEnableForMove(false);
                }
            }
        }

        private Tile HandleTileAttackToggle(Monster monster, int x, int y)
        {
            Tile enabledTile = null;
            string tileKey = x + "_" + y;
            if (tiles.ContainsKey(tileKey))
            {
                Tile tile = tiles[tileKey];
                List<Vector2> routes = ComputeMoveRoute(monster, tile, monster.AttackRange);

                if (routes.Count <= monster.AttackRange)
                {
                    tile.SetEnableForAttack(true);
                    enabledTile = tile;
                }
                else
                {
                    tile.SetEnableForAttack(false);
                }
            }

            return enabledTile;
        }

        private void ComputeMinTileCoords(Character character, int range)
        {
            int posX = Mathf.Max(character.X - range, 1);
            int posY = Mathf.Max(character.Y - range, 1);

            minTileCoords = new Vector2(posX, posY);
            //Debug.Log("minTileCoords " + minTileCoords + " "+ character);
        }

        private void ComputeMaxTileCoords(Character character, int range)
        {
            int posX = Mathf.Min(character.X + range, width);
            int posY = Mathf.Min(character.Y + range, height);
            maxTileCoords = new Vector2(posX, posY);
           // Debug.Log("maxTileCoords " + maxTileCoords);
        }

        public List<Vector2> ComputeMoveRoute(Character character, Tile targetedTile, int range)
        {
            List<Vector2> moveRoute = new List<Vector2>();
            Vector2 currentLocation = new Vector2(character.X, character.Y);
            Vector2 targetLocation = new Vector2(targetedTile.X, targetedTile.Y);

            for(int i=0; i<= range && currentLocation != targetLocation; i++)
            //while(currentLocation != targetLocation)
            {
                Vector2[] possibleMoves = new Vector2[4];
                possibleMoves[0] = new Vector2(Mathf.Max(currentLocation.x -1, 1),  currentLocation.y); // Left
                possibleMoves[1] = new Vector2(currentLocation.x, Mathf.Max(currentLocation.y - 1, 1)); // Down
                possibleMoves[2] = new Vector2(Mathf.Min(currentLocation.x + 1, width), currentLocation.y); // Right
                possibleMoves[3] = new Vector2(currentLocation.x, Mathf.Min(currentLocation.y + 1, height)); // Up

                Vector2 nextMove = Vector2.zero; //possibleMoves[0];
                float minDistance = -1.0f; //Vector2.Distance(nextMove, targetLocation);

                foreach (Vector2 move in possibleMoves)
                {
                    string tileKey = move.x + "_" + move.y;
                    if (tiles.ContainsKey(tileKey) && tiles[tileKey].Walkable == false)
                    {
                        continue;
                    }
                    
                    if (move == currentLocation)
                    {
                        continue; // Skip the current location
                    }

                    float distance = Vector2.Distance(move, targetLocation);
                    if (minDistance == -1.0f || distance < minDistance)
                    {
                        minDistance = distance;
                        nextMove = move;
                    }
                }

                moveRoute.Add(nextMove);

                currentLocation = nextMove;
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

        // Update is called once per frame
        void Update()
        {

        }
    }
}

