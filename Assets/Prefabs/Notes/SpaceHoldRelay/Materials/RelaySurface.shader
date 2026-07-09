Shader "Notes/SpaceHold/RelaySurface"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}

        _CenterColor("Center Color", Color) = (1,1,1,1)
        _OuterColor("Outer Color", Color) = (1,1,1,1)
        _SecondaryColor("Secondary Color (Outside Range)", Color) = (1,1,1,0)

        _MinZ("Visible Range Min Z", Float) = -20.0
        _MaxZ("Visible Range Max Z", Float) = 187.0

        _UVCenter("UV Center", Vector) = (0.5, 0.5, 0, 0)
        _GradientRadius("Gradient Radius", Range(0.01, 1.0)) = 0.707

        _RippleStrength("Ripple Strength", Range(0, 2)) = 0.35
        _RippleColor("Ripple Color", Color) = (1,1,1,1)
        _RippleSpeed("Ripple Speed", Float) = 1.0
        _RippleFrequency("Ripple Frequency", Float) = 10.0
        _RippleWidth("Ripple Width", Range(0.01, 1.0)) = 0.2
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

            fixed4 _CenterColor;
            fixed4 _OuterColor;
            fixed4 _SecondaryColor;
            float _MinZ;
            float _MaxZ;

            float4 _UVCenter;
            float _GradientRadius;

            float _RippleStrength;
            fixed4 _RippleColor;
            float _RippleSpeed;
            float _RippleFrequency;
            float _RippleWidth;

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
                fixed4 texCol = tex2D(_MainTex, i.uv);

                float inRange = step(_MinZ, i.worldPos.z) * step(i.worldPos.z, _MaxZ);

                fixed4 col = lerp(_SecondaryColor, fixed4(1,1,1,1), inRange);

                float2 uvCenter = _UVCenter.xy;
                float dist = distance(i.uv, uvCenter);
                float radialT = saturate(dist / max(_GradientRadius, 0.0001));

                fixed4 gradientColor = lerp(_CenterColor, _OuterColor, radialT);
                col *= gradientColor;
                col *= texCol;

                float wavePhase = (dist * _RippleFrequency) - (_Time.y * _RippleSpeed);
                float wave = sin(wavePhase * 6.2831853) * 0.5 + 0.5;
                float rippleMask = smoothstep(1.0 - _RippleWidth, 1.0, wave);
                float rippleT = saturate(rippleMask * _RippleStrength);
                col.rgb = lerp(col.rgb, _RippleColor.rgb, rippleT);
                col.a = lerp(col.a, _RippleColor.a, rippleT);

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

            fixed4 _CenterColor;
            fixed4 _OuterColor;
            fixed4 _SecondaryColor;
            float _MinZ;
            float _MaxZ;

            float4 _UVCenter;
            float _GradientRadius;
            fixed4 _RippleColor;

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
                fixed4 baseColor = lerp(_SecondaryColor, fixed4(1,1,1,1), inRange);

                float radialT = saturate(distance(i.uv, _UVCenter.xy) / max(_GradientRadius, 0.0001));
                fixed4 gradientColor = lerp(_CenterColor, _OuterColor, radialT);

                fixed finalAlpha = alpha * baseColor.a * gradientColor.a;
                clip(finalAlpha - 0.001h);
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }

    FallBack "Transparent/VertexLit"
}
