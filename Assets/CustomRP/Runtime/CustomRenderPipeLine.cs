using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CustomRenderPipeLine : RenderPipeline
{
    CameraRenderer renderer = new CameraRenderer();
    private bool useDynamicBatching, useGPUInstancing;

    private ShadowSettings shadowSettings = default;
    PostFXSettings postFxSettings = default;
    public CustomRenderPipeLine(bool useDynamicBatching, bool useGPUInstancing,bool useSRPBatcher,ShadowSettings shadowSettings,PostFXSettings postFxSettings)
    {
        
        this.shadowSettings = shadowSettings;
        this.postFxSettings = postFxSettings;
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        // 灯光适用线性强度
        GraphicsSettings.lightsUseLinearIntensity = true;

        InitializeForEditor();
    }
    
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        for (int i = 0; i < cameras.Length; i++)
        {
            renderer.Render(context,cameras[i],useDynamicBatching,useGPUInstancing,shadowSettings,postFxSettings);
        }
    }
}
