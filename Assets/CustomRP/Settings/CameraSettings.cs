using System;
using UnityEngine;
using UnityEngine.Rendering;
[Serializable]
public class CameraSettings 
{
    [Serializable]
    public struct  FinalBlendMode
    {
        public BlendMode source, destination;
    }
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
