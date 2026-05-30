using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TacticalThieves
{
    /// <summary>
    /// Provides simple path-finding utilities used to compute movement routes on the grid.
    /// The implemented algorithm is a greedy step-by-step selector that chooses the neighbor
    /// tile which minimizes the straight-line distance to the target. It is not a full A*/BFS
    /// implementation and may not find a valid route in the presence of obstacles; it is
    /// sufficient for the game's simple movement needs.
    /// </summary>
    public static class PathFinder
    {

        /// <summary>
        /// Computes a movement route from a character's current grid position to the
        /// specified target location. The algorithm will perform at most <paramref name="range"/>
        /// steps and selects at each step the neighbouring tile that reduces the Euclidean
        /// distance to the target. If <paramref name="bUseWalkableParam"/> is <c>true</c>, tiles
        /// marked as not walkable will be ignored.
        /// </summary>
        /// <param name="character">The character performing the move. Used to obtain the start position.</param>
        /// <param name="grid">The grid on which the route is computed. Must not be <c>null</c>.</param>
        /// <param name="targetLocation">The destination coordinates to approach.</param>
        /// <param name="range">Maximum number of steps to attempt when computing the route.</param>
        /// <param name="bUseWalkableParam">If <c>true</c>, non-walkable tiles are excluded from consideration.</param>
        /// <returns>A list of tile coordinates representing the computed route. The list may be
        /// shorter than <paramref name="range"/> or contain positions that do not reach the
        /// target if obstacles prevent a direct approach.</returns>
        public static List<Vector2> ComputeMoveRoute(Character character, Grid grid, Vector2 targetLocation, int range, bool bUseWalkableParam)
        {
            List<Vector2> moveRoute = new List<Vector2>();
            Vector2 currentLocation = new Vector2(character.X, character.Y);

            for (int i = 0; i <= range && currentLocation != targetLocation; i++)
            {
                Vector2[] possibleMoves = new Vector2[4];
                possibleMoves[0] = new Vector2(Mathf.Max(currentLocation.x - 1, 1), currentLocation.y); // Left
                possibleMoves[1] = new Vector2(currentLocation.x, Mathf.Max(currentLocation.y - 1, 1)); // Down
                possibleMoves[2] = new Vector2(Mathf.Min(currentLocation.x + 1, grid.Width), currentLocation.y); // Right
                possibleMoves[3] = new Vector2(currentLocation.x, Mathf.Min(currentLocation.y + 1, grid.Height)); // Up

                Vector2 nextMove = Vector2.zero;
                float minDistance = -1.0f;

                foreach (Vector2 move in possibleMoves)
                {
                    string tileKey = move.x + "_" + move.y;
                    if (grid.Tiles.ContainsKey(tileKey) && bUseWalkableParam == true && grid.Tiles[tileKey].Walkable == false)
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

        /// <summary>
        /// Computes a random movement route for the provided character by selecting a random
        /// destination tile within the full grid bounds and calling <see cref="ComputeMoveRoute"/>.
        /// </summary>
        /// <param name="character">The character for which to compute a random route.</param>
        /// <param name="grid">The grid instance used for bounds and tile queries.</param>
        /// <returns>A list of tile coordinates representing the computed random route.</returns>
        public static List<Vector2> GetRandomMoveRoute(Character character, Grid grid)
        {
            Vector2 randomTileLocation = grid.GetRandomTileLocation(1, 1, grid.Width, grid.Height);
            List<Vector2> randomMoveRoute = ComputeMoveRoute(character, grid, randomTileLocation, character.MoveRange, true);

            return randomMoveRoute;
        }
    }

}
