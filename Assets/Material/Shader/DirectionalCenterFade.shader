Shader "Custom/DirectionalCenterFadeWithEnd"
{
    Properties
    {
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)

        _FadeStart("Fade Start (0-0.5)", Range(0.0, 0.5)) = 0.2
        _FadeEnd("Fade End (Fade=100%)", Range(0.0, 0.5)) = 0.4

        [Toggle] _FadeTop("Fade Top", Float) = 1
        [Toggle] _FadeBottom("Fade Bottom", Float) = 1
        [Toggle] _FadeLeft("Fade Left", Float) = 1
        [Toggle] _FadeRight("Fade Right", Float) = 1
    }

        SubShader
        {
            Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
            LOD 200

            CGPROGRAM
            #pragma surface surf Standard alpha:fade

            sampler2D _MainTex;
            fixed4 _Color;
            float _FadeStart;
            float _FadeEnd;
            float _FadeTop;
            float _FadeBottom;
            float _FadeLeft;
            float _FadeRight;

            struct Input
            {
                float2 uv_MainTex;
            };

            void surf(Input IN, inout SurfaceOutputStandard o)
            {
                float2 uv = IN.uv_MainTex;
                fixed4 col = tex2D(_MainTex, uv) * _Color;

                float fade = 0;

                float fadeRange = max(0.0001, _FadeEnd - _FadeStart); // avoid division by zero

                if (_FadeTop > 0)
                    fade = max(fade, saturate((0.5 - uv.y - _FadeStart) / fadeRange));
                if (_FadeBottom > 0)
                    fade = max(fade, saturate((uv.y - 0.5 - _FadeStart) / fadeRange));
                if (_FadeLeft > 0)
                    fade = max(fade, saturate((0.5 - uv.x - _FadeStart) / fadeRange));
                if (_FadeRight > 0)
                    fade = max(fade, saturate((uv.x - 0.5 - _FadeStart) / fadeRange));

                col.a *= (1.0 - fade);

                o.Albedo = col.rgb;
                o.Alpha = col.a;
            }
            ENDCG
        }

            FallBack "Transparent/VertexLit"
}
