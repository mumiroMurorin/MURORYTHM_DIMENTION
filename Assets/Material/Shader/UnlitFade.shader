Shader "Custom/UnlitFade"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
        // 0 = as it is, 1 = black and white. The chart editor sets it per renderer for the notes that cannot be edited
        // while one hold is edited alone (HoldIsolation). Every material leaves it at 0.
        _Monochrome("Monochrome", Range(0, 1)) = 0
        // 0 = as it is, 1 = pale (lighter, and see-through at alpha 0.45). The chart editor sets it per renderer for locked
        // notes (NoteLock). Every material leaves it at 0. A body that is already faint (a mesh relay, alpha 0.39) is
        // raised to 0.45 instead of being made fainter (recording 20261117_012356: it was nearly invisible).
        _Pale("Pale", Range(0, 1)) = 0
        // 0 = as it is, 1 = a locked mesh relay: nearly white and nearly opaque, so that it stands out on the blue hold
        // band of the same colour (recording 20261117_162135: at alpha 0.45 it still could not be seen). Every material
        // leaves it at 0; NoteLock sets it together with _Pale.
        _PaleStrong("Pale Strong", Range(0, 1)) = 0
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
                fixed _Monochrome;
                fixed _Pale;
                fixed _PaleStrong;
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

                    // black and white by _Monochrome (0 leaves the colour untouched)
                    fixed gray = dot(col.rgb, fixed3(0.299, 0.587, 0.114));
                    col.rgb = lerp(col.rgb, fixed3(gray, gray, gray), _Monochrome);

                    // pale by _Pale (0 leaves the colour untouched)
                    col.rgb = lerp(col.rgb, fixed3(1, 1, 1), _Pale * 0.5);
                    col.a = lerp(col.a, min(col.a * 2, 0.45), _Pale);

                    // a locked mesh relay: whiter, and nearly opaque
                    col.rgb = lerp(col.rgb, fixed3(1, 1, 1), _PaleStrong * 0.6);
                    col.a = lerp(col.a, max(col.a, 0.9), _PaleStrong);
                    return col;
                }
                ENDCG
            }
        }
}
