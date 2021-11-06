Shader "Unlit/ShadowTest01"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Cutoff("cut off",range(0.0,1.0)) = 0.5
    }
    SubShader
    {
        Tags{"RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "UniversalMaterialType" = "Lit" "IgnoreProjector" = "True" "ShaderModel"="4.5"}

        LOD 100

        Pass
        {
            Tags{"LightMode" = "UniversalForward"}
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag       
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ParallaxMapping.hlsl"
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            CBUFFER_START(UnityPerMaterial)
            half4 _MainTex_ST;
            half _Cutoff;
            CBUFFER_END
            TEXTURE2D(_MainTex);     
            SAMPLER(sampler_MainTex);

//////
//////-----------------------------------
// float4 GetShadowCoord(VertexPositionInputs vertexInput)
// {
//     return mul(_PerObjectWorldToShadow, float4(vertexInput.positionWS, 1.0));
// }
//////-----------------------------------
//////

            v2f vert (appdata v)
            {
                v2f o;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex);
                o.vertex = vertexInput.positionCS;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex,i.uv);
                half alpha = col.a - _Cutoff;
                half4 endCol = half4(alpha,alpha,alpha,1);
                return endCol;
            }
            ENDHLSL
        }
  Pass
        {
            Name "ShadowCaster"
            Tags { "RenderType" = "AlphaTest" "LightMode" = "ShadowCaster" }
            Cull Off
            ZWrite On
            ZTest LEqual
            
            HLSLPROGRAM            
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            
            float3 _LightDirection;
            //为了让shader的SRP Batcher能够使用，所以每个pass的Cbuffer都要保持一直。（应该是这样吧）
            // CBUFFER_START(UnityPerMaterial)
            //     float4 _MainTex_ST;
            //     half _Cutoff;
            // CBUFFER_END
            float4 _MainTex_ST;
            struct Attributes
            {
                float4 positionOS: POSITION;
                float3 normalOS: NORMAL;
                float2 texcoord: TEXCOORD0;
            };
            
            struct Varyings
            {
                float2 uv: TEXCOORD0;
                float4 positionCS: SV_POSITION;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            // 获取裁剪空间下的阴影坐标
            float4 GetShadowPositionHClips(Attributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));                
                return positionCS;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.uv = TRANSFORM_TEX(input.texcoord, _MainTex);
                output.positionCS = GetShadowPositionHClips(input);
                return output;
            }
            
            
            half4 frag(Varyings input): SV_TARGET
            {
                half4 albedoAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                //这里要是否需要裁剪透明通道。现在BASE是Opaque渲染模式，没有裁剪。需要用的自己改一下。
                clip(albedoAlpha.a - _Cutoff);
                return 0;
            }     
            ENDHLSL            
        }

}
}