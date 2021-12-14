#ifndef CUSTOM_UNLIT_PASS_INCLUDE
#define CUSTOM_UNLIT_PASS_INCLUDE




struct Attributes
{
    // 顶点色
    float4 color : COLOR;
    #if defined(_FLIPBOOK_BLENDING)
        float4 baseUV : Texcoord0;
        float flipbookBlend : Texcoord1;
        
    #else
        float2 baseUV : Texcoord0;
    #endif
    
    float3 positionOS : POSITION;
    
    // 通过顶点数据 提供该渲染对象的索引
    UNITY_VERTEX_INPUT_INSTANCE_ID
};


struct Varyings
{
#if defined(_VERTEX_COLORS)
    float4 color : VAR_COLOR;
#endif

#if defined(_FLIPBOOK_BLENDING)
    float3 flipbookUVB : VAR_FLIPBOOK;
#endif
    float4 positionCS_SS : SV_POSITION;
    float2 baseUV : VAR_BASE_UV;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};



Varyings UnlitPassVertex(Attributes input)
{
    Varyings output;   
    // 提取渲染对象的索引，并且存储到其它实例宏 所依赖的全局静态变量中
    UNITY_SETUP_INSTANCE_ID(input);    
    // 将索引 转换存储，在片元中还要适用
    UNITY_TRANSFER_INSTANCE_ID(input,output);
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS_SS = TransformWorldToHClip(positionWS);
    output.baseUV = TransformBaseUV(input.baseUV.xy);
    
    #if defined(_FLIPBOOK_BLENDING)
        output.flipbookUVB.xy = TransformBaseUV(input.baseUV.zw);
        output.flipbookUVB.z = input.flipbookBlend;
    #endif
    
    
#if defined(_VERTEX_COLORS)    
    output.color = input.color;
#endif
    return output;
}


float4 UnlitPassFragment(Varyings input): SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
float4 color = float4(1.0,1.0,1.0,1.0);
#if defined(_VERTEX_COLORS)
    color = input.color;
#endif

    bool flipbookBlending = false;
    float3 uvB = 0.0;
 #if defined(_FLIPBOOK_BLENDING)
    flipbookBlending = true;
    uvB = input.flipbookUVB;
 #endif
    bool nearFade = false;
#if defined(_NEARFADE)
    nearFade = true;
#endif
    bool softParticles = false;
#if defined(_SOFT_PARTICLES)
    softParticles = true;
#endif


    Fragment fragment = GetFragment(input.positionCS_SS);
    //return GetBufferColor(fragment,0.05);
    float4 base = GetBase(input.baseUV,fragment,color,uvB,flipbookBlending,nearFade,softParticles);
    //float4 base = GetBase(input.baseUV,color);    
 #if defined(_CLIPPING)
    clip(base.a - GetCutoff(input.baseUV));
 #endif

#if defined(_DISTORTION)
    float2 distortion = GetDistortion( input.baseUV, flipbookBlending,uvB) * base.a;
    float3 bufferColor = GetBufferColor(fragment,distortion).rgb;
    base.rgb = lerp(bufferColor,base.rgb,saturate(base.a - GetDistortionBlend()));
#endif
    return float4(base.rgb,GetFinalAlpha(base.a));
}

#endif