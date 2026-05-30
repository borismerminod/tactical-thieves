//using Palmmedia.ReportGenerator.Core.Common;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TacticalThieves
{
    [System.Serializable]
    /// <summary>
    /// Base class for scene objects that have integer grid coordinates. Provides
    /// clamped properties for X and Y (minimum value of 1) and a JSON-based
    /// <see cref="ToString"/> implementation for debugging.
    /// </summary>
    public abstract class Object : MonoBehaviour
    {
        /// <summary>
        /// Internal X coordinate on the grid. Serialized for editor configuration.
        /// </summary>
        [SerializeField] protected int x;

        /// <summary>
        /// Internal Y coordinate on the grid. Serialized for editor configuration.
        /// </summary>
        [SerializeField] protected int y;

        /// <summary>
        /// Gets or sets the X coordinate. Values less than or equal to zero are clamped to 1.
        /// </summary>
        public int X { 
            get => x;
            set 
            {
                x = value;
                if(x <=0)
                    x = 1;
            }
        }

        /// <summary>
        /// Gets or sets the Y coordinate. Values less than or equal to zero are clamped to 1.
        /// </summary>
        public int Y {
            get => y;
            set
            {
                y = value;
                if(y <=0) 
                    y = 1;  
            }
        }

        /// <summary>
        /// Returns a formatted JSON representation of this object. Useful for logging and
        /// debugging in the editor or runtime.
        /// </summary>
        /// <returns>A JSON string representing the object's serialized fields.</returns>
        public override string ToString()
        {
            string jsonString = JsonUtility.ToJson(this, true);

            return jsonString;
        }
    }

}
