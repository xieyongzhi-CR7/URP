using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/CreateCustomRenderPipline")]
public class CustomRenderPineAsset : RenderPipelineAsset
{
    [SerializeField]
    private bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true;
    [SerializeField]
    private ShadowSettings shadows = default;
    [SerializeField]
    private bool allowHDR = true;

    [SerializeField]
    private PostFXSettings postFxSettings = default;
    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeLine(useDynamicBatching,useGPUInstancing,useSRPBatcher,shadows,postFxSettings,allowHDR);
    }
}
