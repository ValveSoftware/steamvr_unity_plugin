//======= Copyright (c) Valve Corporation, All rights reserved. ===============

using UnityEngine;
using System.Collections;

namespace Valve.VR
{
    /// <summary>
    /// Automatically activates an action set on Start() and deactivates the set on OnDestroy(). Optionally deactivating all other sets as well.
    /// </summary>
    public class SteamVR_ActivateActionSetOnLoad : MonoBehaviour
    {
        [SteamVR_DefaultActionSet("default")]
        public SteamVR_ActionSet actionSet;

        public bool disableAllOtherActionSets = false;

        public bool activateOnStart = true;
        public bool deactivateOnDestroy = true;


        private void Start()
        {
            if (actionSet != null && activateOnStart)
            {
                //Debug.Log(string.Format("<b><color=#1b2838>[SteamVR]</color></b> Activating {0} action set.", actionSet.fullPath));
                actionSet.ActivatePrimary(disableAllOtherActionSets);
            }
        }

        private void OnDestroy()
        {
            if (actionSet != null && deactivateOnDestroy)
            {
                //Debug.Log(string.Format("<b><color=#1b2838>[SteamVR]</color></b> Deactivating {0} action set.", actionSet.fullPath));
                actionSet.Deactivate();
            }
        }
    }
}