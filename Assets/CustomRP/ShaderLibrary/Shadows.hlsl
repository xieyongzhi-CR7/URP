// 计算光照相关库 
#ifndef CUSTOM_SHADOWS_INCLUDE 
#define CUSTOM_SHADOWS_INCLUDE

#include "../../../Library/PackageCache/com.unity.render-pipelines.core@10.3.2/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"


#if defined(_DIRECTIONAL_PCF3)
// 需要4个滤波样本
#define DIRECTIONAL_FILTER_SAMPLES 4
#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(_DIRECTIONAL_PCF5)
// 需要9个滤波样本
#define DIRECTIONAL_FILTER_SAMPLES 9
#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_DIRECTIONAL_PCF5)
// 需要16个滤波样本
#define DIRECTIONAL_FILTER_SAMPLES 16
#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif  



#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4
// 阴影图集
TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);


#define MAX_CASCADE_COUNT 4
//  用于接受CPU 传送的灯光数据
CBUFFER_START(_CustomShadows)
// 级联数量
int _CascadeCount;
// 包围球数据
float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT];
// 级联数据
float4 _CascadeData[MAX_CASCADE_COUNT];
// 阴影转换矩阵
float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];    
// 阴影的最大距离
//float _ShadowDistance;
// 阴影过渡距离
float4 _ShadowDistanceFade;
// x:阴影图集大小，  y：纹素大小
float4 _ShadowAtlasSize;
CBUFFER_END

struct ShadowMask
{
    // 是否  shadowMask模式  而不是 distanceShadowMask模式
    bool always;
    // 是否启用distance shadowMask 模式
    bool distance;
    // 阴影衰减
    float4 shadows;

};


// 阴影数据
struct ShadowData
{
    int cascadeIndex;
    // 是否采样阴影的标志
    float strength;
    //混合级联
    float cascadeBlend;
    //
    ShadowMask shadowMask;
};

// 公式计算阴影过渡时的强度
float FadedShadowStrength(float distance,float scale,float fade)
{
    return saturate((1.0 - distance * scale) * fade);
}





//得到世界空间中的表面阴影数据
ShadowData GetShadowData(Surface surfaceWS)
{
    ShadowData data;
    data.shadowMask.always = false;
    data.shadowMask.distance = false;
    data.shadowMask.shadows = 1.0;
    data.cascadeBlend = 1.0;
    // 通过公式得到有线性过渡的阴影强度    
    data.strength = FadedShadowStrength(surfaceWS.depth,_ShadowDistanceFade.x,_ShadowDistanceFade.y);
    
    int i;
    
    for( i= 0; i < _CascadeCount; i++)
    {
        float4 sphere = _CascadeCullingSpheres[i];
        float distanceSqr = DistanceSquared(sphere.xyz,surfaceWS.position);
        if(distanceSqr < sphere.w)
        {
            // 计算级联阴影的过渡强度
            float fade = FadedShadowStrength(distanceSqr,_CascadeData[i].x, _ShadowDistanceFade.z);
            // 如果绘制的对象在最后一个级联的范围中，计算级联的过渡阴影强度，和阴影最大距离的过渡阴影强度 相乘得到最终阴影强度
            if(i == _CascadeCount - 1)
            {
                data.strength *= fade; 
            }
            else
            {
                data.cascadeBlend = fade;
            }
            break;
        }
    }
    // 如果超出级联范围，不进行阴影采样
    if(i == _CascadeCount)
    {
        data.strength = 0.0;
    }
#if defined(_CASCADE_BLEND_DITHER)
    else if (data.cascadeBlend < surfaceWS.dither)
    {
        i+=1;
    }
#endif
#if !defined(_CASCADE_BLEND_SOFT)
    data.cascadeBlend = 1.0;
#endif
    //  如果超出最后一个级联的范围，标识符设置为0，不对阴影进行采样    
    data.cascadeIndex = i;    
    return data;
}


struct DirectionalShadowData
{
    float strength;
    int tileIndex;
    float normalBias;
    // 一个光源对应的shadowMask中的一个通道中的数据
    int shadowMaskChannel;
};


// 采样阴影图集
float SampleDirectionalShadowAtlas(float3 positionSTS)
{
    return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas,SHADOW_SAMPLER,positionSTS);
}

// 计算阴影的强度
float FilterDirectionalShadow(float3 positionSTS)
{
    #if defined(DIRECTIONAL_FILTER_SETUP)
        // 样本权重
        float weights[DIRECTIONAL_FILTER_SAMPLES];
        //样本位置
        float2 positions[DIRECTIONAL_FILTER_SAMPLES];
            
        float4 size = _ShadowAtlasSize.yyxx;
        DIRECTIONAL_FILTER_SETUP(size,positionSTS.xy,weights,positions);
        float shadow = 0;
        for(int i = 0; i < DIRECTIONAL_FILTER_SAMPLES;i++)
        {
            shadow += weights[i] * SampleDirectionalShadowAtlas(float3(positions[i].xy,positionSTS.z));
        }                              
        return shadow;
    #else
        return  SampleDirectionalShadowAtlas(positionSTS);
    #endif
}


// 多光源的 混合模式  channel  一个通道对应一个光源的shadowMask的数据
// 因为 shadowMask 只有4个通道  所以  只支持4盏灯
float GetBakedShadow(ShadowMask mask,int channel)
{
    float shadow = 1.0; 
    if(mask.distance || mask.always)
    {
        if(channel >=0)
        {
            shadow = mask.shadows[channel];
        }   
    }
    return shadow;
}


float GetBakedShadow(ShadowMask mask,int channel, float strength)
{
    if(mask.distance || mask.always)
    {
        return lerp(1.0,GetBakedShadow(mask,channel),strength);
    }
    return 1.0;
}

// 混合光照  第二个参数  shadow : 是传进来的实时阴影（如果是shadowMask模式， 则使用烘焙阴影替换实时阴影）
float MixBakedAndRealtimeShadows(ShadowData global, float shadow,int shadowMaskChannel,float strength)
{
    float baked = GetBakedShadow(global.shadowMask,shadowMaskChannel);
    if(global.shadowMask.always)
    {
        //                实时强度   全局强度
        shadow = lerp(1.0,shadow,global.strength);
        shadow = min(baked,shadow);
            // 应用灯光的强度
        return lerp(1.0,shadow,strength);
    
    }
    if(global.shadowMask.distance)
    {   
        //            烘焙强度  实时强度   全局强度
        shadow = lerp(baked,shadow,global.strength);
            // 应用灯光的强度
        return lerp(1.0,shadow,strength);
    }
    return lerp(1.0,shadow,strength * global.strength);
}



// 实时阴影的采样代码
float GetCascadeShadow(DirectionalShadowData directional, ShadowData global,Surface surfaceWS)
{
   // 计算法线偏差      世界法线          * 光源中的light.normalBias  * 像素偏移值大小
    float3 normalBias = surfaceWS.interpolatednormal * (directional.normalBias * _CascadeData[global.cascadeIndex].y);
    // 通过阴影转换矩阵和表面位置 得到在阴影纹理（图块）空间的位置，然后对图集进行采样
    // 此位置是在法线方向上偏移后的新值
    float3 positionSTS = mul(_DirectionalShadowMatrices[directional.tileIndex],float4(surfaceWS.position + normalBias,1.0)).xyz;
    float shadow = FilterDirectionalShadow(positionSTS);
    // 如果级联混合小于1 代表在级联层级过渡区域中，必须从下一级联中采样并在两个值之间进行插值
    if(global.cascadeBlend < 1.0)
    {
        normalBias = surfaceWS.interpolatednormal * ( directional.normalBias * _CascadeData[global.cascadeIndex + 1].y);
        positionSTS = mul(_DirectionalShadowMatrices[directional.tileIndex+1],float4(surfaceWS.position + normalBias,1.0)).xyz;
        shadow = lerp(FilterDirectionalShadow(positionSTS),shadow,global.cascadeBlend);
    }
    return shadow;
}




//  计算阴影衰减
float GetDirectionalShadowAttenuation(DirectionalShadowData directional, ShadowData global,Surface surfaceWS)
{
// 如果不接受阴影，阴影衰减为1
#if !defined(_RECEIVE_SHADOWS)
    return 1.0;
#endif
    float shadow;
    //  global.strength : 超出级联范围 strength = 0
    if( directional.strength * global.strength <= 0.0)
    {
        shadow = GetBakedShadow(global.shadowMask,directional.shadowMaskChannel,abs(directional.strength));
    }
    else
    {
        // 实时阴影
        shadow = GetCascadeShadow(directional,global,surfaceWS);
        // 最终的阴影衰减值是阴影强度和衰减因子的插值
        shadow = MixBakedAndRealtimeShadows(global,shadow,directional.shadowMaskChannel,directional.strength);
    }
    return shadow;
}


//--------------------------------------
//------------other ShadowData--------------------------
//--------------------------------------

struct OtherShadowData
{
    float strength;
    int shadowMaskChannel;
};

float GetOtherShadowAttenuation(OtherShadowData other,ShadowData global,Surface surfaceWS)
{
    #if !defined(_RECEIVE_SHADOWS)
        return 1.0;
    #endif
    
    float shadow;
    if(other.strength > 0.0)
    {
        shadow = GetBakedShadow(global.shadowMask,other.shadowMaskChannel,other.strength);
    }
    else
    {
        shadow = 1.0;
    }
    return shadow;
}

#endif
