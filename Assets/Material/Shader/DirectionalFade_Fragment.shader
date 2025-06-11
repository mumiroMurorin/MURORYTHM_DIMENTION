Shader "Custom/DirectionalEdgeFade"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color Tint", Color) = (1,1,1,1)

        _FadeStart ("Fade Start (0-1)", Range(0.0, 1.0)) = 0.2
        _FadeEnd ("Fade End (0-1)", Range(0.0, 1.0)) = 0.4

        [Toggle] _FadeTop ("Fade Top", Float) = 1
        [Toggle] _FadeBottom ("Fade Bottom", Float) = 1
        [Toggle] _FadeLeft ("Fade Left", Float) = 1
        [Toggle] _FadeRight ("Fade Right", Float) = 1
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            float _FadeStart;
            float _FadeEnd;

            float _FadeTop;
            float _FadeBottom;
            float _FadeLeft;
            float _FadeRight;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 uvRaw : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);     // Tiling/Offset適用済み
                o.uvRaw = v.uv;                           // 元のUV（0～1）
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uvRaw = i.uvRaw;
                float fade = 0;
                float fadeRange = max(0.0001, _FadeEnd - _FadeStart);

                if (_FadeTop > 0)
                    fade = max(fade, saturate((1.0 - uvRaw.y - _FadeStart) / fadeRange));
                if (_FadeBottom > 0)
                    fade = max(fade, saturate((uvRaw.y - _FadeStart) / fadeRange));
                if (_FadeLeft > 0)
                    fade = max(fade, saturate((uvRaw.x - _FadeStart) / fadeRange));
                if (_FadeRight > 0)
                    fade = max(fade, saturate((1.0 - uvRaw.x - _FadeStart) / fadeRange));

                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                col.a *= (1.0 - fade);

                return col;
            }
            ENDCG
        }
    }

    FallBack "Unlit/Transparent"
}
