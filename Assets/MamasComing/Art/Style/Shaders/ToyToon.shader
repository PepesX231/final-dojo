// MAMA'S COMING — ToyToon: the shared surface model of the "Toybox Comic" look (docs/MAMA_ART_STYLE.md).
// Matte toy plastic / felt (Human: Fall Flat, Party Animals) + crisp cel shadow, coloured shadows and halftone (comic, a touch of Persona).
// Property names match URP/Lit, so a Lit material can switch to this shader (and back) without losing its data.
// Level-wide knobs come from the MamaToonStyle volume component as globals (_MC_*); with no volume they are 0 and the material values rule.
Shader "MamasComing/ToyToon"
{
    Properties
    {
        _BaseMap ("Albedo", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1,1,1,1)
        _Smoothness ("Smoothness (toon highlight size)", Range(0,1)) = 0.2
        _Metallic ("Metallic", Range(0,1)) = 0
        _ToonShadow ("Shadow Tint", Color) = (0.58, 0.55, 0.82, 1)
        _ToonStep ("Cel Threshold", Range(-1,1)) = 0.0
        _ToonSoft ("Cel Softness", Range(0.005,1)) = 0.12
        _ToonWrap ("Wrap Light", Range(0,1)) = 0.2
        _ToonAmbient ("Ambient Strength", Range(0,2)) = 0.55
        _WorldUV ("World UV: metres per tile (0 = mesh UV)", Float) = 0
        _DetailStrength ("Paint Grain Strength (x level grain)", Range(0,2)) = 1
        [HDR] _EmissionColor ("Emission", Color) = (0,0,0,1)
        _EmissionMap ("Emission Map", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha Clip", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" "UniversalMaterialType"="Unlit" }
        LOD 300

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Smoothness;
            half _Metallic;
            half4 _ToonShadow;
            half _ToonStep;
            half _ToonSoft;
            half _ToonWrap;
            half _ToonAmbient;
            float _WorldUV;
            half _DetailStrength;
            half4 _EmissionColor;
            float4 _EmissionMap_ST;
            half _Cutoff;
        CBUFFER_END
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D(_EmissionMap);

        // level-wide style (MamaToonStyleFeature pushes these from the volume stack)
        half4 _MC_ShadowTint;   // rgb shadow colour, a = how much it replaces the material tint
        half4 _MC_Cel;          // x = extra sharpness 0..1, y = ambient boost, z = shadow-band lift, w = enabled flag
        half4 _MC_Rim;          // rgb rim (fuzz) colour, a = strength
        half4 _MC_Halftone;     // x = strength, y = cell size px @1080p, z = angle (rad)
        half4 _MC_Grain;        // x = strength, y = metres per tile
        TEXTURE2D(_MC_PaintTex); SAMPLER(sampler_MC_PaintTex);

        float2 SurfaceUV(float2 meshUV, float3 positionWS, float3 normalWS)
        {
            if (_WorldUV <= 0) return meshUV;
            float3 an = abs(normalWS); float3 wp = positionWS / _WorldUV;
            float2 uv = an.y > 0.6 ? wp.xz : (an.x > an.z ? wp.zy : wp.xy);
            return uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
        }

        half4 SampleAlbedo(float2 uv)
        {
            half4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;
            #if defined(_ALPHATEST_ON)
                clip(c.a - _Cutoff);
            #endif
            return c;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _EMISSION
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                half fog : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.fog = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half Cel(half ndl)
            {
                half w = (ndl + _ToonWrap) / (1.0h + _ToonWrap);
                half soft = lerp(_ToonSoft, 0.015h, saturate(_MC_Cel.x));
                return smoothstep(_ToonStep - soft, _ToonStep + soft, w);
            }

            half3 ShadowTone(half3 albedo)
            {
                half3 tint = lerp(_ToonShadow.rgb, _MC_ShadowTint.rgb, saturate(_MC_ShadowTint.a));
                return albedo * tint;
            }

            // screen-space comic dots: radius grows with darkness
            half Halftone(float4 positionCS, half darkness)
            {
                if (_MC_Halftone.x <= 0) return 0;
                float cell = max(_MC_Halftone.y, 2.0) * (_ScreenParams.y / 1080.0);
                float2 p = positionCS.xy;
                float s, c; sincos(_MC_Halftone.z, s, c);
                p = float2(c * p.x - s * p.y, s * p.x + c * p.y) / cell;
                float2 f = frac(p) - 0.5;
                float d = length(f) * 2.0;                     // 0 centre .. 1.41 corner
                float r = sqrt(saturate(darkness)) * 1.15;      // dot radius
                float aa = fwidth(d) * 1.2 + 1e-4;
                return (1.0 - smoothstep(r - aa, r + aa, d)) * _MC_Halftone.x;
            }

            half3 Paint(float3 positionWS, half3 n)
            {
                half s = _MC_Grain.x * _DetailStrength;
                if (s <= 0) return 1;
                float scale = max(_MC_Grain.y, 0.01);
                half3 w = pow(abs(n), 4); w /= (w.x + w.y + w.z);
                float3 p = positionWS / scale;
                // r = brush grain (mean 0.5), g = grime stains/drips (0 clean .. 1 filthy)
                half2 gx = SAMPLE_TEXTURE2D(_MC_PaintTex, sampler_MC_PaintTex, p.zy).rg;
                half2 gy = SAMPLE_TEXTURE2D(_MC_PaintTex, sampler_MC_PaintTex, p.xz).rg;
                half2 gz = SAMPLE_TEXTURE2D(_MC_PaintTex, sampler_MC_PaintTex, p.xy).rg;
                half2 g = gx * w.x + gy * w.y + gz * w.z;
                half3 grime = lerp(half3(1,1,1), half3(0.82h, 0.74h, 0.7h), saturate(g.y * _MC_Grain.z * _DetailStrength));
                return (1.0h + (g.x - 0.5h) * s) * grime;
            }

            half4 frag(Varyings i, bool facing : SV_IsFrontFace) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half3 n = normalize(i.normalWS) * (facing ? 1 : -1);
                float2 uv = SurfaceUV(i.uv, i.positionWS, n);
                half4 baseCol = SampleAlbedo(uv);
                half3 albedo = baseCol.rgb * Paint(i.positionWS, n);
                half metal = saturate(_Metallic);
                half3 diffuse = albedo * (1.0h - 0.55h * metal);
                half3 v = normalize(GetWorldSpaceViewDir(i.positionWS));

                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                half4 shadowMask = half4(1,1,1,1);
                Light mainLight = GetMainLight(shadowCoord, i.positionWS, shadowMask);

                // main light: one crisp cel band, cast shadows snapped to the same band
                half ndl = dot(n, mainLight.direction);
                half shadowAtt = smoothstep(0.35h, 0.65h, mainLight.shadowAttenuation);
                half lit = Cel(ndl) * shadowAtt;
                half3 shadowTone = ShadowTone(diffuse);
                // soft secondary band keeps round toys readable inside the shadow (Party Animals volume)
                half lift = smoothstep(-0.6h, 0.4h, ndl) * (0.18h + _MC_Cel.z);
                shadowTone = lerp(shadowTone, diffuse, lift);
                half3 color = lerp(shadowTone * 0.82h, diffuse, lit) * mainLight.color;
                half lightSum = lit;   // how lit this pixel is overall (drives halftone)

                // toon highlight: a small hard blob, larger/brighter on metal and polished props
                half3 h = normalize(mainLight.direction + v);
                half gloss = _Smoothness * _Smoothness;
                half size = lerp(0.985h, 0.93h, gloss);
                half spec = smoothstep(size, size + 0.006h, dot(n, h)) * lit * (gloss * 0.6h + metal * 0.5h);
                color += spec * mainLight.color * lerp(half3(1,1,1), albedo, metal);

                #if defined(_ADDITIONAL_LIGHTS)
                uint lightCount = GetAdditionalLightsCount();
                #if USE_CLUSTER_LIGHT_LOOP
                for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                {
                    Light dl = GetAdditionalLight(lightIndex, i.positionWS, shadowMask);
                    half dlit = Cel(dot(n, dl.direction)) * dl.shadowAttenuation;
                    color += diffuse * dl.color * dlit * 0.7h;
                    lightSum += dlit * Luminance(dl.color) * 0.7h;
                }
                #endif
                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                LIGHT_LOOP_BEGIN(lightCount)
                    Light l = GetAdditionalLight(lightIndex, i.positionWS, shadowMask);
                    half pl = smoothstep(0.0h, 0.25h, dot(n, l.direction) * 0.5h + 0.5h - 0.35h) * l.distanceAttenuation * l.shadowAttenuation;
                    color += diffuse * l.color * pl * 0.85h;
                    lightSum += pl * Luminance(l.color) * 0.85h;
                LIGHT_LOOP_END
                #endif

                // ambient from the scene SH, a little brighter than Lit so nothing goes muddy
                half3 ambient = SampleSH(n) * diffuse * (_ToonAmbient + _MC_Cel.y);
                #if defined(_SCREEN_SPACE_OCCLUSION)
                    AmbientOcclusionFactor ao = GetScreenSpaceAmbientOcclusion(GetNormalizedScreenSpaceUV(i.positionCS));
                    ambient *= ao.indirectAmbientOcclusion;
                    color *= lerp(1.0h, ao.directAmbientOcclusion, 0.5h);
                #endif
                color += ambient;

                // comic halftone in the shadow side
                // only the genuinely dark side gets dots: away from the sun AND not picked up by lamps
                half away = saturate(-ndl * 1.5h + 0.2h);
                half dark = smoothstep(0.35h, 1.0h, saturate(1.0h - lightSum) * away);
                half dots = Halftone(i.positionCS, dark * 0.75h);
                color = lerp(color, color * ShadowTone(half3(0.55h, 0.55h, 0.55h)), dots);

                // fuzzy rim (felt / fur sheen), stronger on the lit side
                half fres = pow(saturate(1.0h - dot(n, v)), 3.0h);
                color += _MC_Rim.rgb * fres * _MC_Rim.a * (0.35h + 0.65h * lit) * albedo * 2.0h;

                #if defined(_EMISSION)
                    color += SAMPLE_TEXTURE2D(_EmissionMap, sampler_BaseMap, i.uv).rgb * _EmissionColor.rgb;
                #endif
                color = MixFog(color, i.fog);
                return half4(color, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            V vertShadow(A v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                V o;
                float3 pw = TransformObjectToWorld(v.positionOS.xyz);
                float3 nw = TransformObjectToWorldNormal(v.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 ld = normalize(_LightPosition - pw);
                #else
                    float3 ld = _LightDirection;
                #endif
                float4 cs = TransformWorldToHClip(ApplyShadowBias(pw, nw, ld));
                #if UNITY_REVERSED_Z
                    cs.z = min(cs.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    cs.z = max(cs.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                o.positionCS = cs;
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }
            half4 fragShadow(V i) : SV_Target { SampleAlbedo(i.uv); return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex vertDepth
            #pragma fragment fragDepth
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_instancing
            struct A { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            V vertDepth(A v) { UNITY_SETUP_INSTANCE_ID(v); V o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); o.uv = TRANSFORM_TEX(v.uv, _BaseMap); return o; }
            half fragDepth(V i) : SV_Target { SampleAlbedo(i.uv); return i.positionCS.z; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex vertDN
            #pragma fragment fragDN
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_instancing
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 normalWS : TEXCOORD1; };
            V vertDN(A v) { UNITY_SETUP_INSTANCE_ID(v); V o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); o.uv = TRANSFORM_TEX(v.uv, _BaseMap); o.normalWS = TransformObjectToWorldNormal(v.normalOS); return o; }
            half4 fragDN(V i, bool facing : SV_IsFrontFace) : SV_Target
            {
                SampleAlbedo(i.uv);
                float3 n = NormalizeNormalPerPixel(i.normalWS) * (facing ? 1 : -1);
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 oct = saturate(PackNormalOctQuadEncode(n) * 0.5 + 0.5);
                    return half4(PackFloat2To888(oct), 0);
                #else
                    return half4(n, 0);
                #endif
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
