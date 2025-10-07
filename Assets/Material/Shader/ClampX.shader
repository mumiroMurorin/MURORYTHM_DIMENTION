Shader "Custom/ClampX" {

	Properties{
		_Color("Color", Color) = (1,1,1,1)
		_SecondaryColor("Secondary Color", Color) = (1,1,1,1)
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0

		_MinX("Visible Range Min X", Float) = 0.0
		_MaxX("Visible Range Max X", Float) = 1.0
	}

		SubShader{
			Tags { "Queue" = "Transparent" }
			LOD 200

			CGPROGRAM
			#pragma surface surf Standard alpha:fade
			#pragma target 3.0

			sampler2D _MainTex;

			struct Input {
				float2 uv_MainTex;
				float3 worldPos;
			};

			half _Glossiness;
			half _Metallic;
			fixed4 _Color;
			fixed4 _SecondaryColor;
			float _MinX;
			float _MaxX;

			UNITY_INSTANCING_BUFFER_START(Props)
			UNITY_INSTANCING_BUFFER_END(Props)

			void surf(Input IN, inout SurfaceOutputStandard o) {
				// z範囲にあるかどうかを判定
				float inRange = step(_MinX, IN.worldPos.x) * step(IN.worldPos.x, _MaxX);

				// _SecondaryColor（範囲外）から_Color（範囲内）へ補間
				fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * lerp(_SecondaryColor, _Color, inRange);

				o.Albedo = c.rgb;
				o.Metallic = _Metallic;
				o.Smoothness = _Glossiness;
				o.Alpha = c.a;
			}

			ENDCG
		}

			FallBack "Diffuse"
}
