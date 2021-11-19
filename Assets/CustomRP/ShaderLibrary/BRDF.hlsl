// BRDF
#ifndef CUSTOM_BRDF_INCLUDE 
#define CUSTOM_BRDF_INCLUDE

struct BRDF
{
    float3 diffuse;
    float3 specular;
    float3 roughness;
    // 真实的粗糙程度 （主要是后面在计算确定 间接光的spec采样CubeMap的mipLev）
    float perceptualRoughness;
    // 菲涅尔反射的颜色
    float fresnel;
};

// 非导体的 最小反射率 (在漫反射中  这个0.04可能微不足道，但是在spec中，作用巨大)
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
    // 非金属的镜面反射应该是白色的，最后我们通过金属度在最小反射率和表面颜色之间进行插值得到brdf的镜面反射颜色
    brdf.specular = lerp(MIN_REFLECTIVITY,surface.color,surface.metallic);
    float perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
    brdf.perceptualRoughness = perceptualRoughness; 
    brdf.roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
    // 菲涅尔反射的颜色
    brdf.fresnel = saturate(surface.smoothness + 1.0 - oneMinusRef);
    return brdf;
}

// 根据公式 得到镜面反射的强度
float SpecularStrength(Surface surface, BRDF brdf, Light light)
{
    float3 h = SafeNormalize(light.direction + surface.viewDirection);
    float nh2 = Square1(saturate(dot(surface.normal, h)));
    float lh2 = Square1(saturate(dot(light.direction,h)));
    float r2 = Square1(brdf.roughness);
    float d2 = Square1(nh2 * (r2 - 1.0) + 1.00001);
    float normalization = brdf.roughness * 4.0 + 2.0;
    return r2 / (d2 * max(0.1,lh2) * normalization); 
}

// 直接光照的表面颜色
float3 DirectBRDF(Surface surface,BRDF brdf,Light light)
{
    return SpecularStrength(surface,brdf,light) * brdf.specular + brdf.diffuse;
}

// 间接光提供的颜色
float3 InDirectBRDF(Surface surface,BRDF brdf,float3 GIDiffuse,float3 GISpecular)
{
    
    float fresnelStrength = surface.fresnelStrength * Pow4(1.0 - saturate(dot(surface.normal,surface.viewDirection)));
    //间接光的镜面反射 ：  GI 的 specular  *   brdf.specular       
    float3 reflection = GISpecular * lerp(brdf.specular,brdf.fresnel,fresnelStrength);
    // 间接光的漫反射 ：  GI 的 diffuse  *  brdf.diffuse
    return (GIDiffuse * brdf.diffuse +  reflection) * surface.occlusion;
}


#endif