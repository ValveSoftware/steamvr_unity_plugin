// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "FX/LightCone" {
	Properties{
		_TintColor("Tint Color", Color) = (0.5,0.5,0.5,0.5)
		_InvFade("Soft Particles Factor", Range(0.01,3.0)) = 1.0
		_Rim ("rim power", Range(0,10)) = 1.0
	}

		Category{
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		Blend SrcAlpha One
		ColorMask RGB
		Cull Off Lighting Off ZWrite Off Fog{ Color(0,0,0,0) }

		ZTest Off

		SubShader{
		Pass{

		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_particles

#include "UnityCG.cginc"

		sampler2D _MainTex;
	fixed4 _TintColor;
	uniform fixed _BWEffectOn;

	struct appdata_t {
		float4 vertex : POSITION;
		float3 normal : NORMAL;
		fixed4 color : COLOR;
		fixed4 viewDir : TEXCOORD0;
	};

	struct v2f {
		float4 vertex : SV_POSITION;
		fixed4 color : COLOR;
#ifdef SOFTPARTICLES_ON
		float4 projPos : TEXCOORD0;
#endif
		float3 normal : NORMAL;
		float3 viewDir : TEXCOORD1;
	};


	v2f vert(appdata_t v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
#ifdef SOFTPARTICLES_ON
		o.projPos = ComputeScreenPos(o.vertex);
		COMPUTE_EYEDEPTH(o.projPos.z);
#endif
		o.color = v.color;
		o.normal = v.normal;
		o.viewDir = normalize(ObjSpaceViewDir(v.vertex));

		return o;
	}

	sampler2D_float _CameraDepthTexture;
	float _InvFade;
	half _Rim;

	fixed4 frag(v2f i) : SV_Target
	{
#ifdef SOFTPARTICLES_ON
		float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
	float partZ = i.projPos.z;
	float fade = 1 - saturate(_InvFade * (partZ - sceneZ));
	i.color.a *= fade;
#endif

	i.color = pow(i.color,3);

	half rim = saturate(pow(saturate(abs(dot(i.viewDir, i.normal))),_Rim));

	i.color.a *= rim;

	fixed4 finalCol = 2.0f * i.color * _TintColor;
	fixed lum = Luminance(finalCol.xyz);
	return finalCol;
	}
		ENDCG
	}
	}
	}
}