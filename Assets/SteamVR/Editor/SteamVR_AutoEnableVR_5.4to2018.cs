//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Prompt developers to use settings most compatible with SteamVR.
//
//=============================================================================

#if (UNITY_5_4_OR_NEWER && !UNITY_2018_1_OR_NEWER)

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;

using UnityEditor.Callbacks;

namespace Valve.VR
{
    public class SteamVR_AutoEnableVR_54to2018
    {
        [DidReloadScripts]
        private static void OnReload()
        {
            EditorApplication.update += Update;
        }

        protected const string openVRString = "OpenVR";

        private static void End()
        {
            EditorApplication.update -= Update;
        }


        public static void Update()
        {
            if (!SteamVR_Settings.instance.autoEnableVR || Application.isPlaying) 
                End();
            
            bool enabledVR = false;

            int shouldInstall = -1;
            if (UnityEditor.PlayerSettings.virtualRealitySupported == false)
            {
                shouldInstall = UnityEditor.EditorUtility.DisplayDialogComplex("SteamVR", "Would you like to enable Virtual Reality mode?\n\nThis will enable Virtual Reality in Player Settings and add OpenVR as a target.", "Yes", "No, and don't ask again", "No");

                switch (shouldInstall)
                {
                    case 0: //yes
                        UnityEditor.PlayerSettings.virtualRealitySupported = true;
                        break;
                    case 1: //no:
                        UnityEditor.EditorApplication.update -= Update;
                        return;
                    case 2: //no, don't ask
                        SteamVR_Settings.instance.autoEnableVR = false;
                        SteamVR_Settings.Save();
                        UnityEditor.EditorApplication.update -= Update;
                        return;
                }
            }

            UnityEditor.BuildTargetGroup currentTarget = UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup;

#if UNITY_5_6_OR_NEWER
            string[] devices = UnityEditorInternal.VR.VREditor.GetVREnabledDevicesOnTargetGroup(currentTarget);
#else
            string[] devices = UnityEditorInternal.VR.VREditor.GetVREnabledDevices(currentTarget);
#endif

            bool hasOpenVR = devices.Any(device => string.Equals(device, openVRString, System.StringComparison.CurrentCultureIgnoreCase));

            if (hasOpenVR == false || enabledVR)
            {
                string[] newDevices;
                if (enabledVR && hasOpenVR == false)
                {
                    newDevices = new string[] { openVRString }; //only list openvr if we enabled it
                }
                else
                {
                    List<string> devicesList = new List<string>(devices); //list openvr as the first option if it wasn't in the list.
                    if (hasOpenVR)
                        devicesList.Remove(openVRString);

                    devicesList.Insert(0, openVRString);
                    newDevices = devicesList.ToArray();
                }

                int shouldEnable = -1;
                if (shouldInstall == 0)
                    shouldEnable = 0;
                else
                    shouldEnable = UnityEditor.EditorUtility.DisplayDialogComplex("SteamVR", "Would you like to enable OpenVR as a VR target?", "Yes", "No, and don't ask again", "No");

                switch (shouldEnable)
                {
                    case 0: //yes
#if UNITY_5_6_OR_NEWER
                        UnityEditorInternal.VR.VREditor.SetVREnabledDevicesOnTargetGroup(currentTarget, newDevices);
#else
                        UnityEditorInternal.VR.VREditor.SetVREnabledDevices(currentTarget, newDevices);
#endif
                        Debug.Log("<b>[SteamVR Setup]</b> Added OpenVR to supported VR SDKs list."); 
                        break;
                    case 1: //no:
                        break;
                    case 2: //no, don't ask
                        SteamVR_Settings.instance.autoEnableVR = false;
                        SteamVR_Settings.Save();
                        break;
                }

            }

            UnityEditor.EditorApplication.update -= Update;

        }
    }
}
#endif