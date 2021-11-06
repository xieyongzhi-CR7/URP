#ifndef CUSTOM_LIT_PASS_INCLUDE
#define CUSTOM_LIT_PASS_INCLUDE

#include "../ShaderLibrary/Common.hlsl"
#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/Shadows.hlsl"
#include "../ShaderLibrary/Lighting.hlsl"
#include "../ShaderLibrary/Light.hlsl"
#include "../ShaderLibrary/BRDF.hlsl"

struct Attributes
{
    float3 positionOS : POSITION;
    float2 baseUV : Texcoord0;
    float3 normalOS : NORMAL;
    
    // 通过顶点数据 提供该渲染对象的索引
    UNITY_VERTEX_INPUT_INSTANCE_ID
};


struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : VAR_POSITION;
    float2 baseUV : VAR_BASE_UV;
    float3 normalWS : VAR_NORMAL;
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

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

Varyings LitPassVertex(Attributes input)
{
    Varyings output;   
    // 提取渲染对象的索引，并且存储到其它实例宏 所依赖的全局静态变量中
    UNITY_SETUP_INSTANCE_ID(input);
    
    // 将索引 转换存储，在片元中还要适用
    UNITY_TRANSFER_INSTANCE_ID(input,output);
    output.positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(output.positionWS);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_BaseMap_ST);
    output.baseUV = input.baseUV * baseST.xy + baseST.zw;
    return output;
}

//CBUFFER_START(UnityPerMaterial) 
    //float4 _BaseColor;
//CBUFFER_END





float4 LitPassFragment(Varyings input): SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_BaseColor);
    float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.baseUV);
    float4 base = baseColor * baseMap;    
 #if defined(_CLIPPING)
    clip(base.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_Cutoff));
 #endif
    Surface surface;
    surface.position = input.positionWS;
    surface.normal = normalize(input.normalWS);
    surface.color = base.rgb;
    surface.alpha = base.a;
    surface.metallic = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_Metallic);
    surface.smoothness = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_Smoothness);
    surface.viewDirection = normalize( _WorldSpaceCameraPos - input.positionWS);
    
 #if defined(_PREMULTIPLY_ALPHA)
    BRDF brdf = GetBRDF(surface,true);
 #else
    BRDF brdf = GetBRDF(surface);
 #endif
    float3 color = GetLighting(surface,brdf);
    
    return float4(color,surface.alpha);
}

#endif