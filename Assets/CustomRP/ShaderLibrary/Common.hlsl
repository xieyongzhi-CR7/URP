// 公共方法库
#ifndef CUSTOM_COMMON_INCLUDE 
#define CUSTOM_COMMON_INCLUDE

#define UNITY_MATRIX_M unity_ObjectToWorld;
#define UNITY_MATRIX_I_M unity_WorldToObject;
#define UNITY_MATRIX_V unity_MatrixV;
#define UNITY_MATRIX_VP unity_MatrixVP;
#define UNITY_MATRIX_P glstate_matrix_projection;
#include "UnityInput.hlsl"

#include "../../../Library/PackageCache/com.unity.render-pipelines.core@10.7.0/ShaderLibrary/Common.hlsl"
#include "../../../Library/PackageCache/com.unity.render-pipelines.core@10.7.0/ShaderLibrary/CommonMaterial.hlsl"
#if defined(_SHADOW_MASK_DISTANCE) || defined(_SHAODW_MASK_ALWAYS)
    #define SHADOW_SHADOWMASK
#endif
#include "../../../Library/PackageCache/com.unity.render-pipelines.core@10.7.0/ShaderLibrary/UnityInstancing.hlsl"
#include "../../../Library/PackageCache/com.unity.render-pipelines.core@10.7.0/ShaderLibrary/SpaceTransforms.hlsl"
#include "../../../Library/PackageCache/com.unity.render-pipelines.core@10.7.0/ShaderLibrary/Packing.hlsl"

// 根据unity_OrthoParams。w  分量是0 或 1  来判断是否使用的是正交相机
bool IsOrthographicCamera()
{
    return unity_OrthoParams.w;
}


// 是否使用了 反向深度缓冲区（反向深度缓冲，存储的深度的精度更高）， 需要进行还原
float OrthographicDepthBufferToLinear(float rawDepth)
{
    #if UNITY_REVERSED_Z 
        rawDepth = 1.0 - rawDepth;
    #endif
    return (_ProjectionParams.z - _ProjectionParams.y) * rawDepth + _ProjectionParams.y;
}

SAMPLER(sampler_linear_clamp);
SAMPLER(sampler_point_clamp);
SAMPLER(sampler_CameraColorTexture);

#include "Fragment.hlsl"




//float4 TransformObjectToWorld(float3 positionOS)
//{
//    return mul(unity_ObjectToWorld,float4(positionOS,1.0));
//}


//float4 TransformWorldToHClip(float3 positionWS)
//{
//    return mul(unity_MatrixVP,float4(positionWS,1.0));
//}


float3 DecodeNormal(float4 sample,float scale)
{
    #if defined(UNITY_NO_DXT5nm)
        return UnpackNormalRGB(sample,scale);
    #else
        return UnpackNormalmapRGorAG(sample,scale);
    #endif
}


float3 NormalTangentToWorld(float3 normalTS,float3 normalWS,float4 tangentWS)
{
    float3x3 tangentToWorld = CreateTangentToWorld(normalWS,tangentWS.xyz,tangentWS.w);
    return TransformTangentToWorld(normalTS,tangentToWorld);
}

//void ClipLOD(Fragment fragment, float fade)
//{
   // #if defined (LOD_FADE_CROSSFADE)
   //     float dither = InterleavedGradientNoise(fragment.positionSS,0);
    //    clip(fade + (fade < 0.0 ? dither : -dither));
   // #endif
//}


void ClipLOD(Fragment fragment,float fade)
{
    #if defined(LOD_FADE_CROSSFADE)
        // 从y方向 垂直渐变开始。每32像素重复一次，那么就会产生交替的水平条纹
        //float dither = (positionCS.y % 32) / 32;
        
        float dither = InterleavedGradientNoise(fragment.positionSS,0);
        clip(fade + (fade>0 ? -dither : dither));
    #endif  

}



float Square1(float v)
{
    return v * v;
}

// 计算两点间距离的平方
float DistanceSquared(float3 pA, float3 pB)
{
    return dot(pA-pB,pA - pB);
}
#endif