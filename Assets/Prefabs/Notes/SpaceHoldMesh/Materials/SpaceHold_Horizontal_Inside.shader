Shader "Notes/SpaceHold/SpaceHold_Horizontal_Inside"
{
    Properties
    {
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
        _SecondaryColor("Secondary Color", Color) = (1,1,1,1)
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0

        _MinZ("Visible Range Min Z", Float) = 0.0
        _MaxZ("Visible Range Max Z", Float) = 1.0

        _PingPongIntensityMin("PingPong Intensity Min", Range(0, 5)) = 0.8
        _PingPongIntensityMax("PingPong Intensity Max", Range(0, 5)) = 1.2
        _PingPongDuration("PingPong Duration", Float) = 1.0

        _StripeColor("Stripe Color", Color) = (1,1,1,1)
        _StripeTex("Stripe Texture", 2D) = "white" {}
        _StripeFrequency("Stripe Frequency", Float) = 4.0
        _StripeSecondaryWidth("Stripe Secondary Width", Range(0.01, 0.95)) = 0.25
        _StripeBlendSoftness("Stripe Blend Softness", Range(0.001, 0.5)) = 0.08
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard alpha:fade
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _StripeTex;
        float4 _StripeTex_ST;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        fixed4 _SecondaryColor;
        fixed4 _StripeColor;
        float _MinZ;
        float _MaxZ;
        float _PingPongIntensityMin;
        float _PingPongIntensityMax;
        float _PingPongDuration;
        float _StripeFrequency;
        float _StripeSecondaryWidth;
        float _StripeBlendSoftness;

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float inRange = step(_MinZ, IN.worldPos.z) * step(IN.worldPos.z, _MaxZ);

            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * lerp(_SecondaryColor, _Color, inRange);

            float duration = max(_PingPongDuration, 0.0001);
            float pingPong = abs(frac(_Time.y / duration) * 2.0 - 1.0);
            float intensity = lerp(_PingPongIntensityMin, _PingPongIntensityMax, pingPong);

            float stripePhase = IN.uv_MainTex.x * _StripeFrequency * UNITY_TWO_PI;
            float stripeWave = (cos(stripePhase) + 1.0) * 0.5;
            float stripeThreshold = saturate(1.0 - _StripeSecondaryWidth);
            float stripeFeather = max(_StripeBlendSoftness, 0.0001);
            float stripeMask = smoothstep(
                stripeThreshold - stripeFeather,
                stripeThreshold + stripeFeather,
                stripeWave);

            fixed4 stripeSample = tex2D(_StripeTex, TRANSFORM_TEX(IN.uv_MainTex, _StripeTex));
            fixed4 stripeCol = stripeSample * _StripeColor;
            stripeCol.a *= c.a;
            c = lerp(c, stripeCol, stripeMask);

            o.Albedo = c.rgb * intensity;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }

        ENDCG
    }

    FallBack "Diffuse"
}
