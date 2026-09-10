Shader "UI/Cyber Horizontal Line Noise"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1, 1, 1, 1)

        [Header(Cyber Line Noise)]
        _LineColor("Line Color", Color) = (0, 1, 0.92, 1)
        _LineAlpha("Line Alpha", Range(0, 1)) = 0.65
        _LineDensity("Line Density", Range(4, 256)) = 72
        _LineThickness("Line Thickness", Range(0.001, 0.25)) = 0.035
        _LineSoftness("Line Softness", Range(0.001, 0.25)) = 0.02
        _LineChance("Line Chance", Range(0, 1)) = 0.45
        _LineSpeed("Line Speed", Float) = 0.16
        _FlickerStrength("Flicker Strength", Range(0, 1)) = 0.35
        _BaseDarken("Base Darken", Range(0, 1)) = 0.08
        _Seed("Seed", Float) = 19.37

        [Header(UI Settings)]
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
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _MainTex_ST;
            float4 _ClipRect;

            fixed4 _LineColor;
            float _LineAlpha;
            float _LineDensity;
            float _LineThickness;
            float _LineSoftness;
            float _LineChance;
            float _LineSpeed;
            float _FlickerStrength;
            float _BaseDarken;
            float _Seed;

            float Random(float value)
            {
                return frac(sin(value * 12.9898 + _Seed * 78.233) * 43758.5453);
            }

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
                float y = IN.texcoord.y + _Time.y * _LineSpeed;
                float scaledY = y * max(_LineDensity, 1.0);
                float row = floor(scaledY);
                float phase = frac(scaledY);
                float distanceToLine = min(phase, 1.0 - phase);
                float lineShape = 1.0 - smoothstep(_LineThickness, _LineThickness + _LineSoftness, distanceToLine);
                float rowNoise = Random(row);
                float enabled = step(1.0 - _LineChance, rowNoise);
                float flicker = lerp(1.0, Random(row + floor(_Time.y * 24.0)), _FlickerStrength);
                float lineMask = lineShape * enabled * flicker * color.a;

                color.rgb *= 1.0 - _BaseDarken * lineMask;
                color.rgb = lerp(color.rgb, _LineColor.rgb, saturate(lineMask * _LineAlpha));
                color.a = saturate(color.a);

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
