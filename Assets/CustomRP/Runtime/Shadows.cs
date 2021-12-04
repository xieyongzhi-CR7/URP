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
    private const int maxShadowedDirectionalLightCount = 4, maxShadowedOtherLightCount = 4;

    // 追踪可见光的索引
    struct ShadowedDirectionalLight
    {
        public int visiableLightIndex;
        // 斜度比例偏差值
        public float slopeScaleBias;
        //近平面偏移字段（在阴影平坠中使用）
        public float nearPlaneOffset;
    }
    // 存储可投射阴影的可见光源的索引
    ShadowedDirectionalLight[] ShadowedDirectionalLights = new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];
    // 已存储可投射阴影的可见光源的索引
    private int ShadowedDirectionalLightCount,     ShadowedOtherLightCount;
    
    // other 的设置
    struct ShadowedOtherLight
    {
        public int visibleLightIndex;
        public float slopeScaleBias;
        public float normalBias;
        // 是否是点光源
        public bool isPoint;
    }
    ShadowedOtherLight[] shadowedOtherLights = new ShadowedOtherLight[maxShadowedOtherLightCount];
    
    private ScriptableRenderContext context;
    private CullingResults cullingResults;
    private ShadowSettings settings;

    
    
    // 级联包围球相关数据
    // 级联数量
    private static int cascadeCountId = Shader.PropertyToID("_CascadeCount");
    // 级联包围球
    private static int cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres");
    // 包围球的数量： xyz 存储包围球的位置数据， w分量存储球体半径
    private static Vector4[] cascadeCullingSpheres = new Vector4[maxCascades];
    
    

    // 
    private static int shadowDistanceId = Shader.PropertyToID("_ShadowDistance");

    // 阴影过渡距离
    private static int shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");
    // 用于控制是否使用阴影平坠（Shadow Pancaking）; 这中技术只适用于：dir灯光，  对有位置的other灯光不适用
    private static int shadowPancakingId = Shader.PropertyToID("_ShadowPancaking");


    private static string[] shadowMaskKeywords =
    {
        "_SHADOW_MASK_ALWAYS",
        "_SHADOW_MASK_DISTANCE",
    };

    private bool useShadowMask;

    
    public void Setup(ScriptableRenderContext context,CullingResults cullingResults,ShadowSettings settings)
    {
        this.context = context;
        this.cullingResults = cullingResults;
        this.settings = settings;
        ShadowedDirectionalLightCount = ShadowedOtherLightCount = 0;
        useShadowMask = false;
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
            buffer.GetTemporaryRT(dirShadowAtlasId,1,1,32,FilterMode.Bilinear,RenderTextureFormat.Shadowmap);
        }
        if (ShadowedOtherLightCount > 0)
        {
            RenderOtherShadows();
        }
        else
        {
            buffer.SetGlobalTexture(otherShadowAtlasId,dirShadowAtlasId);
        }
        buffer.BeginSample(bufferName);
        SetKeywords(shadowMaskKeywords,useShadowMask? QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask ? 0:1 : -1 );
        // 将级联数量和包围球数据发送到GPU
        buffer.SetGlobalInt(cascadeCountId,settings.directional.cascadeCount);
        float f = 1f - settings.directional.cascadeFade;
        buffer.SetGlobalVector(shadowDistanceFadeId,new Vector4(1f/ settings.maxDistance,1f/settings.distanceFade,1f / (1f - f*f)));
        buffer.SetGlobalVector(shadowAtlasSized,atlasSizes);
        buffer.EndSample(bufferName);
        ExcuteBuffer();
    }

    // 存储可见光的阴影数据  
    public Vector4 ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        // 存储可见光的索引，前提是光源开启了阴影投射，且阴影的强度不为0
        if (ShadowedDirectionalLightCount < maxShadowedDirectionalLightCount && light.shadows != LightShadows.None && light.shadowStrength > 0f )
        {
            float maskChannel = -1;
            LightBakingOutput lightBaking = light.bakingOutput;
            if (lightBaking.lightmapBakeType == LightmapBakeType.Mixed && lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask)
            {
                useShadowMask = true;
                maskChannel = lightBaking.occlusionMaskChannel;
            }

            if (! cullingResults.GetShadowCasterBounds(visibleLightIndex,out Bounds b))
            {
                return new Vector4(-light.shadowStrength,0f,0f,maskChannel);
            }
            ShadowedDirectionalLights[ShadowedDirectionalLightCount] = 
                new ShadowedDirectionalLight { visiableLightIndex = visibleLightIndex, slopeScaleBias = light.shadowBias,nearPlaneOffset = light.shadowNearPlane};
            // 返回阴影强度和阴影图块的索引
            return new Vector4(light.shadowStrength,
                settings.directional.cascadeCount 
                * ShadowedDirectionalLightCount++,
                light.shadowNormalBias,maskChannel);
        }

        return new Vector4(0f,0f,0f,1f);
    }

    private static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");


    private static int dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");

    private static int otherShadowAtlasId = Shader.PropertyToID("_OtherShadowAtlas");
    private static int otherShadowMatricesId = Shader.PropertyToID("_OtherShadowMatrices");
    private static int otherShadowTilesId = Shader.PropertyToID("_OtherShadowTiles");
    
    
    //最大级联数量
    private const int maxCascades = 4;

    static Matrix4x4[] dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades];
    static Matrix4x4[] otherShadowMatrices = new Matrix4x4[maxShadowedOtherLightCount];
    
    
    // 级联数据
    private static int cascadeDataId = Shader.PropertyToID("_CascadeData");
    static Vector4[] cascadeData = new Vector4[maxCascades];
    static Vector4[] otherShadowTiles = new Vector4[maxShadowedOtherLightCount];
    private Vector4 atlasSizes;
    
    // 渲染定向光阴影
    void RenderDirectionalShadows()
    {
        int atlasSize = (int)settings.directional.atlasSize;
        atlasSizes.x = atlasSize;
        atlasSizes.y = 1f / atlasSize;
        buffer.GetTemporaryRT(dirShadowAtlasId,atlasSize,atlasSize,32,FilterMode.Bilinear,RenderTextureFormat.Shadowmap);
        //指定渲染数据存储到渲染纹理 而不是帧缓冲区中
        // RenderBufferLoadAction. DontCare : gpu的片上内容，不用加载到内存中
        // RenderBufferStoreAction. Store : 将render内容存储到RAM中
        buffer.SetRenderTarget(dirShadowAtlasId,RenderBufferLoadAction.DontCare,RenderBufferStoreAction.Store);
        //  因为只关心 深度缓冲，所以只需要清除深度
        buffer.ClearRenderTarget(true,false,Color.clear);
        buffer.SetGlobalFloat(shadowPancakingId,1f);
        buffer.BeginSample(bufferName);
        ExcuteBuffer();
        int tiles = ShadowedDirectionalLightCount * settings.directional.cascadeCount;
        // 要分割的图块大小和数量
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;
        for (int i = 0; i < ShadowedDirectionalLightCount; i++)
        {
            RenderDirectionalShadows(i,split,tileSize);
        }
        // 将级联数量和包围球数据发送到GPU
        //buffer.SetGlobalInt(cascadeCountId,settings.directional.cascadeCount);
        buffer.SetGlobalVectorArray(cascadeCullingSpheresId,cascadeCullingSpheres);
        
        //级联数据发送GPU
        buffer.SetGlobalVectorArray(cascadeDataId,cascadeData);
        // 阴影转换矩阵传入GPU
        buffer.SetGlobalMatrixArray(dirShadowMatricesId,dirShadowMatrices);
        // 最大阴影距离和阴影过度距离发送到GPU
        //buffer.SetGlobalFloat(shadowDistanceId,settings.maxDistance);
        
        // float f = 1f - settings.directional.cascadeFade;
        // buffer.SetGlobalVector(shadowDistanceFadeId,new Vector4(1f/ settings.maxDistance,1f/settings.distanceFade,1f / (1f - f*f)));
        SetKeywords(directionalFilterKeywords, (int) settings.directional.filterMode - 1);
        SetKeywords(cascadeBlendKeywords,(int)settings.directional.cascadeBlend-1);
        // 传递图集大小和纹素大小
        //buffer.SetGlobalVector(shadowAtlasSized,new Vector4(atlasSize,1f/atlasSize));
        buffer.EndSample(bufferName);
        ExcuteBuffer();
    }

    // 渲染单个光源阴影
    void RenderDirectionalShadows(int index, int split, int tileSize)
    {
        ShadowedDirectionalLight light = ShadowedDirectionalLights[index];
        var shadowSetting = new ShadowDrawingSettings(cullingResults, light.visiableLightIndex){useRenderingLayerMaskTest = true};
        // 得到级联阴影贴图需要的参数
        int cascadeCount = settings.directional.cascadeCount;
        int tileOffset = index * cascadeCount;
        Vector3 ratios = settings.directional.CascadeRatios;
        
        //  在剔除时 unity 相当保守， 但是我们应该通过级联过渡比例降低它，确保过渡区域中的投影不会被剔除。
        float cullingFactor = Mathf.Max(0f, 0.8f - settings.directional.cascadeFade);
        float tileSzale = 1f / split;
        for (int i = 0; i < cascadeCount; i++)
        {
            cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(light.visiableLightIndex, i, cascadeCount,
                ratios, tileSize, light.nearPlaneOffset, out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix,
                out ShadowSplitData splitData);

            // 得到第一个光源的包围球数据
            //  光的方向和球无关， 所以我们所有的方向光都使用相同的包围球
            if (index == 0)
            {
                
                SetCascadeData(index,splitData.cullingSphere,tileSize);
            }
            // 当此值为1时： 如果大的级联中的投影数据能被小的级联数据覆盖，就可以从大的级联中剔除这些投影。
            splitData.shadowCascadeBlendCullingFactor = cullingFactor;
            // splitData 中包含应该如何剔除对象的信息，我们把它复制到阴影设置中
            shadowSetting.splitData = splitData;
            // 调整图块索引，它等于光源的图块偏移加上级联的索引
            int tileIndex = tileOffset + i;
            Vector2 offset = SetTileViewPort(tileIndex, split, tileSize);
            // 投影矩阵乘以视图矩阵， 得到从世界空间转换到灯光空间的转换矩阵
            dirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(projMatrix * viewMatrix, offset, tileSzale);
            // 应用VP 矩阵
            buffer.SetViewProjectionMatrices(viewMatrix, projMatrix);
            // 设置斜度比例偏差值
            buffer.SetGlobalDepthBias(0,light.slopeScaleBias);
            // 绘制阴影
            ExcuteBuffer();
            context.DrawShadows(ref shadowSetting);
            // 绘制完阴影。将全局深度偏差归零
            buffer.SetGlobalDepthBias(0f,0f);
        }
    }

        // 渲染单个光源阴影
        void RenderOtherShadows()
        {
            int atlasSize = (int)settings.other.atlasSize;
            atlasSizes.z = atlasSize;
            atlasSizes.w = 1f / atlasSize;
            buffer.GetTemporaryRT(otherShadowAtlasId,atlasSize,atlasSize,32,FilterMode.Bilinear,RenderTextureFormat.Shadowmap);
            //指定渲染数据存储到渲染纹理 而不是帧缓冲区中
            buffer.SetRenderTarget(otherShadowAtlasId,RenderBufferLoadAction.DontCare,RenderBufferStoreAction.Store);
            //  因为只关心 深度缓冲，所以只需要清除深度
            buffer.ClearRenderTarget(true,false,Color.clear);
            buffer.SetGlobalFloat(shadowPancakingId,0f);
            buffer.BeginSample(bufferName);
            ExcuteBuffer();
            int tiles = ShadowedOtherLightCount;
            // 要分割的图块大小和数量
            int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
            int tileSize = atlasSize / split;
            for (int i = 0; i < ShadowedOtherLightCount;)// i++)
            {
                if (shadowedOtherLights[i].isPoint)
                {
                    RenderPointShadows(i,split,tileSize);
                    i += 6;
                }
                else
                {
                    RenderSpotShadows(i,split,tileSize);
                    i += 1;
                }
                
            }
            // 将级联数量和包围球数据发送到GPU
            //buffer.SetGlobalInt(cascadeCountId,settings.directional.cascadeCount);
            
        
            //级联数据发送GPU
            //buffer.SetGlobalVectorArray(cascadeDataId,cascadeData);
            // 阴影转换矩阵传入GPU
            buffer.SetGlobalMatrixArray(otherShadowMatricesId,otherShadowMatrices);
            buffer.SetGlobalVectorArray(otherShadowTilesId,otherShadowTiles);            // 最大阴影距离和阴影过度距离发送到GPU
            //buffer.SetGlobalFloat(shadowDistanceId,settings.maxDistance);
        
            // float f = 1f - settings.directional.cascadeFade;
            // buffer.SetGlobalVector(shadowDistanceFadeId,new Vector4(1f/ settings.maxDistance,1f/settings.distanceFade,1f / (1f - f*f)));
            SetKeywords(otherFilterKeywords, (int) settings.other.filterMode - 1);
            //SetKeywords(cascadeBlendKeywords,(int)settings.directional.cascadeBlend-1);
            // 传递图集大小和纹素大小
            //buffer.SetGlobalVector(shadowAtlasSized,new Vector4(atlasSize,1f/atlasSize));
            buffer.EndSample(bufferName);
            ExcuteBuffer();
        }


    // 设置级联数据
    void SetCascadeData(int index, Vector4 cullingSphere, float tileSize)
    {
        // 包围球直径除以阴影图块尺寸 = 纹素大小
        float texelSize = 2f * cullingSphere.w / tileSize;
        //
        cullingSphere.w *= cullingSphere.w;
        cascadeCullingSpheres[index] = cullingSphere;
        
        //  cullingSpher.w  是球的半径
        //  1.4142136f = 根号2的结果值  （纹素是正方形，沿着对角线线 根号2 进行缩放）
        cascadeData[index] = new Vector4(1f / cullingSphere.w,texelSize * 1.4142136f);
        
        
        // // 因为我们想要所有的光源 使用相同的级联， 所以只需要第一个方向光的包围球数据即可
        // // cascadeCullingSpheres[i] = splitData.cullingSphere;
        // Vector4 cullingSphere = splitData.cullingSphere;
        // cullingSphere.w *= cullingSphere.w;
        // cascadeCullingSpheres[i] = cullingSphere;
    }

    Vector2 SetTileViewPort(int index,int split,float tileSize)
    {
        // 计算索引图块的位置
        Vector2 offset = new Vector2(index % split,index / split);
        // 设置渲染视口,拆分成多个块
        buffer.SetViewport(new Rect(offset.x * tileSize,offset.y * tileSize,tileSize,tileSize));
        return offset;
    }

    // 返回一个从世界空间到阴影图块空间的转换矩阵
    Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, float scale)
    {
        // 使用反向Zbuffer
        if (SystemInfo.usesReversedZBuffer)
        {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }

        //float scale = 1f / split;
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
        if (ShadowedOtherLightCount > 0)
        {
            buffer.ReleaseTemporaryRT(otherShadowAtlasId);
        }
        ExcuteBuffer();
    }
   
    // PCF滤波模式

    private static int shadowAtlasSized = Shader.PropertyToID("_ShadowAtlasSize");
    
    private static string[] directionalFilterKeywords =
    {
        "_DIRECTIONAL_PCF3",
        "_DIRECTIONAL_PCF5",
        "_DIRECTIONAL_PCF7",
    };
    
    private static string[] otherFilterKeywords =
    {
        "_OTHER_PCF3",
        "_OTHER_PCF5",
        "_OTHER_PCF7",
    };

    // 设置关键字开启那种滤波模式
    void SetKeywords()
    {
        int enableIndex = (int)settings.directional.filterMode - 1;
        for (int i = 0; i < directionalFilterKeywords.Length; i++)
        {
            if (i== enableIndex)
            {
                Debug.LogError("启用  directionalFilterKeywords ="+directionalFilterKeywords[i]);
                buffer.EnableShaderKeyword(directionalFilterKeywords[i]);
            }
            else
            {
                buffer.DisableShaderKeyword(directionalFilterKeywords[i]);
            }
        }
    }

    //   级联的混合模式设置
    private static string[] cascadeBlendKeywords = {"_CASCADE_BLEND_SOFT", "_CASCADE_BLEND_DITHER"};

    void SetKeywords(string[] keywords, int enabledIndex)
    {
        for (int i = 0; i < keywords.Length; i++)
        {
            if (i == enabledIndex)
            {
                buffer.EnableShaderKeyword(keywords[i]);
            }
            else
            {
                buffer.DisableShaderKeyword(keywords[i]);
            }
        }
    }

    ///--------------------------------
    ///----------point he spot 的shadowMask----------------------
    ///--------------------------------
    ///--------------------------------
    
    // spot 和 piont 光源的shadowMask
    public Vector4 ReserveOtherShadows(Light light, int visibleLightIndex)
    {
        if (light.shadows == LightShadows.None || light.shadowStrength <=0f)
        {
            return new Vector4(0f,0f,0f,-1f);
        }

        float maskChannel = -1f;
        
            LightBakingOutput lightBaking = light.bakingOutput;
            if (lightBaking.lightmapBakeType == LightmapBakeType.Mixed && lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask)
            {
                useShadowMask = true;
                maskChannel = lightBaking.occlusionMaskChannel;
            }

            bool isPoint = light.type == LightType.Point;
            int newLightCount = ShadowedOtherLightCount + (isPoint ? 6 : 1);
            // 检查增加的灯光计数是否会超过最大值， 或者是否没有要渲染的阴影 
            if (newLightCount >= maxShadowedOtherLightCount || !cullingResults.GetShadowCasterBounds(visibleLightIndex,out Bounds bounds))
            {
                return new Vector4(-light.shadowStrength,0f,0f,maskChannel);
            }
            shadowedOtherLights[visibleLightIndex] = new ShadowedOtherLight()
            {
                slopeScaleBias = light.shadowBias,
                normalBias = light.shadowNormalBias,
                visibleLightIndex = visibleLightIndex,
                isPoint = isPoint,
            };
            Vector4 data = new Vector4(light.shadowStrength,ShadowedOtherLightCount,isPoint ? 1f : 0f,maskChannel);
            ShadowedOtherLightCount = newLightCount;
            return data;
    }

    void RenderSpotShadows(int index, int split,int tileSize)
    {
        ShadowedOtherLight light = shadowedOtherLights[index];

        var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);
        cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(light.visibleLightIndex, out Matrix4x4 viewMatrix,
            out Matrix4x4 projMatrix, out ShadowSplitData splitData);
        shadowSettings.splitData = splitData;
        
        //
        float texelSize = 2f / (tileSize * projMatrix.m00);
        float filterSize = texelSize * ((float) settings.other.filterMode + 1f);
        float bias = light.normalBias * filterSize * 1.4142136f;
        Vector2 offset = SetTileViewPort(index, split, tileSize);

        float tileScale = 1f / split;
        SetOtherTileData(index, offset,tileScale,bias);
        
        otherShadowMatrices[index] =
            ConvertToAtlasMatrix(projMatrix * viewMatrix, offset, tileScale);
        buffer.SetViewProjectionMatrices(viewMatrix,projMatrix);
        buffer.SetGlobalDepthBias(0f,light.slopeScaleBias);
        ExcuteBuffer();
        context.DrawShadows(ref shadowSettings);
        buffer.SetGlobalDepthBias(0f,0f);
    }

    void RenderPointShadows(int index, int split, float tileSize)
    {
        ShadowedOtherLight light = shadowedOtherLights[index];

        var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);

        float texelSize = 2f / (tileSize);
        float filterSize = texelSize * ((float) settings.other.filterMode + 1f);
        float bias = light.normalBias * filterSize * 1.4142136f;
        float tileScale = 1f / split;

        // 通过增加视野（fov）来减少这些伪影，因此采样绝不超出tile边缘
        float fovBias = Mathf.Atan(1f + bias + filterSize) * Mathf.Rad2Deg * 2f - 90f;
        for (int i = 0; i < 6; i++)
        {
            cullingResults.ComputePointShadowMatricesAndCullingPrimitives(light.visibleLightIndex, (CubemapFace) i,
                fovBias, out Matrix4x4 viewMatrix,
                out Matrix4x4 projMatrix, out ShadowSplitData splitData);

            // 通过对viewMatrix  取反来撤销反转
            viewMatrix.m11 = -viewMatrix.m11;
            viewMatrix.m12 = -viewMatrix.m12;
            viewMatrix.m13 = -viewMatrix.m13;

            shadowSettings.splitData = splitData;
            int tileIndex = index + i;
            //
            // float texelSize = 2f / (tileSize);
            // float filterSize = texelSize * ((float) settings.other.filterMode + 1f);
            // float bias = light.normalBias * filterSize * 1.4142136f;
            // float tileScale = 1f / split;
            Vector2 offset = SetTileViewPort(tileIndex, split, tileSize);


            SetOtherTileData(tileIndex, offset, tileScale, bias);

            otherShadowMatrices[tileIndex] =
                ConvertToAtlasMatrix(projMatrix * viewMatrix, offset, tileScale);
            buffer.SetViewProjectionMatrices(viewMatrix, projMatrix);
            buffer.SetGlobalDepthBias(0f, light.slopeScaleBias);
            ExcuteBuffer();
            context.DrawShadows(ref shadowSettings);
            buffer.SetGlobalDepthBias(0f, 0f);
        }
    }

    void SetOtherTileData(int index,Vector2 offset,float scale,float bias)
    {
        float border = atlasSizes.w * 0.5f;
        Vector4 data;
        data.x = offset.x * scale + border;
        data.y = offset.y * scale + border;
        data.z =  scale - border - border;
        data.w = bias;
        otherShadowTiles[index] = data;
    }

    
}
