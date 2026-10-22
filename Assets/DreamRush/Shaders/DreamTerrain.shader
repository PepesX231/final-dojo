// DREAM RUSH — DreamTerrain: painterly open-world ground (Final Fantasy field spirit, toon-lit).
// Colour comes from world height + slope + a tiling noise texture: lush two-tone grass with golden patches,
// layered cliff rock with horizontal strata on steep faces, warm sand at the waterline, worn dirt paths
// (vertex colour R). Lit with the DreamToon ramp (plum shadows), receives shadows, fog.
Shader "DreamRush/DreamTerrain"
{
    Properties
    {
        _NoiseTex ("Noise (R: large, G: small)", 2D) = "gray" {}
        _NoiseScale ("Noise Scale (1/m)", Float) = 0.025
        _GrassA ("Grass Deep", Color) = (0.20, 0.45, 0.24, 1)
        _GrassB ("Grass Bright", Color) = (0.48, 0.72, 0.30, 1)
        _GrassGold ("Grass Gold", Color) = (0.86, 0.76, 0.36, 1)
        _GoldAmount ("Gold Patches", Range(0,1)) = 0.35
        _Path ("Path Dirt", Color) = (0.72, 0.58, 0.40, 1)
        _Rock ("Rock", Color) = (0.80, 0.66, 0.54, 1)
        _RockDark ("Rock Strata", Color) = (0.68, 0.53, 0.46, 1)
        _Sand ("Sand", Color) = (0.93, 0.84, 0.62, 1)
        _WaterLevel ("Water Level", Float) = 0
        _CliffStart ("Cliff Slope Start", Range(0,1)) = 0.42
        _ShadowColor ("Shadow Tint", Color) = (0.5, 0.45, 0.7, 1)
        _RampSoftness ("Ramp Softness", Range(0.001,1)) = 0.45
        _Wrap ("Wrap Light", Range(0,1)) = 0.3
        _AmbientStrength ("Ambient", Range(0,2)) = 0.45
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _NoiseTex_ST;
            float _NoiseScale;
            half4 _GrassA, _GrassB, _GrassGold, _Path, _Rock, _RockDark, _Sand, _ShadowColor;
            half _GoldAmount, _WaterLevel, _CliffStart, _RampSoftness, _Wrap, _AmbientStrength;
        CBUFFER_END
        TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; half4 color : COLOR; };
            struct V { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; half4 color : TEXCOORD2; half fog : TEXCOORD3; };

            V vert(A v)
            {
                V o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS; o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.color = v.color;
                o.fog = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half Ramp(half ndl)
            {
                half w = (ndl + _Wrap) / (1.0h + _Wrap);
                return smoothstep(0.05h - _RampSoftness * 0.5h, 0.05h + _RampSoftness * 0.5h, w);
            }

            half4 frag(V i) : SV_Target
            {
                float3 wp = i.positionWS;
                half3 n = normalize(i.normalWS);
                half nBig = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, wp.xz * _NoiseScale).r;
                half nSmall = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, wp.xz * _NoiseScale * 7.3).g;
                half slope = 1.0h - saturate(n.y);

                // grass: deep→bright by big noise (painted clumps), golden meadow patches
                half3 grass = lerp(_GrassA.rgb, _GrassB.rgb, smoothstep(0.3h, 0.7h, nBig + (nSmall - 0.5h) * 0.35h));
                half gold = smoothstep(1.0h - _GoldAmount, 1.0h - _GoldAmount + 0.15h, SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, wp.xz * _NoiseScale * 0.37 + 0.3).r);
                grass = lerp(grass, _GrassGold.rgb, gold * 0.85h);
                // paths painted in vertex colour R
                grass = lerp(grass, _Path.rgb * (0.9h + nSmall * 0.2h), saturate(i.color.r * 1.4h));
                // cliffs: strata bands along height, wobbling with noise
                half strata = frac(wp.y * 0.16 + nBig * 0.9);
                half3 rock = lerp(_Rock.rgb, _RockDark.rgb, smoothstep(0.72h, 0.76h, strata));
                rock = lerp(rock, rock * 1.12h, smoothstep(0.55h, 0.9h, n.y + nSmall * 0.3h));   // sunlit ledges
                rock *= 0.92h + nSmall * 0.16h;
                half cliff = smoothstep(_CliffStart, _CliffStart + 0.12h, slope + (nSmall - 0.5h) * 0.08h);
                half3 albedo = lerp(grass, rock, cliff);
                // beach
                half sand = (1.0h - smoothstep(_WaterLevel + 0.6h, _WaterLevel + 1.8h, wp.y + nSmall * 0.6h)) * i.color.g;   // beaches only where the builder painted them (G)
                albedo = lerp(albedo, _Sand.rgb * (0.95h + nSmall * 0.1h), sand * (1.0h - cliff * 0.7h));

                float4 sc = TransformWorldToShadowCoord(wp);
                Light L = GetMainLight(sc, wp, half4(1,1,1,1));
                half lit = Ramp(dot(n, L.direction)) * L.shadowAttenuation;
                half3 col = lerp(albedo * _ShadowColor.rgb, albedo, lit) * L.color;
                #if defined(_ADDITIONAL_LIGHTS)
                uint lc = GetAdditionalLightsCount();
                InputData inputData = (InputData)0;
                inputData.positionWS = wp; inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                LIGHT_LOOP_BEGIN(lc)
                    Light l = GetAdditionalLight(lightIndex, wp, half4(1,1,1,1));
                    col += albedo * l.color * Ramp(dot(n, l.direction)) * l.distanceAttenuation * 0.8h;
                LIGHT_LOOP_END
                #endif
                col += SampleSH(n) * albedo * _AmbientStrength;
                col = MixFog(col, i.fog);
                return half4(col, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma vertex vs
            #pragma fragment fs
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            float4 vs(A v) : SV_POSITION
            {
                float3 pw = TransformObjectToWorld(v.positionOS.xyz);
                float3 nw = TransformObjectToWorldNormal(v.normalOS);
                float4 cs = TransformWorldToHClip(ApplyShadowBias(pw, nw, _LightDirection));
                #if UNITY_REVERSED_Z
                    cs.z = min(cs.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    cs.z = max(cs.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return cs;
            }
            half4 fs() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex vs
            #pragma fragment fs
            float4 vs(float4 p : POSITION) : SV_POSITION { return TransformObjectToHClip(p.xyz); }
            half fs(float4 p : SV_POSITION) : SV_Target { return p.z; }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
