Shader "Custom/TransparentShadowReceive" {
    Properties{
        _Color("Color", Color) = (0, 0, 0, 1)
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
                };

                struct v2f {
                    float4 pos : SV_POSITION;
                    SHADOW_COORDS(0)
                };

                fixed4 _Color;

                v2f vert(appdata v) {
                    v2f o;
                    o.pos = UnityObjectToClipPos(v.vertex);
                    TRANSFER_SHADOW(o);
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target {
                    fixed4 col = _Color;
                    col.a *= 1 - LIGHT_ATTENUATION(i);
                    return col;
                }
                ENDCG
            }
    }
        Fallback "Diffuse"
}
