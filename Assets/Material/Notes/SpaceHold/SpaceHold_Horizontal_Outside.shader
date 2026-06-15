Shader "Notes/SpaceHold/SpaceHold_Horizontal_Outside"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Color (Inside Range)", Color) = (1,1,1,1)
        _SecondaryColor("Secondary Color (Outside Range)", Color) = (1,1,1,0)

        _MinZ("Visible Range Min Z", Float) = -20.0
        _MaxZ("Visible Range Max Z", Float) = 187.0

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
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        LOD 100
        Cull Back

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _StripeTex;
            float4 _StripeTex_ST;
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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                float inRange = step(_MinZ, i.worldPos.z) * step(i.worldPos.z, _MaxZ);
                fixed4 tintColor = lerp(_SecondaryColor, _Color, inRange);

                float duration = max(_PingPongDuration, 0.0001);
                float pingPong = abs(frac(_Time.y / duration) * 2.0 - 1.0);
                float intensity = lerp(_PingPongIntensityMin, _PingPongIntensityMax, pingPong);

                float stripePhase = i.uv.x * _StripeFrequency * UNITY_TWO_PI;
                float stripeWave = (cos(stripePhase) + 1.0) * 0.5;
                float stripeThreshold = saturate(1.0 - _StripeSecondaryWidth);
                float stripeFeather = max(_StripeBlendSoftness, 0.0001);
                float stripeMask = smoothstep(
                    stripeThreshold - stripeFeather,
                    stripeThreshold + stripeFeather,
                    stripeWave);

                col *= tintColor;
                fixed4 stripeSample = tex2D(_StripeTex, TRANSFORM_TEX(i.uv, _StripeTex));
                fixed4 stripeCol = stripeSample * _StripeColor;
                stripeCol.a *= col.a;
                col = lerp(col, stripeCol, stripeMask);
                col.rgb *= intensity;
                UNITY_APPLY_FOG(i.fogCoord, col);

                return col;
            }
            ENDCG
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull Back

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_shadowcaster

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                V2F_SHADOW_CASTER;
                float2 uv : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _SecondaryColor;
            float _MinZ;
            float _MaxZ;

            v2f vertShadow(appdata v)
            {
                v2f o;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            float4 fragShadow(v2f i) : SV_Target
            {
                fixed alpha = tex2D(_MainTex, i.uv).a;
                float inRange = step(_MinZ, i.worldPos.z) * step(i.worldPos.z, _MaxZ);
                fixed tintAlpha = lerp(_SecondaryColor.a, _Color.a, inRange);

                clip(alpha * tintAlpha - 0.001h);
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }

    FallBack "Transparent/VertexLit"
}
