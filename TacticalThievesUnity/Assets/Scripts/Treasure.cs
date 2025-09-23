using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;

public class Treasure : MonoBehaviour
{
    [SerializeField] private int gold;
    public int Gold { 
        get 
        { 
           return gold; 
        } 
        
        set 
        { 
            gold = value; 
            if(gold < 0)
                gold = 0;
        } 
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Collect()
    {
        gameObject.SetActive(false);
    }
}
