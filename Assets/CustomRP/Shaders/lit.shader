Shader "CustomRP/Lit"
{
    Properties
    {
        [HideInInspector] _MainTex("Texture for Lightmap",2D) = "white"{}
        [HideInInspector] _Color ("Color for Lightmap",Color) = (0.5,0.5,0.5,0.5)
        [Toggle(_MASK_MAP)]_MaskMapToggle("MaskMap",Float) = 0
        
        [NoScaleOffset]_MaskMap("Mask (mods)",2D) = "white"{}
        _Metallic("Metallic",Range(0,1)) = 0
        _Occlusion("Occlusiong",Range(0,1)) = 1
        _Smoothness("_Smoothness",Range(0,1)) = 0.5
        _Fresnel("Fresnel",Range(0,1)) = 1
        _BaseMap("BaseMap",2D) = "white"{}
        _BaseColor("Color",Color) = (0.5,0.5,0.5,1.0)
        _Cutoff("Alpha Cutoff",Range(0.0,1.0)) = 0.5
        
        // 因为法线 很昂贵，在不用的时候  不启用
        [Toggle(_NORMAL_MAP)]_NormalMapToggle("Normal Map",Float) = 0
        [NoScaleOffset]_NormalMap("Normal",2D) = "white"{}
        _NormalScale("Normal scale",Range(0,1)) = 1
        
        
        [NoScaleOffset]_EmissionMap("Emission",2D) = "white"{}
        [HDR]_EmissionColor("EmissionColor",Color) = (0.0,0.0,0.0,0.0)
        [Toggle(_DetailMap)]_DetailMapToggle("DetailMap",Float) = 0
        _DetailMap("Details",2D) = "linearGrey"{}
        _DetailAlbedo("Details Albedo",Range(0,1)) = 1
        _DetailSmoothness("Detail Smoothness",Range(0,1)) = 1
        _DetailNormal("_DetailNormal",2D) = "bump" {}
        _DetailNormalScale("_DetailNormalScale",Range(0,1)) = 1        
        [Toggle(_CLIPPING)]_Clipping("Alpha Clipping",Float) = 0
        // 投影模式
        [KeywordEnum(on,Clip,Dither,Off)]_Shadows("Shadows",Float) = 0
        [Toggle(_RECEIVE_SHADOWS)]_Receive_Shadows("Receive Shadows",Float) = 1
        [Toggle(_PREMULTIPLY_ALPHA)]_PremulAlpha("Premultiply alpha",Float) = 0
        
        [Enum(UnityEngine.Rendering.BlendMode)]_SrcBlend("Src Blend",Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)]_DstBlend("Dst Blend",Float) = 0
        [Enum(Off,0,On,1)]_ZWrite("Z Write",Float) = 1
        
    
    }
    SubShader
    {
        HLSLINCLUDE
        #include "../ShaderLibrary/Common.hlsl"
        #include "LitInput.hlsl"
        ENDHLSL
        Pass
        {
            Tags{ "LightMode" = "CustomLit"}
            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]
            HLSLPROGRAM
            #pragma target 3.5
            #pragma shader_feature _CLIPPING
            
            // 是否混合透明通道
            #pragma shader_feature _PREMULTIPLY_ALPHA
            #pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
            
            #pragma shader_feature _RECEIVE_SHADOWS
            #pragma multi_compile _ _CASCADE_BLEND_SOFT _CASCADE_BLEND_DITHER      
            #pragma multi_compile _ _SHADOW_MASK_ALWAYS _SHADOW_MASK_DISTANCE
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_instancing
            // lod 的过渡
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            
            //
            #pragma shader_feature _NORMAL_MAP;
            #pragma shader_feature _MaskMapToggle;
            
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment
            #include "LitPass.hlsl" 
            ENDHLSL
        }
        
        Pass
        {
            Tags{ "LightMode" = "ShadowCaster"}
            ColorMask 0
            HLSLPROGRAM
            #pragma target 3.5
            //#pragma shader_feature _CLIPPING    
            #pragma shader_feature _ _SHADOWS_CLIP _SHADOWS_DITHER  
            // lod 的过渡
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_instancing
            #pragma vertex ShadowCasterPassVertex
            #pragma fragment ShadowCasterPassFragment
            #include "ShadowCasterPass.hlsl" 
            ENDHLSL
        }
        
        Pass
        {
            // 在烘焙中使用此pass,  此pass 决定了间接光的烘焙效果
            Tags{ "LightMode"="Meta"}
            Cull Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex MetaPassVertex
            #pragma fragment MetaPassFragment
            #include "MetaPass.hlsl"          
            ENDHLSL
        
        }        
    }
    CustomEditor "CustomShaderGUI"
}
