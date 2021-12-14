using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/CreateCustomRenderPipline")]
public partial class CustomRenderPineAsset : RenderPipelineAsset
{
    [SerializeField]
    private bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true;
    [SerializeField]
    private ShadowSettings shadows = default;
    // [SerializeField]
    // private bool allowHDR = true;

    [SerializeField]
    CameraBufferSettings cameraBuffer = new CameraBufferSettings
    {
        allowHDR = true,
        renderScale =  1.0f,
    };
    
    
    [SerializeField]
    private PostFXSettings postFxSettings = default;

    [SerializeField]
    private Shader CameraRendererShader = default;
    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeLine(useDynamicBatching,useGPUInstancing,useSRPBatcher,shadows,postFxSettings,cameraBuffer,CameraRendererShader);
    }
}
