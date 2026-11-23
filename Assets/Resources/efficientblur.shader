Shader "Hidden/EfficientBlur" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader {
		Cull Off
		ZWrite Off
		ZTest Always

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f {
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_TexelSize;
			float size;
			float4 _CropRegion;

			v2f vert(appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target {
				float2 uv = lerp(_CropRegion.xy, _CropRegion.zw, i.uv);
				float2 o = _MainTex_TexelSize.xy * size;
				half4 c = tex2D(_MainTex, uv) * 4.0;
				c += tex2D(_MainTex, uv + float2(o.x, 0.0));
				c += tex2D(_MainTex, uv - float2(o.x, 0.0));
				c += tex2D(_MainTex, uv + float2(0.0, o.y));
				c += tex2D(_MainTex, uv - float2(0.0, o.y));
				c += tex2D(_MainTex, uv + float2(o.x, o.y));
				c += tex2D(_MainTex, uv - float2(o.x, o.y));
				c += tex2D(_MainTex, uv + float2(o.x, -o.y));
				c += tex2D(_MainTex, uv - float2(o.x, -o.y));
				return c / 12.0;
			}
			ENDCG
		}
	}
}
