// 
#ifndef CUSTOM_SURFACE_INCLUDE 
#define CUSTOM_SURFACE_INCLUDE

struct Surface
{
    // 表面位置
    float3 position;
    float3 normal;
    float3 color;
    float alpha;
    float metallic;
    float smoothness;
    float3 viewDirection;
};
#endif
