Shader "SFS/Sprite With Depth" {
	Properties {
		_MainTex ("Sprite", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
	}
	SubShader {
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
		LOD 200
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off
		CGPROGRAM
		#pragma surface surf Lambert alpha:fade
		sampler2D _MainTex;
		struct Input { float2 uv_MainTex; };
		fixed4 _Color;
		void surf (Input IN, inout SurfaceOutput o) {
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	}
}
