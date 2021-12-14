// unity  标准输入库
#ifndef CUSTOM_LIT_INPUT_INCLUDE 
#define CUSTOM_LIT_INPUT_INCLUDE



TEXTURE2D(_BaseMap);
TEXTURE2D(_MaskMap);
TEXTURE2D(_EmissionMap);
SAMPLER(sampler_BaseMap);

TEXTURE2D(_DetailMap);
SAMPLER(sampler_DetailMap);
//SAMPLER(sampler_EmissionMap);

TEXTURE2D(_DetailNormal);


TEXTURE2D(_NormalMap);



struct InputConfig
{
    Fragment fragment;
    float2 baseUV;
    float2 detailUV;
    bool useMask;
    bool useDetail;
};


InputConfig GetInputConfig(float4 positionSS, float2 baseUV, float2 detailUV = 0.0)
{
    InputConfig c;
    c.fragment = GetFragment(positionSS);
    c.baseUV = baseUV;
    c.detailUV = detailUV;
    c.useMask = false;
    c.useDetail = false;
    return c;
}



#define INPUT_PROP(name) UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,name);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4,_BaseMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float4,_DetailMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float4,_BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float,_Cutoff)
    UNITY_DEFINE_INSTANCED_PROP(float,_Metallic)
    UNITY_DEFINE_INSTANCED_PROP(float,_Occlusion)
    UNITY_DEFINE_INSTANCED_PROP(float,_Smoothness)
    UNITY_DEFINE_INSTANCED_PROP(float,_Fresnel)
    UNITY_DEFINE_INSTANCED_PROP(float4,_EmissionColor)
    UNITY_DEFINE_INSTANCED_PROP(float4, _DetailAlbedo)    
    UNITY_DEFINE_INSTANCED_PROP(float4,_DetailSmoothness)
    UNITY_DEFINE_INSTANCED_PROP(float4,_NormalScale)
    UNITY_DEFINE_INSTANCED_PROP(float4,_DetailNormalScale)
    UNITY_DEFINE_INSTANCED_PROP(float,_ZWrite)    
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)


//  不写入深度， 则使用传入的alpha作为alpha
//  不写入深度的，半透明物体
float GetFinalAlpha(float alpha)
{
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_ZWrite) ? 1.0 : alpha;
}


float4 GetMask(InputConfig c)
{
    if(c.useMask)
    {
        return SAMPLE_TEXTURE2D(_MaskMap,sampler_BaseMap,c.baseUV);
    }
    return 1.0;
    
}


// 采样发现并解码
float3 GetNormalTS(InputConfig c)
{
    float4 map = SAMPLE_TEXTURE2D(_NormalMap,sampler_BaseMap,c.baseUV);
    float scale = INPUT_PROP(_NormalScale);
    float3 normal = DecodeNormal(map,scale);
    
    if(c.useDetail)
    {
        map = SAMPLE_TEXTURE2D(_DetailNormal,sampler_DetailMap,c.detailUV);
        scale =  GetMask(c).b *  INPUT_PROP(_DetailNormalScale);
        float3 detail = DecodeNormal(map,scale);
        normal = BlendNormalRNM(normal,detail);
    }     
    return normal;
}


float2 TransformBaseUV(float2 baseUV)
{
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_BaseMap_ST);
    return baseUV * baseST.xy + baseST.zw;
}

float2 TransformDetailUV(float2 detailUV)
{
    float4 detailST = INPUT_PROP(_DetailMap_ST);
    return detailUV * detailST.xy + detailST.zw;
}


float4 GetDetail(InputConfig c)
{
    if(c.useDetail)
    {
        float4 detailColor = SAMPLE_TEXTURE2D(_DetailMap,sampler_DetailMap,c.detailUV);
        return detailColor * 2.0 - 1.0;
    }
    return 0.0;
}

float4 GetBase(InputConfig c)
{
    float4 map = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,c.baseUV);
    float4 color = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_BaseColor);
    if(c.useDetail)
    {
        float detail = GetDetail(c).r * INPUT_PROP(_DetailAlbedo);    
        float mask = GetMask(c).b;
        map.rgb = lerp(sqrt(map.rgb),detail < 0.0 ? 0.0 : 1.0,abs(detail));
        map.rgb *= map.rgb;
    }
    
    return map * color;
}



float2 GetCutoff(InputConfig c)
{
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_Cutoff);
}



float2 GetMetallic(InputConfig c)
{
    float metallic = INPUT_PROP(_Metallic);
    metallic *= GetMask(c).r;
    return  metallic;
}

float2 GetOcclusion(InputConfig c)
{
    float strength = INPUT_PROP(_Occlusion);
    float occlusion = GetMask(c).g;
    occlusion = lerp(occlusion,1.0,strength);
    return occlusion;
}


float2 GetSmoothness(InputConfig c)
{
    float smoothness = INPUT_PROP(_Smoothness);
    smoothness *= GetMask(c).a;
    
    if(c.useDetail)
    {
        float detail = GetDetail(c).b * INPUT_PROP(_DetailSmoothness);
        float mask = GetMask(c).b;
        smoothness = lerp(smoothness,detail < 0.0 ? 0.0 : 1.0,abs(detail) * mask);
    }
    
    return smoothness;
}


float GetFresnel(InputConfig c)
{
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_Fresnel);
}




float3 GetEmission(InputConfig c)
{
    float4 map = SAMPLE_TEXTURE2D(_EmissionMap,sampler_BaseMap,c.baseUV);
    float4 color = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_EmissionColor);
    return map.rgb * color.rgb;
}


#endif