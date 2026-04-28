Shader "Custom/ClampZ_UnlitReceiveShadow"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Color (Inside Range)", Color) = (1,1,1,1)
        _SecondaryColor("Secondary Color (Outside Range)", Color) = (1,1,1,0)
        _MinZ("Visible Range Min Z", Float) = -20.0
        _MaxZ("Visible Range Max Z", Float) = 187.0
        _ShadowStrength("Shadow Strength", Range(0, 1)) = 1
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
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            #include "AutoLight.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                SHADOW_COORDS(2)
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD3;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _SecondaryColor;
            float _MinZ;
            float _MaxZ;
            fixed _ShadowStrength;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                TRANSFER_SHADOW(o);
                UNITY_TRANSFER_FOG(o, o.vertex);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                float inRange = step(_MinZ, i.worldPos.z) * step(i.worldPos.z, _MaxZ);
                fixed4 tintColor = lerp(_SecondaryColor, _Color, inRange);
                col *= tintColor;

                fixed shadowAttenuation = LIGHT_ATTENUATION(i);
                fixed shadowDarken = lerp(1.0h, shadowAttenuation, _ShadowStrength);
                col.rgb *= shadowDarken;

                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }

    FallBack "Transparent/VertexLit"
}
