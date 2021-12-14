
// unity  标准输入库
#ifndef CUSTOM_CAMERA_RENDERER_PASS_INCLUDE 
#define CUSTOM_CAMERA_RENDERER_PASS_INCLUDE
#include "../../../Library/PackageCache/com.unity.render-pipelines.core@10.7.0/ShaderLibrary/Filtering.hlsl"
#include "../../../Library/PackageCache/com.unity.render-pipelines.core@10.7.0/ShaderLibrary/Color.hlsl"
#include "../../../Library/PackageCache/com.unity.render-pipelines.core@10.7.0/ShaderLibrary/ACES.hlsl"
TEXTURE2D(_SourceTexture);

    
    
struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 screenUV : VAR_SCREEN_UV;
};

Varyings DefaultPassVertex(uint vertexID : SV_vertexID)
{
    Varyings output;
    output.positionCS = float4(vertexID <= 1 ? -1.0 : 3.0,vertexID == 1 ? 3.0 : -1.0,0.0,1.0);
    output.screenUV = float2(vertexID <= 1 ? 0.0 : 2.0, vertexID == 1 ? 2.0 : 0.0);
    // y 值上下反转
    if(_ProjectionParams.x < 0.0)
    {
        output.screenUV.y = 1.0 - output.screenUV.y;
    }
    return output;
}


float4 CopyPassFragment(Varyings input) : SV_TARGET
{
    return SAMPLE_TEXTURE2D(_SourceTexture,sampler_linear_clamp,input.screenUV);
}



float CopyDepthPassFragment(Varyings input) : SV_DEPTH
{
    return SAMPLE_DEPTH_TEXTURE_LOD(_SourceTexture,sampler_point_clamp,input.screenUV,0);
}

#endif