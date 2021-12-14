Shader "Hidden/CustomRP/Camera Renderer"
{
    SubShader
    {
    
        Cull Off
        ZTest Always
        Zwrite Off
            HLSLINCLUDE
            #include "../ShaderLibrary/Common.hlsl"
            #include "CameraRendererPasses.hlsl"
            ENDHLSL

        
        
        Pass
        {
            Name "Copy"        
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment CopyPassFragment            
            ENDHLSL
        }      
        
        Pass
        {
            Name "Copy Depth"       
            ColorMask 0
            Zwrite On 
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment CopyDepthPassFragment            
            ENDHLSL
        }      
    }
}
