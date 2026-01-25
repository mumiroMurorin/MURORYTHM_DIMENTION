Shader "UI/InverseRectMaskImage"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)

        [Header(Mask Settings)]
        // マスクの中心座標 (UV座標: 0.0 ~ 1.0)
        _MaskCenter("Mask Center (UV)", Vector) = (0.5, 0.5, 0, 0)
            // マスクのサイズ (UV座標: 幅, 高さ)
            _MaskSize("Mask Size (UV)", Vector) = (0.3, 0.3, 0, 0)
            // 境界のぼかし具合
            _Softness("Edge Softness", Range(0, 0.5)) = 0.01

            // --- UI用標準プロパティ ---
            _StencilComp("Stencil Comparison", Float) = 8
            _Stencil("Stencil ID", Float) = 0
            _StencilOp("Stencil Operation", Float) = 0
            _StencilWriteMask("Stencil Write Mask", Float) = 255
            _StencilReadMask("Stencil Read Mask", Float) = 255
            _ColorMask("Color Mask", Float) = 15
            [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0
    }

        SubShader
        {
            Tags
            {
                "Queue" = "Transparent"
                "IgnoreProjector" = "True"
                "RenderType" = "Transparent"
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

            Cull Off
            Lighting Off
            ZWrite Off
            ZTest[unity_GUIZTestMode]
            Blend SrcAlpha OneMinusSrcAlpha
            ColorMask[_ColorMask]

            Pass
            {
                Name "Default"
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma target 2.0

                #include "UnityCG.cginc"
                #include "UnityUI.cginc"

                #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
                #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

                struct appdata_t
                {
                    float4 vertex   : POSITION;
                    float4 color    : COLOR;
                    float2 texcoord : TEXCOORD0;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f
                {
                    float4 vertex   : SV_POSITION;
                    fixed4 color : COLOR;
                    float2 texcoord  : TEXCOORD0;
                    float4 worldPosition : TEXCOORD1;
                    UNITY_VERTEX_OUTPUT_STEREO
                };

                sampler2D _MainTex;
                fixed4 _Color;
                float4 _TextureSampleAdd;
                float4 _ClipRect;
                float4 _MainTex_ST;

                // マスク用変数
                float4 _MaskCenter;
                float4 _MaskSize;
                float _Softness;

                v2f vert(appdata_t v)
                {
                    v2f OUT;
                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                    OUT.worldPosition = v.vertex;
                    OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                    OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                    OUT.color = v.color * _Color;
                    return OUT;
                }

                fixed4 frag(v2f IN) : SV_Target
                {
                    half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                    // --- 逆マスク処理 (ここから) ---

                    // UV座標とマスク中心の距離を計算 (絶対値)
                    float2 dist = abs(IN.texcoord - _MaskCenter.xy);

                    // 矩形の半径 (サイズの半分)
                    float2 halfSize = _MaskSize.xy * 0.5;

                    // 範囲内判定
                    // x軸、y軸ともに範囲内であれば 1 (透明にする)、外なら 0 (描画する)
                    // smoothstepを使って境界を少しぼかす
                    float inMaskX = smoothstep(halfSize.x, halfSize.x - _Softness, dist.x);
                    float inMaskY = smoothstep(halfSize.y, halfSize.y - _Softness, dist.y);

                    // 両方の軸で範囲内(=重なっている部分)の強度
                    float maskWeight = inMaskX * inMaskY;

                    // アルファを削る (1 - maskWeight)
                    // マスク内(maskWeight=1)なら透明(0)、外ならそのまま(1)
                    color.a *= (1.0 - maskWeight);

                    // --- 逆マスク処理 (ここまで) ---

                    #ifdef UNITY_UI_CLIP_RECT
                    color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                    #endif

                    #ifdef UNITY_UI_ALPHACLIP
                    clip(color.a - 0.001);
                    #endif

                    return color;
                }
                ENDCG
            }
        }
}
