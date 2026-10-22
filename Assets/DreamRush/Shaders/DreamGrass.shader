// DREAM RUSH — DreamGrass: instanced painterly grass tufts (FF field look). Geometric blades (no alpha),
// root→tip colour gradient, colour variation per instance (world noise), wind sway + gust waves, shading that
// uses the GROUND normal (soft, no per-blade noise), receives shadows, fog. Drawn by GrassField (RenderMeshInstanced).
Shader "DreamRush/DreamGrass"
{
    Properties
    {
        _Root ("Root", Color) = (0.13, 0.32, 0.18, 1)
        _Tip ("Tip", Color) = (0.62, 0.82, 0.36, 1)
        _TipGold ("Tip Gold", Color) = (0.95, 0.85, 0.45, 1)
        _NoiseTex ("Noise", 2D) = "gray" {}
        _NoiseScale ("Noise Scale", Float) = 0.03
        _GoldAmount ("Gold", Range(0,1)) = 0.35
        _WindStrength ("Wind", Float) = 0.35
        _WindSpeed ("Wind Speed", Float) = 1.4
        _WindDir ("Wind Dir (xz)", Vector) = (1, 0, 0.4, 0)
        _ShadowColor ("Shadow Tint", Color) = (0.5, 0.45, 0.7, 1)
        _Translucency ("Back-light glow", Range(0,1)) = 0.35
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Cull Off
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _Root, _Tip, _TipGold, _ShadowColor;
            float4 _NoiseTex_ST, _WindDir;
            float _NoiseScale, _GoldAmount, _WindStrength, _WindSpeed, _Translucency;
        CBUFFER_END
        TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);

        float3 Wind(float3 wp, float h)
        {
            float2 d = normalize(_WindDir.xz + 1e-4);
            float gust = sin(dot(wp.xz, d) * 0.18 - _Time.y * _WindSpeed * 1.7) * 0.5 + 0.5;
            float flutter = sin(_Time.y * _WindSpeed * 4.0 + wp.x * 1.7 + wp.z * 1.3) * 0.25;
            float k = h * h * _WindStrength * (0.35 + gust * 0.9 + flutter);
            return float3(d.x, -0.25 * abs(k), d.y) * k;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct A { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; half h : TEXCOORD1; half3 tint : TEXCOORD2; half fog : TEXCOORD3; };
            V vert(A v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                V o;
                float3 wp = TransformObjectToWorld(v.positionOS.xyz);
                float3 root = TransformObjectToWorld(float3(0,0,0));
                float h = v.uv.y;
                wp += Wind(wp, h);
                o.positionWS = wp; o.h = h;
                o.positionCS = TransformWorldToHClip(wp);
                half n1 = SAMPLE_TEXTURE2D_LOD(_NoiseTex, sampler_NoiseTex, root.xz * _NoiseScale, 0).r;
                half n2 = SAMPLE_TEXTURE2D_LOD(_NoiseTex, sampler_NoiseTex, root.xz * _NoiseScale * 0.37 + 0.3, 0).r;
                half gold = smoothstep(1.0 - _GoldAmount, 1.0 - _GoldAmount + 0.15, n2);
                o.tint = lerp(lerp(_Tip.rgb * 0.85, _Tip.rgb * 1.1, n1), _TipGold.rgb, gold);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }
            half4 frag(V i) : SV_Target
            {
                half3 albedo = lerp(_Root.rgb, i.tint, smoothstep(0.0h, 0.9h, i.h));
                Light L = GetMainLight(TransformWorldToShadowCoord(i.positionWS), i.positionWS, half4(1,1,1,1));
                half3 up = half3(0,1,0);
                half lit = smoothstep(-0.1h, 0.45h, dot(up, L.direction)) * L.shadowAttenuation;
                half3 col = lerp(albedo * _ShadowColor.rgb, albedo, lit) * L.color;
                // back-lit tips glow (golden hour rim through the blades)
                half3 vdir = normalize(GetWorldSpaceViewDir(i.positionWS));
                half back = saturate(dot(-vdir, L.direction)) * i.h * i.h * _Translucency * L.shadowAttenuation;
                col += L.color * i.tint * back;
                col += SampleSH(up) * albedo * 0.45h;
                col = MixFog(col, i.fog);
                return half4(col, 1);
            }
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
            #pragma multi_compile_instancing
            struct A { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float4 vs(A v) : SV_POSITION { UNITY_SETUP_INSTANCE_ID(v); float3 wp = TransformObjectToWorld(v.positionOS.xyz); wp += Wind(wp, v.uv.y); return TransformWorldToHClip(wp); }
            half fs(float4 p : SV_POSITION) : SV_Target { return p.z; }
            ENDHLSL
        }
    }
}
