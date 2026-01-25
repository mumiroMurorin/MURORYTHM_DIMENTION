Shader "Custom/GlintEffectWithImageColor"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _GlintColor("Glint Color", Color) = (1,1,1,1)
        _Speed("Glint Speed", Float) = 1.0
        _GlintWidth("Glint Width", Range(0.01, 1)) = 0.2
        _Angle("Glint Angle (Degrees)", Range(0,360)) = 45

            // --- 追加: UIのMaskに対応するための標準プロパティ ---
            _StencilComp("Stencil Comparison", Float) = 8
            _Stencil("Stencil ID", Float) = 0
            _StencilOp("Stencil Operation", Float) = 0
            _StencilWriteMask("Stencil Write Mask", Float) = 255
            _StencilReadMask("Stencil Read Mask", Float) = 255
            _ColorMask("Color Mask", Float) = 15
            // ------------------------------------------------
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

            // --- 修正: 固定値ではなくプロパティを使用するように変更 ---
            Stencil
            {
                Ref[_Stencil]
                Comp[_StencilComp]
                Pass[_StencilOp]
                ReadMask[_StencilReadMask]
                WriteMask[_StencilWriteMask]
            }
            // -------------------------------------------------------

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            Lighting Off
            ZWrite Off
            // 追加: カラーマスク設定（UIの標準動作に合わせるため）
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

                    // --- 前回の修正ロジック（そのまま維持） ---
                    float2 centeredUV = i.uv - float2(0.5, 0.5);
                    float rad = radians(_Angle);
                    float2 dir = float2(cos(rad), sin(rad));
                    float proj = dot(centeredUV, dir);

                    float loopSpan = 2.0 + _GlintWidth;
                    float timeVal = frac(_Time.y * _Speed);
                    float glintPos = (timeVal - 0.5) * loopSpan;

                    float dist = abs(proj - glintPos);
                    float mask = smoothstep(_GlintWidth, 0.0, dist);

                    col.rgb += _GlintColor.rgb * mask * _GlintColor.a;

                    return col;
                }
                ENDCG
            }
        }
}