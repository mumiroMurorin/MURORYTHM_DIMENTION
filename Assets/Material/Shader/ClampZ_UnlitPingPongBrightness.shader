Shader "Custom/ClampZ_UnlitPingPongBrightness"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Color (Inside Range)", Color) = (1,1,1,1)
        _SecondaryColor("Secondary Color (Outside Range)", Color) = (1,1,1,0)

        _MinZ("Visible Range Min Z", Float) = -20.0
        _MaxZ("Visible Range Max Z", Float) = 187.0

        _BrightnessMin("Brightness Min", Range(0, 5)) = 0.6
        _BrightnessMax("Brightness Max", Range(0, 5)) = 1.4
        _BrightnessSpeed("Brightness Speed", Float) = 1.0
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
            float _BrightnessMin;
            float _BrightnessMax;
            float _BrightnessSpeed;

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

                float pingPong = abs(frac(_Time.y * _BrightnessSpeed) * 2.0 - 1.0);
                float brightness = lerp(_BrightnessMin, _BrightnessMax, pingPong);
                col.rgb *= brightness;

                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
