using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

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

    private int fxSourceId = Shader.PropertyToID("_PostFXSource");
    private int fxSource2Id = Shader.PropertyToID("_PostFXSource2");
    private int bloomBucibicUpsamplingId = Shader.PropertyToID("_BloomBicubicUpsampling");
    private int bloomPrefilterId = Shader.PropertyToID("_BloomPrefilter");
    private int bloomThresholdId = Shader.PropertyToID("_BloomThreshold");
    private int bloomIntensityId = Shader.PropertyToID("_BloomIntensity");
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


    void DoBloom(int sourceId)
    {
        buffer.BeginSample("Bloom");
        PostFXSettings.BloomSettings bloom = settings.Bloom;
        int width = camera.pixelWidth / 2, height = camera.pixelHeight / 2;

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
        Draw(formId, BuiltinRenderTextureType.CameraTarget, finalPass);
        buffer.ReleaseTemporaryRT(formId);
        buffer.EndSample("Bloom");
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
    public void Setup(ScriptableRenderContext context, Camera camera, PostFXSettings settings,bool useHDR)
    {
        this.context = context;
        this.camera = camera;
        this.settings = camera.cameraType <= CameraType.SceneView ? settings : null;
        this.useHDR = useHDR;
        ApplySceneViewState();
    }
    
    public void Render(int sourceId)
    {
        //Draw(sourceId,BuiltinRenderTextureType.CameraTarget,Pass.Copy);
        DoBloom(sourceId);
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
    
}
