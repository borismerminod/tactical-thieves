using System.Collections;
using System.Collections.Generic;
using TacticalThieves;
using UnityEngine;

namespace TacticalThieves
{
    public class GridActionHandler : MonoBehaviour
    {
        /// <summary>
        /// Minimum tile coordinates used when enabling/disabling tiles for an action. Computed
        /// from the character position and its action range.
        /// </summary>
        [SerializeField] private Vector2 minTileCoords;

        /// <summary>
        /// Maximum tile coordinates used when enabling/disabling tiles for an action. Computed
        /// from the character position and its action range.
        /// </summary>
        [SerializeField] private Vector2 maxTileCoords;

        /// <summary>
        /// Reference to the active <see cref="Grid"/> instance. Assigned via
        /// <see cref="OnGridStarted(Grid)"/> when the grid component starts.
        /// </summary>
        [SerializeField] private Grid currentGrid;

        /// <summary>
        /// Called by the <see cref="Grid"/> when it has been initialized. Stores a reference
        /// to the provided grid instance for subsequent tile operations.
        /// </summary>
        /// <param name="grid">The grid instance that was started.</param>
        public void OnGridStarted(Grid grid)
        {
            currentGrid = grid;
        }

        /// <summary>
        /// Enables tiles that are reachable for movement by the provided <see cref="Character"/>.
        /// If the character or the current grid is null or if the grid has no tiles, the method
        /// returns without modifying any tiles.
        /// </summary>
        /// <param name="character">The character for which movement tiles should be enabled.</param>
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

        /// <summary>
        /// Disables tiles previously enabled for movement and resets the cached min/max tile
        /// coordinates used for iteration.
        /// </summary>
        public void DisableTilesForMove()
        {

            DisableTileForCharacterAction(false);
            minTileCoords = Vector2.zero;
            maxTileCoords = Vector2.zero;
        }

        /// <summary>
        /// Disables either attack or movement on the tiles in the current min/max coordinate
        /// rectangle.
        /// </summary>
        /// <param name="actionIsAttack">If <c>true</c> disables attack tiles; otherwise disables
        /// movement tiles.</param>
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

        /// <summary>
        /// Computes the minimum tile coordinates that should be considered for an action based
        /// on the character position and range. Values are clamped to a minimum of 1.
        /// </summary>
        /// <param name="character">The character used as origin for the computation.</param>
        /// <param name="range">The action range to apply.</param>
        private void ComputeMinTileCoords(Character character, int range)
        {
            int posX = Mathf.Max(character.X - range, 1);
            int posY = Mathf.Max(character.Y - range, 1);

            minTileCoords = new Vector2(posX, posY);
        }

        /// <summary>
        /// Computes the maximum tile coordinates that should be considered for an action based
        /// on the character position and range. Values are clamped to the grid's width and
        /// height.
        /// </summary>
        /// <param name="character">The character used as origin for the computation.</param>
        /// <param name="range">The action range to apply.</param>
        private void ComputeMaxTileCoords(Character character, int range)
        {
            int posX = Mathf.Min(character.X + range, currentGrid.Width);
            int posY = Mathf.Min(character.Y + range, currentGrid.Height);
            maxTileCoords = new Vector2(posX, posY);
        }

        /// <summary>
        /// Enables tiles for the given character action (move or attack) within the previously
        /// computed min/max coordinate rectangle. For each eligible tile, the method will call
        /// the appropriate handler and collect the positions of enabled tiles.
        /// </summary>
        /// <param name="character">The character for which to enable tiles.</param>
        /// <param name="actionIsAttack">If <c>true</c>, the method enables attack tiles; otherwise enables movement tiles.</param>
        /// <returns>A list of enabled tile coordinates.</returns>
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

        /// <summary>
        /// Determines whether a tile at the specified coordinates is reachable for the
        /// character's movement range and toggles its move-enabled state accordingly.
        /// </summary>
        /// <param name="character">The character attempting to move to the tile.</param>
        /// <param name="x">Tile X coordinate.</param>
        /// <param name="y">Tile Y coordinate.</param>
        /// <returns>The <see cref="Tile"/> if it was enabled for movement; otherwise <c>null</c>.</returns>
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

        /// <summary>
        /// Determines whether a tile at the specified coordinates is reachable for the
        /// monster's attack range and toggles its attack-enabled state accordingly.
        /// </summary>
        /// <param name="monster">The monster evaluating attack reachability.</param>
        /// <param name="x">Tile X coordinate.</param>
        /// <param name="y">Tile Y coordinate.</param>
        /// <returns>The <see cref="Tile"/> if it was enabled for attack; otherwise <c>null</c>.</returns>
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

        /// <summary>
        /// Enables tiles that are reachable for attack by the provided <see cref="Monster"/>.
        /// </summary>
        /// <param name="monster">The monster for which attack tiles should be enabled.</param>
        /// <returns>A list of enabled tile coordinates for attack.</returns>
        public List<Vector2> OnMonsterAttackEnable(Monster monster)
        {
            return OnMonsterActionEnable(monster, monster.AttackRange, true);
        }

        /// <summary>
        /// Enables tiles that are reachable for movement by the provided <see cref="Monster"/>.
        /// </summary>
        /// <param name="monster">The monster for which movement tiles should be enabled.</param>
        /// <returns>A list of enabled tile coordinates for movement.</returns>
        public List<Vector2> OnMonsterMoveEnable(Monster monster)
        {
            return OnMonsterActionEnable(monster, monster.MoveRange, false);
        }

        /// <summary>
        /// Generic handler used to enable tiles for a monster action (move or attack). Returns
        /// the list of enabled tile coordinates. If the monster or grid is invalid, an empty
        /// list is returned.
        /// </summary>
        /// <param name="monster">The monster performing the action.</param>
        /// <param name="range">The action range to consider.</param>
        /// <param name="actionIsAttack">If <c>true</c> enables attack tiles; otherwise movement tiles.</param>
        /// <returns>The list of enabled tile coordinates.</returns>
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


        /// <summary>
        /// Disables tiles previously enabled for monster attack and resets the cached min/max
        /// tile coordinates used for iteration.
        /// </summary>
        public void OnMonsterAttackDisable()
        {
            DisableTileForCharacterAction(true);
            minTileCoords = Vector2.zero;
            maxTileCoords = Vector2.zero;
        }


    }

}
