Shader "SFS/Reentry" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _Emission ("Emission", 2D) = "white" {}
        _Streaks ("Streaks", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Temperature ("Temperature", Range(0,1)) = 0
        _TemperaturePercent ("Temperature Percent", Range(0,1)) = 0
        _TemperatureShift ("Temperature Shift", Float) = 1
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
            sampler2D _Emission;
            sampler2D _Streaks;
            fixed4 _Color;
            half _Temperature;
            half _TemperaturePercent;
            half _TemperatureShift;
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
                fixed4 tex = tex2D(_MainTex, i.uv);
                fixed4 streaks = tex2D(_Streaks, i.uv);
                float t = saturate(pow(_Temperature, max(0.01, _TemperatureShift)) + _TemperaturePercent);
                fixed3 tempColor = tex2D(_Emission, float2(t, 0.5)).rgb;
                float density = tex.r * (1.0 + streaks.r * 2.0);
                fixed4 c = fixed4(tempColor * density * _Color.rgb * _Multiplier, density * _Opacity);
                c.a *= i.color.a * i.color.b;
                return c;
            }
            ENDCG
        }
    }
}