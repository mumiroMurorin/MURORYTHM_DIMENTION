Shader "Custom/GlintEffect_Rainbow"
{
    Properties
    {
        [PerRendererData] _MainTex("Texture", 2D) = "white" {}
    // _GlintColorはRGBは使いませんが、Alpha(透明度)を光の強さ調整に使います
    _GlintColor("Glint Intensity (Alpha only)", Color) = (1,1,1,1)

        // --- 追加: 虹色のグラデーションテクスチャ ---
        [NoScaleOffset] _RampTex("Rainbow Ramp Texture", 2D) = "white" {}
    // ---------------------------------------

    _Speed("Glint Speed", Float) = 1.0
    _GlintWidth("Glint Width", Range(0.01, 1)) = 0.2
    _Angle("Glint Angle (Degrees)", Range(0,360)) = 45

        // UI Mask用プロパティ
        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255
        _ColorMask("Color Mask", Float) = 15
    }

        SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref[_Stencil]
            Comp[_StencilComp]
            Pass[_StencilOp]
            ReadMask[_StencilReadMask]
            WriteMask[_StencilWriteMask]
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off
        ColorMask[_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            struct v2f
            {
                float2 uv     : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color  : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            // 追加したランプテクスチャ
            sampler2D _RampTex;
            float4 _GlintColor;
            float _Speed;
            float _GlintWidth;
            float _Angle;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                col *= i.color;

                // --- グリント位置計算 ---
                float2 centeredUV = i.uv - float2(0.5, 0.5);
                float rad = radians(_Angle);
                float2 dir = float2(cos(rad), sin(rad));
                float proj = dot(centeredUV, dir);

                float loopSpan = 2.0 + _GlintWidth;
                float timeVal = frac(_Time.y * _Speed);
                float glintPos = (timeVal - 0.5) * loopSpan;

                // 中心からの「符号付き」距離を計算
                float signedDist = proj - glintPos;
                // マスク用の絶対距離
                float dist = abs(signedDist);

                // 光の形状マスク (中心が濃く、端が薄い)
                float mask = smoothstep(_GlintWidth, 0.0, dist);

                // --- 虹色処理の追加 ---

                // 1. 光の筋の幅の中での位置を 0～1 に正規化する
                // signedDist は -_GlintWidth ～ +_GlintWidth の範囲
                // これを 0 (左端) ～ 0.5 (中心) ～ 1.0 (右端) に変換
                float rampUV = (signedDist / _GlintWidth) * 0.5 + 0.5;

                // 2. ランプテクスチャから色を取得
                // 横方向(U)にrampUVを使い、縦(V)は中央の0.5で固定
                fixed4 rainbowColor = tex2D(_RampTex, float2(rampUV, 0.5));

                // 3. 合成
                // rainbowColor.rgb : テクスチャの虹色
                // mask : 光の形
                // _GlintColor.a : 全体の明るさ調整用
                col.rgb += rainbowColor.rgb * mask * _GlintColor.a;

                return col;
            }
            ENDCG
        }
    }
}