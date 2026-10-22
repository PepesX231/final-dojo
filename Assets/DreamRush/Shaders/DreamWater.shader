// DREAM RUSH — DreamWater: stylised lake/sea. Gentle vertex swell, view-angle colour (deep→shallow), sky
// fresnel, animated caustic-like sparkle lines and sun glints. Transparent, no depth texture required.
Shader "DreamRush/DreamWater"
{
    Properties
    {
        _Deep ("Deep", Color) = (0.10, 0.32, 0.55, 0.92)
        _Shallow ("Shallow", Color) = (0.35, 0.78, 0.86, 0.75)
        _Sky ("Sky Reflection", Color) = (0.95, 0.80, 0.70, 1)
        _NoiseTex ("Noise", 2D) = "gray" {}
        _Swell ("Swell Height", Float) = 0.12
        [HDR] _Glint ("Glint", Color) = (3, 2.6, 2, 1)
        _SunDir ("Sun Direction (to sun)", Vector) = (-0.4, 0.18, 0.9, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent-10" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Deep, _Shallow, _Sky, _Glint;
                float4 _NoiseTex_ST, _SunDir;
                float _Swell;
            CBUFFER_END
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
            struct A { float4 positionOS : POSITION; };
            struct V { float4 positionCS : SV_POSITION; float3 wp : TEXCOORD0; half fog : TEXCOORD1; };
            V vert(A v)
            {
                V o;
                float3 wp = TransformObjectToWorld(v.positionOS.xyz);
                wp.y += (sin(wp.x * 0.15 + _Time.y * 0.9) + sin(wp.z * 0.21 + _Time.y * 1.3)) * _Swell;
                o.wp = wp; o.positionCS = TransformWorldToHClip(wp); o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }
            half4 frag(V i) : SV_Target
            {
                float3 vd = normalize(GetWorldSpaceViewDir(i.wp));
                float2 uv = i.wp.xz * 0.05;
                half n1 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv + _Time.y * float2(0.01, 0.006)).g;
                half n2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv * 1.7 - _Time.y * float2(0.008, 0.012)).g;
                half3 n = normalize(half3((n1 - 0.5) * 0.35, 1, (n2 - 0.5) * 0.35));
                half fres = pow(1 - saturate(dot(n, vd)), 4);
                half4 c = lerp(_Shallow, _Deep, saturate(dot(vd, half3(0,1,0)) * 1.2));
                c.rgb = lerp(c.rgb, _Sky.rgb, fres * 0.8);
                // painted caustic lines
                half lines = smoothstep(0.47, 0.5, abs(frac((n1 + n2) * 3.0) - 0.5));
                c.rgb += lines * 0.12;
                // sun glints
                float3 h = normalize(normalize(_SunDir.xyz) + vd);
                half g = smoothstep(0.995, 0.999, dot(n, h));
                c.rgb += _Glint.rgb * g;
                c.a = saturate(c.a + fres * 0.2 + g);
                c.rgb = MixFog(c.rgb, i.fog);
                return c;
            }
            ENDHLSL
        }
    }
}
