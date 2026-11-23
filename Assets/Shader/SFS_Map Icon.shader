Shader "SFS/Map Icon" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _CamDistance ("Camera Distance", Float) = 0.19
    }
    SubShader {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
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
            fixed4 _Color;
            float _CamDistance;
            v2f vert (appdata v) {
                v2f o;
                // 图标中心与角点（世界空间，含物体缩放）
                float3 center = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                float3 corner = mul(unity_ObjectToWorld, v.vertex).xyz - center;
                // 中心移到相机前方固定距离（在地形之前），角点保留世界偏移，
                // 使图标大小/旋转由 transform 决定，屏幕上大小恒定。
                float3 dir = normalize(center - _WorldSpaceCameraPos);
                if (dot(dir, dir) < 1e-6) dir = float3(0, 0, 1);
                float fixedDist = max(_CamDistance, 1.0);
                // 固定屏幕尺寸：UV 线性映射到四边形，并跟随图标的变换旋转（火箭航向）
                float3 right = normalize(mul((float3x3)unity_ObjectToWorld, float3(1, 0, 0)));
                float3 up = normalize(mul((float3x3)unity_ObjectToWorld, float3(0, 1, 0)));
                // 原版图标尺寸：精灵 1.3×1.59 × 受控缩放 0.016 → 0.0208×0.0254（角大小即此值）
                float sx = (v.uv.x - 0.5) * 0.0208;
                float sy = (v.uv.y - 0.5) * 0.0254;
                float3 worldPos = _WorldSpaceCameraPos + dir * fixedDist + right * sx + up * sy;
                o.vertex = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target {
                return tex2D(_MainTex, i.uv) * i.color;
            }
            ENDCG
        }
    }
}
