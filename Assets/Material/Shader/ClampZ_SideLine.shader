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

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows alpha:fade addshadow
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        fixed4 _SecondaryColor;
        fixed4 _LineColor;
        float _MinZ;
        float _MaxZ;
        float _LineDistanceFromEdge;
        float _LineWidth;
        float _LineIntensity;

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

        float GetLineMask(float uvX, float targetX, float lineWidth)
        {
            float halfWidth = max(lineWidth * 0.5, 0.0001);
            float distanceToLine = abs(uvX - targetX);
            return saturate(1.0 - (distanceToLine / halfWidth));
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float inRange = step(_MinZ, IN.worldPos.z) * step(IN.worldPos.z, _MaxZ);

            fixed4 tint = lerp(_SecondaryColor, _Color, inRange);
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * tint;

            float leftLine = GetLineMask(IN.uv_MainTex.x, _LineDistanceFromEdge, _LineWidth);
            float rightLine = GetLineMask(IN.uv_MainTex.x, 1.0 - _LineDistanceFromEdge, _LineWidth);
            float lineMask = saturate(max(leftLine, rightLine));

            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
            o.Emission = _LineColor.rgb * lineMask * _LineIntensity * c.a;
        }
        ENDCG
    }

    FallBack "Standard"
}
