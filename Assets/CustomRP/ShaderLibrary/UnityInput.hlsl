// unity  标准输入库
#ifndef CUSTOM_UNITY_INPUT_INCLUDE 
#define CUSTOM_UNITY_INPUT_INCLUDE
#include "../../../Library/PackageCache/com.unity.render-pipelines.core@10.7.0/ShaderLibrary/Common.hlsl"



// 所有unityPerDraw 的属性，都可以在GPU Instancing中使用
CBUFFER_START(UnityPerDraw)
    //定义一个从模型空间 转换到 世界空间的转换矩阵
    float4x4 unity_ObjectToWorld;
    
    float4x4 unity_WorldToObject;
    real4 unity_WorldTransformParams;
    
    
    
    
    // x 分量存储的是过渡因子
    // y 分量存储的是相同的过渡因子 只不过它被量化为16步
    float4 unity_LODFade;
    float4 _WorldSpaceCameraPos;
    
    float4 unity_ProbesOcclusion;
    float4 unity_SpecCube0_HDR;
    float4 unity_LightmapST;
    float4 unity_DynamicLightmapST;

//  lightProber参数（球谐参数）
    float4 unity_SHAr;
    float4 unity_SHAg;
    float4 unity_SHAb;
    float4 unity_SHBr;
    float4 unity_SHBg;
    float4 unity_SHBb;
    float4 unity_SHC;
    
//  LightProbeVolume (lppvs)
    float4 unity_ProbeVolumeParams;
    float4x4 unity_ProbeVolumeWorldToObject;
    float4 unity_ProbeVolumeSizeInv;
    float4 unity_ProbeVolumeMin;    
    
    // 用于反转 y坐标
    float4 _ProjectionParams;
CBUFFER_END


CBUFFER_START(UnityPerFrame)
// 定义一个 从世界转换到裁剪空间的矩阵
float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4x4 glstate_matrix_projection;
CBUFFER_END
#endif