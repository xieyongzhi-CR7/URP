// 计算光照相关库 
#ifndef CUSTOM_SHADOWS_INCLUDE 
#define CUSTOM_SHADOWS_INCLUDE

#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4
// 阴影图集
TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);



//  用于接受CPU 传送的灯光数据
CBUFFER_START(_CustomShadows)
// 阴影转换矩阵
float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT];    
CBUFFER_END




struct DirectionalShadowData
{
    float strength;
    int tileIndex;
};


// 采样阴影图集
float SampleDirectionalShadowAtlas(float3 positionSTS)
{
    return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas,SHADOW_SAMPLER,positionSTS);
}



//  计算阴影衰减
float GetDirectionalShadowAttenuation(DirectionalShadowData data, Surface surfaceWS)
{
    if( data.strength <= 0.0)
    {
        return 1.0;
    }
    // 通过阴影转换矩阵和表面位置 得到在阴影纹理（图块）空间的位置，然后对图集进行采样
    float3 positionSTS = mul(_DirectionalShadowMatrices[data.tileIndex],float4(surfaceWS.position,1.0)).xyz;
    float shadow = SampleDirectionalShadowAtlas(positionSTS);
        // 最终的阴影衰减值是阴影强度和衰减因子的插值
    return lerp(1.0,shadow,data.strength);
}






#endif
