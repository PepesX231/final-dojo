// MAMA'S COMING — ComicInk: screen-space ink lines from depth + normals (Full Screen Pass renderer feature).
// Strength/colour/thickness come from the MamaToonStyle volume component (_MC_Ink*). With no style volume the pass is a plain copy.
Shader "MamasComing/ComicInk"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off
        Pass
        {
            Name "ComicInk"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            half4 _MC_Ink;        // x strength, y thickness px @1080p, z depth threshold (relative), w normal threshold
            half4 _MC_InkColor;   // rgb ink, a = how much the ink keeps the surface colour (0 = pure ink)
            half4 _MC_InkFade;    // x fade start (m), y fade end (m), z minimum strength far away

            float Eye(float2 uv)
            {
                float raw = SampleSceneDepth(uv);
                return LinearEyeDepth(raw, _ZBufferParams);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                if (_MC_Ink.x <= 0) return col;

                float px = max(_MC_Ink.y, 0.5) * (_ScreenParams.y / 1080.0);
                float2 t = px / _ScreenParams.xy;
                float d0 = Eye(uv);
                float3 n0 = SampleSceneNormals(uv);

                float2 o[4] = { float2(t.x, 0), float2(-t.x, 0), float2(0, t.y), float2(0, -t.y) };
                float depthEdge = 0, normalEdge = 0;
                [unroll] for (int k = 0; k < 4; k++)
                {
                    float d = Eye(uv + o[k]);
                    // one-sided: only the nearer surface draws the line, so silhouettes stay one line thick
                    depthEdge = max(depthEdge, (d - d0) / max(d0, 1e-3));
                    float3 n = SampleSceneNormals(uv + o[k]);
                    normalEdge = max(normalEdge, 1.0 - saturate(dot(n0, n)));
                }
                float dz = _MC_Ink.z;
                half e = smoothstep(dz, dz * 1.6, depthEdge);
                half nw = _MC_Ink.w;
                e = max(e, smoothstep(nw, nw * 1.5 + 0.02, normalEdge));

                // sky / far plane never inks itself
                #if UNITY_REVERSED_Z
                    if (SampleSceneDepth(uv) <= 1e-6) e = 0;
                #else
                    if (SampleSceneDepth(uv) >= 1 - 1e-6) e = 0;
                #endif

                half fade = lerp(1.0h, _MC_InkFade.z, saturate((d0 - _MC_InkFade.x) / max(_MC_InkFade.y - _MC_InkFade.x, 1e-3)));
                e *= _MC_Ink.x * fade;
                half3 ink = lerp(_MC_InkColor.rgb, col.rgb * _MC_InkColor.rgb * 2.0h, _MC_InkColor.a);
                col.rgb = lerp(col.rgb, ink, saturate(e));
                return col;
            }
            ENDHLSL
        }
    }
}
