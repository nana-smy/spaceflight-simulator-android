Shader "SFS/Part" {
    Properties {
        _Intensity ("Shade Intensity", Float) = 0
        _BurnMarkTex ("Burn Stripes", 2D) = "white" {}
        _GradientTex ("Gradient", 2D) = "white" {}
    }
    SubShader {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Cull Off
        Lighting Off
        Blend One OneMinusSrcAlpha
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnityCG.cginc"
            struct appdata_t {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float3 UVQ_0 : TEXCOORD0;
                float3 UVQ_1 : TEXCOORD1;
                float3 UVQ_2 : TEXCOORD2;
                float3 depth : TEXCOORD3;
                float3 burnUV : TEXCOORD4;
                float3 shading : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float3 UVQ_0 : TEXCOORD0;
                float3 UVQ_1 : TEXCOORD1;
                float3 UVQ_2 : TEXCOORD2;
                float3 depth : TEXCOORD3;
                float3 burnUV : TEXCOORD4;
                float3 shading : TEXCOORD5;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            v2f vert (appdata_t IN) {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.color = IN.color;
                OUT.UVQ_0 = IN.UVQ_0;
                OUT.UVQ_1 = IN.UVQ_1;
                OUT.UVQ_2 = IN.UVQ_2;
                OUT.depth = IN.depth;
                OUT.burnUV = IN.burnUV;
                OUT.shading = IN.shading;
                return OUT;
            }
            // Part（与原版一致：颜色贴图 × 顶点色，形状/阴影/着色走 albedo 公式）
            sampler2D _ColorTexture, _ShapeTexture, _ShadowTexture;
            float _Intensity;
            // Depth（原版：片元写 SV_Depth，深度值越大越靠前）
            float _BaseDepth, _DepthMultiplier;
            // Burn（再入烧灼）
            sampler2D _BurnMarkTex, _GradientTex;
            float _MaxSutBlackness;
            float _Opacity;
            sampler2D _Offset;
            #if defined(SHADER_API_GLES)
            fixed4 frag (v2f IN) : SV_Target
            #else
            fixed4 frag (v2f IN, out float depth : SV_Depth) : SV_Target
            #endif
            {
                // IN.depth: x = t_x, y = width, z = 1
                float x = abs(IN.depth.x / max(IN.depth.z, 0.0001) * 2 - 1);
                // Depth
                #if defined(SHADER_API_GLES)
                #else
                    float _depth = _BaseDepth + (1 - x) * (IN.depth.y / max(IN.depth.z, 0.0001)) * _DepthMultiplier;
                    #if defined(UNITY_REVERSED_Z)
                    depth = _depth;
                    #else
                    depth = 1.0f - _depth;
                    #endif
                #endif
                // Color texture // Vertex color
                fixed4 c = tex2D(_ColorTexture, IN.UVQ_0.xy / max(IN.UVQ_0.z, 0.0001)) * IN.color;
                // Shape texture // Shadow texture // Shading
                float albedo = tex2D(_ShapeTexture, IN.UVQ_1.xy / max(IN.UVQ_1.z, 0.0001)).r * 1.9 * (1 - (1 - tex2D(_ShadowTexture, IN.UVQ_2.xy / max(IN.UVQ_2.z, 0.0001)).r) * _Intensity) * (1 + (IN.shading.x / max(IN.shading.z, 0.0001)) * x);
                c.rgb *= albedo;
                // Burn marks
                if (_Opacity > 0) {
                    fixed4 offset = tex2D(_Offset, float2(IN.burnUV.z, 0));
                    float2 uv = float2(IN.burnUV.x, (IN.burnUV.y - (offset.r * 2 - 1) * 10) / (offset.g * 12) + 1);
                    fixed output = tex2D(_BurnMarkTex, uv).r * _Opacity;
                    fixed4 burn = tex2D(_GradientTex, float2(output, 0));
                    c.rgb = (c.rgb * (1 - burn.a)) + (burn.rgb * burn.a);
                }
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
