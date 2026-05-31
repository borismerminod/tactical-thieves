using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TacticalThieves
{
    public static class Utils
    {
        public static void SetMaterialTransparent(Material mat)
        {
            mat.SetFloat("_Mode", 2); // Fade mode
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }

        public static class AnimatorParam
        {
            public const string Run = "Run";
            public const string Defeat = "Defeat";
            public const string Win = "Win";
            public const string Attack = "Attack";
        }

        [Serializable]
        public class ServerMessage
        {
            public string Type;
            public int Level;
        }

    }

}
