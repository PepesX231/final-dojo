// DREAM RUSH — FantasySky: painterly golden-hour sky (Final Fantasy field spirit).
// Zenith→horizon gradient with a warm sunset band, sun disc + wide halo toward _SunDir, soft cumulus layer
// (fbm projected on a dome) lit from the sun with silver linings, faint stars near the zenith, a second moon.
Shader "DreamRush/FantasySky"
{
    Properties
    {
        _Zenith ("Zenith", Color) = (0.20, 0.32, 0.68, 1)
        _Horizon ("Horizon", Color) = (0.98, 0.74, 0.52, 1)
        _Band ("Sunset Band", Color) = (1.0, 0.55, 0.45, 1)
        _Ground ("Below Horizon", Color) = (0.35, 0.30, 0.45, 1)
        _SunDir ("Sun Direction (to sun)", Vector) = (-0.4, 0.18, 0.9, 0)
        [HDR] _SunColor ("Sun", Color) = (4, 3, 2, 1)
        _SunSize ("Sun Size", Range(0.9,0.9999)) = 0.9985
        _HaloStrength ("Halo", Range(0,3)) = 1.1
        _CloudColor ("Cloud", Color) = (1, 0.96, 0.92, 1)
        _CloudShade ("Cloud Shade", Color) = (0.62, 0.52, 0.72, 1)
        _CloudCover ("Cloud Cover", Range(0,1)) = 0.48
        _CloudScale ("Cloud Scale", Float) = 1.6
        _CloudSpeed ("Cloud Speed", Float) = 0.006
        _StarStrength ("Stars", Range(0,2)) = 0.35
        _MoonDir ("Moon Direction", Vector) = (0.5, 0.45, -0.6, 0)
        [HDR] _MoonColor ("Moon", Color) = (1.4, 1.35, 1.6, 1)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Zenith, _Horizon, _Band, _Ground, _SunColor, _CloudColor, _CloudShade, _MoonColor;
                float4 _SunDir, _MoonDir;
                float _SunSize, _HaloStrength, _CloudCover, _CloudScale, _CloudSpeed, _StarStrength;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; };
            struct V { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };
            V vert(A v) { V o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); o.dir = v.positionOS.xyz; return o; }

            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float noise(float2 p)
            {
                float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f);
                return lerp(lerp(hash(i), hash(i + float2(1, 0)), f.x), lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), f.x), f.y);
            }
            float fbm(float2 p) { float s = 0, a = 0.5; for (int k = 0; k < 5; k++) { s += noise(p) * a; p = p * 2.03 + 17.1; a *= 0.5; } return s; }

            half4 frag(V i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float3 sun = normalize(_SunDir.xyz);
                float h = d.y;
                // sky gradient
                half3 col = lerp(_Horizon.rgb, _Zenith.rgb, pow(saturate(h), 0.55));
                float toSun = dot(d, sun);
                col = lerp(col, _Band.rgb, saturate(1 - abs(h) * 6) * (0.35 + 0.5 * saturate(toSun)));
                if (h < 0) col = lerp(col, _Ground.rgb, saturate(-h * 4));
                // halo + sun disc
                col += _SunColor.rgb * pow(saturate(toSun), 12) * 0.12 * _HaloStrength;
                col += _SunColor.rgb * pow(saturate(toSun), 200) * 0.6 * _HaloStrength;
                col += _SunColor.rgb * smoothstep(_SunSize, _SunSize + 0.0006, toSun);
                // moon
                float3 moon = normalize(_MoonDir.xyz);
                float md = dot(d, moon);
                col = lerp(col, _MoonColor.rgb, smoothstep(0.9993, 0.9996, md));
                col += _MoonColor.rgb * pow(saturate(md), 400) * 0.25;
                // stars (zenith only, faded by the sun)
                float2 sp = d.xz / max(d.y, 0.05) * 60;
                float st = step(0.996, hash(floor(sp))) * smoothstep(0.25, 0.8, h) * (0.6 + 0.4 * sin(_Time.y * 2 + hash(floor(sp)) * 40));
                col += st * _StarStrength;
                // clouds on a dome
                if (h > 0.0)
                {
                    float2 uv = d.xz / (h + 0.18) * _CloudScale + _Time.y * _CloudSpeed * float2(1, 0.3);
                    float c = fbm(uv);
                    float cover = smoothstep(1 - _CloudCover, 1 - _CloudCover + 0.25, c);
                    float thick = fbm(uv + sun.xz * 0.12);                    // shift toward the sun = self-shadow
                    half3 cc = lerp(_CloudColor.rgb, _CloudShade.rgb, saturate((thick - c) * 4 + 0.35));
                    cc += _SunColor.rgb * pow(saturate(toSun), 6) * 0.25 * (1 - cover * 0.6);   // silver lining near the sun
                    col = lerp(col, cc, cover * smoothstep(0.0, 0.12, h) * 0.95);
                }
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
