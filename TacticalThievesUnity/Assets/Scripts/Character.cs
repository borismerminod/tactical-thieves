using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TacticalThieves
{
    public class Character : Object
    {
        [SerializeField] protected int moveRange;
        public int MoveRange { get => moveRange; set => moveRange = value; }

        [SerializeField] protected bool bIsYourTurn;
        public bool IsYourTurn { get => bIsYourTurn; set => bIsYourTurn = value; }
    }

}
