// 计算光照相关库 
#ifndef CUSTOM_LIGHT_INCLUDE 
#define CUSTOM_LIGHT_INCLUDE

#define MAX_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_OTHER_LIGHT_COUNT 64
//  用于接受CPU 传送的灯光数据
CBUFFER_START(_CustomLight)
    //float3 _DirectionalLightColor;
    //float3 _DirectionalLightDirection;
    int _DirectionalLightCount;
    float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
    // 阴影数据
    float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];
    
    
    int _OtherLightCount;
    float4 _OtherLightColors[MAX_OTHER_LIGHT_COUNT];
    float4 _OtherLightPositions[MAX_OTHER_LIGHT_COUNT];
    float4 _OtherLightDirections[MAX_OTHER_LIGHT_COUNT];
    float4 _OtherLightSpotAngles[MAX_OTHER_LIGHT_COUNT];
    float4 _OtherLightShadowData[MAX_OTHER_LIGHT_COUNT];    
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


// 获取平行光阴影数据
DirectionalShadowData GetDirectionalShadowData(int lightIndex,ShadowData shadowData)
{
    DirectionalShadowData data;
    //  乘以 shadowData.strength 可以剔除掉最后一个级联范围外的所有阴影。。。。
    //data.strength = _DirectionalLightShadowData[lightIndex].x * shadowData.strength;
    data.strength = _DirectionalLightShadowData[lightIndex].x ;
    data.tileIndex = _DirectionalLightShadowData[lightIndex].y + shadowData.cascadeIndex;
    data.normalBias = _DirectionalLightShadowData[lightIndex].z;
    data.shadowMaskChannel = _DirectionalLightShadowData[lightIndex].w;
    return data;
}


Light GetDirectionalLight(int index,Surface surfaceWS,ShadowData shadowData)
{
    Light light;
    light.color = _DirectionalLightColors[index].rgb;
    light.direction = _DirectionalLightDirections[index].xyz;
    // 得到阴影数据
    DirectionalShadowData dirShadowData = GetDirectionalShadowData(index,shadowData);
    // 得到阴影衰减
    light.attenuation = GetDirectionalShadowAttenuation(dirShadowData,shadowData,surfaceWS);
    return light;
}

/////----------------------------------------------------
/////----------------------------------------------------
/////----------------otherLight------------------------------------
/////----------------------------------------------------

int GetOtherLightCount()
{
    return _OtherLightCount;
}

OtherShadowData GetOtherShadowData(int lightIndex)
{
    OtherShadowData data;
    data.strength = _OtherLightShadowData[lightIndex].x;
    data.shadowMaskChannel = _OtherLightShadowData[lightIndex].w;
    return data;
}

//  点光源的衰减： 光范围的衰减（限制光照范围）和 距离光源远近的衰减 共同作用
Light GetOtherLight(int index, Surface surfaceWS,ShadowData shadowData)
{
    Light light;
    light.color = _OtherLightColors[index].rgb; 
    float3 ray = _OtherLightPositions[index].xyz - surfaceWS.position;
    light.direction = normalize(ray);
    float distanceSqr = max(dot(ray,ray),0.00001);
    float rangeAttenuation = Square1(saturate(1.0 - Square1(distanceSqr * _OtherLightPositions[index].w)));
    
    float4 spotAngle = _OtherLightSpotAngles[index];
    // spot 的衰减
    float spotAttenuation =Square1( saturate(dot(_OtherLightDirections[index].xyz,light.direction) * spotAngle.x + spotAngle.y));    
    OtherShadowData otherShadowData = GetOtherShadowData(index);
    light.attenuation = GetOtherShadowAttenuation(otherShadowData,shadowData,surfaceWS) * spotAttenuation * rangeAttenuation / distanceSqr;
  return light;
} 






#endif
