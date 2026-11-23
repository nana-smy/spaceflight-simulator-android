Shader "SFS/Terrain" {
    Properties {
        _PlanetTexture ("Planet", 2D) = "white" {}
        _TextureA ("Texture A", 2D) = "white" {}
        _TextureB ("Texture B", 2D) = "white" {}
        _TextureTerrain ("Terrain", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Fog ("Fog", Color) = (0,0,0,0)
        _ShadowIntensity ("Shadow Intensity", Range(0,10)) = 0.5
        _ShadowSize ("Shadow Size", Float) = 100
        _RepeatA ("Repeat A", Vector) = (10,10,0,0)
        _RepeatB ("Repeat B", Vector) = (10,10,0,0)
        _RepeatTerrain ("Repeat Terrain", Vector) = (10,10,0,0)
        _SurfaceSize ("Surface Size", Float) = 1
        _Min ("Min Fade", Float) = 0
        _Max ("Max Fade", Float) = 1
    }
    SubShader {
        // 透明队列 + 写入深度：地形在星星(Back/-2)之后绘制，星球盘面会盖住背后的星星；
        // 同时保留深度写入，保证不同 LOD 地形块之间以及前后景遮挡正常。
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Pass {
            Cull Off
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata {
                float4 vertex : POSITION;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
                float3 normal : NORMAL;
            };
            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uvPlanet : TEXCOORD0;
                float2 uvData : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                float3 worldPos : TEXCOORD3;
            };
            sampler2D _PlanetTexture;
            sampler2D _TextureA;
            sampler2D _TextureB;
            sampler2D _TextureTerrain;
            fixed4 _Color;
            fixed4 _Fog;
            half _ShadowIntensity;
            half _SurfaceSize;
            half _Min;
            half _Max;
            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uvPlanet = v.uv1;
                o.uvData = v.uv2;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target {
                // 地图模式（网格无 UV）：直接输出 mapColor 纯色
                if (length(i.uvPlanet) < 0.01)
                {
                    return fixed4(_Color.rgb, 1);
                }
                fixed4 planet = tex2D(_PlanetTexture, i.uvPlanet);
                float2 surfaceUV = i.worldPos.xy / max(_SurfaceSize, 0.001);
                fixed4 texA = tex2D(_TextureA, surfaceUV);
                fixed4 texB = tex2D(_TextureB, surfaceUV);
                fixed4 texT = tex2D(_TextureTerrain, surfaceUV * 2.0);
                fixed3 c = planet.rgb;
                float slope = saturate(abs(i.uvData.x));
                fixed3 surface = lerp(texA.rgb, texB.rgb, saturate(slope * 3.0));
                c = lerp(c, surface, 0.15);
                float texSample = saturate(i.uvData.y);
                float fade = smoothstep(_Min, _Max, texSample);
                c = lerp(c, texT.rgb, fade * 0.40);
                // 原始游戏地形无方向性变暗：恒定亮度
                float light = 1.0;
                c *= light;
                // 雾色弱化（原始数据 alpha 最高 1.0，直接混合会把贴图冲成纯色）
                c = lerp(c, _Fog.rgb, saturate(_Fog.a * 0.15));
                return fixed4(c, 1);
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
