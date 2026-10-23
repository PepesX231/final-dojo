// MAMA'S COMING — ToonSky: banded comic sky with two-tone puffy clouds, a sun/moon with halo rings,
// halftone horizon glow and optional stars. One shader, a material per level (day: golden hour, witch: moonlit).
Shader "MamasComing/ToonSky"
{
    Properties
    {
        _Top ("Zenith", Color) = (0.22, 0.42, 0.86, 1)
        _Mid ("Upper sky", Color) = (0.45, 0.66, 0.98, 1)
        _Horizon ("Horizon", Color) = (1.0, 0.80, 0.55, 1)
        _Ground ("Below horizon", Color) = (0.45, 0.36, 0.52, 1)
        _Bands ("Gradient bands (0 = smooth)", Range(0, 12)) = 6
        [Toggle] _FollowSun ("Use main light as sun", Float) = 1
        _SunDir ("Sun/Moon direction (if not following)", Vector) = (0.3, 0.35, 0.8, 0)
        [HDR] _SunColor ("Sun / Moon", Color) = (1.6, 1.35, 0.9, 1)
        _SunGlow ("Sun glow", Color) = (1.0, 0.62, 0.35, 1)
        _SunSize ("Disc size", Range(0.0005, 0.03)) = 0.004
        _MoonPhase ("Moon crescent (0 = full disc)", Range(0, 1)) = 0
        _CloudLit ("Cloud lit", Color) = (1.0, 0.95, 0.92, 1)
        _CloudShade ("Cloud shade", Color) = (0.72, 0.62, 0.86, 1)
        _CloudInk ("Cloud rim", Color) = (0.36, 0.30, 0.52, 1)
        _CloudCover ("Cloud cover", Range(0, 1)) = 0.48
        _CloudScale ("Cloud scale", Range(0.2, 6)) = 1.6
        _CloudSpeed ("Cloud drift", Range(0, 0.1)) = 0.008
        _Halftone ("Horizon halftone", Range(0, 1)) = 0.35
        _Stars ("Stars", Range(0, 1)) = 0
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
                half4 _Top, _Mid, _Horizon, _Ground, _SunColor, _SunGlow, _CloudLit, _CloudShade, _CloudInk;
                float4 _SunDir;
                half _Bands, _FollowSun, _SunSize, _MoonPhase, _CloudCover, _CloudScale, _CloudSpeed, _Halftone, _Stars;
            CBUFFER_END

            struct A { float4 positionOS : POSITION; };
            struct V { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };
            V vert(A v) { V o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); o.dir = v.positionOS.xyz; return o; }

            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float hash3(float3 p) { return frac(sin(dot(p, float3(127.1, 311.7, 74.7))) * 43758.5453); }
            float noise(float2 p)
            {
                float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f);
                return lerp(lerp(hash(i), hash(i + float2(1,0)), f.x), lerp(hash(i + float2(0,1)), hash(i + float2(1,1)), f.x), f.y);
            }
            float fbm(float2 p) { float s = 0, a = 0.55; [unroll] for (int k = 0; k < 3; k++) { s += noise(p) * a; p = p * 2.03 + 7.1; a *= 0.5; } return s; }

            half3 Band(half3 a, half3 b, float t)
            {
                t = saturate(t);
                if (_Bands > 0.5) { float q = t * _Bands; t = (floor(q) + smoothstep(0.42, 0.58, frac(q))) / _Bands; }
                return lerp(a, b, t);
            }

            half4 frag(V i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float y = d.y;
                float3 sunDir = _FollowSun > 0.5 ? normalize(_MainLightPosition.xyz) : normalize(_SunDir.xyz);
                float s = dot(d, sunDir);

                half3 c;
                if (y < 0) c = lerp(_Horizon.rgb * 0.8, _Ground.rgb, saturate(-y * 5));
                else
                {
                    c = Band(_Horizon.rgb, _Mid.rgb, pow(saturate(y * 2.6), 0.7));
                    c = lerp(c, Band(_Mid.rgb, _Top.rgb, saturate((y - 0.35) * 1.6)), step(0.35, y));
                }

                // warm glow around the sun hugging the horizon, printed as halftone dots
                float glow = pow(saturate(s), 5) * saturate(1 - abs(y) * 1.6);
                float2 sp = i.positionCS.xy / (6.0 * _ScreenParams.y / 1080.0);
                float2 sr = float2(sp.x * 0.7071 - sp.y * 0.7071, sp.x * 0.7071 + sp.y * 0.7071);
                float dotMask = 1 - smoothstep(0.0, 0.08, length(frac(sr) - 0.5) - sqrt(glow) * 0.55);
                c = lerp(c, _SunGlow.rgb, saturate(glow * 0.65 + dotMask * glow * _Halftone));

                // stars
                if (_Stars > 0 && y > 0)
                {
                    float3 cell = floor(d * 140);
                    float h = hash3(cell);
                    float star = step(0.985, h) * smoothstep(0.35, 0.0, length(frac(d * 140) - 0.5));
                    float tw = 0.6 + 0.4 * sin(_Time.y * (2 + h * 5) + h * 40);
                    c += star * tw * _Stars * saturate(y * 4);
                }

                // sun / moon disc + comic halo rings
                float disc = smoothstep(1 - _SunSize, 1 - _SunSize * 0.9, s) * smoothstep(-0.03, 0.0, y);
                if (_MoonPhase > 0)
                {
                    float3 side = normalize(cross(sunDir, float3(0, 1, 0)) + float3(0, 0.4, 0));
                    float3 off = normalize(sunDir + side * sqrt(2 * _SunSize) * 1.1 * _MoonPhase);
                    disc *= 1 - smoothstep(1 - _SunSize, 1 - _SunSize * 0.9, dot(d, off));
                }
                float ring = smoothstep(0.0012, 0.0, abs(s - (1 - _SunSize * 4.0))) + smoothstep(0.0012, 0.0, abs(s - (1 - _SunSize * 9.0))) * 0.6;
                c = lerp(c, _SunGlow.rgb * 1.3, ring * 0.45 * saturate(y * 8 + 0.5));
                c = lerp(c, _SunColor.rgb, disc);

                // two-tone puffy clouds with an ink rim
                if (y > 0.015)
                {
                    float2 uv = d.xz / (y + 0.18) * _CloudScale + _Time.y * _CloudSpeed * float2(1, 0.3);
                    float n = fbm(uv);
                    float2 toSun = normalize(sunDir.xz + 1e-4) * 0.22;
                    float nSun = fbm(uv + toSun);
                    float cover = 1 - _CloudCover;
                    float body = smoothstep(cover, cover + 0.015, n);
                    float rim = body - smoothstep(cover + 0.02, cover + 0.035, n);
                    float lit = 1 - step(cover + 0.1, nSun) * step(cover + 0.05, n);   // more cloud toward the sun = shaded core
                    half3 cloud = lerp(_CloudShade.rgb, _CloudLit.rgb, lit);
                    cloud = lerp(cloud, _SunGlow.rgb * 1.1, pow(saturate(s), 8) * 0.5 * lit);
                    float fadeH = saturate(y * 10) * saturate((0.75 - y) * 2.5);
                    c = lerp(c, cloud, body * fadeH);
                    c = lerp(c, _CloudInk.rgb, saturate(rim) * fadeH * 0.8);
                }
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
