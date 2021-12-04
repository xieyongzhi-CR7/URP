// 
#ifndef CUSTOM_SURFACE_INCLUDE 
#define CUSTOM_SURFACE_INCLUDE

struct Surface
{
    // 表面位置(worldPos)
    float3 position;
    float3 normal;
    //
    float3 interpolatednormal;
    float3 color;
    float alpha;
    float metallic;
    float occlusion;
    float smoothness;
    float3 viewDirection;
    //表面深度
    float depth;
    //抖动属性（级联间的混合模式）
    float dither;
    // 菲涅尔强度
    float fresnelStrength;
    uint renderingLayerMask;
};
#endif
