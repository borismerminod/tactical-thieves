using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hellmade.Sound;

namespace TacticalThieves
{
    /// <summary>
    /// Manages audio playback for various game events, such as tile selection, treasure chest opening, monster attacks,
    /// and door opening.
    /// </summary>
    /// <remarks>This class provides methods to play specific audio clips associated with common game events.
    /// It is designed to be used in conjunction with the <see cref="GameManager"/> to ensure proper initialization and
    /// event handling. Audio clips are serialized fields and should be assigned in the Unity Editor.</remarks>
    public class AudioManager : MonoBehaviour
    {
        [SerializeField] AudioClip openTheDoor;
        [SerializeField] AudioClip selectTile;
        [SerializeField] AudioClip monsterAttack;
        [SerializeField] AudioClip treasureChest;


        // Start is called before the first frame update
        void Start()
        {
            GameManager.Instance?.OnAudioManagerStarted(this);
        }


        public void OnTileSelected()
        {
            EazySoundManager.PlaySound(selectTile);
        }

        public void OnTreasureChestOpenned()
        {
            EazySoundManager.PlaySound(treasureChest);
        }

        public void OnMonsterAttack()
        {
            EazySoundManager.PlaySound(monsterAttack);
        }

        public void OnDoorOpenned()
        {
            EazySoundManager.PlaySound(openTheDoor);
        }
    }

}
