//======= Copyright (c) Valve Corporation, All rights reserved. ===============

using UnityEngine;
using System.Collections;
using Valve.VR.InteractionSystem;
using UnityEngine.UI;

namespace Valve.VR.InteractionSystem.Sample
{
	public class TeleportOptions : MonoBehaviour
	{
		public TeleportPoint NavMeshTeleportPoint = null;
		public LinearMapping maxPathControlInput = null;
		public Text maxPathDistanceDisplay = null;

		private const int lowestMaxPathDistanceValue = 1;
		private const int highestMaxPathDistanceValue = 15;
		private int maxPathDistanceRange = highestMaxPathDistanceValue - lowestMaxPathDistanceValue;
		private float lastInputValue = 0.0f;

		private void Awake()
		{
			if (maxPathControlInput == null)
			{
				maxPathControlInput = GetComponent<LinearMapping>();
				if (maxPathControlInput == null)
				{
					Debug.LogError("DisplayLinearMappingValue: Linear Mapping to input from was not set in inspector or added as a component to this gameobject!");
				}
			}

			if (maxPathDistanceDisplay == null)
			{
				maxPathDistanceDisplay = GetComponent<Text>();
				if (maxPathDistanceDisplay == null)
				{
					Debug.LogError("DisplayLinearMappingValue: Text to output to was not set in inspector or added as a component to this gameobject!");
				}
			}
		}

		// Update is called once per frame
		void Update()
		{
			if (maxPathDistanceDisplay != null && maxPathControlInput != null)
			{
				if (lastInputValue != maxPathControlInput.value)
				{
					// Value from the control has changed
					int maxPathValue = lowestMaxPathDistanceValue + (int)(maxPathControlInput.value * maxPathDistanceRange);
					maxPathDistanceDisplay.text = maxPathValue.ToString();
					Teleport.instance.maxNavMeshPathDistance = maxPathValue;
					lastInputValue = maxPathControlInput.value;
				}
			}
		}


		public void EnableNavMeshTeleport()
		{
			Teleport.instance.AllowTeleportOnNavMesh = true;
			NavMeshTeleportPoint?.SetLocked(false);
		}

		public void DisableNavMeshTeleport()
		{
			Teleport.instance.AllowTeleportOnNavMesh = false;
			NavMeshTeleportPoint?.SetLocked(true);
		}
	}
}