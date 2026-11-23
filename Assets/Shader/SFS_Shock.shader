Shader "SFS/Shock" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _Vertical ("Vertical", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Opacity ("Opacity", Range(0,2)) = 1
        _Multiplier ("Multiplier", Float) = 1
    }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; fixed4 color : COLOR; };
            sampler2D _MainTex;
            sampler2D _Vertical;
            fixed4 _Color;
            half _Opacity;
            half _Multiplier;
            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target {
                float gradient = tex2D(_MainTex, i.uv).r;
                float layers = tex2D(_Vertical, i.uv * 2.0).r;
                float density = gradient * (0.35 + 0.65 * layers);
                fixed4 c = fixed4(_Color.rgb * density * _Multiplier, density * _Opacity);
                c.a *= i.color.a * i.color.b;
                return c;
            }
            ENDCG
        }
    }
}