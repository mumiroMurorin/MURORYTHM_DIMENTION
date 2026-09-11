Shader "Notes/SpaceHold/Shadow"
{
    Properties
    {
        _Color("Color", Color) = (0,0,0,0.28)
        _MinZ("Visible Range Min Z", Float) = -20.0
        _MaxZ("Visible Range Max Z", Float) = 187.0
        [HideInInspector] _TrackClipEnabled("Track Clip Enabled", Float) = 0
        [HideInInspector] _TrackVisibleMin("Track Visible Min", Float) = 0
        [HideInInspector] _TrackVisibleMax("Track Visible Max", Float) = 100
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
                float2 trackVisibility : TEXCOORD1;
            };

            struct v2f
            {
                UNITY_FOG_COORDS(0)
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                float trackDistance : TEXCOORD2;
            };

            fixed4 _Color;
            float _MinZ;
            float _MaxZ;
            float _TrackClipEnabled;
            float _TrackVisibleMin;
            float _TrackVisibleMax;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.trackDistance = v.trackVisibility.x;
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                if (_TrackClipEnabled > 0.5)
                {
                    clip(i.trackDistance - _TrackVisibleMin);
                    clip(_TrackVisibleMax - i.trackDistance);
                }

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
