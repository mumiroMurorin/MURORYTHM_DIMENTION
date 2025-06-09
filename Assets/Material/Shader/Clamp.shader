Shader "Custom/Clamp" {

	Properties{
		_Color("Color", Color) = (1,1,1,1)
		_SecondaryColor("Secondary Color", Color) = (1,1,1,1)
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0

		_MinZ("Visible Range Min Z", Float) = 0.0  // ←追加
		_MaxZ("Visible Range Max Z", Float) = 1.0  // ←追加
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
			float _MinZ;
			float _MaxZ;

			UNITY_INSTANCING_BUFFER_START(Props)
			UNITY_INSTANCING_BUFFER_END(Props)

			void surf(Input IN, inout SurfaceOutputStandard o) {
				// z範囲にあるかどうかを判定
				float inRange = step(_MinZ, IN.worldPos.z) * step(IN.worldPos.z, _MaxZ);

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
