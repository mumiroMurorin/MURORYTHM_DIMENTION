Shader "Notes/SpaceBreak/Shadow"
{
    Properties
    {
        _Color("Color", Color) = (0,0,0,0.35)
        _MinZ("Visible Range Min Z", Float) = 0.0
        _MaxZ("Visible Range Max Z", Float) = 183.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Off

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
            };

            struct v2f
            {
                UNITY_FOG_COORDS(0)
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };

            fixed4 _Color;
            float _MinZ;
            float _MaxZ;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float inRange = step(_MinZ, i.worldPos.z) * step(i.worldPos.z, _MaxZ);
                clip(inRange - 0.5);

                fixed4 col = _Color;
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
