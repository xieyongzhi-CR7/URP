// 计算光照相关库 
#ifndef CUSTOM_SHADOW_CASTER_PASS_INCLUDE 
#define CUSTOM_SHADOW_CASTER_PASS_INCLUDE

#include "../ShaderLibrary/Common.hlsl"



TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);


UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)

UNITY_DEFINE_INSTANCED_PROP(float,_Metallic)
UNITY_DEFINE_INSTANCED_PROP(float,_Smoothness)
// 提供纹理缩放
UNITY_DEFINE_INSTANCED_PROP(float4,_BaseMap_ST)
UNITY_DEFINE_INSTANCED_PROP(float4,_BaseColor)
UNITY_DEFINE_INSTANCED_PROP(float,_Cutoff)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)



struct Attributes
{
    float3 positionOS : POSITION;
    float2 baseUV : Texcoord0;
    
    // 通过顶点数据 提供该渲染对象的索引
    UNITY_VERTEX_INPUT_INSTANCE_ID
};


struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 baseUV : VAR_BASE_UV;    
    UNITY_VERTEX_INPUT_INSTANCE_ID
};



Varyings ShadowCasterPassVertex(Attributes input)
{
    Varyings output;   
    // 提取渲染对象的索引，并且存储到其它实例宏 所依赖的全局静态变量中
    UNITY_SETUP_INSTANCE_ID(input);
    
    // 将索引 转换存储，在片元中还要适用
    UNITY_TRANSFER_INSTANCE_ID(input,output);
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(positionWS);
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_BaseMap_ST);
    output.baseUV = input.baseUV * baseST.xy + baseST.zw;
    return output;
}

//CBUFFER_START(UnityPerMaterial) 
    //float4 _BaseColor;
//CBUFFER_END





void ShadowCasterPassFragment(Varyings input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.baseUV);
    float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_BaseColor);
    float4 base = baseColor * baseMap;    
 #if defined(_CLIPPING)
    // 透明度低于阈值 舍弃
    clip(base.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_Cutoff));
 #endif
}

#endif
