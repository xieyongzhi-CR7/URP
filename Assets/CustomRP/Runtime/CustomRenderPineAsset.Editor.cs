using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

 partial class CustomRenderPineAsset
{
 

  #if UNITY_EDITOR

    private static string[] renderingLayerNames;

    static CustomRenderPineAsset()
    {
        renderingLayerNames = new string[31];

        for (int i = 0; i < renderingLayerNames.Length; i++)
        {
            renderingLayerNames[i] = "Layer" + (i + 1);
        }
    }

    public override string[] renderingLayerMaskNames => renderingLayerNames;

   
    
    
#endif

}
