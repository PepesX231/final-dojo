// DREAM RUSH — particle/billboard sprite shader: texture × vertex colour × tint, alpha or additive blend
// (set _SrcBlend/_DstBlend), optional soft camera-near fade so god-ray cards never slice the screen.
Shader "DreamRush/DreamParticle"
{
    Properties
    {
        _BaseMap ("Sprite", 2D) = "white" {}
        [MainColor] [HDR] _BaseColor ("Tint", Color) = (1,1,1,1)
        _NearFade ("Fade within distance of camera", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst", Float) = 10
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Pass
        {
            Name "Sprite"
            Tags { "LightMode"="UniversalForward" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _NearFade;
            CBUFFER_END
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            struct A { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; half fog : TEXCOORD1; float viewZ : TEXCOORD2; };
            V vert(A v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                V o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.color = v.color * _BaseColor;
                o.fog = ComputeFogFactor(p.positionCS.z);
                o.viewZ = -p.positionVS.z;
                return o;
            }
            half4 frag(V i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * i.color;
                if (_NearFade > 0) c.a *= saturate((i.viewZ - 0.3) / _NearFade);
                c.rgb = MixFog(c.rgb, i.fog);
                return c;
            }
            ENDHLSL
        }
    }
}
