using UnityEngine;
using UnityEngine.Rendering;
public class Shadows
{
    private const string bufferName = "Shadows";

    private CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    // 可以投射阴影的定向光的数量
    private const int maxShadowedDirectionalLightCount = 4;

    // 追踪可见光的索引
    struct ShadowedDirectionalLight
    {
        public int visiableLightIndex;
    }
    // 存储可投射阴影的可见光源的索引
    ShadowedDirectionalLight[] ShadowedDirectionalLights = new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];
    // 已存储可投射阴影的可见光源的索引
    private int ShadowedDirectionalLightCount;
    private ScriptableRenderContext context;
    private CullingResults cullingResults;
    private ShadowSettings settings;

    
    // 级联阴影相关数据
    //最大级联数量
    private const int maxCascades = 4;
    //
    //static Matrix4x4[] dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades];

    public void Setup(ScriptableRenderContext context,CullingResults cullingResults,ShadowSettings settings)
    {
        this.context = context;
        this.cullingResults = cullingResults;
        this.settings = settings;
        ShadowedDirectionalLightCount = 0;
    }

    void ExcuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }


    public void Render()
    {
        if (ShadowedDirectionalLightCount > 0)
        {
            RenderDirectionalShadows();    
        }
        else
        {
            
        }
    }

    // 存储可见光的阴影数据
    public Vector2 ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        // 存储可见光的索引，前提是光源开启了阴影投射，且阴影的强度不为0
        if (ShadowedDirectionalLightCount < maxShadowedDirectionalLightCount && light.shadows != LightShadows.None && light.shadowStrength > 0f && cullingResults.GetShadowCasterBounds(visibleLightIndex,out Bounds b))
        {
            ShadowedDirectionalLights[ShadowedDirectionalLightCount] = new ShadowedDirectionalLight { visiableLightIndex = visibleLightIndex};
            return new Vector2(light.shadowStrength,ShadowedDirectionalLightCount++);
        }

        return Vector2.zero;
    }

    private static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");


    private static int dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");
    static Matrix4x4[] dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount];
    
    
    
    
    // 渲染定向光阴影
    void RenderDirectionalShadows()
    {
        int atlasSize = (int)settings.directional.atlasSize;
        buffer.GetTemporaryRT(dirShadowAtlasId,atlasSize,atlasSize,32,FilterMode.Bilinear,RenderTextureFormat.Shadowmap);
        //指定渲染数据存储到渲染纹理 而不是帧缓冲区中
        buffer.SetRenderTarget(dirShadowAtlasId,RenderBufferLoadAction.DontCare,RenderBufferStoreAction.Store);
        //  因为只关心 深度缓冲，所以只需要清除深度
        buffer.ClearRenderTarget(true,false,Color.clear);
        
        buffer.BeginSample(bufferName);
        ExcuteBuffer();
        // 要分割的图块大小和数量
        int split = ShadowedDirectionalLightCount <= 1 ? 1 : 2;
        int tileSize = atlasSize / split;
        for (int i = 0; i < ShadowedDirectionalLightCount; i++)
        {
            RenderDirectionalShadows(i,split,tileSize);
        }
        buffer.SetGlobalMatrixArray(dirShadowMatricesId,dirShadowMatrices);
        buffer.EndSample(bufferName);
        ExcuteBuffer();
    }

    // 渲染单个光源阴影
    void RenderDirectionalShadows(int index,int split, int tileSize)
    {
        ShadowedDirectionalLight light = ShadowedDirectionalLights[index];
        var shadowSetting = new ShadowDrawingSettings(cullingResults,light.visiableLightIndex);

        cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(light.visiableLightIndex, 0, 1,
            Vector3.zero, tileSize, 0f, out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix,
            out ShadowSplitData splitData);

        shadowSetting.splitData = splitData;
        Vector2 offset = SetTileViewPort(index, split, tileSize);
        // 投影矩阵乘以视图矩阵， 得到从世界空间转换到灯光空间的转换矩阵
        dirShadowMatrices[index] = ConvertToAtlasMatrix(projMatrix * viewMatrix, offset, split);
        buffer.SetViewProjectionMatrices(viewMatrix,projMatrix);
        ExcuteBuffer();
        context.DrawShadows(ref shadowSetting);
    }


    Vector2 SetTileViewPort(int index,int split,int tileSize)
    {
        // 计算索引图块的位置
        Vector2 offset = new Vector2(index % split,index / split);
        // 设置渲染视口,拆分成多个块
        buffer.SetViewport(new Rect(offset.x * tileSize,offset.y * tileSize,tileSize,tileSize));
        return offset;
    }

    // 返回一个从世界空间到阴影图块空间的转换矩阵
    Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split)
    {
        // 使用反向Zbuffer
        if (SystemInfo.usesReversedZBuffer)
        {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }

        float scale = 1f / split;
        // 设置矩阵坐标
        m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
        m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
        m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
        m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
        
        m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
        m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
        m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
        m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
        
        m.m20 = 0.5f * (m.m20 + m.m30);
        m.m21 = 0.5f * (m.m21 + m.m31);
        m.m22 = 0.5f * (m.m22 + m.m32);
        m.m23 = 0.5f * (m.m23 + m.m33);
        
        return m;
    }
    
    
    public void Cleanup()
    {
        buffer.ReleaseTemporaryRT(dirShadowAtlasId);
        ExcuteBuffer();
    }
    
    
}
