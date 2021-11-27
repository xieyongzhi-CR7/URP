using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
}
