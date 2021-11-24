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

    [SerializeField] private BloomSettings bloom = new BloomSettings
    {
        scatter = 0.7f,
    };

    #region ToneMapping
    [System.Serializable]
    public struct ToneMappingSettings
    {
        public enum Mode {None = -1,ACES,Neutral,Reinhard,}

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
