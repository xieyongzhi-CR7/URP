// 计算光照相关库 
#ifndef CUSTOM_LIGHT_INCLUDE 
#define CUSTOM_LIGHT_INCLUDE

#define MAX_DIRECTIONAL_LIGHT_COUNT 4
//  用于接受CPU 传送的灯光数据
CBUFFER_START(_CustomLight)
    //float3 _DirectionalLightColor;
    //float3 _DirectionalLightDirection;
    int _DirectionalLightCount;
    float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
    // 阴影数据
    float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

int GetDirectionalLightCount()
{
    return _DirectionalLightCount;
}



struct Light 
{
    float3 color;
    float3 direction;
    float attenuation;
};


DirectionalShadowData GetDirectionalShadowData(int lightIndex)
{
    DirectionalShadowData data;
    data.strength = _DirectionalLightShadowData[lightIndex].x;
    data.tileIndex = _DirectionalLightShadowData[lightIndex].y;
    return data;
}


Light GetDirectionalLight(int index,Surface surfaceWS)
{
    Light light;
    light.color = _DirectionalLightColors[index].rgb;
    light.direction = _DirectionalLightDirections[index].xyz;
    DirectionalShadowData shadowData = GetDirectionalShadowData(index);
    light.attenuation = GetDirectionalShadowAttenuation(shadowData,surfaceWS);
    return light;
}
#endif
