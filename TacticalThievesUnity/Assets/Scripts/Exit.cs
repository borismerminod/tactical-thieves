using System.Collections;
using System.Collections.Generic;
using TacticalThieves;
using UnityEngine;

public class Exit : MonoBehaviour
{

    [SerializeField] private GameObject model;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        Thief thief = other.GetComponent<Thief>();
        if(thief == null)
            return;

        //TODO Log en cas d'erreur
        OnThiefReachExit(GameManager.Instance);
        thief.OnThiefReachedExit();

        Animator animator = model?.GetComponent<Animator>();
        animator.Play("OpenDoor");
    }

    public bool OnThiefReachExit(GameManager gameManager)
    {
        if (gameManager == null)
            return false;
        if( gameManager.GetGameState() != GameManager.GameState.IN_GAME)
            return false;


        gameManager.OnThiefReachExit();
        return true;
    }
}
