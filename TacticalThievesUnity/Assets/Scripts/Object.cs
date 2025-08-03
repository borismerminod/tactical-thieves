using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TacticalThieves
{
    public abstract class Object : MonoBehaviour
    {
        [SerializeField] protected int x;
        [SerializeField] protected int y;

        public int X { get => x; private set => x = value; }
        public int Y { get => y; private set => y = value; }

        // Start is called before the first frame update
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }

}
