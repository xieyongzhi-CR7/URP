using System;
using UnityEngine;
using UnityEngine.Rendering;
[Serializable]
public class CameraSettings 
{
    public enum RenderScaleMode
    {
        // 使用管线值
        Inherit,
        // 管线值 * 当前相机值
        Multiply,
        // 使用当前相机值
        Override,
    }

    public RenderScaleMode renderScaleMode = RenderScaleMode.Inherit;
    [Range(0.1f,2f)]
    public float renderScale = 1.0f;


    public float GetRenderScale(float scale)
    {
        return renderScaleMode == RenderScaleMode.Inherit ? scale :
            renderScaleMode == RenderScaleMode.Override ? renderScale : renderScale * scale;

    }
    
    
    [Serializable]
    public struct  FinalBlendMode
    {
        public BlendMode source, destination;
    }

    
    // 拷贝颜色
    public bool copyColor = true;
    // 相机拷贝深度的开关
    public bool copyDepth = true;
    // 除了使用 CullingMask   ； 这里额外自定义用  renderLayerMask 来渲染
    [RenderingLayerMaskField]
    public int renderingLayerMask = -1;
    public bool maskLights = false;
    public bool overridePostFX = false;
    public PostFXSettings postFxSettings = default;
    public FinalBlendMode finalBlendMode = new FinalBlendMode
    {
        source = BlendMode.One,
        destination = BlendMode.OneMinusSrcAlpha
    };
    
}