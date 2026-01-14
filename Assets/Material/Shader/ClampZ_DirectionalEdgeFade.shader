Shader "Custom/ClampZ_DirectionalEdgeFade"
{
    Properties
    {
        [Header(Base Settings)]
        _MainTex("Texture", 2D) = "white" {}
        _Color("Inside Range Color", Color) = (1,1,1,1)
        _SecondaryColor("Outside Range Color", Color) = (1,1,1,0)

        [Header(Z Range Settings)]
        _MinZ("Visible Range Min Z", Float) = -20.0
        _MaxZ("Visible Range Max Z", Float) = 187.0

        [Header(Edge Fade Settings)]
        _FadeStart("Fade Start (0-1)", Range(0.0, 1.0)) = 0.2
        _FadeEnd("Fade End (0-1)", Range(0.0, 1.0)) = 0.4

        [Toggle] _FadeTop("Fade Top", Float) = 1
        [Toggle] _FadeBottom("Fade Bottom", Float) = 1
        [Toggle] _FadeLeft("Fade Left", Float) = 1
        [Toggle] _FadeRight("Fade Right", Float) = 1
    }

        SubShader
        {
            Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
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
                fixed4 _SecondaryColor;

                float _MinZ;
                float _MaxZ;

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
                    float3 worldPos : TEXCOORD2; // Z判定用に追加
                };

                v2f vert(appdata v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                    o.uvRaw = v.uv;
                    // モデル座標をワールド座標に変換
                    o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    // --- 1. Z軸による色の決定 (ClampZロジック) ---
                    float inRange = step(_MinZ, i.worldPos.z) * step(i.worldPos.z, _MaxZ);
                    fixed4 tintColor = lerp(_SecondaryColor, _Color, inRange);

                    // --- 2. 端のフェード計算 (EdgeFadeロジック) ---
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

                    // --- 3. 最終的な色の合成 ---
                    fixed4 col = tex2D(_MainTex, i.uv) * tintColor;

                    // フェード値をアルファに適用 (1.0 - fade で内側を不透明にする)
                    col.a *= (1.0 - fade);

                    return col;
                }
                ENDCG
            }
        }

            FallBack "Unlit/Transparent"
}