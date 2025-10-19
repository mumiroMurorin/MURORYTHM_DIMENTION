Shader "Custom/PolygonGlow"
{
    Properties
    {
        _Color("Glow Color", Color) = (1,1,1,1)
        _GlowWidth("Glow Width", Range(0.001, 0.5)) = 0.1
        _Intensity("Intensity", Range(0,5)) = 2.0
    }

        SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            fixed4 _Color;
            float _GlowWidth;
            float _Intensity;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                // UVは0～1範囲で正規化
                o.uv = v.vertex.xy;
                return o;
            }

            // 中心から境界までの距離ベースで発光
            fixed4 frag(v2f i) : SV_Target
            {
                // ここでは簡易的に、距離0を中心、1を外縁とする仮想距離
                float dist = length(i.uv - 0.5);
                float glow = smoothstep(0.5, 0.5 - _GlowWidth, dist);
                fixed4 col = _Color * glow * _Intensity;
                col.a = glow;
                return col;
            }
            ENDHLSL
        }
    }
}
