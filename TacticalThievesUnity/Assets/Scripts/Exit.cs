using System.Collections;
using System.Collections.Generic;
using TacticalThieves;
using UnityEngine;
using System.Threading.Tasks;

/// <summary>
/// Manages the exit area in the scene. When a <see cref="Thief"/> reaches the exit collider,
/// this component notifies the <see cref="GameManager"/>, triggers thief-specific logic and
/// plays the exit animation and sound.
/// </summary>
public class Exit : MonoBehaviour
{

    /// <summary>
    /// Reference to the exit model GameObject that contains the animation to play when the door opens.
    /// Can be <c>null</c>; animation calls use the null-conditional operator when accessing components.
    /// </summary>
    [SerializeField] private GameObject model;

    /// <summary>
    /// Unity callback invoked when another collider enters this object's trigger collider.
    /// If the collider belongs to a <see cref="Thief"/>, the thief is processed as having reached the exit:
    /// the game manager is notified, thief-specific completion logic is executed, and the exit open
    /// animation is played.
    /// </summary>
    /// <param name="other">The other collider that entered the trigger.</param>
    private void OnTriggerEnter(UnityEngine.Collider other)
    {
        try
        {
            Thief thief = other.GetComponent<Thief>();
            if(thief == null)
                return;

            OnThiefReachExit(GameManager.Instance);
            thief.OnThiefReachedExit();

            Animator animator = model?.GetComponent<Animator>();
            animator.Play("OpenDoor");
        }
        catch(System.Exception e)
        {
            Debug.LogError($"Error in OnTriggerEnter of Exit: {e.Message}");
        }
    }

    /// <summary>
    /// Notifies the given <see cref="GameManager"/> that a thief has reached the exit and plays the
    /// associated door open sound.
    /// </summary>
    /// <param name="gameManager">The game manager instance to notify. If <c>null</c> or if the game is
    /// not in <c>IN_GAME</c> state, the method returns <c>false</c> and no action is taken.</param>
    /// <returns><c>true</c> if the notification was performed; otherwise <c>false</c>.</returns>
    public bool OnThiefReachExit(GameManager gameManager)
    {
        if (gameManager == null || gameManager.GetGameState() != GameManager.GameState.IN_GAME)
            return false;


        gameManager.CurrentAudioManager?.OnDoorOpenned();
        gameManager.OnThiefReachExit();

        return true;
    }
}
