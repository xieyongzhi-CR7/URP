using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PerObjectMaterialProperties : MonoBehaviour
{
    private static int baseColorId = Shader.PropertyToID("_BaseColor");
    [Range(0,1)]
    private static int cutoffId = Shader.PropertyToID("_Cutoff");

    private static int metallicId = Shader.PropertyToID("_Metallic");
    private static int smoothnessId = Shader.PropertyToID("_Smoothness");
    
    [SerializeField]
    Color baseColor = Color.white;
    [SerializeField]
    private float cutoff = 0.5f;
    [SerializeField][Range(0,1)]
    private float metallic = 0;
    [SerializeField][Range(0,1)]
    private float smoothness = 0.5f;
    
    private static MaterialPropertyBlock block;

    private void OnValidate()
    {
        if (block== null)
        {
            block = new MaterialPropertyBlock();
        }
        block.SetColor(baseColorId,baseColor);
        block.SetFloat(cutoffId,cutoff);
        block.SetFloat(metallicId,metallic);
        block.SetFloat(smoothnessId,smoothness);
        GetComponent<Renderer>().SetPropertyBlock(block);
    }

    private void Awake()
    {
        OnValidate();
    }
}
