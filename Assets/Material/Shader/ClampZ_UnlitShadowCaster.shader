Shader "Custom/ClampZ_UnlitShadowCaster"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Color (Inside Range)", Color) = (1,1,1,1)
        _SecondaryColor("Secondary Color (Outside Range)", Color) = (1,1,1,0)

        _MinZ("Visible Range Min Z", Float) = -20.0
        _MaxZ("Visible Range Max Z", Float) = 187.0
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
            fixed4 _Color;
            fixed4 _SecondaryColor;
            float _MinZ;
            float _MaxZ;

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

                col *= tintColor;
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

                // Visible fragments cast a fully opaque shadow; invisible fragments do not cast.
                clip(alpha * tintAlpha - 0.001h);
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }

    FallBack "Transparent/VertexLit"
}
