Shader "CustomRP/Unlit"
{
    Properties
    {
    
        _BaseMap("BaseMap",2D) = "white"{}
        [HDR]_BaseColor("Color",Color) = (1.0,1.0,1.0,1.0)
        _Cutoff("Alpha Cutoff",Range(0.0,1.0)) = 0.5
        [Toggle(_CLIPPING)]_Clipping("Alpha Clipping",Float) = 0
        [Toggle(_DATA)]_Data("Data",Float) = 0
        
        [Enum(UnityEngine.Rendering.BlendMode)]_SrcBlend("Src Blend",Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)]_DstBlend("Dst Blend",Float) = 0
        [Enum(Off,0,On,1)]_ZWrite("Z Write",Float) = 1

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

            #pragma multi_compile_instancing
            #pragma vertex UnlitPassVertex
            #pragma fragment UnlitPassFragment
            #include "UnlitPass.hlsl" 
            ENDHLSL
        }
    }
        CustomEditor "CustomShaderGUI"
}
