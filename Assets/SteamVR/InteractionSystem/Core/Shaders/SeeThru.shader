//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Used for objects that can be seen through objects in front of them
//
//=============================================================================
// UNITY_SHADER_NO_UPGRADE
Shader "Valve/VR/SeeThru"
{
	Properties
	{
		_Color( "Color", Color ) = ( 1, 1, 1, 1 )
	}
	SubShader
	{
		Tags{ "Queue" = "Geometry+1" "RenderType" = "Transparent" }
		LOD 100

		Pass
		{
			// Render State ---------------------------------------------------------------------------------------------------------------------------------------------
			Blend SrcAlpha OneMinusSrcAlpha // Alpha blending
			Cull Off
			ZWrite Off
			ZTest Greater
			Stencil
			{
				Ref 2
				Comp notequal
				Pass replace
				Fail keep
			}

			CGPROGRAM
				#pragma target 5.0
#if UNITY_VERSION >= 560
				#pragma only_renderers d3d11 vulkan glcore
#else
				#pragma only_renderers d3d11 glcore
#endif
				#pragma exclude_renderers gles
				#pragma multi_compile_instancing

				#pragma vertex MainVS
				#pragma fragment MainPS
				
				// Includes -------------------------------------------------------------------------------------------------------------------------------------------------
				#include "UnityCG.cginc"
				
				// Structs --------------------------------------------------------------------------------------------------------------------------------------------------
				struct VertexInput
				{
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
#if UNITY_VERSION >= 560
					UNITY_VERTEX_INPUT_INSTANCE_ID
#endif
				};
				
				struct VertexOutput
				{
					float2 uv : TEXCOORD0;
					float4 vertex : SV_POSITION;

#if UNITY_VERSION >= 560
					UNITY_VERTEX_OUTPUT_STEREO
#endif
				};

#if UNITY_VERSION >= 201810
				// Globals --------------------------------------------------------------------------------------------------------------------------------------------------
				UNITY_INSTANCING_BUFFER_START( Props )
					UNITY_DEFINE_INSTANCED_PROP( float4, _Color )
					UNITY_DEFINE_INSTANCED_PROP( sampler2D, _MainTex )
					UNITY_DEFINE_INSTANCED_PROP( float4, _MainTex_ST )
				UNITY_INSTANCING_BUFFER_END( Props )
				
				
				// MainVs ---------------------------------------------------------------------------------------------------------------------------------------------------
				VertexOutput MainVS( VertexInput i )
				{
					VertexOutput o;

					UNITY_SETUP_INSTANCE_ID( i );
					UNITY_INITIALIZE_OUTPUT( VertexOutput, o );
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );

					o.vertex = UnityObjectToClipPos(i.vertex);
					o.uv = TRANSFORM_TEX( i.uv, UNITY_ACCESS_INSTANCED_PROP( Props, _MainTex ) );
					
					return o;
				}
				
				// MainPs ---------------------------------------------------------------------------------------------------------------------------------------------------
				float4 MainPS( VertexOutput i ) : SV_Target
				{
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( i );

					float4 vColor = UNITY_ACCESS_INSTANCED_PROP( Props, _Color.rgba );
				
					return vColor.rgba;
				}
#else
				// Globals --------------------------------------------------------------------------------------------------------------------------------------------------
				sampler2D _MainTex;
				float4 _MainTex_ST;
				float4 _Color;
				
				// MainVs ---------------------------------------------------------------------------------------------------------------------------------------------------
				VertexOutput MainVS( VertexInput i )
				{
					VertexOutput o;
#if UNITY_VERSION >= 540
					o.vertex = UnityObjectToClipPos(i.vertex);
#else
					o.vertex = mul(UNITY_MATRIX_MVP, i.vertex);
#endif					
					o.uv = TRANSFORM_TEX( i.uv, _MainTex );
					
					return o;
				}
				
				// MainPs ---------------------------------------------------------------------------------------------------------------------------------------------------
				float4 MainPS( VertexOutput i ) : SV_Target
				{
					float4 vColor = _Color.rgba;
				
					return vColor.rgba;
				}
#endif

			ENDCG
		}
	}
}
