Shader "Notes/SpaceHold/SpaceHold_Default_Inside"
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

        _PingPongIntensityMin("PingPong Intensity Min", Range(0, 5)) = 0.8
        _PingPongIntensityMax("PingPong Intensity Max", Range(0, 5)) = 1.2
        _PingPongDuration("PingPong Duration", Float) = 1.0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard alpha:fade
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
        float _MinZ;
        float _MaxZ;
        float _PingPongIntensityMin;
        float _PingPongIntensityMax;
        float _PingPongDuration;

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float inRange = step(_MinZ, IN.worldPos.z) * step(IN.worldPos.z, _MaxZ);

            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * lerp(_SecondaryColor, _Color, inRange);

            float duration = max(_PingPongDuration, 0.0001);
            float pingPong = abs(frac(_Time.y / duration) * 2.0 - 1.0);
            float intensity = lerp(_PingPongIntensityMin, _PingPongIntensityMax, pingPong);

            o.Albedo = c.rgb * intensity;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }

        ENDCG
    }

    FallBack "Diffuse"
}
