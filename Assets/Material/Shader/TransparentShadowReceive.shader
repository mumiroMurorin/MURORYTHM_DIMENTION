Shader "Custom/TransparentShadowReceive" {
    Properties{
        _Color("Color", Color) = (0, 0, 0, 1)
        [HideInInspector] _GroundClipEnabled("Ground Clip Enabled", Float) = 0
        [HideInInspector] _GroundVisibleDistance("Ground Visible Distance", Float) = 100
    }
        SubShader{
            Tags {
                "RenderType" = "Transparent"
                "LightMode" = "ForwardBase"
            }
            Blend SrcAlpha OneMinusSrcAlpha

            Pass {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile_fwdbase

                #include "UnityCG.cginc"
                #include "AutoLight.cginc"

                struct appdata {
                    float4 vertex : POSITION;
                    float2 groundVisibility : TEXCOORD1;
                };

                struct v2f {
                    float4 pos : SV_POSITION;
                    SHADOW_COORDS(0)
                    float groundDistance : TEXCOORD1;
                };

                fixed4 _Color;
                float _GroundClipEnabled;
                float _GroundVisibleDistance;

                v2f vert(appdata v) {
                    v2f o;
                    o.pos = UnityObjectToClipPos(v.vertex);
                    TRANSFER_SHADOW(o);
                    o.groundDistance = v.groundVisibility.x;
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target {
                    if (_GroundClipEnabled > 0.5)
                    {
                        clip(_GroundVisibleDistance - i.groundDistance);
                    }

                    fixed4 col = _Color;
                    col.a *= 1 - LIGHT_ATTENUATION(i);
                    return col;
                }
                ENDCG
            }
    }
        Fallback "Diffuse"
}
