Shader "Hidden/UI/TranslucentImage (SoftMaskable)" {
	Properties {
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		[HideInInspector] _BlurTex ("Blur Texture", 2D) = "gray" {}
		_Vibrancy ("Vibrancy", Float) = 1
		_Brightness ("Brightness", Float) = 0
		_Flatten ("Flatten", Float) = 0
		_SoftMaskTex ("Soft Mask Texture", 2D) = "white" {}
		_Softness ("Softness", Float) = 0
		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255
		_ColorMask ("Color Mask", Float) = 15
		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
	}
	SubShader {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
		Stencil {
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp]
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}
		Cull Off
		Lighting Off
		ZWrite Off
		ZTest [unity_GUIZTestMode]
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask [_ColorMask]
		Pass {
			Name "Default"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#include "UnityCG.cginc"
			#include "UnityUI.cginc"
			#pragma multi_compile __ UNITY_UI_ALPHACLIP

			struct appdata_t {
				float4 vertex : POSITION;
				float4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				float2 texcoord1 : TEXCOORD1; // 软遮罩坐标
			};
			struct v2f {
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				float2 maskUV : TEXCOORD1;
				float4 worldPosition : TEXCOORD2;
			};

			sampler2D _MainTex;
			sampler2D _BlurTex;
			sampler2D _SoftMaskTex;
			float _Vibrancy;
			float _Brightness;
			float _Flatten;
			float _Softness;
			fixed4 _Color;
			float4 _ClipRect;
			float2 _UIMaskSoftnessX, _UIMaskSoftnessY;

			v2f vert (appdata_t IN) {
				v2f OUT;
				OUT.worldPosition = IN.vertex;
				OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
				OUT.texcoord = IN.texcoord;
				OUT.color = IN.color * _Color;
				OUT.maskUV = IN.texcoord1;
				return OUT;
			}

			fixed4 frag (v2f IN) : SV_Target {
				// 普通半透明面板：贴图 x 颜色（不使用毛玻璃模糊）
				fixed4 color = tex2D(_MainTex, IN.texcoord) * IN.color;
				color.a = lerp(color.a, 1.0, saturate(_Flatten));
				color.rgb = (color.rgb - 0.5) * _Vibrancy + 0.5;
				color.rgb += _Brightness;
				#ifdef UNITY_UI_ALPHACLIP
				clip(color.a - 0.001);
				#endif
				return color;
			}
			ENDCG
		}
	}
}
