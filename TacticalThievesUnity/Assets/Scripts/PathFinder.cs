using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TacticalThieves
{   public static class PathFinder
    {

        public static List<Vector2> ComputeMoveRoute(Character character, Grid grid, Vector2 targetLocation, int range, bool bUseWalkableParam)
        {
            List<Vector2> moveRoute = new List<Vector2>();
            Vector2 currentLocation = new Vector2(character.X, character.Y);

            for (int i = 0; i <= range && currentLocation != targetLocation; i++)
            //while(currentLocation != targetLocation)
            {
                Vector2[] possibleMoves = new Vector2[4];
                possibleMoves[0] = new Vector2(Mathf.Max(currentLocation.x - 1, 1), currentLocation.y); // Left
                possibleMoves[1] = new Vector2(currentLocation.x, Mathf.Max(currentLocation.y - 1, 1)); // Down
                possibleMoves[2] = new Vector2(Mathf.Min(currentLocation.x + 1, grid.Width), currentLocation.y); // Right
                possibleMoves[3] = new Vector2(currentLocation.x, Mathf.Min(currentLocation.y + 1, grid.Height)); // Up

                Vector2 nextMove = Vector2.zero; //possibleMoves[0];
                float minDistance = -1.0f; //Vector2.Distance(nextMove, targetLocation);

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

        public static List<Vector2> GetRandomMoveRoute(Character character, Grid grid)
        {
            Vector2 randomTileLocation = grid.GetRandomTileLocation(1, 1, grid.Width, grid.Height);
            List<Vector2> randomMoveRoute = ComputeMoveRoute(character, grid, randomTileLocation, character.MoveRange, true);

            return randomMoveRoute;
        }
    }

}
