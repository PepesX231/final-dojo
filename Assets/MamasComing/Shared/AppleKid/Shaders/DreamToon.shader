// DREAM RUSH — DreamToon: the single shading model of Applebook Surrealism (GDD §5.4–5.6).
// Soft two-tone ramp, plum-tinted shadows (never black), wrap light, optional rim for characters,
// optional alpha clip + double-sided for paper. URP 17 (Forward / Forward+), SRP-batcher compatible.
Shader "DreamRush/DreamToon"
{
    Properties
    {
        _BaseMap ("Albedo", 2D) = "white" {}
        [HDR] _BaseColor ("Tint", Color) = (1,1,1,1)
        _BaseMapStrength ("Texture Strength (0 = flat tint)", Range(0,1)) = 1
        _ShadowColor ("Shadow Tint (multiplied)", Color) = (0.55, 0.47, 0.72, 1)
        _RampThreshold ("Ramp Threshold", Range(-1,1)) = 0.05
        _RampSoftness ("Ramp Softness", Range(0.001,1)) = 0.35
        _Wrap ("Wrap Light", Range(0,1)) = 0.25
        _AmbientStrength ("Ambient (SH) Strength", Range(0,2)) = 0.35
        _RimColor ("Rim Color", Color) = (1, 0.95, 0.85, 1)
        _RimStrength ("Rim Strength", Range(0,1)) = 0
        _RimPower ("Rim Power", Range(0.5,8)) = 3
        _SpecStrength ("Soft Highlight", Range(0,1)) = 0
        _SpecSize ("Highlight Size", Range(0.8,0.999)) = 0.97
        [HDR] _Emission ("Emission (glow, blooms)", Color) = (0,0,0,0)
        _EmissionMap ("Emission Map (lit windows, neon; × Emission)", 2D) = "white" {}
        _EmissionMapStrength ("Emission Map Strength (0 = flat)", Range(0,1)) = 0
        _WorldUV ("World-space UV: metres per tile (0 = mesh UV)", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha Clip", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 300

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _BaseMapStrength;
            half4 _ShadowColor;
            half _RampThreshold;
            half _RampSoftness;
            half _Wrap;
            half _AmbientStrength;
            half4 _RimColor;
            half _RimStrength;
            half _RimPower;
            half _SpecStrength;
            half _SpecSize;
            half _Cutoff;
            half4 _Emission;
            float _WorldUV;
            half _EmissionMapStrength;
        CBUFFER_END
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D(_EmissionMap);
        // per-level art style (StylePack globals; 0 = material values untouched)
        half _DR_CelHard;          // 1 = razor cel edge (Densha / Persona), 0 = the material's own ramp softness
        half4 _DR_ShadowTint;      // rgb = shadow colour for the whole level, a = how much it replaces the material's

        half4 SampleAlbedo(float2 uv)
        {
            half4 t = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
            half4 c = half4(lerp(half3(1,1,1), t.rgb, _BaseMapStrength), t.a) * _BaseColor;
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

            half Ramp(half ndl)
            {
                half w = (ndl + _Wrap) / (1.0h + _Wrap);
                half soft = lerp(_RampSoftness, 0.025h, saturate(_DR_CelHard));
                return smoothstep(_RampThreshold - soft * 0.5h, _RampThreshold + soft * 0.5h, w);
            }

            half3 Shade(half3 albedo, half3 n, half3 v, Light light, half lightScale)
            {
                half ndl = dot(n, light.direction);
                half lit = Ramp(ndl) * light.shadowAttenuation * light.distanceAttenuation;
                half3 shadowTone = albedo * lerp(_ShadowColor.rgb, _DR_ShadowTint.rgb, saturate(_DR_ShadowTint.a));   // plum (or the level's tint), never black
                half3 c = lerp(shadowTone * light.distanceAttenuation, albedo, lit) * light.color * lightScale;
                // soft, small painted highlight (not plastic)
                half3 h = normalize(light.direction + v);
                half spec = smoothstep(_SpecSize, _SpecSize + 0.01h, dot(n, h)) * _SpecStrength * lit;
                return c + spec * light.color * lightScale;
            }

            half4 frag(Varyings i, bool facing : SV_IsFrontFace) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float2 uv = i.uv;
                if (_WorldUV > 0)
                {   // wallpaper / floors: tile in metres along the dominant axis so every box gets the same density
                    float3 an = abs(i.normalWS); float3 wp = i.positionWS / _WorldUV;
                    uv = an.y > 0.6 ? wp.xz : (an.x > an.z ? wp.zy : wp.xy);
                }
                half4 albedo = SampleAlbedo(uv);
                half3 n = normalize(i.normalWS) * (facing ? 1 : -1);
                half3 v = normalize(GetWorldSpaceViewDir(i.positionWS));

                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.normalWS = n;
                inputData.viewDirectionWS = v;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                inputData.shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                half4 shadowMask = half4(1,1,1,1);

                Light mainLight = GetMainLight(inputData.shadowCoord, i.positionWS, shadowMask);
                half3 color = Shade(albedo.rgb, n, v, mainLight, 1.0h);

                #if defined(_ADDITIONAL_LIGHTS)
                uint lightCount = GetAdditionalLightsCount();
                #if USE_CLUSTER_LIGHT_LOOP
                for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                {
                    Light dl = GetAdditionalLight(lightIndex, i.positionWS, shadowMask);
                    color += Shade(albedo.rgb, n, v, dl, 0.7h);
                }
                #endif
                LIGHT_LOOP_BEGIN(lightCount)
                    Light l = GetAdditionalLight(lightIndex, i.positionWS, shadowMask);
                    half pl = Ramp(dot(n, l.direction)) * l.distanceAttenuation * l.shadowAttenuation;
                    color += albedo.rgb * l.color * pl * 0.8h;                // practical lights add warmth, no shadow tint
                LIGHT_LOOP_END
                #endif

                // ambient: soft, from the scene's SH (lavender sky / plum ground)
                half3 ambient = SampleSH(n) * albedo.rgb * _AmbientStrength;
                #if defined(_SCREEN_SPACE_OCCLUSION)
                    AmbientOcclusionFactor ao = GetScreenSpaceAmbientOcclusion(inputData.normalizedScreenSpaceUV);
                    ambient *= ao.indirectAmbientOcclusion;
                    color *= lerp(1.0h, ao.directAmbientOcclusion, 0.6h);
                #endif
                color += ambient;

                // rim: thin cream edge that separates characters from the floor
                half rim = pow(saturate(1.0h - dot(n, v)), _RimPower) * _RimStrength;
                color += _RimColor.rgb * rim * (0.5h + 0.5h * saturate(dot(n, mainLight.direction) + 0.5h));

                half3 em = _Emission.rgb;
                if (_EmissionMapStrength > 0) em *= lerp(half3(1,1,1), SAMPLE_TEXTURE2D(_EmissionMap, sampler_BaseMap, uv).rgb, _EmissionMapStrength);
                color += em;
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
            #pragma multi_compile_instancing
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 normalWS : TEXCOORD1; };
            V vertDN(A v) { UNITY_SETUP_INSTANCE_ID(v); V o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); o.uv = TRANSFORM_TEX(v.uv, _BaseMap); o.normalWS = TransformObjectToWorldNormal(v.normalOS); return o; }
            half4 fragDN(V i) : SV_Target { SampleAlbedo(i.uv); return half4(NormalizeNormalPerPixel(i.normalWS), 0); }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
