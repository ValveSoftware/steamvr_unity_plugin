//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Masks out pixels that cannot be seen through the connected hmd.
//
//=============================================================================

using UnityEngine;
using UnityEngine.Rendering;
using Valve.VR;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SteamVR_CameraMask : MonoBehaviour
{
	static Material material;
	static Mesh[] hiddenAreaMeshes = new Mesh[] { null, null };

	MeshFilter meshFilter;

	void Awake()
	{
		meshFilter = GetComponent<MeshFilter>();

		if (material == null)
			material = new Material(Shader.Find("Custom/SteamVR_HiddenArea"));

		var mr = GetComponent<MeshRenderer>();
		mr.material = material;
		mr.castShadows = false;
		mr.receiveShadows = false;
		mr.useLightProbes = false;
	}

	public void Set(SteamVR vr, Valve.VR.EVREye eye)
	{
		int i = (int)eye;
		if (hiddenAreaMeshes[i] == null)
			hiddenAreaMeshes[i] = SteamVR_Utils.CreateHiddenAreaMesh(vr.hmd.GetHiddenAreaMesh(eye, EHiddenAreaMeshType.k_eHiddenAreaMesh_Standard), vr.textureBounds[i]);
		meshFilter.mesh = hiddenAreaMeshes[i];
	}

	public void Clear()
	{
		meshFilter.mesh = null;
	}
}

