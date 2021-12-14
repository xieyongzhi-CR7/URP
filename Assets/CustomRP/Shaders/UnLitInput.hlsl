// unity  标准输入库
#ifndef CUSTOM_UNLIT_INPUT_INCLUDE 
#define CUSTOM_UNLIT_INPUT_INCLUDE

#include "Fragment.hlsl"


TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);


TEXTURE2D(_DistortionMap);
SAMPLER(sampler_DistortionMap);


UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4,_BaseMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float4,_BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float,_Cutoff)
    UNITY_DEFINE_INSTANCED_PROP(float,_ZWrite)
    UNITY_DEFINE_INSTANCED_PROP(float4,color)
    UNITY_DEFINE_INSTANCED_PROP(float,_NearFadeDistance)
    UNITY_DEFINE_INSTANCED_PROP(float,_NearFadeRange)
    UNITY_DEFINE_INSTANCED_PROP(float,_SoftParticlesDistance)
    UNITY_DEFINE_INSTANCED_PROP(float,_SoftParticlesRange)
    UNITY_DEFINE_INSTANCED_PROP(float,_DistortionStrength)
    UNITY_DEFINE_INSTANCED_PROP(float,_DistortionBlend)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)


float GetFinalAlpha( float alpha)
{
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_ZWrite) ? 1.0 : alpha;
}

float2 TransformBaseUV(float2 baseUV)
{
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_BaseMap_ST);
    return baseUV * baseST.xy + baseST.zw;
}

float GetDistortionBlend()
{
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_DistortionBlend);
}


float2 GetDistortion(float2 baseUV,bool flipbookBlending,float3 flipbookUVB)
{
   
    float4 rawMap = SAMPLE_TEXTURE2D_LOD(_DistortionMap,sampler_DistortionMap,baseUV,0);
    if(flipbookBlending)
    {
        float4 color = SAMPLE_TEXTURE2D_LOD(_DistortionMap,sampler_DistortionMap,flipbookUVB.xy,0);
        rawMap = lerp(rawMap,color,flipbookUVB.z);
    }  
    return DecodeNormal(rawMap,UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_DistortionStrength)).xy;
}


float4 GetBase(float2 baseUV,Fragment fragment,float4 vertexColor = 1.0,float3 uvB = 0.0,bool flipbookBlending = false,bool nearFade = false,bool softPartilces = false)
{
    float4 map = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,baseUV);
    if (flipbookBlending)
    {
        map = lerp(map,SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uvB.xy),uvB.z);
    } 
    if(nearFade)
    {
        float nearFadeDis = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_NearFadeDistance);
        float nearFadeRange = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_NearFadeRange);
        float nearAttenuation = (fragment.depth - nearFadeDis) / nearFadeRange;
        map.a = saturate(nearAttenuation);
    }
    
    if(softPartilces)
    {
        float depthDelta = fragment.bufferDepth - fragment.depth;
        float softPDistance = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_SoftParticlesDistance);
        float softPRange = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_SoftParticlesRange);
        float nearAttenuation = (depthDelta - softPDistance) / softPRange;
        map.a = saturate(nearAttenuation);
    }    
    float4 color = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_BaseColor);
    return map * color * vertexColor;
}


float2 GetCutoff(float2 baseUV)
{
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_Cutoff);
}


float3 GetEmission(float2 baseUV,Fragment fragment)
{
    return GetBase(baseUV,fragment).rgb;
}

float3 GetSmoothness(float2 baseUV,Fragment fragment)
{
    return GetBase(baseUV,fragment).rgb;
}


float GetFresnel(float2 baseUV)
{
    return 0.0;
}

#endif