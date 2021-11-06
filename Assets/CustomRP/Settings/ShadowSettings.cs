using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ShadowSettings
{

    //最大阴影距离
    [Min(0f)]
    public float maxDistance = 100f;

    // 贴图的大小
    public enum TextureSize
    {
        _256 = 256,_512 = 512,_1024 = 1024,_2048 = 2048,_4096 = 4096,_8192 = 8192
    }
    
    // 方向光的阴影配置
    [System.Serializable]
    public struct Directional
    {
        public TextureSize atlasSize;
        // 级联阴影的数量
        [Range(1,4)]
        public int cascadeCount;
        //级联比例
        [Range(0f,1f)]
        public float cascadeRatiol, cascadeRatio2, cascadeRatio3;
    }

    // 默认尺寸是1024
    public Directional directional = new Directional { atlasSize = TextureSize._1024};
}
