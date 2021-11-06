#ifndef CUSTOM_UNLIT_PASS_INCLUDE
#define CUSTOM_UNLIT_PASS_INCLUDE




struct Attributes
{
    float3 positionOS : POSITION;
    float2 baseUV : Texcoord0;
    // 通过顶点数据 提供该渲染对象的索引
    UNITY_VERTEX_INPUT_INSTANCE_ID
};


struct Varyings
{
    float4 positionCS : SV_POSITION;
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
    output.positionCS = TransformWorldToHClip(positionWS);
    output.baseUV = TransformBaseUV(input.baseUV);
    return output;
}


float4 UnlitPassFragment(Varyings input): SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    float4 base = GetBase(input.baseUV);    
 #if defined(_CLIPPING)
    clip(base.a - GetCutoff(input.baseUV));
 #endif
    return base;
}

#endif