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
    public void Render(ScriptableRenderContext context, Camera camera,bool useDynamicBatching,bool useGPUInstancing,ShadowSettings shadowSettings)
    {
        this.camera = camera;
        this.context = context;
        // 设置命令缓冲区的名字
        PrepareBuffer();
        //  在Game 视图绘制的几何体也绘制到scene视图中
        PrepareForSceneWindow();
        if (!Cull(shadowSettings.maxDistance))
        {
            return;
        }
        buffer.BeginSample(SampleName);
        ExecuteCommandBuffer();
        lighting.Setup(context,cullingResults,shadowSettings);
        buffer.EndSample(SampleName);
        Setup();
        
        DrawVisibleGeometry(useDynamicBatching,useGPUInstancing);
        lighting.Cleanup();
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
    

    void DrawVisibleGeometry(bool useDynamicBetching,bool useGPUInstancing)
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
        };
        drawingSettings.SetShaderPassName(1,litShaderTagId);
        // 设置哪些类型的渲染队列可以被绘制
        var filteringSettings =  new FilteringSettings(RenderQueueRange.opaque);
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
        DrawGizmos();
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

}


///  Color.clear(黑色)