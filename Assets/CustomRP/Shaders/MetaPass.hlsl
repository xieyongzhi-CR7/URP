
#ifndef CUSTOM_META_PASS_INCLUDE 
#define CUSTOM_META_PASS_INCLUDE

#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/Shadows.hlsl"
#include "../ShaderLibrary/Light.hlsl"
#include "../ShaderLibrary/BRDF.hlsl"


struct Attributes
{
    float3 positionOS : POSITION;
    float2 baseUV : TEXCOORD0;
    float2 lightMapUV : TEXCOORD1;
};


struct Varings
{
    float4 positionCS : SV_POSITION;
    //  只是从vertex中 往 fragment中传递的参数， 除了命名体现的含义， 不需要GPU特定的命名
    // VAR_BASE_UV 只是标记接受的定义， 不需要GPU 的特别关注，所以后面的 VAR_LIGHT_MAP_UV;等也只是一个
    float2 baseUV : VAR_BASE_UV;
};

bool4 unity_MetaFragmentControl;

float unity_OneOverOutputBoost;
float unity_MaxOutputValue;

Varings MetaPassVertex(Attributes input)
{
    Varings output;
    input.positionOS.xy = input.lightMapUV * unity_LightmapST.xy + unity_LightmapST.zw;
    input.positionOS.z = input.positionOS.z > 0.0 ? FLT_MIN : 0.0;
    output.positionCS = TransformWorldToHClip(input.positionOS);
    output.baseUV = TransformBaseUV(input.baseUV);  
    return output;
}


float4 MetaPassFragment(Varings input):SV_TARGET
{
    InputConfig inputConfig = GetInputConfig(input.positionCS,input.baseUV,0.0);
    float4 base = GetBase(inputConfig);
    Surface surface;
    ZERO_INITIALIZE(Surface,surface);
    surface.color = base.rgb;
    surface.metallic = GetMetallic(inputConfig);
    surface.smoothness = GetSmoothness(inputConfig);
    BRDF brdf = GetBRDF(surface);
    float4 meta = 0.0;
    if(unity_MetaFragmentControl.x)
    {// 烘焙的 弹射后的间接光 的颜色设置
        meta = float4(brdf.diffuse,1.0);
        // brdf    高镜面但粗糙度的材质  ，也可以传递通过一些间接光，，也当作是间接反射的一部分
        meta.rgb += brdf.specular * brdf.roughness * 0.5;
        meta.rgb = min(PositivePow(meta.rgb,unity_OneOverOutputBoost),unity_MaxOutputValue);
    }
    else if (unity_MetaFragmentControl.y)
    {
        // 决定烘焙自发光的效果
        meta = float4(GetEmission(inputConfig),1.0);
    }
    return meta;
}
#endif