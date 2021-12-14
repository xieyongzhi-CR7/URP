// unity  标准输入库
#ifndef FRAGMENT_INCLUDE 
#define FRAGMENT_INCLUDE


float4 _CameraBufferSize;
TEXTURE2D(_CameraDepthTexture);
TEXTURE2D(_CameraColorTexture);
struct Fragment 
{
    float2 positionSS;
    // 屏幕坐标
    float2 screenUV;
    // 此点自身在视图空间的深度
    float depth;
    // 在深度图中存储的深度（可能不是此点本身的深度）
    float bufferDepth;
};



///
// positionSS  是传入的  positionSS :SV_POSITION的值

// 在Vertex 函数中    positionSS  是齐次裁剪坐标，此时并未进行齐次除法 也 没有归一化
// 在 Fragment函数中  positionSS 是屏幕坐标 （此时 距离转换到 视口坐标 screenUV 坐标， 就差一个 屏幕宽高的缩放了）

float4 GetBufferColor(Fragment fragment, float2 uvOffset = float2(0.0,0.0))
{
    float2 uv = fragment.screenUV + uvOffset;
    return SAMPLE_TEXTURE2D_LOD(_CameraColorTexture,sampler_CameraColorTexture,uv,0);   
}



Fragment GetFragment(float4 positionSS)
{
    Fragment f;
    f.positionSS = positionSS.xy;
    f.screenUV = f.positionSS * _CameraBufferSize; // _ScreenParams.xy;
    
    // 正交相机的深度   存储在 w 分量中
    f.depth = IsOrthographicCamera() ? OrthographicDepthBufferToLinear(positionSS.z) : positionSS.w;
    f.bufferDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_point_clamp, f.screenUV);  
    f.bufferDepth = IsOrthographicCamera() ? OrthographicDepthBufferToLinear(f.bufferDepth) : LinearEyeDepth(f.bufferDepth,_ZBufferParams);
    return f;
}


#endif