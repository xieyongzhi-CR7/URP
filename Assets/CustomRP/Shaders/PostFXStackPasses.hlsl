
// unity  标准输入库
#ifndef CUSTOM_POST_FX_STACE_INCLUDE 
#define CUSTOM_POST_FX_STACE_INCLUDE
#include "../../../Library/PackageCache/com.unity.render-pipelines.core@10.7.0/ShaderLibrary/Filtering.hlsl"
#include "../../../Library/PackageCache/com.unity.render-pipelines.core@10.7.0/ShaderLibrary/Color.hlsl"
#include "../../../Library/PackageCache/com.unity.render-pipelines.core@10.7.0/ShaderLibrary/ACES.hlsl"
TEXTURE2D(_PostFXSource);
TEXTURE2D(_PostFXSource2);

SAMPLER(sampler_linear_clamp);
float4 _PostFXSource_TexelSize;    

bool _BloomBicubicUpsampling;
float4 _BloomThreshold;
float _BloomIntensity;

// 白平衡
float4 _WhiteBalance;




float Luminance(float3 color, bool useACES)
{
    return useACES ? AcesLuminance(color) : Luminance(color);
}


//// 色调分离
float4 _SplitToningShadows,_SplitToningHightLights;
float3 ColorGradeSplitToning(float3 color, bool useACES )
{
    color = PositivePow(color,1.0 / 2.0);
    //float3 shadows = _SplitToningShadows.rbg;
    //float3 hightLights = _SplitToningHightLights.rgb;
    float t = saturate(Luminance(saturate(color),useACES) + _SplitToningShadows.w);
    float3 shadows = lerp(0.5,_SplitToningShadows.rgb,1.0-t);
    float3 hightLights = lerp(0.5, _SplitToningHightLights.rgb,t);
    color = SoftLight(color,shadows);
    color = SoftLight(color,hightLights);  
    return PositivePow(color,2.2);
}


//  通道混合  channelMixer
float4 _ChannelMixerRed,_ChannelMixerGreen,_ChannelMixerBlue;
float3 ColorGradingChannelMixer(float3 color)
{
    return mul(float3x3(_ChannelMixerRed.rgb,_ChannelMixerGreen.rgb,_ChannelMixerBlue.rgb),color);
}


//////  

float4 _SMHShadows;
float4 _SMHMidtones;
float4 _SMHHighlights;
float4 _SMHRange;

float3 ColorGradingShadowsMidtonesHighlights(float3 color,bool useACES)
{
    float luminance = Luminance(color, useACES);
    float shadowsWeight = 1.0 - smoothstep(_SMHRange.x,_SMHRange.y,luminance);
    float highlightsWeight = smoothstep(_SMHRange.z,_SMHRange.w,luminance);
    float midtonesWeight = 1.0 - shadowsWeight - highlightsWeight;
    return color * _SMHShadows.rgb * shadowsWeight + color * _SMHHighlights.rgb * highlightsWeight 
            + color  * _SMHMidtones * midtonesWeight;

}





/////--------------------------------
/////--------------------------------
// 颜色分级
/////--------------------------------
float4 _ColorAdjustments;
float4 _ColorFilterId;

// 后曝光
float3 ColorGradePostExposure(float3 color)
{
    return color * _ColorAdjustments.x;
}

// 对比度： 最亮的白 和 最黑的黑的 对比（对比度高，可以提高画质的明亮清晰程度； 低对比度灰蒙蒙）
float3 ColorGradeingContrast(float3 color, bool useACES)
{
    color =  useACES ? ACES_to_ACEScc(unity_to_ACES(color)) : LinearToLogC(color);
    color =  (color - ACEScc_MIDGRAY) * _ColorAdjustments.y + ACEScc_MIDGRAY;
    return  useACES ? ACES_to_ACEScg(ACEScc_to_ACES(color)) : LogCToLinear(color);
}

//  滤镜
float3 ColorGradeColorFilter(float3 color)
{
    return color * _ColorFilterId.rgb;
}


float3 ColorGradingHueShift(float3 color)
{
    color = RgbToHsv(color);
    float hue = color.x + _ColorAdjustments.z;
    //  将色调限制在（0，1）  小于0： hue+1;    大于1：hue-1
    color.x = RotateHue(hue,0.0,1.0);
    return HsvToRgb(color);
}

//  饱和度： 指色彩的鲜艳程度，
//取决于 该色中含色成分和  消色成分（灰色）的比例
//   含色成分越大，饱和度越大，    消色成分越大 饱和度越小                
float3 ColorGradingSaturation(float3 color,bool useACES)
{
    float luminance = Luminance(color, useACES);
    return (color - luminance) * _ColorAdjustments.w + luminance;
}


// 白平衡
float3 ColorGradeWhiteBalance(float3 color)
{
    color = LinearToLMS(color);
    color *= _WhiteBalance.rgb;
    return LMSToLinear(color);
}







float3 ColorGrade(float3 color,bool useACES = false)
{
    // 将颜色组件限制到60
    color = min(color,60.0);
    //  后曝光
    color = ColorGradePostExposure(color);
    
    //白平衡
    color = ColorGradeWhiteBalance(color);
    
    // 对比度
    color = ColorGradeingContrast(color,useACES);
    // 滤镜
    color = ColorGradeColorFilter(color);
    color = max(color,0.0);
    // 色调分离
    color = ColorGradeSplitToning(color,useACES);
    // 通道混合
    color = ColorGradingChannelMixer(color);
    color = max(color,0.0);
    color = ColorGradingShadowsMidtonesHighlights(color,useACES);
    // 色调偏移
    color = ColorGradingHueShift(color);
    // 饱和度
    color = ColorGradingSaturation(color,useACES);
    return max(useACES ? ACEScg_to_ACES(color) : color ,0.0);
}



/////--------------------------------
/////--------------------------------
// 颜色分级  end
/////--------------------------------

float4 GetSourceTexelSize()
{
    return _PostFXSource_TexelSize;
}

//luminance = 0.2125 * Red + 0.7154 * Green + 0.0721 * Blue
//思路是从把从贴图采样到的颜色代入明亮度公式得到明亮度,  即 置灰
//Luminance 是客观测量发光体的亮度，是个客观值

//float Luminance(float3 color)
//{
  //  return dot(color,float3(0.2125,0.7154,0.0721));
//}



float4 GetSource(float2 screenUV)
{
    return SAMPLE_TEXTURE2D_LOD(_PostFXSource,sampler_linear_clamp,screenUV,0);
}


float4 GetSource2(float2 screenUV)
{
    return SAMPLE_TEXTURE2D_LOD(_PostFXSource2,sampler_linear_clamp,screenUV,0);
}


float4 GetSourceBicubic(float2 screenUV)
{
    return SampleTexture2DBicubic(TEXTURE2D_ARGS(_PostFXSource,sampler_linear_clamp),screenUV,_PostFXSource_TexelSize.zwxy,1.0,1.0);
}



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
    // 用于调试
    //return float4(input.screenUV,0.0,1.0);
    return GetSource(input.screenUV);
}

//----------------
//----------------
//----------------
//----------------



// 水平方向的模糊
float4 BloomHorizontalPassFragment(Varyings input) : SV_TARGET
{
    float3 color = 0.0;
    float offsets[] = {-4.0,-3.0,-2.0,-1.0,0.0,
                        1.0,2.0,3.0,4.0 };
    float weights[] = { 0.01621622,0.05405405,0.12162162,0.19459459, 0.22702703,
                        0.19459459,0.12162162 ,0.05405405,0.01621622};
    
   for(int i= 0; i<9; i++)
   {
    float offset = offsets[i] *2.0 * GetSourceTexelSize().x;
    color += GetSource(input.screenUV + float2(offset,0.0)).rgb * weights[i];
   }
    return float4(color,1.0);
}


//  在vertical 中可以缩减到5次， 但是在 BloomHorizontal中不能，因为已经在该pass中使用了双线性过滤
float4 BloomVerticalPassFragment(Varyings input) : SV_TARGET
{
    float3 color = 0.0;
    float offsets[] = {-3.23076923,-1.38461538, 0.0,1.38461538 ,3.23076923};
    float weights[] = { 0.07027027,0.31621622, 0.22702703,
                        0.31621622,0.07027027 };
                            
   for(int i = 0;i<5; i++)
   {
        float offset = offsets[i] * GetSourceTexelSize().y;
        color += GetSource(input.screenUV + float2(0.0,offset)).rgb * weights[i];
   }
    return float4(color,1.0);
}

float4 BloomCombinePassFragment(Varyings input):SV_TARGET
{
    float3 lowRes;
    if (_BloomBicubicUpsampling) 
    {
        lowRes = GetSourceBicubic(input.screenUV).rgb;
    }
    else
    {
        lowRes = GetSource(input.screenUV).rgb;
    }
    
    float3 hightRes = GetSource2(input.screenUV).rgb;
    return float4(lowRes * _BloomIntensity + hightRes,1.0);
}

// 应用阈值， 
float3 ApplyBloomThreshold(float3 color)
{
    float brightness = Max3(color.r,color.g,color.b);
    float soft = brightness + _BloomThreshold.y;
    soft = clamp(soft,0.0, _BloomThreshold.z);
    soft = soft * soft * _BloomThreshold.w;
    float contribution = max(soft,brightness - _BloomThreshold.x);
    contribution /= max(brightness,0.00001);
    return color * contribution; 
}


float4 BloomprefilterPassFragment(Varyings input):SV_TARGET
{
    float3 color = ApplyBloomThreshold(GetSource(input.screenUV).rgb);
    return float4(color,1.0);
}

float4 BloomPrefilterFirefilesPassFragment(Varyings input) : SV_TARGET
{
    float3 color = 0.0;
    float3 wightSum = 0.0;
    float2 offsets[] = {float2(0.0,0.0),float2(-1.0,-1.0),float2(-1.0,1.0),float2(1.0,-1.0),float2(1.0,1.0)//,
                                        //float2(-1.0,0.0),float2(1.0,0.0),float2(0.0,-1.0),float2(0.0,1.0)
                        };

    for(int i = 0; i < 5; i++)
    {
        float3 c = GetSource(input.screenUV + offsets[i] * GetSourceTexelSize().xy * 2.0).rgb;
        c = ApplyBloomThreshold(c);
        float w = 1.0 / (Luminance(c) + 1.0);
        color += c * w;
        wightSum += w;
    }
    color /= wightSum;
    return float4(color,1.0);
    
}

float4 BloomScatterPassFragment(Varyings input) : SV_TARGET
{
    float3 lowRes;
    if (_BloomBicubicUpsampling)
    {
        lowRes = GetSourceBicubic(input.screenUV).rgb;
    }
    else    
    {
        lowRes = GetSource(input.screenUV).rgb;
    }
    float3 hightRes = GetSource2(input.screenUV).rgb;
    return float4(lerp(hightRes,lowRes,_BloomIntensity),1.0);
}


// 补偿丢失的散射光
float4 BloomScatterFinalFragment(Varyings input) : SV_TARGET
{
    float3 lowRes;
    if (_BloomBicubicUpsampling)
    {
        lowRes = GetSourceBicubic(input.screenUV).rgb;
    }
    else    
    {
        lowRes = GetSource(input.screenUV).rgb;
    }
    float3 hightRes = GetSource2(input.screenUV).rgb;
    // 补偿丢失的散射光
    lowRes += hightRes - ApplyBloomThreshold(hightRes);
    return float4(lerp(hightRes,lowRes,_BloomIntensity),1.0);
}

//------------------------------------------------------------------------
//------------------------------------------------------------------------
//------------------------------------------------------------------------

//  颜色分级  : 在toneMapping 之前 设置颜色分级
//float3 ColorGrade(float3 color)
//{
    // 将颜色组件限制到60
  //  color = min(color,60.0);
    //color = ColorGradePostExposure(color);
    //return color;
//}



float4 ToneMappingReinhardPassFragment(Varyings input) : SV_TARGET
{
    float4 color = GetSource(input.screenUV);
    color.rgb = ColorGrade(color.rgb);
    // 画面整体会变暗
    color.rgb /= color.rgb + 1.0;
    return color;
}


float4 ToneMappingNeutralPassFragment(Varyings input) : SV_TARGET
{
    float4 color = GetSource(input.screenUV);
    color.rgb = ColorGrade(color.rgb);
    // 画面整体会变暗
    color.rgb /= color.rgb + 1.0;// NeutralTonemap(color.rgb);
    return color;
}


float4 ToneMappingACESPassFragment(Varyings input) : SV_TARGET
{
    float4 color = GetSource(input.screenUV);
    color.rgb = ColorGrade(color.rgb,true);
    color.rgb = AcesTonemap(color.rgb);
    return color;
}


float4 ToneMappingNonePassFragment(Varyings input) : SV_TARGET
{
    float4 color = GetSource(input.screenUV);
    color.rgb = ColorGrade(color.rgb);
    return color;
}




#endif