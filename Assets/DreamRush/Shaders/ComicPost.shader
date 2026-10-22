// DREAM RUSH — ComicPost: the per-level "style pack" full-screen pass (FullScreenPassRendererFeature, before post).
// Everything is driven by GLOBAL floats set by the scene's StylePack (all 0 = pass-through, so un-styled scenes are
// untouched):
//   _DR_Ink        ink lines from depth + normal edges (Densha/P5 comic line on EVERY object, not just outlined ones)
//   _DR_Halftone   print dots in the darks (screen-space 45° grid), tinted _DR_HalftoneColor
//   _DR_Posterize  flattens the light into bands (cel/poster look)
//   _DR_Speed      radial speed lines + edge chroma split when Pip is fast (Densha Attack / Hi-Fi Rush energy)
//   _DR_Glitch     Cyberpunk-style RGB block glitch (time stop, damage)
Shader "DreamRush/ComicPost"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always
        Pass
        {
            Name "Comic"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            float _DR_Ink, _DR_InkWidth, _DR_InkFar, _DR_Halftone, _DR_HalftoneScale, _DR_Posterize, _DR_Speed, _DR_Glitch, _DR_Saturate, _DR_Sketch;
            float4 _DR_InkColor, _DR_HalftoneColor, _DR_SpeedColor, _DR_PaperColor;

            float Hash(float2 p) { return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453); }

            float Edge(float2 uv, float2 px)
            {
                float d0 = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
                float3 n0 = SampleSceneNormals(uv);
                float dd = 0, nd = 0;
                float2 o[4] = { float2(px.x, 0), float2(-px.x, 0), float2(0, px.y), float2(0, -px.y) };
                [unroll] for (int k = 0; k < 4; k++)
                {
                    float d = LinearEyeDepth(SampleSceneDepth(uv + o[k]), _ZBufferParams);
                    dd = max(dd, abs(d - d0) / max(d0, 0.001));
                    nd = max(nd, 1 - saturate(dot(n0, SampleSceneNormals(uv + o[k]))));
                }
                float e = max(smoothstep(0.035, 0.09, dd), smoothstep(0.25, 0.55, nd));
                e *= 1 - smoothstep(_DR_InkFar * 0.6, _DR_InkFar, d0);   // lines fade into the distance (atmosphere)
                return e;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float2 uv = i.texcoord;
                // glitch: horizontal block offsets + RGB split
                if (_DR_Glitch > 0.001)
                {
                    float row = floor(uv.y * 24 + floor(_Time.y * 14) * 7);
                    float g = step(1 - _DR_Glitch * 0.35, Hash(float2(row, floor(_Time.y * 14))));
                    uv.x += (Hash(float2(row, 3)) - 0.5) * 0.08 * g * _DR_Glitch;
                }
                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                float2 c = uv - 0.5; float r = length(c * float2(_ScreenParams.x / _ScreenParams.y, 1));
                if (_DR_Glitch > 0.001 || _DR_Speed > 0.001)
                {
                    float split = 0.004 * _DR_Glitch + 0.006 * _DR_Speed * smoothstep(0.25, 0.75, r);
                    col.r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + c * split * 2).r;
                    col.b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - c * split * 2).b;
                }
                half lum = dot(col.rgb, half3(0.299, 0.587, 0.114));
                // posterize the light (keeps hue, bands the value)
                if (_DR_Posterize > 0.001)
                {
                    half bands = 4;
                    half q = (floor(lum * bands + 0.5) / bands);
                    col.rgb = lerp(col.rgb, col.rgb * (q / max(lum, 0.001)), _DR_Posterize * 0.6);
                    lum = dot(col.rgb, half3(0.299, 0.587, 0.114));
                }
                if (_DR_Saturate != 0)
                {
                    col.rgb = lerp(lum.xxx, col.rgb, 1 + _DR_Saturate);
                }
                // halftone dots in the darks
                if (_DR_Halftone > 0.001)
                {
                    float2 sp = uv * _ScreenParams.xy / max(_DR_HalftoneScale, 2);
                    float2 rot = float2(sp.x * 0.7071 - sp.y * 0.7071, sp.x * 0.7071 + sp.y * 0.7071);
                    float2 cell = frac(rot) - 0.5;
                    float dark = saturate((0.42 - lum) / 0.36);   // dots live in the real shadows only (mid-tones stay clean)
                    float dotR = sqrt(dark) * 0.62;
                    float m = 1 - smoothstep(dotR - 0.08, dotR + 0.02, length(cell));
                    // dots only ever darken (a print dot is ink, never a lift — keeps the blacks crisp)
                    half3 dotCol = min(col.rgb, lerp(col.rgb * 0.45, _DR_HalftoneColor.rgb, _DR_HalftoneColor.a * 0.5));
                    col.rgb = lerp(col.rgb, dotCol, m * _DR_Halftone * step(0.02, dark));
                }
                // World 3 Pencil Sketch / Ink Cross-Hatching & Screentone Pass (with selective crimson-red accent preservation)
                if (_DR_Sketch > 0.001)
                {
                    float2 sp = uv * _ScreenParams.xy;
                    float grain = (Hash(floor(sp * 0.5)) - 0.5) * 0.045;
                    half3 paper = (_DR_PaperColor.a > 0.01 ? _DR_PaperColor.rgb : half3(0.96, 0.94, 0.90)) + grain;
                    half3 graphite = half3(0.11, 0.10, 0.14);

                    // Selective red accent preservation (Pip's apple red, crimson laser rings & rhythmic cues stay vivid)
                    float redDominance = col.r - max(col.g, col.b);
                    float redMask = smoothstep(0.16, 0.36, redDominance) * smoothstep(0.22, 0.45, col.r);

                    // Paper tonal base + multi-angle pencil cross-hatching
                    float tone = saturate(lum * 1.12 + 0.06);
                    float w1 = abs(frac((sp.x + sp.y + sin(sp.y * 0.09) * 2.2) * 0.14) - 0.5) * 2.0;
                    float w2 = abs(frac((sp.x - sp.y + cos(sp.x * 0.09) * 2.2) * 0.14) - 0.5) * 2.0;
                    float w3 = abs(frac((sp.y + sin(sp.x * 0.06) * 1.5) * 0.20) - 0.5) * 2.0;

                    float h1 = (1.0 - smoothstep(0.18, 0.36, w1)) * smoothstep(0.74, 0.46, tone);
                    float h2 = (1.0 - smoothstep(0.18, 0.36, w2)) * smoothstep(0.50, 0.24, tone);
                    float h3 = (1.0 - smoothstep(0.16, 0.32, w3)) * smoothstep(0.28, 0.08, tone);

                    // Screentone dots in mid-darks
                    float2 stCell = frac(float2(sp.x * 0.7071 - sp.y * 0.7071, sp.x * 0.7071 + sp.y * 0.7071) / 5.2) - 0.5;
                    float stDark = saturate((0.66 - tone) / 0.42);
                    float stDot = (1.0 - smoothstep(sqrt(stDark) * 0.48 - 0.08, sqrt(stDark) * 0.48 + 0.04, length(stCell))) * 0.55 * step(0.03, stDark);

                    float pencilShade = saturate(max(max(h1 * 0.65, h2 * 0.82), max(h3 * 0.95, stDot)));
                    half3 sketchRgb = lerp(paper * lerp(0.76, 1.04, smoothstep(0.12, 0.88, tone)), graphite, pencilShade);

                    // Double-pencil contour jitter (hand-drawn outer sketch line)
                    float2 jitterUv = uv + (float2(Hash(floor(sp * 0.25)), Hash(floor(sp * 0.25) + 17.0)) - 0.5) * (2.2 / _ScreenParams.xy);
                    float eJitter = Edge(jitterUv, (_DR_InkWidth * 0.9) / _ScreenParams.xy);
                    sketchRgb = lerp(sketchRgb, graphite, eJitter * 0.55);

                    // Blend back selective vivid crimson-red accents
                    half3 vividRed = half3(0.92, 0.10, 0.18) * max(col.r, 0.65);
                    sketchRgb = lerp(sketchRgb, vividRed, redMask * 0.88);
                    col.rgb = lerp(col.rgb, sketchRgb, _DR_Sketch);
                }
                // ink lines
                if (_DR_Ink > 0.001)
                {
                    float2 px = _DR_InkWidth / _ScreenParams.xy;
                    float e = Edge(uv, px);
                    col.rgb = lerp(col.rgb, _DR_InkColor.rgb, e * _DR_Ink);
                }
                // speed lines (radial streaks from the centre, only in the outer ring)
                if (_DR_Speed > 0.001)
                {
                    float ang = atan2(c.y, c.x);
                    float streak = Hash(float2(floor(ang * 90), floor(_Time.y * 18)));
                    float ln = step(0.82, streak) * smoothstep(0.32, 0.75, r);
                    col.rgb = lerp(col.rgb, _DR_SpeedColor.rgb, ln * _DR_Speed * _DR_SpeedColor.a);
                }
                return col;
            }
            ENDHLSL
        }
    }
}
