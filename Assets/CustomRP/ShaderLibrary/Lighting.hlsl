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
    ShadowData shadowData = GetShadowData(surfaceWS);
    shadowData.shadowMask = gi.shadowMask;
    //return  gi.diffuse;//shadowData.shadowMask.shadows.rgb;
    float3 color = gi.diffuse * brdf.diffuse;
    //float3 color = 0.0;//gi.diffuse * brdf.diffuse;
    //float3 color = gi.diffuse;
     
    for(int i= 0; i < GetDirectionalLightCount();i++)
    {
        Light light = GetDirectionalLight(i,surfaceWS,shadowData); 
        color += GetLighting(surfaceWS,brdf,light);
    }
    return color;
}

#endif