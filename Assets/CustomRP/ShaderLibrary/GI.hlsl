// 计算光照相关库 
#ifndef CUSTOM_GI_INCLUDE 
#define CUSTOM_GI_INCLUDE


#include "../../../Library/PackageCache/com.unity.render-pipelines.core@10.3.2/ShaderLibrary/EntityLighting.hlsl"




#if defined(LIGHTMAP_ON)
    #define GI_ATTRIBUTE_DATA float2 lightMapUV : TEXCOORD1;
    #define GI_VARYINGS_DATA float2 lightMapUV : VAR_LIGHT_MAP_UV;
    //  如果每个宏的末尾 都有 反斜杠， 则可以将宏定义分成多行。
    #define TRANSFER_GI_DATA(input,output) \
            output.lightMapUV = input.lightMapUV * \
            unity_LightmapST.xy + unity_LightmapST.zw;
    #define GI_FRAGMENT_DATA(input) input.lightMapUV
#else
    #define GI_ATTRIBUTE_DATA
    #define GI_VARYINGS_DATA
    #define TRANSFER_GI_DATA(input,output)
    #define GI_FRAGMENT_DATA(input) 0.0
#endif

TEXTURE2D(unity_Lightmap);
SAMPLER(samplerunity_Lightmap);

TEXTURE2D(unity_ShadowMask);
SAMPLER(samplerunity_ShadowMask);

TEXTURE3D_FLOAT(unity_ProbeVolumeSH);
SAMPLER(samplerunity_ProbeVolumeSH);

// GI 只提供diffuse(即简介光漫反射);  包含lightProbe（动态）和lightMap（静态）提供;  
struct GI 
{
    float3 diffuse;
    
    ShadowMask shadowMask;
};

// 采样烘焙LightMap光照
float3 SampleLightMap(float2 lightMapUV)
{
    #if defined(LIGHTMAP_ON)
        return SampleSingleLightmap(TEXTURE2D_ARGS(unity_Lightmap,samplerunity_Lightmap),lightMapUV,float4(1.0,1.0,0.0,0.0),
        #if defined(UNITY_LIGHTMAP_FULL_HDR) 
            false,
        #else
            true,
        #endif
        float4(LIGHTMAP_HDR_MULTIPLIER,LIGHTMAP_HDR_EXPONENT,0.0,0.0)
           );
    #else
        return 0.0;
    #endif
}

// 采样LightProbe球谐函数
float3 SampleLightProbe(Surface surfaceWS)
{
    #if defined(LIGHTMAP_ON)
        return 0.0;
    #else
        if(unity_ProbeVolumeParams.x)
        {
            return SampleProbeVolumeSH4(TEXTURE3D_ARGS(unity_ProbeVolumeSH,samplerunity_ProbeVolumeSH),surfaceWS.position,surfaceWS.normal,unity_ProbeVolumeWorldToObject,unity_ProbeVolumeParams.y,unity_ProbeVolumeParams.z,unity_ProbeVolumeMin.xyz,unity_ProbeVolumeSizeInv.xyz);
        }
        else
        {
            float4 coefficients[7];
            coefficients[0] = unity_SHAr;
            coefficients[1] = unity_SHAg;
            coefficients[2] = unity_SHAb;
            coefficients[3] = unity_SHBr;
            coefficients[4] = unity_SHBg;
            coefficients[5] = unity_SHBb;
            coefficients[6] = unity_SHC;
            return max(0.0,SampleSH9(coefficients,surfaceWS.normal));
        }
    #endif

}

// 采样烘焙的阴影衰减
float4 SampleBackedShadows(float2 lightMapUV,Surface surfaceWS)
{
    // 静态物体使用lightMap
    #if defined(LIGHTMAP_ON)
        return SAMPLE_TEXTURE2D(unity_ShadowMask,samplerunity_ShadowMask,lightMapUV);
    #else
        
        if(unity_ProbeVolumeParams.x)
        {
            // 动态物体使用 lppv
            return SampleProbeOcclusion(
            TEXTURE2D_ARGS(unity_ProbeVolumeSH,samplerunity_ProbeVolumeSH),surfaceWS.position,unity_ProbeVolumeWorldToObject,
            unity_ProbeVolumeParams.y,unity_ProbeVolumeParams.z,unity_ProbeVolumeMin.xyz,unity_ProbeVolumeSizeInv.xyz
            );
        }
        else
        { 
        // 动态物体使用 probesOcclusion  遮罩探针
            return unity_ProbesOcclusion;    
        }           
    #endif


}



GI GetGI(float2 lightMapUV,Surface surfaceWS)
{
    GI gi;
    //gi.diffuse = SampleLightMap(lightMapUV);
    //gi.diffuse = SampleLightProbe(surfaceWS);
    gi.diffuse = SampleLightMap(lightMapUV) + SampleLightProbe(surfaceWS);
    gi.shadowMask.always = false;
    gi.shadowMask.distance = false;
    gi.shadowMask.shadows = 1.0;
    #if defined(_SHADOW_MASK_DISTANCE)
        gi.shadowMask.distance = true;
        gi.shadowMask.shadows = SampleBackedShadows(lightMapUV,surfaceWS);
    #elif defined( _SHADOW_MASK_ALWAYS)
        gi.shadowMask.always = true;
        gi.shadowMask.shadows = SampleBackedShadows(lightMapUV,surfaceWS);
    #endif    
    return gi;
}

#endif
