#ifndef CUSTOM_LIT_PASS_INCLUDE
#define CUSTOM_LIT_PASS_INCLUDE



#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/Shadows.hlsl"
#include "../ShaderLibrary/Light.hlsl"
#include "../ShaderLibrary/BRDF.hlsl"
#include "../ShaderLibrary/GI.hlsl"
#include "../ShaderLibrary/Lighting.hlsl"

struct Attributes
{
    float3 positionOS : POSITION;
    float2 baseUV : Texcoord0;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT; 
    //lightmap 的uv 坐标
    GI_ATTRIBUTE_DATA   
    // 通过顶点数据 提供该渲染对象的索引
    UNITY_VERTEX_INPUT_INSTANCE_ID
};


struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : VAR_POSITION;
    float2 baseUV : VAR_BASE_UV;
    float2 detailUV : VAR_DETAIL_UV;
    float3 normalWS : VAR_NORMAL;
    #if defined(_NORMAL_MAP)
        float4 tangentWS : VAR_TANGENT;
    #endif
    GI_VARYINGS_DATA
    UNITY_VERTEX_INPUT_INSTANCE_ID
};


Varyings LitPassVertex(Attributes input)
{
    Varyings output;   
    // 提取渲染对象的索引，并且存储到其它实例宏 所依赖的全局静态变量中
    UNITY_SETUP_INSTANCE_ID(input);
    
    // 将索引 转换存储，在片元中还要适用
    UNITY_TRANSFER_INSTANCE_ID(input,output);
    
    //
    TRANSFER_GI_DATA(input,output);
    
    
    output.positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(output.positionWS);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    #if defined(_NORMAL_MAP)
        output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz),input.tangentOS.w);
    #endif
    output.baseUV = TransformBaseUV(input.baseUV);
    #if defined(_DETAIL_MAP)
        output.detailUV = TransformDetailUV(input.baseUV);
    #endif    
    return output;
}

//CBUFFER_START(UnityPerMaterial) 
    //float4 _BaseColor;
//CBUFFER_END


void ClipLOD(float4 positionCS,float fade)
{
    #if defined(LOD_FADE_CROSSFADE)
        // 从y方向 垂直渐变开始。每32像素重复一次，那么就会产生交替的水平条纹
        //float dither = (positionCS.y % 32) / 32;
        
        float dither = InterleavedGradientNoise(positionCS.xy,0);
        clip(fade + (fade>0 ? -dither : dither));
    #endif  

}



float4 LitPassFragment(Varyings input): SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    //  unity_LODFade.x  的值 是随着视距的变化而变化的
    // 对于前一LOD 的物体  fade>0
    // 对于后一lod的物体   fade < 0
    #if defined(LOD_FADE_CROSSFADE)
    //    return unity_LODFade.x;
    #endif
    
    ClipLOD(input.positionCS,unity_LODFade.x);
    
    InputConfig inputConfig = GetInputConfig(input.baseUV);
    #if defined(_MASK_MAP)
        inputConfig.useMask = true;
    #endif
    #if defined(_DETAIL_MAP)
        inputConfig.useDetail = true;
        inputConfig.detailUV = input.detailUV;
    #endif
    //float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_BaseColor);
    //float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.baseUV);
    float4 base = GetBase(inputConfig);    
 #if defined(_CLIPPING)
    clip(base.a - GetCutoff(inputConfig));
 #endif
    Surface surface;
    surface.position = input.positionWS;
    #if defined(_NORMAL_MAP)
        surface.normal = NormalTangentToWorld(GetNormalTS(inputConfig), input.normalWS,input.tangentWS);
    #else
        surface.normal = normalize(input.normalWS);
    #endif
    surface.color = base.rgb;
    surface.alpha = base.a;
    surface.metallic = GetMetallic(inputConfig);
    surface.fresnelStrength = GetFresnel(inputConfig);
    surface.smoothness = GetSmoothness(inputConfig);
    surface.occlusion = GetOcclusion(inputConfig);
    surface.interpolatednormal = input.normalWS;
    surface.viewDirection = normalize( _WorldSpaceCameraPos - input.positionWS);
    //获取表面深度
    surface.depth = - TransformWorldToView(input.positionWS).z;
    // 计算抖动值
    surface.dither = InterleavedGradientNoise(input.positionCS.xy,0);
    
    surface.renderingLayerMask = asuint(unity_RenderingLayer.x);
    
 #if defined(_PREMULTIPLY_ALPHA)
    BRDF brdf = GetBRDF(surface,true);
 #else
    BRDF brdf = GetBRDF(surface);
 #endif
    GI gi = GetGI(GI_FRAGMENT_DATA(input),surface,brdf);
    float3 color = GetLighting(surface,brdf,gi);
    // 加上自发光
    color += GetEmission(inputConfig);
    
    return float4(color,GetFinalAlpha(surface.alpha));
}

#endif