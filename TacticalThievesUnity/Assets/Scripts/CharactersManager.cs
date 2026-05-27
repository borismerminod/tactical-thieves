using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TacticalThieves
{
    /// <summary>
    /// Manager responsible for tracking characters present in the current level.
    /// </summary>
    /// <remarks>
    /// This component maintains a list of Character instances and provides utility
    /// methods to register characters and to query global character-related states
    /// (for example whether all thieves are dead).
    /// </remarks>
    public class CharactersManager : MonoBehaviour
    {
        [SerializeField] private List<Character> characters;

        /// <summary>
        /// Read-only access to the list of characters managed by this component.
        /// </summary>
        /// <remarks>The list instance itself is returned; callers should not assume it is
        /// immutable. Use the provided AddCharacter method to register new characters.
        /// </remarks>
        public List<Character> Characters { get => characters; private set => characters = value; }


        /// <summary>
        /// Adds a character to the manager if it is not null and not already registered.
        /// </summary>
        /// <param name="character">The character instance to add.</param>
        public void AddCharacter(Character character)
        {
            if (character == null) return;
            if (characters.Contains(character)) return;

            characters.Add(character);
        }

        /// <summary>
        /// Determines whether all thief characters managed by this component are dead.
        /// </summary>
        /// <returns>True if every Thief in the managed collection has status <c>Dead</c>; otherwise false.</returns>
        /// <remarks>
        /// This method iterates through the registered characters, checks which ones are
        /// Thief instances and inspects their Status property. If no thieves are present
        /// the method returns true (vacuous truth) which matches the original behaviour.
        /// </remarks>
        public bool AreAllThievesDied()
        {
            bool AllThievesAreDead = true;

            foreach (Character character in characters)
            {
                Thief thief = character as Thief;
                if (thief == null) continue;
                if (thief.Status != Thief.eThiefStatus.Dead)
                {
                    AllThievesAreDead = false;
                    break;
                }
            }

            return AllThievesAreDead;

        }

    }

}
