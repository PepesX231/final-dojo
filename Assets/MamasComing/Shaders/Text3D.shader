Shader "MamasComing/Text3D"
{
    Properties { _MainTex ("Font Texture", 2D) = "white" {} _Color ("Text Color", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Lighting Off Cull Off ZWrite Off ZTest LEqual Blend SrcAlpha OneMinusSrcAlpha
        Offset -1, -1
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _MainTex_ST; float4 _Color;
            struct a { float4 v:POSITION; float4 c:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float4 c:COLOR; float2 uv:TEXCOORD0; };
            v2f vert(a i){ v2f o; o.pos=UnityObjectToClipPos(i.v); o.c=i.c*_Color; o.uv=TRANSFORM_TEX(i.uv,_MainTex); return o; }
            float4 frag(v2f i):SV_Target { float4 c=i.c; c.a*=tex2D(_MainTex,i.uv).a; return c; }
            ENDHLSL
        }
    }
}
