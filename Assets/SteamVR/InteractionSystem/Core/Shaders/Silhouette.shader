//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Used to show the outline of the object
//
//=============================================================================
// UNITY_SHADER_NO_UPGRADE
Shader "Valve/VR/Silhouette"
{
	//-------------------------------------------------------------------------------------------------------------------------------------------------------------
	Properties
	{
		g_vOutlineColor( "Outline Color", Color ) = ( .5, .5, .5, 1 )
		g_flOutlineWidth( "Outline width", Range ( .001, 0.03 ) ) = .005
		g_flCornerAdjust( "Corner Adjustment", Range( 0, 2 ) ) = .5
	}

	//-------------------------------------------------------------------------------------------------------------------------------------------------------------
	CGINCLUDE

		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		#pragma target 5.0
		#pragma multi_compile_instancing

		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		#include "UnityCG.cginc"


#if UNITY_VERSION >= 201810
		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		UNITY_INSTANCING_BUFFER_START( Props )
			UNITY_DEFINE_INSTANCED_PROP( float4, g_vOutlineColor )
			UNITY_DEFINE_INSTANCED_PROP( float, g_flOutlineWidth )
			UNITY_DEFINE_INSTANCED_PROP( float, g_flCornerAdjust )
		UNITY_INSTANCING_BUFFER_END( Props )

		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		struct VS_INPUT
		{
			float4 vPositionOs : POSITION;
			float3 vNormalOs : NORMAL;

			UNITY_VERTEX_INPUT_INSTANCE_ID
		};

		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		struct PS_INPUT
		{
			float4 vPositionOs : TEXCOORD0;
			float3 vNormalOs : TEXCOORD1;
			float4 vPositionPs : SV_POSITION;

			UNITY_VERTEX_INPUT_INSTANCE_ID
			UNITY_VERTEX_OUTPUT_STEREO
		};

		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		PS_INPUT MainVs( VS_INPUT i )
		{
			PS_INPUT o;

			UNITY_SETUP_INSTANCE_ID( i );
			UNITY_INITIALIZE_OUTPUT( PS_INPUT, o );
			UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );

			o.vPositionOs.xyzw = i.vPositionOs.xyzw;
			o.vNormalOs.xyz = i.vNormalOs.xyz;
			o.vPositionPs = UnityObjectToClipPos( i.vPositionOs.xyzw );
			return o;
		}

		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		PS_INPUT Extrude( PS_INPUT vertex )
		{
			PS_INPUT extruded = vertex;

			// Offset along normal in projection space
			float3 vNormalVs = mul( ( float3x3 )UNITY_MATRIX_IT_MV, vertex.vNormalOs.xyz );
			float2 vOffsetPs = TransformViewToProjection( vNormalVs.xy );
			vOffsetPs.xy = normalize( vOffsetPs.xy );

			// Calculate position
			extruded.vPositionPs = UnityObjectToClipPos( vertex.vPositionOs.xyzw );
			extruded.vPositionPs.xy += vOffsetPs.xy * extruded.vPositionPs.w * UNITY_ACCESS_INSTANCED_PROP( Props, g_flOutlineWidth );

			return extruded;
		}

		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		[maxvertexcount(18)]
		void ExtrudeGs( triangle PS_INPUT inputTriangle[3], inout TriangleStream<PS_INPUT> outputStream )
		{
			UNITY_SETUP_INSTANCE_ID ( inputTriangle[ 0 ] )
			UNITY_SETUP_INSTANCE_ID ( inputTriangle[ 1 ] )
			UNITY_SETUP_INSTANCE_ID ( inputTriangle[ 2 ] )

			DEFAULT_UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX ( inputTriangle[ 0 ] )
			DEFAULT_UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX ( inputTriangle[ 1 ] )
			DEFAULT_UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX ( inputTriangle[ 2 ] )

			PS_INPUT inputTriangle0 = inputTriangle[ 0 ];
			PS_INPUT inputTriangle1 = inputTriangle[ 1 ];
			PS_INPUT inputTriangle2 = inputTriangle[ 2 ];

			float3 a = normalize(inputTriangle0.vPositionOs.xyz - inputTriangle1.vPositionOs.xyz);
		    float3 b = normalize(inputTriangle1.vPositionOs.xyz - inputTriangle2.vPositionOs.xyz);
		    float3 c = normalize(inputTriangle2.vPositionOs.xyz - inputTriangle0.vPositionOs.xyz);

		    inputTriangle0.vNormalOs = inputTriangle0.vNormalOs + normalize( a - c) * UNITY_ACCESS_INSTANCED_PROP( Props, g_flCornerAdjust );
		    inputTriangle1.vNormalOs = inputTriangle1.vNormalOs + normalize(-a + b) * UNITY_ACCESS_INSTANCED_PROP( Props, g_flCornerAdjust );
		    inputTriangle2.vNormalOs = inputTriangle2.vNormalOs + normalize(-b + c) * UNITY_ACCESS_INSTANCED_PROP( Props, g_flCornerAdjust );

		    PS_INPUT extrudedTriangle0;
		    PS_INPUT extrudedTriangle1;
		    PS_INPUT extrudedTriangle2;

			UNITY_INITIALIZE_OUTPUT( PS_INPUT, extrudedTriangle0 );
			UNITY_INITIALIZE_OUTPUT( PS_INPUT, extrudedTriangle0 );
			UNITY_INITIALIZE_OUTPUT( PS_INPUT, extrudedTriangle0 );
			
			extrudedTriangle0 = Extrude( inputTriangle0 );
			extrudedTriangle1 = Extrude( inputTriangle1 );
			extrudedTriangle2 = Extrude( inputTriangle2 );

		    outputStream.Append( inputTriangle0 );
		    outputStream.Append( extrudedTriangle0 );
		    outputStream.Append( inputTriangle1 );
		    outputStream.Append( extrudedTriangle0 );
		    outputStream.Append( extrudedTriangle1 );
		    outputStream.Append( inputTriangle1 );

		    outputStream.Append( inputTriangle1 );
		    outputStream.Append( extrudedTriangle1 );
		    outputStream.Append( extrudedTriangle2 );
		    outputStream.Append( inputTriangle1 );
		    outputStream.Append( extrudedTriangle2 );
		    outputStream.Append( inputTriangle2 );

		    outputStream.Append( inputTriangle2 );
		    outputStream.Append( extrudedTriangle2 );
		    outputStream.Append( inputTriangle0 );
		    outputStream.Append( extrudedTriangle2 );
		    outputStream.Append( extrudedTriangle0 );
		    outputStream.Append( inputTriangle0 );
		}

		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		fixed4 MainPs( PS_INPUT i ) : SV_Target
		{
			UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( i );

			return UNITY_ACCESS_INSTANCED_PROP( Props, g_vOutlineColor );
		}

		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		fixed4 NullPs( PS_INPUT i ) : SV_Target
		{
			UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( i );

			return float4( 1.0, 0.0, 1.0, 1.0 );
		}
#else
		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		float4 g_vOutlineColor;
		float g_flOutlineWidth;
		float g_flCornerAdjust;

		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		struct VS_INPUT
		{
			float4 vPositionOs : POSITION;
			float3 vNormalOs : NORMAL;
		};

		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		struct PS_INPUT
		{
			float4 vPositionOs : TEXCOORD0;
			float3 vNormalOs : TEXCOORD1;
			float4 vPositionPs : SV_POSITION;
		};

		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		PS_INPUT MainVs( VS_INPUT i )
		{
			PS_INPUT o;
			o.vPositionOs.xyzw = i.vPositionOs.xyzw;
			o.vNormalOs.xyz = i.vNormalOs.xyz;
#if UNITY_VERSION >= 540
			o.vPositionPs = UnityObjectToClipPos( i.vPositionOs.xyzw );
#else
			o.vPositionPs = mul( UNITY_MATRIX_MVP, i.vPositionOs.xyzw );
#endif
			return o;
		}

		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		PS_INPUT Extrude( PS_INPUT vertex )
		{
			PS_INPUT extruded = vertex;

			// Offset along normal in projection space
			float3 vNormalVs = mul( ( float3x3 )UNITY_MATRIX_IT_MV, vertex.vNormalOs.xyz );
			float2 vOffsetPs = TransformViewToProjection( vNormalVs.xy );
			vOffsetPs.xy = normalize( vOffsetPs.xy );

			// Calculate position
#if UNITY_VERSION >= 540
			extruded.vPositionPs = UnityObjectToClipPos( vertex.vPositionOs.xyzw );
#else
			extruded.vPositionPs = mul( UNITY_MATRIX_MVP, vertex.vPositionOs.xyzw );
#endif
			extruded.vPositionPs.xy += vOffsetPs.xy * extruded.vPositionPs.w * g_flOutlineWidth;

			return extruded;
		}

		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		[maxvertexcount(18)]
		void ExtrudeGs( triangle PS_INPUT inputTriangle[3], inout TriangleStream<PS_INPUT> outputStream )
		{
		    float3 a = normalize(inputTriangle[0].vPositionOs.xyz - inputTriangle[1].vPositionOs.xyz);
		    float3 b = normalize(inputTriangle[1].vPositionOs.xyz - inputTriangle[2].vPositionOs.xyz);
		    float3 c = normalize(inputTriangle[2].vPositionOs.xyz - inputTriangle[0].vPositionOs.xyz);

		    inputTriangle[0].vNormalOs = inputTriangle[0].vNormalOs + normalize( a - c) * g_flCornerAdjust;
		    inputTriangle[1].vNormalOs = inputTriangle[1].vNormalOs + normalize(-a + b) * g_flCornerAdjust;
		    inputTriangle[2].vNormalOs = inputTriangle[2].vNormalOs + normalize(-b + c) * g_flCornerAdjust;

		    PS_INPUT extrudedTriangle0 = Extrude( inputTriangle[0] );
		    PS_INPUT extrudedTriangle1 = Extrude( inputTriangle[1] );
		    PS_INPUT extrudedTriangle2 = Extrude( inputTriangle[2] );

		    outputStream.Append( inputTriangle[0] );
		    outputStream.Append( extrudedTriangle0 );
		    outputStream.Append( inputTriangle[1] );
		    outputStream.Append( extrudedTriangle0 );
		    outputStream.Append( extrudedTriangle1 );
		    outputStream.Append( inputTriangle[1] );

		    outputStream.Append( inputTriangle[1] );
		    outputStream.Append( extrudedTriangle1 );
		    outputStream.Append( extrudedTriangle2 );
		    outputStream.Append( inputTriangle[1] );
		    outputStream.Append( extrudedTriangle2 );
		    outputStream.Append( inputTriangle[2] );

		    outputStream.Append( inputTriangle[2] );
		    outputStream.Append( extrudedTriangle2 );
		    outputStream.Append(inputTriangle[0]);
		    outputStream.Append( extrudedTriangle2 );
		    outputStream.Append( extrudedTriangle0 );
		    outputStream.Append( inputTriangle[0] );
		}

		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		fixed4 MainPs( PS_INPUT i ) : SV_Target
		{
			return g_vOutlineColor;
		}

		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		fixed4 NullPs( PS_INPUT i ) : SV_Target
		{
			return float4( 1.0, 0.0, 1.0, 1.0 );
		}
#endif
	ENDCG

	SubShader
	{
		Tags { "RenderType"="Outline" "Queue" = "Geometry-1"  }

		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		// Render the object with stencil=1 to mask out the part that isn't the silhouette
		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		Pass
		{
			Tags { "LightMode" = "Always" }
			ColorMask 0
			Cull Off
			ZWrite Off
			Stencil
			{
				Ref 1
				Comp always
				Pass replace
			}

			CGPROGRAM
				#pragma vertex MainVs
				#pragma fragment NullPs
			ENDCG
		}

		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		// Render the outline by extruding along vertex normals and using the stencil mask previously rendered. Only render depth, so that the final pass executes
		// once per fragment (otherwise alpha blending will look bad).
		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		Pass
		{
			Tags { "LightMode" = "Always" }
			Cull Off
			ZWrite On
			Stencil
			{
				Ref 1
				Comp notequal
				Pass keep
				Fail keep
			}

			CGPROGRAM
				#pragma vertex MainVs
				#pragma geometry ExtrudeGs
				#pragma fragment MainPs
			ENDCG
		}
	}
}