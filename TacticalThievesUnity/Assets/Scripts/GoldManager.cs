using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple manager that tracks the player's gold amount.
/// </summary>
public class GoldManager : MonoBehaviour
{
    [SerializeField] private int playerGold;

    public int PlayerGold
    {
        get => playerGold;
        private set
        {
            playerGold = value;
            if (playerGold < 0)
                playerGold = 0;
        }
    }

    private void Start()
    {
        PlayerGold = 0;
    }
    
    public void AddGold(int amount)
    {
        PlayerGold += amount;
    }
}
