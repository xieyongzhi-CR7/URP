// BRDF
#ifndef CUSTOM_BRDF_INCLUDE 
#define CUSTOM_BRDF_INCLUDE

struct BRDF
{
    float3 diffuse;
    float3 specular;
    float3 roughness;
};

// 非导体的 最小反射率
#define MIN_REFLECTIVITY 0.04

float oneMinusReflectivity(float metallic)
{
    float range = 1.0 - MIN_REFLECTIVITY;
    return range - metallic * range;
}

BRDF GetBRDF(Surface surface, bool applyAlphaToDiffuse = false)
{
    BRDF brdf;
    float oneMinusRef = oneMinusReflectivity(surface.metallic);
    brdf.diffuse = surface.color * oneMinusRef;
    if(applyAlphaToDiffuse)
    {
        // 透明度预乘
        brdf.diffuse *= surface.alpha; 
    }
    //  根据能量守恒： 镜面反射应该等于 ： brdf.specular = Surface.color - brdf.diffuse;
    // 但是忽略了一个事实： 即金属影响镜面反射的颜色，而非金属不影响。。
    // 非金属的镜面反射应该是白色的，最后威名通过金属度在最小反射率和表面颜色之间进行插值得到brdf的镜面反射颜色
    brdf.specular = lerp(MIN_REFLECTIVITY,surface.color,surface.metallic);
    float perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
    brdf.roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
    return brdf;
}

// 根据公式 得到镜面反射的强度
float SpecularStrength(Surface surface, BRDF brdf, Light light)
{
    float3 h = SafeNormalize(light.direction + surface.viewDirection);
    float nh2 = Square(saturate(dot(surface.normal, h)));
    float lh2 = Square(saturate(dot(light.direction,h)));
    float r2 = Square(brdf.roughness);
    float d2 = Square(nh2 * (r2 - 1.0) + 1.00001);
    float normalization = brdf.roughness * 4.0 + 2.0;
    return r2 / (d2 * max(0.1,lh2) * normalization); 
}

// 直接光照的表面颜色
float3 DirectBRDF(Surface surface,BRDF brdf,Light light)
{
    return SpecularStrength(surface,brdf,light) * brdf.specular + brdf.diffuse;
}


#endif