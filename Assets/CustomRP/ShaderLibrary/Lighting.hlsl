// 计算光照相关库 
#ifndef CUSTOM_LIGHTING_INCLUDE 
#define CUSTOM_LIGHTING_INCLUDE
#include "../ShaderLibrary/Light.hlsl"
#include "../ShaderLibrary/BRDF.hlsl"


float3 IncomingLight(Surface surface,Light light)
{
    return saturate(dot(surface.normal,light.direction) * light.attenuation) * light.color;
}

float3 GetLighting(Surface surface,BRDF brdf,Light light)
{
    return IncomingLight(surface,light) * DirectBRDF(surface,brdf,light);
}

float3 GetLighting(Surface surfaceWS,BRDF brdf,GI gi)
{
    // shadowData.strength  :  这个强度是  根据 positionWS  在  阴影最大距离 中的比例  算出的 宏观的阴影的强度 ，(不是从shadowMask 中采样出的结果，这是第一步 阴影插值的开始)
    // 
    ShadowData shadowData = GetShadowData(surfaceWS);
    shadowData.shadowMask = gi.shadowMask;
    //return  gi.diffuse;//shadowData.shadowMask.shadows.rgb;
    //float3 color = gi.diffuse * brdf.diffuse;
    float3 color = InDirectBRDF(surfaceWS, brdf,gi.diffuse,gi.specular);

    //float3 color = 0.0;//gi.diffuse * brdf.diffuse;
    //float3 color = gi.diffuse;
     
    for(int i= 0; i < GetDirectionalLightCount();i++)
    {
        Light light = GetDirectionalLight(i,surfaceWS,shadowData); 
        color += GetLighting(surfaceWS,brdf,light);
    }
    
    for(int j = 0; j < GetOtherLightCount(); j++)
    {
        Light light = GetOtherLight(j,surfaceWS,shadowData);
        color += GetLighting(surfaceWS,brdf,light);
    }
    
    return color;
}

#endif