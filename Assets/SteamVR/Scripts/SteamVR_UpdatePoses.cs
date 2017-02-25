//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Helper to update poses when using native OpenVR integration.
//
//=============================================================================

using UnityEngine;
using Valve.VR;

public class SteamVR_UpdatePoses : MonoBehaviour
{
	void OnEnable()
	{
		Camera.onPreCull += OnCameraPreCull;
	}

	void OnDisable()
	{
		Camera.onPreCull -= OnCameraPreCull;
	}

	void OnCameraPreCull(Camera cam)
	{
		// Only update poses for one camera
		if (SteamVR_Render.Top().gameObject != cam.gameObject)
		{
			return;
		}

		var compositor = OpenVR.Compositor;
		if (compositor != null)
		{
			var render = SteamVR_Render.instance;
			compositor.GetLastPoses(render.poses, render.gamePoses);
			SteamVR_Events.NewPoses.Send(render.poses);
			SteamVR_Events.NewPosesApplied.Send();
		}
	}
}

