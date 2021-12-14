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
    //  将Frame buffer 分离成 2个
    private static int frameBufferId = Shader.PropertyToID("_CameraFrameBuffer");
    
    private static int colorAttachmentId = Shader.PropertyToID("_CameraColorAttachment");
    private static int depthAttachmentId = Shader.PropertyToID("_CameraDepthAttachment");
    private static int depthTextureId = Shader.PropertyToID("_CameraDepthTexture");
    private static int colorTextureId = Shader.PropertyToID("_CameraColorTexture");
    private static int sourceTextureId = Shader.PropertyToID("_SourceTexture");

    private static int bufferSizeId = Shader.PropertyToID("_CameraBufferSize");

    // 是否正在使用深度纹理(不能在深度缓冲区用于渲染   的同时  进行渲染，所以我们必须复制它)
    private bool useDepthTexture;

    private bool useColorTexture;
    // 是否使用中间帧缓冲区， （拷贝深度的前提是存在深度附件，而只有启用后处理才有深度附件=》  为了在没有后处理的情况下也能使用，我们需要在慎用深度纹理时 使用中间帧缓冲区）
    private bool useIntermediateBuffer;
    private bool useHDR;
    // 标记是否使用渲染缩放
    private bool useScaledRendering;
    //最终使用缓冲区的大小
    private Vector2Int bufferSize;
    static CameraSettings defaultCameraSettings = new CameraSettings();
    private Material material;

    // 当深度纹理不存在时，默认使用此 错误的图片
    private Texture2D missionTexture;

    // copyTexture 在WebGL2.0上不支持使用，只能通过着色器进行拷贝（尽管效率低，但是在WebGL2.0能正常工作）
    private static bool copyTextureSupported = SystemInfo.copyTextureSupport > CopyTextureSupport.None;


    public const float renderScaleMin = 0.1f;
    public const float renderScaleMax = 2f;
    
    public CameraRenderer(Shader shader)
    {
        material = CoreUtils.CreateEngineMaterial(shader);
        
        missionTexture = new Texture2D(1,1)
        {
            hideFlags = HideFlags.HideAndDontSave,
            name = "Missing"
        };
        missionTexture.SetPixel(0,0,Color.white * 0.5f);
        missionTexture.Apply(true,true);
    }


    public void Dipose()
    {
        CoreUtils.Destroy(material);
        CoreUtils.Destroy(missionTexture);
    }
    
    
    
    
    
    
    
    
    
    
    
    
    
    public void Render(ScriptableRenderContext context, Camera camera,bool useDynamicBatching,bool useGPUInstancing,
        ShadowSettings shadowSettings,PostFXSettings postFxSettings,CameraBufferSettings bufferSettings)
    {
        this.camera = camera;
        this.context = context;
        var crpCamera = camera.GetComponent<CustomRenderPipelineCamera>();
        CameraSettings cameraSettings = crpCamera ? crpCamera.Settings : defaultCameraSettings;
        //useDepthTexture = true;
        if (camera.cameraType == CameraType.Reflection)
        {
            useDepthTexture = bufferSettings.copyDepthReflection;
            useColorTexture = bufferSettings.copyColorReflection;
        }
        else
        {
            useColorTexture = bufferSettings.copyColor && cameraSettings.copyColor;
            useDepthTexture = bufferSettings.copyDepth && cameraSettings.copyDepth;
        }
        
        if (cameraSettings.overridePostFX)
        {
            // 使用  管线的后处理  覆盖此相机的FX设置
            postFxSettings = cameraSettings.postFxSettings;
        }

        // 是否使用渲染缩放 在  PrePareForScene之前确定
        float renderScale =cameraSettings.GetRenderScale(bufferSettings.renderScale);
        useScaledRendering = renderScale < 0.99f || renderScale > 1.01f; 
        
        // 设置命令缓冲区的名字
        PrepareBuffer();
        //  在Game 视图绘制的几何体也绘制到scene视图中
        PrepareForSceneWindow();
        if (!Cull(shadowSettings.maxDistance))
        {
            return;
        }
        
        
        if (useScaledRendering)
        {
            renderScale = Mathf.Clamp(renderScale, 0.1f, 2f);
            bufferSize.x = (int)(camera.pixelWidth * renderScale);
            bufferSize.y = (int)(camera.pixelHeight * renderScale);
        }
        else
        {
            bufferSize.x = camera.pixelWidth;
            bufferSize.y = camera.pixelHeight;
        }

        useHDR = bufferSettings.allowHDR && camera.allowHDR;
        buffer.BeginSample(SampleName);
        buffer.SetGlobalVector(bufferSizeId,new Vector4(1f/bufferSize.x,1f/bufferSize.y,bufferSize.x,bufferSize.y));
        ExecuteCommandBuffer();
        lighting.Setup(context,cullingResults,shadowSettings);
        postFxStack.Setup(context,camera,bufferSize,postFxSettings,useHDR,cameraSettings.finalBlendMode);
        buffer.EndSample(SampleName);
        Setup();
        
        DrawVisibleGeometry(useDynamicBatching,useGPUInstancing,cameraSettings.renderingLayerMask);
        DrawGizmosBeforeFX();
        if (postFxStack.IsActive)
        {
            postFxStack.Render(colorAttachmentId);    
        }
        else if (useIntermediateBuffer)
        {
            if (camera.targetTexture)
            {
                buffer.CopyTexture(colorAttachmentId,camera.targetTexture);
            }
            else
            {
                Draw(colorAttachmentId,BuiltinRenderTextureType.CameraTarget);
                ExecuteCommandBuffer();    
            }
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
        if (useColorTexture || useDepthTexture)
        {
            CopyAttachments();    
        }
        
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
        useIntermediateBuffer = useScaledRendering || useColorTexture || useDepthTexture || postFxStack.IsActive;
        if (useIntermediateBuffer)
        {
            if (flags > CameraClearFlags.Color)
            {
                // 保证绘制在后处理中的 是最新的数据，而不能是上一帧的数据，（当 DepthOnly  或 dontCare 这两个不会清除颜色 ,这里强制覆盖清理上一帧的颜色）
                flags = CameraClearFlags.Color;
            }
            // 将frameBuffer 分为 颜色和 深度 两份存储 ： 所以 在颜色缓冲去中 不需要写入深度
            buffer.GetTemporaryRT(colorAttachmentId,bufferSize.x,bufferSize.y,0,FilterMode.Bilinear,useHDR ? RenderTextureFormat.DefaultHDR :RenderTextureFormat.Default);
            buffer.GetTemporaryRT(depthAttachmentId,bufferSize.x,bufferSize.y,32,FilterMode.Point,RenderTextureFormat.Depth);
            buffer.SetRenderTarget(colorAttachmentId,RenderBufferLoadAction.DontCare,RenderBufferStoreAction.Store,
                depthAttachmentId,RenderBufferLoadAction.DontCare,RenderBufferStoreAction.Store);
        }
        buffer.ClearRenderTarget(flags<=CameraClearFlags.Depth,flags==CameraClearFlags.Color,flags==CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
        // 为了保证下一帧绘制的图像正确，通常需要清除渲染目标，清除旧的数据
        //  参数 ：  清除深度，  清除颜色， 清除颜色数据的颜色
        //buffer.ClearRenderTarget(true,true,Color.clear);
        buffer.BeginSample(SampleName);
        buffer.SetGlobalTexture(colorTextureId,missionTexture);
        buffer.SetGlobalTexture(depthTextureId,missionTexture);
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
        if (useIntermediateBuffer)
        {
            buffer.ReleaseTemporaryRT(colorAttachmentId);
            buffer.ReleaseTemporaryRT(depthAttachmentId);
            if (useDepthTexture)
            {
                buffer.ReleaseTemporaryRT(depthTextureId);
            }

            if (useColorTexture)
            {
                buffer.ReleaseTemporaryRT(colorTextureId);
            }
        }
        
    }

    //  将深度信息  存储在 depthTexture中
    void CopyAttachments()
    {
        if (useColorTexture)
        {
            buffer.GetTemporaryRT(colorTextureId,bufferSize.x,bufferSize.y,0,FilterMode.Bilinear,useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
            if (copyTextureSupported)
            {
                buffer.CopyTexture(colorAttachmentId,colorTextureId);
            }
            else
            {
                Draw(colorAttachmentId,colorTextureId);
            }
        }
        if (useDepthTexture)
        {
            buffer.GetTemporaryRT(depthTextureId,bufferSize.x,bufferSize.y,32,FilterMode.Point,RenderTextureFormat.Depth);

            if (copyTextureSupported)
            {
                buffer.CopyTexture(depthAttachmentId,depthTextureId);    
            }
            else
            {
                Draw(depthAttachmentId,depthTextureId,true);
                
                //buffer.SetRenderTarget(colorAttachmentId,RenderBufferLoadAction.Load,RenderBufferStoreAction.Store,
                  //  depthAttachmentId,RenderBufferLoadAction.Load,RenderBufferStoreAction.Store);
                
            }
            
        }

        if (!copyTextureSupported)
        {
            buffer.SetRenderTarget(colorAttachmentId,RenderBufferLoadAction.Load,RenderBufferStoreAction.Store,
                depthAttachmentId,RenderBufferLoadAction.Load,RenderBufferStoreAction.Store);
        }
        ExecuteCommandBuffer();
    }


    void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to,bool isDepth = false)
    {
        buffer.SetGlobalTexture(sourceTextureId,from);
        buffer.SetRenderTarget(to,RenderBufferLoadAction.DontCare,RenderBufferStoreAction.Store);
        buffer.DrawProcedural(Matrix4x4.identity, material,isDepth ? 1 : 0,MeshTopology.Triangles,3);
        
    }
    
    
    
}


///  Color.clear(黑色)