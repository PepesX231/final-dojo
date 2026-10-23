// DREAM RUSH — InkOutline: inverted-hull ink line with constant screen-space width (GDD §5.6 selective outlines).
// Applied as a separate shell renderer (DreamLook.AddOutline) only to the player, chair and interactables.
Shader "DreamRush/InkOutline"
{
    Properties
    {
        _OutlineColor ("Ink", Color) = (0.188, 0.165, 0.239, 1)
        _OutlineWidth ("Width (px at 1080p)", Range(0, 8)) = 2.5
        _MinDistanceScale ("Far shrink", Range(0, 1)) = 0.6
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry+10" }
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlineWidth;
                float _MinDistanceScale;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; half fog : TEXCOORD0; };
            V vert(A v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                V o;
                float4 cs = TransformObjectToHClip(v.positionOS.xyz);
                float3 nWS = TransformObjectToWorldNormal(v.normalOS);
                float2 nCS = mul((float3x3)UNITY_MATRIX_VP, nWS).xy;
                float len = max(length(nCS), 1e-4);
                // pixels → clip space; thinner far away so distant props don't turn into ink blobs
                float dist = cs.w;
                float shrink = lerp(1.0, _MinDistanceScale, saturate((dist - 6.0) / 20.0));
                float2 px = (nCS / len) * _OutlineWidth * shrink * (_ScreenParams.y / 1080.0) * 2.0 / _ScreenParams.xy;
                cs.xy += px * cs.w;
                o.positionCS = cs;
                o.fog = ComputeFogFactor(cs.z);
                return o;
            }
            half4 frag(V i) : SV_Target { return half4(MixFog(_OutlineColor.rgb, i.fog), 1); }
            ENDHLSL
        }
    }
}
