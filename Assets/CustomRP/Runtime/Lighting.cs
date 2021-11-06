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
        int dirLightCount = 0;
        for (int i = 0; i < visibleLights.Length; i++)
        {
            VisibleLight visibleLight = visibleLights[i];
            if (visibleLight.lightType == LightType.Directional)
            {
                // VisibleLight 结构很大，这里使用引用传递 而不是值传递，这样不会生成副本
                SetupDirectionLight(dirLightCount++,ref visibleLight);
                if (dirLightCount >= dirLightCount)
                {
                    break;
                }
            }
        }
        buffer.SetGlobalInt(dirLightCountId,dirLightCount);
        buffer.SetGlobalVectorArray(dirLightColorsId,direLightColors);
        buffer.SetGlobalVectorArray(dirLightDirectionsId,direLightDirections);
        buffer.SetGlobalVectorArray(dirLightShadowDataId,dirLightShadowData);
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

    public void Cleanup()
    {
        shadows.Cleanup();
    }
    
    
}
