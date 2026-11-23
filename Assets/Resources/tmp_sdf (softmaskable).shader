Shader "Hidden/TextMeshPro/Distance Field (SoftMaskable)" {
	Properties {
		_FaceColor ("Face Color", Color) = (1, 1, 1, 1)
		_FaceDilate ("Face Dilate", Range(-1, 1)) = 0
		_OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
		_OutlineWidth ("Outline Thickness", Range(0, 1)) = 0
		_MainTex ("Font Atlas", 2D) = "white" {}
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
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
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
			#pragma multi_compile __ UNITY_UI_ALPHACLIP

			struct appdata_t {
				float4 vertex : POSITION;
				float4 color : COLOR;
				float2 uv : TEXCOORD0;
				float2 uv1 : TEXCOORD1; // 软遮罩坐标
			};
			struct v2f {
				float4 vertex : SV_POSITION;
				float4 color : COLOR;
				float2 uv : TEXCOORD0;
				float2 maskUV : TEXCOORD1;
			};

			sampler2D _MainTex;
			sampler2D _SoftMaskTex;
			float4 _FaceColor;
			float _FaceDilate;
			float4 _OutlineColor;
			float _OutlineWidth;
			float _Softness;

			v2f vert (appdata_t IN) {
				v2f OUT;
				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.color = IN.color;
				OUT.uv = IN.uv;
				OUT.maskUV = IN.uv1;
				return OUT;
			}

			fixed4 frag (v2f IN) : SV_Target {
				// SDF 值在字模纹理的 alpha 通道（0.5 = 字面边缘）
				float d = tex2D(_MainTex, IN.uv).a;
				float halfpx = 0.02; // 抗锯齿过渡宽度（简化）
				float face = smoothstep(0.5 - _FaceDilate - halfpx, 0.5 - _FaceDilate + halfpx, d);
				float outline = smoothstep(0.5 - _FaceDilate - _OutlineWidth - halfpx, 0.5 - _FaceDilate - _OutlineWidth + halfpx, d);
				fixed4 color = _FaceColor * IN.color;
				color.rgb = lerp(_OutlineColor.rgb, color.rgb, face);
				color.a = lerp(outline, color.a * face, face);
				#ifdef UNITY_UI_ALPHACLIP
				clip(color.a - 0.001);
				#endif
				return color;
			}
			ENDCG
		}
	}
}
