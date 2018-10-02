//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Masks out pixels that cannot be seen through the connected hmd.
//
//=============================================================================

using UnityEngine;

namespace Valve.VR
{
    [ExecuteInEditMode]
    public class SteamVR_CameraMask : MonoBehaviour
    {
        void Awake()
        {
            Debug.Log("<b><color=#1b2838>[SteamVR]</color></b> SteamVR_CameraMask is deprecated in Unity 5.4 - REMOVING");
            DestroyImmediate(this);
        }
    }
}