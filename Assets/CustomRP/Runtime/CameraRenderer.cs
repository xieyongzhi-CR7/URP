using UnityEditor;
using UnityEngine;
using  UnityEngine.Rendering;

public partial class CameraRenderer
{
    private ScriptableRenderContext context;
    private Camera camera;

    private const string bufferName = "Render Camera_CommandBuffer";
    
    //  SRPDefaultUnlit
    static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");//UniversalForward
    static ShaderTagId litShaderTagId = new ShaderTagId("CustomLit");
    
    //  命令缓冲区
    CommandBuffer buffer = new CommandBuffer
    {
        // 此name 在FrameDebug中可以识别到
        name = bufferName
    };

    /// <summary>
    /// 存储剔除后的结果
    /// </summary>
    private CullingResults cullingResults;
    Lighting lighting = new Lighting();
    // 后处理相关
    PostFXStack postFxStack = new PostFXStack();
    private static int frameBufferId = Shader.PropertyToID("_CameraFrameBuffer");
    private bool useHDR;
    
    static CameraSettings defaultCameraSettings = new CameraSettings();
    public void Render(ScriptableRenderContext context, Camera camera,bool useDynamicBatching,bool useGPUInstancing,ShadowSettings shadowSettings,PostFXSettings postFxSettings,bool allowHDR)
    {
        this.camera = camera;
        this.context = context;
        var crpCamera = camera.GetComponent<CustomRenderPipelineCamera>();
        CameraSettings cameraSettings = crpCamera ? crpCamera.Settings : defaultCameraSettings;
        if (cameraSettings.overridePostFX)
        {
            // 使用  管线的后处理  覆盖此相机的FX设置
            postFxSettings = cameraSettings.postFxSettings;
        } 
        
        // 设置命令缓冲区的名字
        PrepareBuffer();
        //  在Game 视图绘制的几何体也绘制到scene视图中
        PrepareForSceneWindow();
        if (!Cull(shadowSettings.maxDistance))
        {
            return;
        }

        useHDR = allowHDR && camera.allowHDR;
        buffer.BeginSample(SampleName);
        ExecuteCommandBuffer();
        lighting.Setup(context,cullingResults,shadowSettings);
        postFxStack.Setup(context,camera,postFxSettings,useHDR,cameraSettings.finalBlendMode);
        buffer.EndSample(SampleName);
        Setup();
        
        DrawVisibleGeometry(useDynamicBatching,useGPUInstancing,cameraSettings.renderingLayerMask);
        DrawGizmosBeforeFX();
        if (postFxStack.IsActive)
        {
            postFxStack.Render(frameBufferId);    
        }
        DrawGizmosAfterFX();
        Cleanup();
        Submit();
    }
    
    bool Cull(float maxShadowDistance)
    {
        ScriptableCullingParameters p;
        //  得到所有需要进行剔除检查的所有物体，将结果存在
        if (camera.TryGetCullingParameters(out p))
        {
            // 最大阴影距离和远裁剪平面比较， 取最小值
            p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
            // 执行剔除后的结果存储起来
            cullingResults = context.Cull(ref p);
            return true;
        }
        return false;
    }
    

    void DrawVisibleGeometry(bool useDynamicBetching,bool useGPUInstancing,int renderingLayerMask)
    {
        //  设置绘制顺序 和 指定相机
        var sortingSettings = new SortingSettings(camera)
        {
            criteria   = SortingCriteria.CommonOpaque
        };
        
        // 设置渲染的Shader Pass  和排序模式
        var drawingSettings = new DrawingSettings(unlitShaderTagId,sortingSettings)
        {
            enableDynamicBatching = useDynamicBetching,
            enableInstancing =  useGPUInstancing,
            perObjectData = PerObjectData.Lightmaps| PerObjectData.LightProbe| PerObjectData.LightProbeProxyVolume
            | PerObjectData.ShadowMask | PerObjectData.OcclusionProbe | PerObjectData.OcclusionProbeProxyVolume
            | PerObjectData.ReflectionProbes
        };
        drawingSettings.SetShaderPassName(1,litShaderTagId);
        // 设置哪些类型的渲染队列可以被绘制
        var filteringSettings =  new FilteringSettings(RenderQueueRange.opaque,renderingLayerMask:(uint)renderingLayerMask);
        //（1） 绘制不透明物体
        context.DrawRenderers(cullingResults,ref drawingSettings,ref filteringSettings);
        // (2)绘制天空盒
        context.DrawSkybox(camera);

        //  (3)修改配置， 只绘制 半透物体
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;
        context.DrawRenderers(cullingResults,ref drawingSettings,ref filteringSettings);
        //  绘制半透明物体结束
        DrawUnsupportedShaders();
        
        // DrawGizmosBeforeFX();
        // if (postFxStack.IsActive)
        // {
        //     postFxStack.Render(frameBufferId);    
        // }
        // DrawGizmosAfterFX();
    }

    void Submit()
    {
        // 提交缓冲区渲染命令后才进行渲染
        context.Submit();
        buffer.EndSample(SampleName);
    }

    // 设置相机属性和矩阵
    void Setup()
    {
        
        // 在清楚前 设置相机属性，后面就能快速清除
        context.SetupCameraProperties(camera);
        CameraClearFlags flags = camera.clearFlags;
        if (postFxStack.IsActive)
        {
            if (flags > CameraClearFlags.Color)
            {
                // 保证绘制在后处理中的 是最新的数据，而不能是上一帧的数据，（当 DepthOnly  或 dontCare 这两个不会清除颜色 ,这里强制覆盖清理上一帧的颜色）
                flags = CameraClearFlags.Color;
            }
            buffer.GetTemporaryRT(frameBufferId,camera.pixelWidth,camera.pixelHeight,32,FilterMode.Bilinear,useHDR ? RenderTextureFormat.DefaultHDR :RenderTextureFormat.Default);
            buffer.SetRenderTarget(frameBufferId,RenderBufferLoadAction.DontCare,RenderBufferStoreAction.Store);
        }
        buffer.ClearRenderTarget(flags<=CameraClearFlags.Depth,flags==CameraClearFlags.Color,flags==CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
        // 为了保证下一帧绘制的图像正确，通常需要清除渲染目标，清除旧的数据
        //  参数 ：  清除深度，  清除颜色， 清除颜色数据的颜色
        //buffer.ClearRenderTarget(true,true,Color.clear);
        buffer.BeginSample(SampleName);
        ExecuteCommandBuffer();
    }

    //  buff 执行完毕 ，直接清除
    void ExecuteCommandBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    void Cleanup()
    {
        lighting.Cleanup();
        if (postFxStack.IsActive)
        {
            buffer.ReleaseTemporaryRT(frameBufferId);
        }
    }
}


///  Color.clear(黑色)