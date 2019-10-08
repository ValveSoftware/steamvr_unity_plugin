Shader "Custom/SquishyDeform" {
	Properties {
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Metallic("Metallic/Smoothness", 2D) = "white" {}
		_Normal("Normal", 2D) = "bump" {}
		_Deform("Deform Factor", Range(0,1)) = 0
		_PinchDeform("Pinch Deform Factor", Range(0,1)) = 0
		_Rough("Deform Roughness", Range(0,1)) = 0.1

		_Goo("Goo Texture", 2D) = "white" {}
		_ColorA ("Goo Color A", Color) = (1,1,1,1)
		_ColorB("Goo Color B", Color) = (1,0,0,1)
		_GooN("Goo Flow", 2D) = "bump" {}

		_Flow("Flow Speed", Vector) = (0,1,0,-1)
		_FlowFac("Flow Factor", Range(-1,1)) = 1


	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		#pragma target 3.0
		#include "Tessellation.cginc"

		struct appdata {
			float4 vertex : POSITION;
			float4 tangent : TANGENT;
			float3 normal : NORMAL;
			float2 texcoord : TEXCOORD0;
		};





		sampler2D _MainTex;
		sampler2D _Metallic;
		sampler2D _Normal;
		sampler2D _Goo;
		sampler2D _GooN;

		

		struct Input {
			float2 uv_MainTex;
			float2 uv_Goo;
			float2 uv_GooN;
			float4 col : COLOR;
		};

		half _Deform;
		half _PinchDeform;
		half _Rough;
		fixed4 _ColorA;
		fixed4 _ColorB;
		half4 _Flow;

		half _FlowFac;


		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color

			fixed4 goo = _ColorA;
			half t = _Time.r;
			float2 goon = UnpackNormal(tex2D(_GooN, IN.uv_GooN + float2(t*_Flow.x, _Time.r * _Flow.y)));
			half gool = tex2D(_Goo, IN.uv_Goo + float2(t*_Flow.z, t* _Flow.w) + goon * _FlowFac);

			goo = lerp(goo, _ColorB, gool);

			fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
			half def = lerp(saturate(_Deform)* saturate(pow(IN.col.r, 0.4)),1, saturate(_PinchDeform)* saturate(pow(IN.col.g, 0.4))) ;
			o.Albedo = lerp(c.rgb,goo,def);

			o.Normal = UnpackNormal(tex2D(_Normal, IN.uv_MainTex));

			fixed4 ms = tex2D(_Metallic, IN.uv_MainTex);
			o.Metallic = ms.r;
			o.Smoothness = lerp(ms.a, 1-_Rough, def);
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
