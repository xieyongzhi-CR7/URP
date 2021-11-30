using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Rendering/Custom Post FX Settings")]
public class PostFXSettings : ScriptableObject
{
    [SerializeField]
    private Shader shader = default;

    [System.Serializable]
    public struct BloomSettings
    {
        [Range(0f,16f)]
        public int maxIterations;
        [FormerlySerializedAs("downscaleLimit")] [Min(1f)]
        public int downscaleLimit;
        // 是否使用三线性插值滤波
        public bool bicubicUpsampling;

        [Min(0f)]
        public float threshold;
        [Range(0f,1f)]
        public float thresholdKnee;
        [Min(0f)]
        public float intensity;

        public bool fadeFirefiles;
        
        public enum Mode
        {
            Additive,Scattering
        }

        public Mode mode;
        [Range(0.05f,0.95f)]
        public float scatter;
    }
    [Serializable]
    public struct WihteBalanceSettings
    {
        [Range(-100f,100f)]//   第一个参数是 色温： 使图像更冷或更热 、、、、 第二个参数是 Tint 色调: 调整温度变化后的颜色
        public float temperature, tint;
    }

    [SerializeField]
    private WihteBalanceSettings whiteBalance;
    
    public WihteBalanceSettings WhiteBalance => whiteBalance;
    
    
    

    [Serializable]
    public struct ColorAdjustmentsSettings
    {
        // 后曝光，调整场景的整体曝光度
        public float postExposure;
        // 对比度，扩大或缩小色调值的总体范围
        [Range(-100f,100f)]
        public float contrast;

        // 颜色滤镜， 通过乘以颜色来给渲染器着手
        [ColorUsage(false,true)]
        public Color colorFilter;

        // 色调偏移，改变所有颜色的色调
        [Range(-180f,180f)]
        public float hueShift;

        // 饱和度， 推动所有颜色的强度
        [Range(-100f,100f)]
        public float saturation;
    }

    [SerializeField]
    private ColorAdjustmentsSettings colorAdjustments = new ColorAdjustmentsSettings
    {
        colorFilter =  Color.white,
    };

    public ColorAdjustmentsSettings ColorAdjustments => colorAdjustments;
    
    
    [SerializeField] private BloomSettings bloom = new BloomSettings
    {
        scatter = 0.7f,
    };

    #region ToneMapping
    [System.Serializable]
    public struct ToneMappingSettings
    {
        public enum Mode {None,ACES,Neutral,Reinhard,}

        public Mode mode;

    }

    [SerializeField]
    private ToneMappingSettings toneMapping = default;
    public ToneMappingSettings ToneMapping => toneMapping;

    #endregion
    
    public BloomSettings Bloom => bloom;

    [System.NonSerialized]
    private Material material;

    public Material Material
    {
        get
        {
            if (material==null && shader != null)
            {
                material = new Material(shader);
                material.hideFlags = HideFlags.HideAndDontSave;
            }
            return material;
        }
    }


    #region 色调分离

    [Serializable]
    public struct SplitToningSettings
    {
        [ColorUsage(false)]
        public Color shadows, hightLights;
        [Range(-100f,100f)]
        public float balance;
    }
    [SerializeField]
    SplitToningSettings splitToning = new SplitToningSettings
    {
        shadows =   Color.gray,
        hightLights = Color.gray
    };

    public SplitToningSettings SplitToning => splitToning;


    #endregion

    

    
    #region 通道混合 ChannelMixer
    
    [Serializable]
    public struct ChannelMixerSettings
    {
        public Vector3 red, green, blue;
    }
    [SerializeField]
    private ChannelMixerSettings channelMixer = new ChannelMixerSettings
    {
        red = Vector3.right,
        green = Vector3.up,
        blue = Vector3.forward,
    };

    public ChannelMixerSettings ChannelMixer => channelMixer;
    #endregion
    #region 控制阴影和中间色调

    [Serializable]
    public struct ShadowsMidtonesHightlightsSettings
    {
        [ColorUsage(false)]
        public Color shadows, midtones,hightlights;
        [Range(0f,2f)]
        public float shadowStart,shadowsEnd,hightlightsStart,highLightsEnd;
    }
    [SerializeField]
    ShadowsMidtonesHightlightsSettings shadowsMidtonesHightlights = new ShadowsMidtonesHightlightsSettings
    {
        shadows =   Color.white,
        midtones = Color.white,
        hightlights = Color.white,
        shadowsEnd = 0.3f,
        hightlightsStart = 0.55f,
        highLightsEnd = 1f,
    };

    public ShadowsMidtonesHightlightsSettings ShadowsMidtonesHightlights => shadowsMidtonesHightlights;


    #endregion
}
