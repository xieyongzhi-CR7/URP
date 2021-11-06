// unity  标准输入库
#ifndef CUSTOM_UNITY_INPUT_INCLUDE 
#define CUSTOM_UNITY_INPUT_INCLUDE
#include "../../../Library/PackageCache/com.unity.render-pipelines.core@10.3.2/ShaderLibrary/Common.hlsl"

CBUFFER_START(UnityPerDraw)
//定义一个从模型空间 转换到 世界空间的转换矩阵
float4x4 unity_ObjectToWorld;

float4x4 unity_WorldToObject;
real4 unity_WorldTransformParams;
float4 unity_LODFade;
float3 _WorldSpaceCameraPos;
CBUFFER_END


CBUFFER_START(UnityPerFrame)
// 定义一个 从世界转换到裁剪空间的矩阵
float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4x4 glstate_matrix_projection;
CBUFFER_END
#endif