Shader "SFS/Part 2" {
    Properties {
        _ColorTex ("Color Tex", 2D) = "white" {}
        _NormalTex ("Normal Tex", 2D) = "bump" {}
        _Color ("Color", Color) = (1,1,1,1)
        _AmbientLight ("Ambient Light", Float) = 0.25
        _LightNormal ("Light Normal", Vector) = (0,0,1,0)
        _UseNormals ("Use Normals", Float) = 0
        _Intensity ("Shade Intensity", Float) = 0
        _BurnMarkTex ("Burn Stripes", 2D) = "white" {}
        _GradientTex ("Gradient", 2D) = "white" {}
        _DepthStart ("Depth Start", Float) = 0
        _DepthM ("Depth M", Float) = 0
    }
    SubShader {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100
        Pass {
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
                float4 texcoord3 : TEXCOORD3;
            };
            struct v2f {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldTangent : TEXCOORD2;
                float3 worldBinormal : TEXCOORD3;
                float3 lightDir : TEXCOORD4;
            };
            sampler2D _ColorTex;
            sampler2D _NormalTex;
            fixed4 _Color;
            fixed4 _LightNormal;
            half _AmbientLight;
            half _UseNormals;
            half _DepthStart;
            half _DepthM;
            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
                o.worldBinormal = cross(o.worldNormal, o.worldTangent) * v.tangent.w;
                o.lightDir = normalize(mul((float3x3)unity_ObjectToWorld, _LightNormal.xyz));
                return o;
            }
            fixed4 frag (v2f i) : SV_Target {
                fixed4 c = tex2D(_ColorTex, i.uv) * i.color * _Color;
                // 大楼贴图为带透明通道的抠图，透明部分直接丢弃
                clip(c.a - 0.5);
                if (_UseNormals > 0.5)
                {
                    fixed3 n = UnpackNormal(tex2D(_NormalTex, i.uv));
                    float3 N = normalize(i.worldTangent * n.x + i.worldBinormal * n.y + i.worldNormal * n.z);
                    float ndl = saturate(dot(N, normalize(i.lightDir)));
                    c.rgb *= lerp(_AmbientLight, 1.0, ndl);
                }
                c.a = 1;
                return c;
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
