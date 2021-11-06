using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ShadowSettings
{

    //最大阴影距离
    [Min(0.001f)]
    public float maxDistance = 100f;

    // 阴影过渡距离
    [Range(0.001f,1f)]
    public float distanceFade = 0.1f;
    // 贴图的大小
    public enum TextureSize
    {
        _256 = 256,_512 = 512,_1024 = 1024,_2048 = 2048,_4096 = 4096,_8192 = 8192
    }
    

    // PCF 滤波模式  （百分比切近滤波）
    public enum FilterMode
    {
        PCF2x2,PCF3x3,PCF5x5,PCF7x7,
    }
    
    public enum CascadeBlendMode
    {
        // dither  抖动
        Hard,Soft,Dither
    }
    
    // 方向光的阴影配置
    [System.Serializable]
    public struct Directional
    {
        public TextureSize atlasSize;
        
        public FilterMode filterMode;
        // 级联阴影的数量
        [Range(1,4)]
        public int cascadeCount;
        //级联比例(最大是4级， 第4级= 1 - cascadeRatio1 - cascadeRatio2 - cascadeRatio3)
        [Range(0f,1f)]
        public float cascadeRatio1, cascadeRatio2, cascadeRatio3;
        
        public Vector3 CascadeRatios => new Vector3(cascadeRatio1,cascadeRatio2,cascadeRatio3);
        [Range(0.001f,1f)]
        public float cascadeFade;
     
        public CascadeBlendMode cascadeBlend;
    }
        
       
    // 默认尺寸是1024
    public Directional directional = new Directional { atlasSize = TextureSize._1024,filterMode = FilterMode.PCF2x2,
        cascadeCount = 4,
        cascadeRatio1 = 0.1f,cascadeRatio2 = 0.25f,cascadeRatio3 = 0.5f,cascadeFade = 0.1f,cascadeBlend = CascadeBlendMode.Hard};

    

}
