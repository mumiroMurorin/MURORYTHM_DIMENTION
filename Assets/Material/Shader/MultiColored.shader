Shader "Custom/MultiColored"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _SecondaryColor("Secondary Color", Color) = (1,1,1,1)
        _MainTex("Texture", 2D) = "white" {}
        _SeparationHeight("Separation Z", Float) = 0.0
    }

        SubShader
        {
            Tags
            {
                "Queue" = "Transparent"
                "RenderType" = "Transparent"
                "IgnoreProjector" = "True"
            }

            LOD 100

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            Lighting Off

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                sampler2D _MainTex;
                float4 _MainTex_ST;

                fixed4 _Color;
                fixed4 _SecondaryColor;
                float _SeparationHeight;

                struct appdata
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                };

                struct v2f
                {
                    float2 uv : TEXCOORD0;
                    float4 vertex : SV_POSITION;
                    float3 worldPos : TEXCOORD1;
                };

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
                    fixed4 texColor = tex2D(_MainTex, i.uv);
                    fixed4 colorBlend = lerp(_SecondaryColor, _Color, step(_SeparationHeight, i.worldPos.z));
                    fixed4 finalColor = texColor * colorBlend;
                    return finalColor;
                }
                ENDCG
            } 
        }

        FallBack Off
}
