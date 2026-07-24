Shader "Custom/ClampZ_SideLine"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _SecondaryColor("Secondary Color", Color) = (1,1,1,1)
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0

        _MinZ("Visible Range Min Z", Float) = 0.0
        _MaxZ("Visible Range Max Z", Float) = 1.0

        _LineColor("Line Color", Color) = (1,1,1,1)
        _LineDistanceFromEdge("Line Distance From Edge", Range(0,0.5)) = 0.08
        _LineWidth("Line Width", Range(0.001,0.25)) = 0.03
        _LineIntensity("Line Intensity", Range(0,10)) = 1.5
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }
        LOD 200

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            fixed4 _Color;
            fixed4 _SecondaryColor;
            fixed4 _LineColor;
            float _MinZ;
            float _MaxZ;
            float _LineDistanceFromEdge;
            float _LineWidth;
            float _LineIntensity;

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

            float GetLineMask(float uvX, float targetX, float lineWidth)
            {
                float halfWidth = max(lineWidth * 0.5, 0.0001);
                float distanceToLine = abs(uvX - targetX);
                return saturate(1.0 - (distanceToLine / halfWidth));
            }

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
                float inRange = step(_MinZ, i.worldPos.z) * step(i.worldPos.z, _MaxZ);

                fixed4 tint = lerp(_SecondaryColor, _Color, inRange);
                fixed4 c = tex2D(_MainTex, i.uv) * tint;

                float leftLine = GetLineMask(i.uv.x, _LineDistanceFromEdge, _LineWidth);
                float rightLine = GetLineMask(i.uv.x, 1.0 - _LineDistanceFromEdge, _LineWidth);
                float lineMask = saturate(max(leftLine, rightLine));

                // ライト計算を行わず、元の色とライン発光だけで描画する
                c.rgb += _LineColor.rgb * lineMask * _LineIntensity * c.a;
                return c;
            }
            ENDCG
        }
    }

    FallBack Off
}
