using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TacticalThieves.GameManager;

namespace TacticalThieves
{
    public class TurnManager : MonoBehaviour
    {

        [SerializeField] private int characterTurnIndex;
        public int CharacterTurnIndex { get => characterTurnIndex; set => characterTurnIndex = value; }

        // Start is called before the first frame update
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        public void InitCharacterTurnIndex(CharactersManager charactersManager, PlayerController playerController, AIController aiController)
        {

            characterTurnIndex = 0;

            Character character = charactersManager.Characters[characterTurnIndex];
            Thief thief = character as Thief;
            if (thief != null)
            {
                playerController.OnThiefSelected(thief, true);
                return;
            }

            Monster monster = character as Monster;
            if (monster != null)
            {
                aiController.OnMonsterSelected(monster);
            }

        }

        public void IncrementCharacterTurnIndex(CharactersManager charactersManager, PlayerController playerController, AIController aiController)
        {
            
            characterTurnIndex++;
            if (characterTurnIndex >= charactersManager.Characters.Count)
                characterTurnIndex = 0;

            playerController.OnThiefSelected(null, true);
            aiController.OnMonsterSelected(null);

            Character character = charactersManager.Characters[characterTurnIndex];
            Thief thief = character as Thief;
            if (thief != null)
            {
                playerController.OnThiefSelected(thief, true);
                return;
            }

            Monster monster = character as Monster;
            if (monster != null)
            {
                aiController.OnMonsterSelected(monster);
            }
        }
    }

}
