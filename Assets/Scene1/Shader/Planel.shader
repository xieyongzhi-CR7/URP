Shader "Custom/ToonGroundExam"
{
	Properties
	{
		_MainTex("主贴图", 2D) = "white" {}
        _CutOff("cutoffAlpha",Range(0,1.0)) = 0.5
	}
		SubShader
		{
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			Tags
			{
				"LightMode" = "UniversalForward"
			}
			Zwrite off
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile _ Anti_Aliasing_ON
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 
			struct appdata
			{
				float4 vertex : POSITION;
				float4 uv : TEXCOORD0;
 
			};
 
			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 worldPos : TEXCOORD1;
			};
 
			sampler2D _MainTex;
			float4 _MainTex_ST;
 
			v2f vert(appdata v)
			{
				v2f o;
				o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
 
				return o;
			}
 
			float4 _Color;
 
			float4 frag(v2f i) : SV_Target
			{
				float4 SHADOW_COORDS = TransformWorldToShadowCoord(i.worldPos);
 
				Light mainLight = GetMainLight(SHADOW_COORDS);
				half shadow = MainLightRealtimeShadow(SHADOW_COORDS);
                //half shadow = 1.0;
                float3 clor =  tex2D(_MainTex,i.uv);
				//return float4(clor * shadow ,1.0);
                return float4(shadow, shadow, shadow,1);
			}
			ENDHLSL
		}
		pass {
			Name "ShadowCast"
 
			Tags{ "LightMode" = "ShadowCaster" }
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 
				struct appdata
			{
				float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
			};
 
			struct v2f
			{
				float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
			};
 
			sampler2D _MainTex;
			float4 _MainTex_ST;
            half _CutOff;
			v2f vert(appdata v)
			{
				v2f o;
				o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = v.uv;
                return o;
			}
			float4 frag(v2f i) : SV_Target
			{
				float4 color;
				color.xyz = float3(0.0, 0.0, 0.0);
                half alpha =  tex2D(_MainTex,i.uv).a;
                clip(alpha -_CutOff);			
                return 0;
			}
			ENDHLSL
		}
	}
}