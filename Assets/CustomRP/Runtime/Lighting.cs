using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Lighting
{
    private const string bufferName = "Lighting";
    CommandBuffer buffer = new CommandBuffer()
        {name = bufferName};

    // 限制最大可见的平行光数量为4
    private const int maxDirLightCount = 4;
    private CullingResults cullingResults;
    // private static int dirLightColorId = Shader.PropertyToID("_DirectionalLightColor");
    // private static int dirLightDirectionId = Shader.PropertyToID("_DirectionalLightDirection");

    private static int dirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
    private static int dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
    private static int dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");


    private static int dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");
    // 存储阴影数据
    static Vector4[] dirLightShadowData = new Vector4[maxDirLightCount];
    
    //
    static Vector4[] direLightColors = new Vector4[maxDirLightCount];
    static Vector4[] direLightDirections = new Vector4[maxDirLightCount];


    #region 点光源和聚光灯
    // 限制最大非dir光的数量（只是针对单帧的限制，不是针对整个场景）
    private const int maxOtherLightCount = 64;
    private static int otherLightCountId = Shader.PropertyToID("_OtherLightCount");
    private static int otherLightColorsId = Shader.PropertyToID("_OtherLightColors");
    private static int otherLightPositionsId = Shader.PropertyToID("_OtherLightPositions");


    private static int OtherLightShadowDataId = Shader.PropertyToID("_OtherLightShadowData");
    
    
    // 聚光灯
    private static int otherLightDirectionsId = Shader.PropertyToID("_OtherLightDirections");
    private static int  otherLightSpotAnglesId = Shader.PropertyToID("_OtherLightSpotAngles");
    static Vector4[] otherLightColors = new Vector4[maxOtherLightCount];
    static Vector4[] otherLightPositions = new Vector4[maxOtherLightCount];
    // 聚光灯
    static Vector4[] otherLightDirections = new Vector4[maxOtherLightCount];
    static Vector4[] otherLightSpotAngles = new Vector4[maxOtherLightCount];
    static Vector4[] otherLightShadowData = new Vector4[maxOtherLightCount];
    #endregion
    
    
    
    Shadows shadows = new Shadows();
    public void Setup(ScriptableRenderContext context,CullingResults cullingResults,ShadowSettings shadowSettings)
    {
        this.cullingResults = cullingResults;
        //buffer.BeginSample(bufferName);
        // 传递阴影数据
        shadows.Setup(context,cullingResults,shadowSettings);
        
        // 在传递光的数据的时候，把可见光源 传递给shadows，为shadowMap做准备
        SetupLights();
        // 根据上一步中的光源信息，画shadowMap
        shadows.Render();
        context.ExecuteCommandBuffer(buffer);
        //buffer.EndSample(bufferName);
    }

    void SetupLights()
    {
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
        int dirLightCount = 0,otherLightCount = 0;
        for (int i = 0; i < visibleLights.Length; i++)
        {
            VisibleLight visibleLight = visibleLights[i];
            switch (visibleLight.lightType)
            {
                case LightType.Directional:
                    if (dirLightCount < maxDirLightCount)
                    {
                        // VisibleLight 结构很大，这里使用引用传递 而不是值传递，这样不会生成副本
                        SetupDirectionLight(dirLightCount++,ref visibleLight);
                    }
                    break;
                case LightType.Point:
                    if (otherLightCount <maxOtherLightCount)
                    {
                        SetupPointLight(otherLightCount++,ref visibleLight);
                    }
                    break;
                case LightType.Spot:
                    if (otherLightCount < maxOtherLightCount)
                    {
                        //Debug.LogError("spot otherLightCount="+otherLightCount);
                        SetupSpotLight(otherLightCount++,ref visibleLight);
                    }
                    break;
            }
        }
        
        buffer.SetGlobalInt(dirLightCountId,dirLightCount);
        if (dirLightCount > 0)
        {
            buffer.SetGlobalVectorArray(dirLightColorsId,direLightColors);
            buffer.SetGlobalVectorArray(dirLightDirectionsId,direLightDirections);
            buffer.SetGlobalVectorArray(dirLightShadowDataId,dirLightShadowData);    
        }

        buffer.SetGlobalInt(otherLightCountId,otherLightCount);
        if (otherLightCount > 0)
        {
            buffer.SetGlobalVectorArray(otherLightColorsId,otherLightColors);
            buffer.SetGlobalVectorArray(otherLightPositionsId,otherLightPositions);
            buffer.SetGlobalVectorArray(otherLightDirectionsId,otherLightDirections);    
            buffer.SetGlobalVectorArray(otherLightSpotAnglesId,otherLightSpotAngles);
            buffer.SetGlobalVectorArray(OtherLightShadowDataId,otherLightShadowData);
        }
        
    }
    void SetupDirectionLight(int index,ref VisibleLight visibleLight)
    {
        direLightColors[index] = visibleLight.finalColor;
        direLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        // buffer.SetGlobalVector(dirLightColorId,light.color.linear * light.intensity);
        // buffer.SetGlobalVector(dirLightDirectionId, - light.transform.forward);
        //shadows.ReserveDirectionalShadows(visibleLight.light,index);
        dirLightShadowData[index] = shadows.ReserveDirectionalShadows(visibleLight.light, index);
    }

    
    // 点光源的处理
    void SetupPointLight(int index,ref VisibleLight visibleLight)
    {
        otherLightColors[index] = visibleLight.finalColor;
        Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
        position.w = 1f / Mathf.Max(visibleLight.range, visibleLight.range, 0.00001f);
        otherLightPositions[index] = position;
        // 为了确保点光源不受聚光灯角度衰减的影响， 设置点光源数据时 将聚光灯角度设置为 0 和 1
        otherLightSpotAngles[index] = new Vector4(0f,1f);
        
        otherLightShadowData[index] = shadows.ReserveOtherShadows(visibleLight.light, index);
    }

    void SetupSpotLight(int index,ref VisibleLight visibleLight)
    {
        otherLightColors[index] = visibleLight.finalColor;
        Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
        position.w = 1f / Mathf.Max(visibleLight.range, visibleLight.range, 0.00001f);
        otherLightPositions[index] = position;
        // 本地到世界的转换矩阵的第三列 在求反得到光照方向
        otherLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        
        Light light = visibleLight.light;
        float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
        float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);
        float angleRangeInv = 1f / Mathf.Max(innerCos - outerCos, 0.001f);
        otherLightSpotAngles[index] = new Vector4(angleRangeInv,-outerCos * angleRangeInv);

        otherLightShadowData[index] = shadows.ReserveOtherShadows(light, index);
    }
    
    public void Cleanup()
    {
        shadows.Cleanup();
    }
    
    
}
