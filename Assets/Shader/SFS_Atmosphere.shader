Shader "SFS/Atmosphere" {
    Properties {
        _Version ("Version", Float) = 11
        _GradientTex ("Gradient", 2D) = "white" {}
        _CouldTex ("Clouds", 2D) = "white" {}
        _Alpha ("Alpha", Range(0,1)) = 1
        _GradientMultiplier ("Gradient Multiplier", Float) = 1
        _GradientHeight ("Gradient Height", Float) = 40000
        _CloudStartY ("Cloud Start Y", Float) = 0
        _CloudSizeY ("Cloud Size Y", Float) = 1
        _CloudSizeX ("Cloud Size X", Float) = 1
        _PosX ("Cloud Offset", Float) = 0
        _CloudMode ("Cloud Mode", Float) = 0
        _CloudBandStart ("Cloud Band Start T", Float) = 0
        _CloudBandWidth ("Cloud Band Width T", Float) = 0.05
        _GlowStrength ("Edge Glow", Range(0,2)) = 0.8
        _PP_Hue ("PP Hue", Float) = 0
        _PP_Saturation ("PP Saturation", Float) = 1
        _PP_Contrast ("PP Contrast", Float) = 1
        _PP_Multiplier ("PP Multiplier", Vector) = (1,1,1,1)
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
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float3 worldPos : TEXCOORD0; float2 uv : TEXCOORD1; };
            sampler2D _GradientTex;
            sampler2D _CouldTex;
            half _Alpha;
            half _GradientMultiplier;
            half _GradientHeight;
            half _CloudStartY;
            half _CloudSizeY;
            half _CloudSizeX;
            half _PosX;
            half _CloudMode;
            half _CloudBandStart;
            half _CloudBandWidth;
            half _GlowStrength;
            half _PP_Hue;
            half _PP_Saturation;
            half _PP_Contrast;
            half4 _PP_Multiplier;
            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = v.uv;
                return o;
            }
            fixed3 ApplyHueShift (fixed3 c, half shift) {
                half angle = shift * 3.14159265;
                half cosA = cos(angle);
                half sinA = sin(angle);
                half3 k = half3(0.57735, 0.57735, 0.57735);
                return c * cosA + cross(k, c) * sinA + k * dot(k, c) * (1.0 - cosA);
            }

            fixed4 frag (v2f i) : SV_Target {
                float3 center = float3(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13, unity_ObjectToWorld._m23);
                float scale = length(float3(unity_ObjectToWorld._m00, unity_ObjectToWorld._m10, unity_ObjectToWorld._m20));
                float M = max(_GradientMultiplier, 0.001);
                float h = scale / (M + 1.0);
                float R = scale - h;
                // 径向渐变：星球边缘(r=R/scale)浅蓝 -> 圆盘外缘(r=1)黑透明
                float2 localXY = (i.worldPos.xy - center.xy) / max(scale, 0.001);
                float r = length(localXY);
                float rPlanet = R / max(scale, 0.001);
                // 非线性曲线：边缘亮蓝快速衰减为深蓝，外缘转黑
                float t = saturate((r - rPlanet) / max(1.0 - rPlanet, 0.001));
                float v = pow(t, 0.5);
                fixed4 grad = tex2D(_GradientTex, float2(0.5, v));
                // 云层：u——默认星球与扩展包统一用极坐标镜像方向 + 原版环绕次数（_CloudSizeX）；
                // v：0=默认星球——贴图纵向内容压缩进 1250~4000m 高度带；
                //    1=扩展包全景（None渐变）——t 直接映射贴图高度
                float cloudAngle = atan2(localXY.y, localXY.x);
                float cloudU = 0.5 - cloudAngle / 6.2831853;
                // 云层带：按物理高度带（_CloudBandStart~+_CloudBandWidth），随视角缩放同步缩小
                float bandHalfT = _CloudBandWidth * 0.5;
                float bandCenterT = _CloudBandStart + _CloudBandWidth * 0.5;
                float bandT = saturate((t - bandCenterT + bandHalfT) / max(2.0 * bandHalfT, 0.0001));
                float cloudV = lerp(lerp(0.515, 0.9, bandT), t, _CloudMode);
                float2 cloudUV = float2(cloudU * _CloudSizeX + _PosX, cloudV);
                fixed4 clouds = tex2D(_CouldTex, cloudUV);
                // 云层强度：中间最强，上下两端缓慢淡出（仅默认星球）
                float bellFade = 1.0 - smoothstep(0.15, 0.45, abs(bandT - 0.5));
                clouds.a *= lerp(bellFade, 1.0, _CloudMode);
                fixed4 c = grad;
                c.rgb = lerp(c.rgb, clouds.rgb, saturate(clouds.a) * _Alpha);
                // 星球边缘辉光：向更蓝的浅天蓝 (140,199,255) 混合，避免青色渐变透出
                fixed3 glowCol = fixed3(0.55, 0.78, 1.0);
                // 辉光沿地平线向外衰减约 0~0.67（默认地球约 0~30km），500m 处保持约 98% 亮度
                float rim = pow(saturate(1.0 - v * 1.5), 1.5);
                // 辉光高度遮罩：用未缩放的真实米数（t × 渐变高度），缩放空间同样准确
                float glowAlt = t * _GradientHeight;
                float glowMask = 1.0 - smoothstep(450.0, 1200.0, glowAlt);
                c.rgb = lerp(c.rgb, glowCol, rim * _GlowStrength * glowMask);
                // 不做硬裁切（原版设计）：内缘由渐变贴图本身决定（不透明蓝），
                // 星球盘面由排在 Default/10 的地形在之后绘制盖住，避免黑缝。
                c.a = saturate(grad.a + clouds.a * _Alpha);
                // 原始分级公式：对比度 -> 饱和度 -> 色相 -> RGB 倍率
                c.rgb = (c.rgb - 0.5) * _PP_Contrast + 0.5;
                half luma = dot(c.rgb, fixed3(0.299, 0.587, 0.114));
                c.rgb = lerp(fixed3(luma, luma, luma), c.rgb, _PP_Saturation);
                c.rgb = ApplyHueShift(c.rgb, _PP_Hue);
                c.rgb *= _PP_Multiplier.rgb;
                // 高层大气降饱和：v 越大（越高层）饱和度越低
                half lumaH = dot(c.rgb, fixed3(0.299, 0.587, 0.114));
                c.rgb = lerp(fixed3(lumaH, lumaH, lumaH), c.rgb, 1.0 - saturate(v) * 0.5);
                return c;
            }
            ENDCG
        }
    }
}
