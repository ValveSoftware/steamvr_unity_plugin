//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: 
//
//=============================================================================
// UNITY_SHADER_NO_UPGRADE
Shader "Valve/VR/ControllerButtonHints"
{
	Properties
	{
		_MainTex ( "Texture", 2D ) = "white" {}
		_Color( "Color", Color ) = ( 1, 1, 1, 1 )
		_SceneTint( "SceneTint", Color ) = ( 1, 1, 1, 1 )
	}
		
	SubShader
	{
		Tags{ "Queue" = "Transparent+1" "RenderType" = "Transparent" }
		LOD 100
		Pass
		{
			// Render State ---------------------------------------------------------------------------------------------------------------------------------------------
			Blend Zero SrcColor // Alpha blending
			Cull Off
			ZWrite Off
			ZTest Off
			Stencil
			{
				Ref 2
				Comp notequal
				Pass replace
				Fail keep
			}

			CGPROGRAM
			#pragma vertex MainVS
			#pragma fragment MainPS

			// Includes -------------------------------------------------------------------------------------------------------------------------------------------------
			#include "UnityCG.cginc"

#if UNITY_VERSION >= 201810	

			// Structs --------------------------------------------------------------------------------------------------------------------------------------------------
			struct VertexInput
			{
				float4 vertex : POSITION;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 vertex : SV_POSITION;

				UNITY_VERTEX_OUTPUT_STEREO
			};

			// Globals --------------------------------------------------------------------------------------------------------------------------------------------------
			UNITY_INSTANCING_BUFFER_START( Props )
				UNITY_DEFINE_INSTANCED_PROP( float4, _SceneTint )
			UNITY_INSTANCING_BUFFER_END( Props )

			
			// MainVs ---------------------------------------------------------------------------------------------------------------------------------------------------
			VertexOutput MainVS( VertexInput i )
			{
				VertexOutput o;
				UNITY_SETUP_INSTANCE_ID( i );
				UNITY_INITIALIZE_OUTPUT( VertexOutput, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o ); 

				o.vertex = UnityObjectToClipPos(i.vertex);

				return o;
			}
			
			// MainPs ---------------------------------------------------------------------------------------------------------------------------------------------------
			float4 MainPS( VertexOutput i ) : SV_Target
			{
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( i );

				return UNITY_ACCESS_INSTANCED_PROP( Props, _SceneTint.rgba );
			}
#else

			// Structs --------------------------------------------------------------------------------------------------------------------------------------------------
			struct VertexInput
			{
				float4 vertex : POSITION;
			};

			struct VertexOutput
			{
				float4 vertex : SV_POSITION;
			};

			// Globals --------------------------------------------------------------------------------------------------------------------------------------------------
			float4 _SceneTint;
			
			// MainVs ---------------------------------------------------------------------------------------------------------------------------------------------------
			VertexOutput MainVS( VertexInput i )
			{
				VertexOutput o;
#if UNITY_VERSION >= 540
				o.vertex = UnityObjectToClipPos(i.vertex);
#else
				o.vertex = mul(UNITY_MATRIX_MVP, i.vertex);
#endif				
				return o;
			}
			
			// MainPs ---------------------------------------------------------------------------------------------------------------------------------------------------
			float4 MainPS( VertexOutput i ) : SV_Target
			{
				return _SceneTint.rgba;
			}
#endif
			ENDCG
		}
		Pass
		{
			// Render State ---------------------------------------------------------------------------------------------------------------------------------------------
			Blend SrcAlpha OneMinusSrcAlpha // Alpha blending
			Cull Off
			ZWrite Off
			ZTest Always

			CGPROGRAM

			#pragma vertex MainVS
			#pragma fragment MainPS

			// Includes -------------------------------------------------------------------------------------------------------------------------------------------------
			#include "UnityCG.cginc"

#if UNITY_VERSION >= 201810	

			// Structs --------------------------------------------------------------------------------------------------------------------------------------------------
			struct VertexInput
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID 
			};

			struct VertexOutput
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;

				UNITY_VERTEX_OUTPUT_STEREO
			};

			// Globals --------------------------------------------------------------------------------------------------------------------------------------------------
			UNITY_INSTANCING_BUFFER_START( Props )
				UNITY_DEFINE_INSTANCED_PROP( sampler2D, _MainTex )
				UNITY_DEFINE_INSTANCED_PROP( float4, _MainTex_ST )
				UNITY_DEFINE_INSTANCED_PROP( float4, _Color )
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

				float4 vColor;
				vColor.rgb = lerp( tex2D( UNITY_ACCESS_INSTANCED_PROP( Props, _MainTex ), i.uv).rgb, UNITY_ACCESS_INSTANCED_PROP( Props, _Color.rgb ), UNITY_ACCESS_INSTANCED_PROP( Props, _Color.a ) );
				vColor.a = UNITY_ACCESS_INSTANCED_PROP( Props, _Color.a );

				return vColor.rgba;
			}
#else
			// Structs --------------------------------------------------------------------------------------------------------------------------------------------------
			struct VertexInput
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct VertexOutput
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

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
				float4 vColor;
				vColor.rgb = lerp( tex2D(_MainTex, i.uv).rgb, _Color.rgb, _Color.a );
				vColor.a = _Color.a;

				return vColor.rgba;
			}
#endif

			ENDCG
		}
	}
}
