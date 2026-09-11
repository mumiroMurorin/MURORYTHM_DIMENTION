Shader "Custom/UnlitFade"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
        [HideInInspector] _GroundClipEnabled("Ground Clip Enabled", Float) = 0
        [HideInInspector] _GroundVisibleDistance("Ground Visible Distance", Float) = 100
    }
        SubShader
        {
            Tags {
                "RenderType" = "Transparent"
                "Queue" = "Transparent"
                "IgnoreProjector" = "True"
            }

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
                fixed4 _Color;
                float4 _MainTex_ST;
                float _GroundClipEnabled;
                float _GroundVisibleDistance;

                struct appdata
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                    float2 groundVisibility : TEXCOORD1;
                };

                struct v2f
                {
                    float2 uv : TEXCOORD0;
                    float4 vertex : SV_POSITION;
                    float groundDistance : TEXCOORD1;
                };

                v2f vert(appdata v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                    o.groundDistance = v.groundVisibility.x;
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    if (_GroundClipEnabled > 0.5)
                    {
                        clip(_GroundVisibleDistance - i.groundDistance);
                    }

                    fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                    return col;
                }
                ENDCG
            }
        }
}
