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
    }
    [SerializeField]
    private BloomSettings bloom = default;
    
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
