//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Marker used for Terrain teleportation
//
//=============================================================================

using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Valve.VR.InteractionSystem
{
	//-------------------------------------------------------------------------
	public class NavMeshTeleportMarker : TeleportMarkerBase
	{
		//Private data
		private bool gotReleventComponents = false;
		private MeshRenderer markerMesh;
		private int tintColorID = 0;
		private Color tintColor = Color.clear;


		//-------------------------------------------------
		public override bool showReticle
		{
			get
			{
				return false;
			}
		}


		//-------------------------------------------------
		void Awake()
		{
			GetRelevantComponents();
			UpdateVisuals();
			markerActive = false;
		}


		//-------------------------------------------------
		void Start()
		{
		}


		//-------------------------------------------------
		void Update()
		{
		}


		//-------------------------------------------------
		public override bool ShouldActivate(Vector3 playerPosition)
		{
			return true;
		}


		//-------------------------------------------------
		public override bool ShouldMovePlayer()
		{
			return true;
		}


		//-------------------------------------------------
		public override void Highlight(bool highlight)
		{
		}


		//-------------------------------------------------
		public override void UpdateVisuals()
		{
			if (!gotReleventComponents)
			{
				return;
			}
		}


		//-------------------------------------------------
		public void SetMeshMaterials(Material material)
		{
			markerMesh.material = material;
		}


		//-------------------------------------------------
		public void GetRelevantComponents()
		{
			markerMesh = transform.Find("teleport_marker_mesh").GetComponent<MeshRenderer>();

			gotReleventComponents = true;
		}


		//-------------------------------------------------
		public void ReleaseRelevantComponents()
		{
			markerMesh = null;
		}


		//-------------------------------------------------
		public void UpdateVisualsInEditor()
		{
			if (Application.isPlaying)
			{
				return;
			}

			GetRelevantComponents();

			if (locked)
			{
				markerMesh.sharedMaterial = Teleport.instance.pointLockedMaterial;
			}
			else
			{
				markerMesh.sharedMaterial = Teleport.instance.pointVisibleMaterial;
			}

			ReleaseRelevantComponents();
		}

		public override void SetAlpha(float tintAlpha, float alphaPercent)
		{
			tintColor = markerMesh.material.GetColor(tintColorID);
			tintColor.a = tintAlpha;

			markerMesh.material.SetColor(tintColorID, tintColor);
		}
	}


#if UNITY_EDITOR
	//-------------------------------------------------------------------------
	[CustomEditor(typeof(NavMeshTeleportMarker))]
	public class TeleportTerrainEditor : Editor
	{
		//-------------------------------------------------
		void OnEnable()
		{
			if (Selection.activeTransform)
			{
				NavMeshTeleportMarker teleportTerrain = Selection.activeTransform.GetComponent<NavMeshTeleportMarker>();
				teleportTerrain.UpdateVisualsInEditor();
			}
		}


		//-------------------------------------------------
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			if (Selection.activeTransform)
			{
				NavMeshTeleportMarker teleportTerrain = Selection.activeTransform.GetComponent<NavMeshTeleportMarker>();
				if (GUI.changed)
				{
					teleportTerrain.UpdateVisualsInEditor();
				}
			}
		}
	}
#endif
}