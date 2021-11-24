Shader "Hidden/CustomRP/PostFXStack"
{
    SubShader
    {
    
        Cull Off
        ZTest Always
        Zwrite Off
                    
            HLSLINCLUDE
            #include "../ShaderLibrary/Common.hlsl"
            #include "PostFXStackPasses.hlsl"
            ENDHLSL

        
        
        Pass
        {
            Name "Bloom Vertical"        
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomVerticalPassFragment            
            ENDHLSL
        }    
//            
        Pass
        {
            Name "Bloom Horizontal"        
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomHorizontalPassFragment            
            ENDHLSL
        }
        
        Pass
        {
            Name "Bloom Add"        
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomCombinePassFragment            
            ENDHLSL
        }
                    
        Pass
        {
            Name "Bloom Prefilter"        
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomprefilterPassFragment            
            ENDHLSL
        }
        
        Pass
        {
            Name "Bloom Prefilter Firefiles"        
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomPrefilterFirefilesPassFragment            
            ENDHLSL
        }
        
         Pass
        {
            Name "Bloom Scatter"        
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomScatterPassFragment            
            ENDHLSL
        }
        
            
            
         Pass
        {
            Name "Bloom Scatter Final"        
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomScatterFinalFragment            
            ENDHLSL
        }
        
        
        Pass
        {
            Name "Copy"        
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment CopyPassFragment            
            ENDHLSL
        }
       
        
         
        
    }
}
