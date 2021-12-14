Shader "CustomRP/Particles/Unlit"
{
    Properties
    {
    
        _BaseMap("BaseMap",2D) = "white"{}
        [HDR]_BaseColor("Color",Color) = (1.0,1.0,1.0,1.0)
        _Cutoff("Alpha Cutoff",Range(0.0,1.0)) = 0.5
        [Toggle(_CLIPPING)]_Clipping("Alpha Clipping",Float) = 0
        
        [Enum(UnityEngine.Rendering.BlendMode)]_SrcBlend("Src Blend",Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)]_DstBlend("Dst Blend",Float) = 0
        [Enum(Off,0,On,1)]_ZWrite("Z Write",Float) = 1
        [Toggle(_VERTEX_COLORS)]_VertexColor("Vertext Color",Float) = 1
        [Toggle(_FLIPBOOK_BLENDING)]_FilpbookBlending("Flipbook Blending",Float) = 0 
        [Toggle(_NEAR_FADE)]_NearFade("Near Fade",Float) = 0
        _NearFadeDistance("Near Fade Distance",Range(0.0,10.0)) = 1
        _NearFadeRange("Near Fade Range",Range(0.0,10.0)) = 1

        [Toggle(_SOFT_PARTICLES)]_SoftParticles ("SoftParticles",Float) = 0
        _SoftParticlesDistance("Soft Particles Distance",Range(0.0,10.0)) = 0
        _SoftParticlesRange("Soft Particles Range",Range(0.01,10.0)) = 1

        [Toggle(DISTORTION)]_Distortion("Distortion",Float) = 0
        [NoScaleOffset]_DistortionMap("DistortionMap",2D) = "bump"{}
        _DistortionStrength("Distortion Strength",Range(0.0,0.2)) = 0.1
        _DistortionBlend("Distortion Blend",Range(0.0,1.0)) = 0.1
        

    }
    SubShader
    {
        HLSLINCLUDE
        #include "../ShaderLibrary/Common.hlsl"
        #include "UnlitInput.hlsl"
        ENDHLSL
        Pass
        {
            Blend[_SrcBlend][_DstBlend],One OneMinusSrcAlpha
            ZWrite[_ZWrite]
            HLSLPROGRAM
            #pragma shader_feature _CLIPPING
            #pragma shader_feature _VERTEX_COLORS
            #pragma shader_feature _FLIPBOOK_BLENDING
            #pragma shader_feature _NEAR_FADE
            #pragma shader_feature _SOFT_PARTICLES
            #pragma shader_feature _DISTORTION
            #pragma multi_compile_instancing
            #pragma vertex UnlitPassVertex
            #pragma fragment UnlitPassFragment
            #include "UnlitPass.hlsl" 
            ENDHLSL
        }
    }
        CustomEditor "CustomShaderGUI"
}
