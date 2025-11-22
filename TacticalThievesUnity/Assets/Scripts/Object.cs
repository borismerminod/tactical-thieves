//using Palmmedia.ReportGenerator.Core.Common;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TacticalThieves
{
    [System.Serializable]
    public abstract class Object : MonoBehaviour
    {
        [SerializeField] protected int x;
        [SerializeField] protected int y;

        public int X { 
            get => x;
            set 
            {
                x = value;
                if(x <=0)
                    x = 1;
            }
        }
        public int Y {
            get => y;
            set
            {
                y = value;
                if(y <=0) 
                    y = 1;  
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

        public override string ToString()
        {
            string jsonString = JsonUtility.ToJson(this, true);

            return jsonString;
        }
    }

}
