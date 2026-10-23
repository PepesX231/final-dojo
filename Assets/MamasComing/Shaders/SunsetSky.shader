Shader "MamasComing/SunsetSky"
{
    Properties
    {
        _Top ("Zenith", Color) = (0.20,0.22,0.52,1)
        _Mid ("Upper sky", Color) = (0.55,0.42,0.78,1)
        _Low ("Lower sky", Color) = (0.98,0.55,0.55,1)
        _Horizon ("Horizon", Color) = (1.0,0.72,0.42,1)
        _Ground ("Below horizon", Color) = (0.42,0.30,0.38,1)
        _SunColor ("Sun", Color) = (1.0,0.86,0.6,1)
        _SunGlow ("Sun glow", Color) = (1.0,0.55,0.3,1)
        _SunSize ("Sun size", Range(0.001,0.1)) = 0.035
        _Clouds ("Cloud streaks", Range(0,1)) = 0.35
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Top,_Mid,_Low,_Horizon,_Ground,_SunColor,_SunGlow; float _SunSize,_Clouds;
            struct v2f { float4 pos:SV_POSITION; float3 dir:TEXCOORD0; };
            v2f vert(float4 v:POSITION){ v2f o; o.pos=UnityObjectToClipPos(v); o.dir=v.xyz; return o; }
            float hash(float2 p){ return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float noise(float2 p){ float2 i=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x), lerp(hash(i+float2(0,1)),hash(i+float2(1,1)),f.x), f.y); }
            float4 frag(v2f i):SV_Target
            {
                float3 d=normalize(i.dir); float y=d.y;
                float3 sunDir=normalize(-_WorldSpaceLightPos0.xyz*-1);
                float3 c;
                if (y < 0) c = lerp(_Horizon.rgb*0.85, _Ground.rgb, saturate(-y*6));
                else {
                    c = lerp(_Horizon.rgb, _Low.rgb, saturate(y*7));
                    c = lerp(c, _Mid.rgb, saturate((y-0.12)*3.2));
                    c = lerp(c, _Top.rgb, saturate((y-0.42)*1.9));
                }
                float s = saturate(dot(d, sunDir));
                // warm halo hugging the horizon around the sun
                float halo = pow(s, 6) * 0.55 + pow(s, 40) * 0.6;
                c += _SunGlow.rgb * halo * saturate(1 - abs(y)*1.5);
                // soft pink cloud streaks
                if (y > 0.02) {
                    float2 uv = d.xz / (y + 0.25) * float2(1.2, 4.0);
                    float n = noise(uv*1.3) * 0.6 + noise(uv*3.1) * 0.4;
                    float band = smoothstep(0.55, 0.8, n) * saturate((0.45 - y) * 3) * _Clouds;
                    float3 cloud = lerp(float3(1.0,0.62,0.66), float3(1.0,0.85,0.6), pow(s,4));
                    c = lerp(c, cloud, band);
                }
                float disk = smoothstep(1 - _SunSize, 1 - _SunSize*0.7, s);
                c = lerp(c, _SunColor.rgb*1.6, disk);
                return float4(c,1);
            }
            ENDHLSL
        }
    }
}
