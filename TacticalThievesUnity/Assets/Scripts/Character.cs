using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TacticalThieves
{
    /// <summary>
    /// Represents a character in the game, including its movement range and turn status.
    /// </summary>
    /// <remarks>This class provides properties to manage the character's movement range and whether it is
    /// currently the character's turn.</remarks>
    public class Character : Object
    {
        [SerializeField] protected int moveRange;
        public int MoveRange { get => moveRange; set => moveRange = value; }

        [SerializeField] protected bool bIsYourTurn;
        public bool IsYourTurn { get => bIsYourTurn; set => bIsYourTurn = value; }
    }

}
