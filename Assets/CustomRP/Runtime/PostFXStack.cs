using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
//  using  static 类似于使用命名空间， 但使用的是类型。。。 它可以直接访类或者结果的所有常量
using static PostFXSettings;
partial class PostFXStack
{
    private const string bufferName = "Post FX";
    CommandBuffer buffer = new CommandBuffer()
    {
        name = bufferName
    };

    private ScriptableRenderContext context;
    private Camera camera;
    private PostFXSettings settings;
    private CameraSettings.FinalBlendMode finalBlendMode;
    private int fxSourceId = Shader.PropertyToID("_PostFXSource");
    private int fxSource2Id = Shader.PropertyToID("_PostFXSource2");
    private int bloomBucibicUpsamplingId = Shader.PropertyToID("_BloomBicubicUpsampling");
    private int bloomPrefilterId = Shader.PropertyToID("_BloomPrefilter");
    private int bloomThresholdId = Shader.PropertyToID("_BloomThreshold");
    private int bloomIntensityId = Shader.PropertyToID("_BloomIntensity");

    private int bloomResultId = Shader.PropertyToID("_BloomResult");
    public bool IsActive => settings != null;
    private const int maxBloomPyramidLevels = 16;
    private int bloomPyramidId;
    enum Pass
    {
        BloomVertical,
        BloomHorizontal,
        BloomAdd,
        BloomPrefilter,
        BloomPrefilterFirefiles,
        BloomScatter,
        // 补偿丢失的散射光
        BloomScatterFinal,
        
        // toneMapping 的不同模式
        ToneMappingNone,
        ToneMappingACES,
        ToneMappingNeutral,
        ToneMappingReinhard,
        Final,
        Copy,
    }
    // 是否使用HDR
    private bool useHDR;
    
    public PostFXStack()
    {
        bloomPyramidId = Shader.PropertyToID("_BloomPyramid0");
        for (int i = 0; i < maxBloomPyramidLevels * 2; i++)
        {
            // id的结果是 申请的顺序 +1的结果
            Shader.PropertyToID("_BloomPyramid"+i);
        }
    }


    

    void DoToneMapping(int sourceId)
    {
        PostFXSettings.ToneMappingSettings.Mode mode = settings.ToneMapping.mode;
        Pass pass =  Pass.ToneMappingNone + (int)mode;
        Draw(sourceId,BuiltinRenderTextureType.CameraTarget,pass);
    }

    bool DoBloom(int sourceId)
    {
        PostFXSettings.BloomSettings bloom = settings.Bloom;
        int width = camera.pixelWidth / 2, height = camera.pixelHeight / 2;
        if (bloom.maxIterations == 0 || bloom.intensity <= 0 || height < bloom.downscaleLimit * 2 ||
            width < bloom.downscaleLimit * 2)
        {
            // Draw(sourceId,BuiltinRenderTextureType.CameraTarget,Pass.Copy);
            // buffer.EndSample("Bloom");
            //Debug.LogError(" 000000 width="+width+"  height="+height+"  bloom.maxIterations="+bloom.maxIterations+"  bloom.intensity="+bloom.intensity);
            Debug.LogError(" bloom 效果已经禁用 ");
            return false;
        }
        buffer.BeginSample("Bloom");
        Vector4 threshold;
        threshold.x = Mathf.GammaToLinearSpace(bloom.threshold);
        threshold.y = threshold.x * bloom.thresholdKnee;
        threshold.z = threshold.y * 2f;
        threshold.w = 0.25f / (threshold.y + 0.00001f);
        threshold.y -= threshold.x;
        buffer.SetGlobalVector(bloomThresholdId, threshold);



        RenderTextureFormat format = useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
        buffer.GetTemporaryRT(bloomPrefilterId, width, height, 0, FilterMode.Bilinear, format);
        Draw(sourceId, bloomPrefilterId, bloom.fadeFirefiles ? Pass.BloomPrefilterFirefiles : Pass.BloomPrefilter);
        width /= 2;
        height /= 2;
        int formId = bloomPrefilterId;
        int toId = bloomPyramidId + 1;

        int i;
        for (i = 0; i < bloom.maxIterations; i++)
        {
            if (bloom.maxIterations == 0 || bloom.intensity <= 0 || height < bloom.downscaleLimit * 2 ||
                width < bloom.downscaleLimit * 2)
            {
                //Debug.LogError(" 111111 width="+width+"  height="+height+"  bloom.maxIterations="+bloom.maxIterations+"  bloom.intensity="+bloom.intensity);
                // Draw(sourceId,BuiltinRenderTextureType.CameraTarget,Pass.Copy);
                // buffer.EndSample("Bloom");
                //return false;
                break;
            }
            int midId = toId - 1;
            buffer.GetTemporaryRT(midId, width, height, 0, FilterMode.Bilinear, format);
            buffer.GetTemporaryRT(toId, width, height, 0, FilterMode.Bilinear, format);
            Draw(formId, midId, Pass.BloomHorizontal);
            Draw(midId, toId, Pass.BloomVertical);
            formId = toId;
            toId += 2;
            width /= 2;
            height /= 2;
        }

        buffer.ReleaseTemporaryRT(bloomPrefilterId);
        buffer.SetGlobalFloat(bloomBucibicUpsamplingId, bloom.bicubicUpsampling ? 1f : 0f);

        Pass combinePass, finalPass;
        float finalIntensity;
        if (bloom.mode == PostFXSettings.BloomSettings.Mode.Additive)
        {
            combinePass =finalPass= Pass.BloomAdd;
            buffer.SetGlobalFloat(bloomIntensityId, 1f);
            finalIntensity = bloom.intensity;
        }
        else
        {
            combinePass = Pass.BloomScatter;
            finalPass = Pass.BloomScatterFinal;
            buffer.SetGlobalFloat(bloomIntensityId, bloom.scatter);
            finalIntensity = Mathf.Min(bloom.intensity, bloom.scatter);
        }



        //Draw(formId,BuiltinRenderTextureType.CameraTarget,Pass.BloomHorizontal);
        if (i > 1)
        {


            buffer.ReleaseTemporaryRT(formId - 1);
            toId -= 5;
            for (i -= 1; i > 0; i--)
            {
                buffer.SetGlobalTexture(fxSource2Id, toId + 1);
                Draw(formId, toId, combinePass);
                buffer.ReleaseTemporaryRT(formId);
                buffer.ReleaseTemporaryRT(toId + 1);
                formId = toId;
                toId -= 2;
            }
        }
        else
        {
            buffer.ReleaseTemporaryRT(bloomPyramidId);
        }
        buffer.SetGlobalFloat(bloomIntensityId,finalIntensity);
        buffer.SetGlobalTexture(fxSource2Id, sourceId);
        // 由于应用toneMapping,把bloom的结果先应用toneMapping 后再画到相机显示
        buffer.GetTemporaryRT(bloomResultId,camera.pixelWidth,camera.pixelHeight,0,FilterMode.Bilinear,format);
        Draw(formId, bloomResultId, finalPass);
        buffer.ReleaseTemporaryRT(formId);
        buffer.EndSample("Bloom");
        return true;
    }


    /// <summary>
    /// 用来替换 buff.Blit 的函数， 比Blit 更高效
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="pass"></param>
    void Draw(RenderTargetIdentifier from,RenderTargetIdentifier to, Pass pass)
    {
        buffer.SetGlobalTexture(fxSourceId,from);
        buffer.SetRenderTarget(to,RenderBufferLoadAction.DontCare,RenderBufferStoreAction.Store);
        buffer.DrawProcedural(Matrix4x4.identity, settings.Material,(int)pass,MeshTopology.Triangles,3);
    }

    private int finalSrcBlendId = Shader.PropertyToID("_FinalSrcBlend");
    private int finalDesBlendId = Shader.PropertyToID("_FinalDesBlend");
    void DrawFinal(RenderTargetIdentifier from,Pass pass)
    {
        buffer.SetGlobalFloat(finalSrcBlendId,(float)finalBlendMode.source);
        buffer.SetGlobalFloat(finalDesBlendId,(float)finalBlendMode.destination);
        
        buffer.SetGlobalTexture(fxSourceId,from);
        //buffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget,RenderBufferLoadAction.DontCare,RenderBufferStoreAction.Store);
        buffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, finalBlendMode.destination == BlendMode.Zero ? RenderBufferLoadAction.DontCare :  RenderBufferLoadAction.Load,RenderBufferStoreAction.Store);
        // 设置视口 (在应用后处理后， 设置renderTarget 后， 设置 视口， 最后画在前面两步 确定的位置)
        buffer.SetViewport(camera.pixelRect);
        Debug.LogError(" pass = "+ pass.ToString()+"  ="+(int)pass);
        buffer.DrawProcedural(Matrix4x4.identity, settings.Material,(int)pass,MeshTopology.Triangles,3);
    }
    public void Setup(ScriptableRenderContext context, Camera camera, PostFXSettings settings,bool useHDR, CameraSettings.FinalBlendMode finalBlendMode)
    {
        this.context = context;
        this.camera = camera;
        this.settings = camera.cameraType <= CameraType.SceneView ? settings : null;
        this.useHDR = useHDR;
        this.finalBlendMode = finalBlendMode;
        ApplySceneViewState();
    }
    
    public void Render(int sourceId)
    {
        //Draw(sourceId,BuiltinRenderTextureType.CameraTarget,Pass.Copy);
        if (DoBloom(sourceId))
        {
            Debug.LogError("执行bloom   toneMapping");
            // 有bloom  则 将bloom结果应用 toneMapping
            DoColorGradingAndMapping(bloomResultId);
            buffer.ReleaseTemporaryRT(bloomResultId);
        }
        else
        {
            //Debug.LogError("不执行 bloom  只toneMapping");
            // 不存在bloom  则直接将结果应用 toneMapping
            DoColorGradingAndMapping(sourceId);
        }
        //buffer.Blit(sourceId,BuiltinRenderTextureType.CameraTarget);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    // void ApplySceneViewState()
    // {
    //     if (camera.cameraType == CameraType.SceneView && !SceneView.currentDrawingSceneView.sceneViewState.showImageEffects )
    //     {
    //         settings = null;
    //     }
    // }
//------------------------------------------------
//-------------颜色分级--------------------------
//------------------------------------------------

    private static int colorAdjustmentsId = Shader.PropertyToID("_ColorAdjustments");
    private static int colorFilterId = Shader.PropertyToID("_ColorFilterId");
    private static int whiteBlanceId = Shader.PropertyToID("_WhiteBalance");
    void ConfigureColorAdjustments()
    {
        ColorAdjustmentsSettings colorAdjustments = settings.ColorAdjustments;
        
        buffer.SetGlobalVector(colorAdjustmentsId,
            new Vector4(
                Mathf.Pow(2f,colorAdjustments.postExposure),
            colorAdjustments.contrast * 0.01f + 1f,
            colorAdjustments.hueShift * (1f / 360f),
            colorAdjustments.saturation * 0.01f + 1f
            ));
        buffer.SetGlobalColor(colorFilterId,colorAdjustments.colorFilter.linear);
    }


    void DoColorGradingAndMapping(int sourceId)
    {
        ConfigureColorAdjustments();
        ConfigureWhiteBlance();
        ConfigureSplitToning();
        ConfigureChannelMixer();
        ConfigureShadowsMidtonesHighlights();
        ToneMappingSettings.Mode mode = settings.ToneMapping.mode;
        Pass pass = Pass.ToneMappingNone + (int) mode;
        //Draw(sourceId,BuiltinRenderTextureType.CameraTarget,pass);
        DrawFinal(sourceId,pass);
    }


    // 白平衡
    
    void ConfigureWhiteBlance()
    {
        WihteBalanceSettings whiteBalance = settings.WhiteBalance; 
        buffer.SetGlobalVector(whiteBlanceId,ColorUtils.ColorBalanceToLMSCoeffs(whiteBalance.temperature,whiteBalance.tint));
    }

    private static int splitToningShadowsId = Shader.PropertyToID("_SplitToningShadows");
    private static int splitToningHightLihgtsId = Shader.PropertyToID("_SplitToningHightLihgts");
    void ConfigureSplitToning()
    {
        SplitToningSettings splitToning = settings.SplitToning;
        Color splitColor = splitToning.shadows;
        splitColor.a = splitToning.balance * 0.01f;
        buffer.SetGlobalColor(splitToningShadowsId,splitColor);
        buffer.SetGlobalColor(splitToningHightLihgtsId,splitToning.hightLights);
    }

    private static int channelMixerRedId = Shader.PropertyToID("_ChannelMixerRed");
    private static int channelMixerGreenId = Shader.PropertyToID("_ChannelMixerGreen");
    private static int channelMixerBlueId = Shader.PropertyToID("_ChannelMixerBlue");


    void ConfigureChannelMixer()
    {
        ChannelMixerSettings channelMixer = settings.ChannelMixer;
        buffer.SetGlobalVector(channelMixerRedId,channelMixer.red);
        buffer.SetGlobalVector(channelMixerGreenId,channelMixer.green);
        buffer.SetGlobalVector(channelMixerBlueId,channelMixer.blue);
    }


    private static int smhShadowId = Shader.PropertyToID("_SMHShadows");
    private static int smhMidtonesId = Shader.PropertyToID("_SMHMidtones");
    private static int smhHighlightsId = Shader.PropertyToID("_SMHHighlights");
    private static int smhRangeId = Shader.PropertyToID("_SMHRange");
    void ConfigureShadowsMidtonesHighlights()
    {
        ShadowsMidtonesHightlightsSettings smh = settings.ShadowsMidtonesHightlights;
        buffer.SetGlobalColor(smhShadowId,smh.shadows);
        buffer.SetGlobalColor(smhMidtonesId,smh.midtones);
        buffer.SetGlobalColor(smhHighlightsId,smh.hightlights);
        buffer.SetGlobalVector(smhRangeId,new Vector4(smh.shadowStart,smh.shadowsEnd,smh.hightlightsStart,smh.highLightsEnd));
    }    
    
    
    
    
    
    
    
    
    
    
    
    

}
