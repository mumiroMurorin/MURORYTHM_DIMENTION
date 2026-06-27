Shader "Custom/Judgement/WorldPositionRadialFadeUnlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)

        _FadeCenter ("Fade Center (World Position)", Vector) = (0,0,0,0)
        _FadeStartDistance ("Fade Start Distance", Float) = 0
        _FadeEndDistance ("Fade End Distance", Float) = 5
        _CenterAlpha ("Center Alpha", Range(0,1)) = 0
        _FadePower ("Fade Strength", Range(0.1,8)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float4 _FadeCenter;
            float _FadeStartDistance;
            float _FadeEndDistance;
            float _CenterAlpha;
            float _FadePower;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, i.uv) * _Color;

                float startDistance = max(0.0, _FadeStartDistance);
                float endDistance = max(startDistance + 0.0001, _FadeEndDistance);
                float distanceFromCenter = distance(i.worldPos, _FadeCenter.xyz);

                // 中心ではCenter Alpha、終了距離の外側では元の透明度になる
                float fade = saturate(
                    (distanceFromCenter - startDistance) /
                    (endDistance - startDistance));
                fade = smoothstep(0.0, 1.0, fade);
                fade = pow(fade, max(0.0001, _FadePower));

                color.a *= lerp(_CenterAlpha, 1.0, fade);
                return color;
            }
            ENDCG
        }
    }

    FallBack Off
}
