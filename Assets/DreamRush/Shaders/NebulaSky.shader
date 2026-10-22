// DREAM RUSH — "in the middle of a dream" sky: vertical gradient + drifting fbm nebula + twinkling star field.
// Use as RenderSettings.skybox. Colours per chapter (Galaxy select, Toy Galaxy, Pop-up Book night...).
Shader "DreamRush/NebulaSky"
{
    Properties
    {
        _TopColor ("Top", Color) = (0.06, 0.05, 0.16, 1)
        _HorizonColor ("Horizon", Color) = (0.34, 0.2, 0.46, 1)
        _BottomColor ("Bottom", Color) = (0.1, 0.06, 0.2, 1)
        _NebulaA ("Nebula A", Color) = (0.85, 0.3, 0.75, 1)
        _NebulaB ("Nebula B", Color) = (0.25, 0.45, 1.0, 1)
        _NebulaStrength ("Nebula Strength", Range(0, 2)) = 0.9
        _NebulaScale ("Nebula Scale", Float) = 2.2
        _StarDensity ("Star Density", Range(0, 1)) = 0.55
        _StarBrightness ("Star Brightness", Range(0, 4)) = 1.6
        _Drift ("Drift Speed", Float) = 0.01
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
                half4 _TopColor, _HorizonColor, _BottomColor, _NebulaA, _NebulaB;
                float _NebulaStrength, _NebulaScale, _StarDensity, _StarBrightness, _Drift;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; };
            struct V { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };
            V vert(A v) { V o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); o.dir = v.positionOS.xyz; return o; }

            float hash(float3 p) { p = frac(p * 0.3183099 + 0.1); p *= 17.0; return frac(p.x * p.y * p.z * (p.x + p.y + p.z)); }
            float noise(float3 x)
            {
                float3 i = floor(x); float3 f = frac(x); f = f * f * (3 - 2 * f);
                return lerp(lerp(lerp(hash(i + float3(0,0,0)), hash(i + float3(1,0,0)), f.x), lerp(hash(i + float3(0,1,0)), hash(i + float3(1,1,0)), f.x), f.y),
                            lerp(lerp(hash(i + float3(0,0,1)), hash(i + float3(1,0,1)), f.x), lerp(hash(i + float3(0,1,1)), hash(i + float3(1,1,1)), f.x), f.y), f.z);
            }
            float fbm(float3 p) { float a = 0.5, s = 0; for (int k = 0; k < 5; k++) { s += a * noise(p); p *= 2.03; a *= 0.5; } return s; }

            half4 frag(V i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float h = d.y;
                half3 col = h > 0 ? lerp(_HorizonColor.rgb, _TopColor.rgb, pow(saturate(h), 0.55)) : lerp(_HorizonColor.rgb, _BottomColor.rgb, pow(saturate(-h), 0.5));
                float3 q = d * _NebulaScale + float3(_Time.y * _Drift, 0, 0);
                float n1 = fbm(q), n2 = fbm(q * 1.7 + 4.3);
                float neb = smoothstep(0.45, 0.85, n1) * _NebulaStrength;
                col += lerp(_NebulaA.rgb, _NebulaB.rgb, saturate(n2 * 1.4 - 0.2)) * neb * 0.55;
                col += _NebulaA.rgb * pow(saturate(n1 - 0.55), 2) * _NebulaStrength * 0.8;      // bright filaments
                // stars: hashed cells on the direction sphere, twinkle
                float3 sp = d * 220.0;
                float3 cell = floor(sp);
                float r = hash(cell);
                float star = step(1.0 - _StarDensity * 0.025, r);
                float3 fp = frac(sp) - 0.5;
                float tw = 0.6 + 0.4 * sin(_Time.y * (2 + r * 5) + r * 40);
                col += star * smoothstep(0.35, 0.0, length(fp)) * _StarBrightness * tw;
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
