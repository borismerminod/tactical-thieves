using System.Collections;
using System.Collections.Generic;
using TacticalThieves;
using UnityEngine;

namespace TacticalThieves
{
    public class GridActionHandler : MonoBehaviour
    {
        [SerializeField] private Vector2 minTileCoords;
        [SerializeField] private Vector2 maxTileCoords;
        [SerializeField] private Grid currentGrid;

        public void OnGridStarted(Grid grid)
        {
            currentGrid = grid;
        }

        public void EnableTilesForMove(Character character)
        {
            if (character == null || currentGrid == null || currentGrid.Tiles == null || currentGrid.Tiles.Count == 0)
            {
                return;
            }

            ComputeMinTileCoords(character, character.MoveRange);
            ComputeMaxTileCoords(character, character.MoveRange);
            EnableTilesForCharacterAction(character, false);

        }

        public void DisableTilesForMove()
        {

            DisableTileForCharacterAction(false);
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
                    if (currentGrid.Tiles.ContainsKey(tileKey))
                    {
                        if (actionIsAttack)
                            currentGrid.Tiles[tileKey].SetEnableForAttack(false);
                        else
                            currentGrid.Tiles[tileKey].SetEnableForMove(false);
                    }
                }
            }
        }

        private void ComputeMinTileCoords(Character character, int range)
        {
            int posX = Mathf.Max(character.X - range, 1);
            int posY = Mathf.Max(character.Y - range, 1);

            minTileCoords = new Vector2(posX, posY);
        }

        private void ComputeMaxTileCoords(Character character, int range)
        {
            int posX = Mathf.Min(character.X + range, currentGrid.Width);
            int posY = Mathf.Min(character.Y + range, currentGrid.Height);
            maxTileCoords = new Vector2(posX, posY);
        }

        private List<Vector2> EnableTilesForCharacterAction(Character character, bool actionIsAttack)
        {
            List<Vector2> enabledTiles = new List<Vector2>();
            int squareMoveRange = character.MoveRange * character.MoveRange;
            for (int x = (int)minTileCoords.x; x <= (int)maxTileCoords.x; x++)
            {
                for (int y = (int)minTileCoords.y; y <= (int)maxTileCoords.y; y++)
                {
                    if (actionIsAttack)
                    {
                        Monster monster = (Monster)character;
                        Tile enabledTile = HandleTileAttackToggle(monster, x, y);
                        if (enabledTile != null)
                        {
                            Vector2 enabledTilePos = new Vector2(enabledTile.X, enabledTile.Y);
                            enabledTiles.Add(enabledTilePos);
                        }
                    }
                    else
                    {
                        Tile enabledTile = HandleTileMoveToggle(character, x, y);
                        if (enabledTile != null)
                        {
                            Vector2 enabledTilePos = new Vector2(enabledTile.X, enabledTile.Y);
                            enabledTiles.Add(enabledTilePos);
                        }
                    }
                }
            }


            return enabledTiles;
        }

        private Tile HandleTileMoveToggle(Character character, int x, int y)
        {
            Tile enabledTile = null;
            string tileKey = x + "_" + y;
            if (currentGrid.Tiles.ContainsKey(tileKey))
            {
                Tile tile = currentGrid.Tiles[tileKey];
                Vector2 tileLocation = new Vector2(tile.X, tile.Y);
                List<Vector2> routes = PathFinder.ComputeMoveRoute(character, currentGrid, tileLocation, character.MoveRange, true);
                if (routes.Count <= character.MoveRange)
                {
                    tile.SetEnableForMove(true);
                    enabledTile = tile;
                }
                else
                {
                    tile.SetEnableForMove(false);
                }
            }
            return enabledTile;
        }

        private Tile HandleTileAttackToggle(Monster monster, int x, int y)
        {
            Tile enabledTile = null;
            string tileKey = x + "_" + y;
            if (currentGrid.Tiles.ContainsKey(tileKey))
            {
                Tile tile = currentGrid.Tiles[tileKey];
                Vector2 tileLocation = new Vector2(tile.X, tile.Y);
                List<Vector2> routes = PathFinder.ComputeMoveRoute(monster, currentGrid, tileLocation, monster.AttackRange, false);

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

        public List<Vector2> OnMonsterAttackEnable(Monster monster)
        {
            return OnMonsterActionEnable(monster, monster.AttackRange, true);
        }

        public List<Vector2> OnMonsterMoveEnable(Monster monster)
        {
            return OnMonsterActionEnable(monster, monster.MoveRange, false);
        }

        private List<Vector2> OnMonsterActionEnable(Monster monster, int range, bool actionIsAttack)
        {
            List<Vector2> enabledTiles = new List<Vector2>();
            if (monster == null || currentGrid.Tiles == null || currentGrid.Tiles.Count == 0)
            {
                return enabledTiles;
            }

            ComputeMinTileCoords(monster, range);
            ComputeMaxTileCoords(monster, range);
            enabledTiles = EnableTilesForCharacterAction(monster, actionIsAttack);

            return enabledTiles;
        }


        public void OnMonsterAttackDisable()
        {
            DisableTileForCharacterAction(true);
            minTileCoords = Vector2.zero;
            maxTileCoords = Vector2.zero;
        }


    }

}
