Shader "UI/Colored Image Outline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width", Float) = 2
        _BlurWidth ("Blur Width", Float) = 0
        _BlurSamples ("Blur Samples", Range(1,4)) = 2
        _AlphaThreshold ("Alpha Threshold", Range(0,1)) = 0.01
        _SpriteUVRect ("Sprite UV Rect", Vector) = (0,0,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
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
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _OutlineColor;
            float _OutlineWidth;
            float _BlurWidth;
            float _BlurSamples;
            float _AlphaThreshold;
            float4 _MainTex_TexelSize;
            float4 _SpriteUVRect;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed SampleAlpha(float2 uv)
            {
                if (uv.x < _SpriteUVRect.x || uv.y < _SpriteUVRect.y || uv.x > _SpriteUVRect.z || uv.y > _SpriteUVRect.w)
                {
                    return 0;
                }

                return tex2D(_MainTex, uv).a;
            }

            fixed SampleOutlineAlpha(float2 uv, float width)
            {
                float2 offset = _MainTex_TexelSize.xy * width;

                fixed alpha = 0;
                alpha = max(alpha, SampleAlpha(uv + float2(offset.x, 0)));
                alpha = max(alpha, SampleAlpha(uv + float2(-offset.x, 0)));
                alpha = max(alpha, SampleAlpha(uv + float2(0, offset.y)));
                alpha = max(alpha, SampleAlpha(uv + float2(0, -offset.y)));
                alpha = max(alpha, SampleAlpha(uv + float2(offset.x, offset.y)));
                alpha = max(alpha, SampleAlpha(uv + float2(-offset.x, offset.y)));
                alpha = max(alpha, SampleAlpha(uv + float2(offset.x, -offset.y)));
                alpha = max(alpha, SampleAlpha(uv + float2(-offset.x, -offset.y)));
                return alpha;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 baseColor = tex2D(_MainTex, IN.texcoord) * IN.color;

                fixed outsideMask = 0;
                fixed hardAlpha = SampleOutlineAlpha(IN.texcoord, _OutlineWidth);
                outsideMask = max(outsideMask, saturate((hardAlpha - baseColor.a) / max(_AlphaThreshold, 0.0001)));

                [unroll]
                for (int i = 1; i <= 4; i++)
                {
                    float sampleEnabled = step(i, _BlurSamples) * step(0.0001, _BlurWidth);
                    float t = i / max(_BlurSamples, 1);
                    float width = _OutlineWidth + _BlurWidth * t;
                    fixed blurAlpha = SampleOutlineAlpha(IN.texcoord, width);
                    fixed blurMask = saturate((blurAlpha - baseColor.a) / max(_AlphaThreshold, 0.0001));
                    outsideMask = max(outsideMask, blurMask * (1 - t) * sampleEnabled);
                }

                fixed4 outline = _OutlineColor;
                outline.a *= outsideMask * IN.color.a;

                fixed4 color = lerp(outline, baseColor, baseColor.a);
                color.a = max(outline.a, baseColor.a);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
