using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hellmade.Sound;

namespace TacticalThieves
{
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

        // Update is called once per frame
        void Update()
        {
        
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
