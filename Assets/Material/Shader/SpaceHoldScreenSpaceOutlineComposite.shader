Shader "Hidden/SpaceHold/ScreenSpaceOutlineComposite"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _VisibleOutlineTex("Visible Outline Id Texture", 2D) = "black" {}
        _AllOutlineTex("All Outline Id Texture", 2D) = "black" {}
        _OutlineColor("Outline Color", Color) = (1,1,1,1)
        _OccludedOutlineColor("Occluded Outline Color", Color) = (1,1,1,0.25)
        _OutlineThickness("Outline Thickness", Range(1, 4)) = 1
        _OccludedOutlineThickness("Occluded Outline Thickness", Range(1, 4)) = 1
        _DebugViewMode("Debug View Mode", Float) = 0
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _VisibleOutlineTex;
            sampler2D _AllOutlineTex;
            fixed4 _OutlineColor;
            fixed4 _OccludedOutlineColor;
            float _OutlineThickness;
            float _OccludedOutlineThickness;
            float _DebugViewMode;
            float4 _OutlineTexelSize;

            float IdDistance(float3 a, float3 b)
            {
                return max(max(abs(a.r - b.r), abs(a.g - b.g)), abs(a.b - b.b));
            }

            float CalcEdge(sampler2D outlineTex, float2 uv, float thickness)
            {
                float3 center = tex2D(outlineTex, uv).rgb;
                float edge = 0;

                [unroll]
                for (int stepIndex = 1; stepIndex <= 4; stepIndex++)
                {
                    if (stepIndex > thickness) { continue; }

                    float2 offsetX = float2(_OutlineTexelSize.x * stepIndex, 0);
                    float2 offsetY = float2(0, _OutlineTexelSize.y * stepIndex);

                    edge = max(edge, IdDistance(center, tex2D(outlineTex, uv + offsetX).rgb));
                    edge = max(edge, IdDistance(center, tex2D(outlineTex, uv - offsetX).rgb));
                    edge = max(edge, IdDistance(center, tex2D(outlineTex, uv + offsetY).rgb));
                    edge = max(edge, IdDistance(center, tex2D(outlineTex, uv - offsetY).rgb));
                }

                return step(0.001, edge);
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                fixed4 src = tex2D(_MainTex, i.uv);

                float visibleEdge = CalcEdge(_VisibleOutlineTex, i.uv, _OutlineThickness);
                float allEdge = CalcEdge(_AllOutlineTex, i.uv, _OccludedOutlineThickness);
                float occludedEdge = allEdge;

                if (_DebugViewMode > 3.5)
                {
                    float3 coverage = tex2D(_AllOutlineTex, i.uv).rgb;
                    return fixed4(coverage, 1);
                }

                if (_DebugViewMode > 2.5)
                {
                    return fixed4(occludedEdge, occludedEdge, occludedEdge, 1);
                }

                if (_DebugViewMode > 1.5)
                {
                    return fixed4(allEdge, allEdge, allEdge, 1);
                }

                if (_DebugViewMode > 0.5)
                {
                    return fixed4(visibleEdge, visibleEdge, visibleEdge, 1);
                }

                src.rgb = lerp(src.rgb, _OccludedOutlineColor.rgb, occludedEdge * _OccludedOutlineColor.a);
                src.rgb = lerp(src.rgb, _OutlineColor.rgb, visibleEdge * _OutlineColor.a);
                return src;
            }
            ENDCG
        }
    }
}
