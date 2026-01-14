Shader "Custom/ClampZ_Unlit"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Color (Inside Range)", Color) = (1,1,1,1)
        _SecondaryColor("Secondary Color (Outside Range)", Color) = (1,1,1,0)

        _MinZ("Visible Range Min Z", Float) = -20.0
        _MaxZ("Visible Range Max Z", Float) = 187.0
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

            // 透明合成の設定
            Blend SrcAlpha OneMinusSrcAlpha
            // 透明オブジェクトなのでデプス書き込みをオフにする（重なり順の問題を防ぐため）
            ZWrite Off
            // カリング（裏面描画）の設定。必要に応じて Off にしてください
            Cull Back

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                // フォグ（霧）を使いたい場合は以下を有効化
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
                    float3 worldPos : TEXCOORD2; // ワールド座標を受け渡すための変数
                };

                sampler2D _MainTex;
                float4 _MainTex_ST;
                fixed4 _Color;
                fixed4 _SecondaryColor;
                float _MinZ;
                float _MaxZ;

                v2f vert(appdata v)
                {
                    v2f o;
                    // オブジェクト空間 -> クリップ空間（画面上の位置）へ変換
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    // UV座標の計算
                    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                    // ワールド座標の計算（Z判定に必要）
                    o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                    UNITY_TRANSFER_FOG(o,o.vertex);
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    // テクスチャの色を取得
                    fixed4 col = tex2D(_MainTex, i.uv);

                // --- Z範囲判定ロジック ---
                // MinZ以上 かつ MaxZ以下 なら 1、それ以外は 0
                float inRange = step(_MinZ, i.worldPos.z) * step(i.worldPos.z, _MaxZ);

                // 範囲内なら _Color、範囲外なら _SecondaryColor をブレンド
                fixed4 tintColor = lerp(_SecondaryColor, _Color, inRange);

                // テクスチャカラーと計算した色を乗算
                col *= tintColor;

                // フォグの適用
                UNITY_APPLY_FOG(i.fogCoord, col);

                return col;
            }
            ENDCG
        }
        }
}