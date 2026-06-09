Shader "UI/InnerShadowImage"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)

        [Header(Inner Shadow)]
        _ShadowColor("Shadow Color", Color) = (0,0,0,1)
        _ShadowStrength("Shadow Strength", Range(0, 1)) = 0.5
        _ShadowWidth("Shadow Width", Range(0.001, 0.5)) = 0.08
        _ShadowSoftness("Shadow Softness", Range(0.001, 0.5)) = 0.04
        _ShadowOffsetX("Shadow Offset X", Range(-0.5, 0.5)) = 0
        _ShadowOffsetY("Shadow Offset Y", Range(-0.5, 0.5)) = 0
        _ShadowAspect("Shadow Aspect (X,Y)", Vector) = (1,1,0,0)

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
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _ShadowColor;
            float _ShadowStrength;
            float _ShadowWidth;
            float _ShadowSoftness;
            float _ShadowOffsetX;
            float _ShadowOffsetY;
            float4 _ShadowAspect;
            float4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

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

                float2 shiftedUv = IN.texcoord - float2(_ShadowOffsetX, _ShadowOffsetY);
                float leftDist = shiftedUv.x;
                float rightDist = 1.0 - shiftedUv.x;
                float bottomDist = shiftedUv.y;
                float topDist = 1.0 - shiftedUv.y;

                float horizontalScale = max(_ShadowAspect.x, 0.0001);
                float verticalScale = max(_ShadowAspect.y, 0.0001);

                float horizontalWidth = max(_ShadowWidth * horizontalScale, 0.0001);
                float verticalWidth = max(_ShadowWidth * verticalScale, 0.0001);
                float horizontalSoftness = max(_ShadowSoftness * horizontalScale, 0.0001);
                float verticalSoftness = max(_ShadowSoftness * verticalScale, 0.0001);

                float leftMask = 1.0 - smoothstep(horizontalWidth, horizontalWidth + horizontalSoftness, leftDist);
                float rightMask = 1.0 - smoothstep(horizontalWidth, horizontalWidth + horizontalSoftness, rightDist);
                float bottomMask = 1.0 - smoothstep(verticalWidth, verticalWidth + verticalSoftness, bottomDist);
                float topMask = 1.0 - smoothstep(verticalWidth, verticalWidth + verticalSoftness, topDist);

                float shadowMask = saturate(max(max(leftMask, rightMask), max(bottomMask, topMask)));
                shadowMask *= saturate(_ShadowStrength) * saturate(_ShadowColor.a) * color.a;

                color.rgb *= lerp(1.0.xxx, _ShadowColor.rgb, shadowMask);

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
