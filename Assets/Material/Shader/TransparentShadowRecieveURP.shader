Shader "Custom/Matte Shadow"
{
    Properties
    {
        _Color("Main Color", Color) = (1,1,1,1)
        _MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.5
    }

        SubShader
        {
            Tags { "Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TransparentCutout" }
            LOD 200

            // "Blend Zero SrcColor" は「背景色に乗算（マットシャドウ表現）」するため
            Blend Zero SrcColor
            Cull Off
            ZWrite On

            CGPROGRAM
            // サーフェスシェーダ宣言
            #pragma surface surf ShadowOnly alphatest:_Cutoff

            sampler2D _MainTex;
            fixed4 _Color;

            struct Input
            {
                float2 uv_MainTex;
            };

            void surf(Input IN, inout SurfaceOutput o)
            {
                fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
                o.Albedo = c.rgb;
                o.Alpha = c.a;
            }

            // 影だけ描画するカスタムライティング関数
            inline half4 LightingShadowOnly(SurfaceOutput s, half3 lightDir, half atten)
            {
                half shadow = 1 - atten; // 影部分だけ出したいので反転
                return half4(s.Albedo * shadow, s.Alpha * shadow);
            }

            ENDCG
        }

            FallBack "Transparent/Cutout/Diffuse"
}