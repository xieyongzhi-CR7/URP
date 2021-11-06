// 公共方法库
#ifndef CUSTOM_COMMON_INCLUDE 
#define CUSTOM_COMMON_INCLUDE

#define UNITY_MATRIX_M unity_ObjectToWorld;
#define UNITY_MATRIX_I_M unity_WorldToObject;
#define UNITY_MATRIX_V unity_MatrixV;
#define UNITY_MATRIX_VP unity_MatrixVP;
#define UNITY_MATRIX_P glstate_matrix_projection;
#include "UnityInput.hlsl"

#include "../../../Library/PackageCache/com.unity.render-pipelines.core@10.3.2/ShaderLibrary/Common.hlsl"
#include "../../../Library/PackageCache/com.unity.render-pipelines.core@10.3.2/ShaderLibrary/CommonMaterial.hlsl"
#include "../../../Library/PackageCache/com.unity.render-pipelines.core@10.3.2/ShaderLibrary/UnityInstancing.hlsl"
#include "../../../Library/PackageCache/com.unity.render-pipelines.core@10.3.2/ShaderLibrary/SpaceTransforms.hlsl"

//float4 TransformObjectToWorld(float3 positionOS)
//{
//    return mul(unity_ObjectToWorld,float4(positionOS,1.0));
//}


//float4 TransformWorldToHClip(float3 positionWS)
//{
//    return mul(unity_MatrixVP,float4(positionWS,1.0));
//}


float Square(float v)
{
    return v * v;
}

#endif