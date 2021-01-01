//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Prompt developers to use settings most compatible with SteamVR.
//
//=============================================================================

//2019 will use some of this
#if (UNITY_2018_1_OR_NEWER && !UNITY_2020_1_OR_NEWER)

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;

using Valve.VR.InteractionSystem;
using UnityEditor.Callbacks;

#pragma warning disable CS0618
#pragma warning disable CS0219
#pragma warning disable CS0414


namespace Valve.VR
{
#if (UNITY_2018_1_OR_NEWER && !UNITY_2019_1_OR_NEWER)
    public class SteamVR_AutoEnableVR_2018to2019
    {
        [DidReloadScripts]
        private static void OnReload()
        {
            SteamVR_AutoEnableVR_UnityPackage.InstallAndEnableUnityVR();
        }
    }
#endif

    public class SteamVR_AutoEnableVR_UnityPackage
    {
        private static bool? _forceInstall;
        private const string forceInstallKey = "steamvr.autoenablevr.forceInstall";
        private static bool? _forceEnable;
        private const string forceEnableKey = "steamvr.autoenablevr.forceEnable";
        private static PackageStates? _updateState;
        private const string updateStateKey = "steamvr.autoenablevr.updateState";

        private static bool forceInstall
        {
            get
            {
                if (_forceInstall.HasValue == false)
                {
                    if (EditorPrefs.HasKey(forceInstallKey))
                        _forceInstall = EditorPrefs.GetBool(forceInstallKey);
                    else
                        _forceInstall = false;
                }

                return _forceInstall.Value;
            }
            set
            {
                _forceInstall = value;
                EditorPrefs.SetBool(forceInstallKey, value);
            }
        }
        private static bool forceEnable
        {
            get
            {
                if (_forceEnable.HasValue == false)
                {
                    if (EditorPrefs.HasKey(forceEnableKey))
                        _forceEnable = EditorPrefs.GetBool(forceEnableKey);
                    else
                        _forceEnable = false;
                }

                return _forceEnable.Value;
            }
            set
            {
                _forceEnable = value;
                EditorPrefs.SetBool(forceEnableKey, value);
            }
        }

        private static void UpdateUpdateStateFromPrefs()
        {
            if (_updateState.HasValue == false)
            {
                if (EditorPrefs.HasKey(updateStateKey))
                    _updateState = (PackageStates)EditorPrefs.GetInt(updateStateKey);
                else
                    _updateState = PackageStates.None;
            }
        }

        private static PackageStates updateState
        {
            get
            {
                if (_updateState.HasValue == false)
                    UpdateUpdateStateFromPrefs();
                return _updateState.Value;
            }
            set
            {
                _updateState = value;
                EditorPrefs.SetInt(updateStateKey, (int)value);
            }
        }

        public static void InstallAndEnableUnityVR(bool forceInstall = false, bool forceEnable = false)
        {
            _forceInstall = forceInstall;
            _forceEnable = forceEnable;
            EditorApplication.update += Update;
        }

        protected const string openVRString = "OpenVR";
        protected const string unityOpenVRPackageString = "com.unity.xr.openvr.standalone";
        protected const string valveOpenVRPackageString = "com.valvesoftware.unity.openvr";

        private enum PackageStates
        {
            None,
            WaitingForList,
            WaitingForAdd,
            WaitingForAddConfirm,
            Installed,
            Failed,
        }

        private static UnityEditor.PackageManager.Requests.ListRequest listRequest;
        private static UnityEditor.PackageManager.Requests.AddRequest addRequest;
        private static System.Diagnostics.Stopwatch addingPackageTime = new System.Diagnostics.Stopwatch();
        private static System.Diagnostics.Stopwatch addingPackageTimeTotal = new System.Diagnostics.Stopwatch();
        private static float estimatedTimeToInstall = 80;
        private static int addTryCount = 0;

        private static void End()
        {
            updateState = PackageStates.None;
            addingPackageTime.Stop();
            addingPackageTimeTotal.Stop();
            UnityEditor.EditorUtility.ClearProgressBar();
            EditorApplication.update -= Update;
        }

        public static void Update()
        {
            if (!SteamVR_Settings.instance.autoEnableVR || Application.isPlaying)
                End();

            if (UnityEditor.PlayerSettings.virtualRealitySupported == false)
            {
                if (forceInstall == false)
                {
                    int shouldInstall = UnityEditor.EditorUtility.DisplayDialogComplex("SteamVR", "Would you like to enable Virtual Reality mode?\n\nThis will install the OpenVR for Desktop package and enable it in Player Settings.", "Yes", "No, and don't ask again", "No");

                    switch (shouldInstall)
                    {
                        case 0: //yes
                            UnityEditor.PlayerSettings.virtualRealitySupported = true;
                            break;
                        case 1: //no
                            End();
                            return;
                        case 2: //no, don't ask
                            SteamVR_Settings.instance.autoEnableVR = false;
                            SteamVR_Settings.Save();
                            End();
                            return;
                    }
                }

                Debug.Log("<b>[SteamVR Setup]</b> Enabled virtual reality support in Player Settings. (you can disable this by unchecking Assets/SteamVR/SteamVR_Settings.autoEnableVR)");
            }

            switch (updateState)
            {
                case PackageStates.None:
                    //see if we have the package
                    listRequest = UnityEditor.PackageManager.Client.List(true);
                    updateState = PackageStates.WaitingForList;
                    break;

                case PackageStates.WaitingForList:
                    if (listRequest == null)
                    {
                        listRequest = UnityEditor.PackageManager.Client.List(true);
                        updateState = PackageStates.WaitingForList;
                    }
                    else if (listRequest.IsCompleted)
                    {
                        if (listRequest.Error != null || listRequest.Status == UnityEditor.PackageManager.StatusCode.Failure)
                        {
                            updateState = PackageStates.Failed;
                            break;
                        }

                        string packageName = unityOpenVRPackageString;

                        bool hasPackage = listRequest.Result.Any(package => package.name == packageName);

                        if (hasPackage == false)
                        {
                            //if we don't have the package - then install it
                            addRequest = UnityEditor.PackageManager.Client.Add(packageName);
                            updateState = PackageStates.WaitingForAdd;
                            addTryCount++;

                            Debug.Log("<b>[SteamVR Setup]</b> Installing OpenVR package...");
                            addingPackageTime.Start();
                            addingPackageTimeTotal.Start();
                        }
                        else
                        {
                            //if we do have the package, make sure it's enabled.
                            updateState = PackageStates.Installed; //already installed
                        }
                    }
                    break;

                case PackageStates.WaitingForAdd:
                    if (addRequest.IsCompleted)
                    {
                        if (addRequest.Error != null || addRequest.Status == UnityEditor.PackageManager.StatusCode.Failure)
                        {
                            updateState = PackageStates.Failed;
                            break;
                        }
                        else
                        {
                            //if the package manager says we added it then confirm that with the list
                            listRequest = UnityEditor.PackageManager.Client.List(true);
                            updateState = PackageStates.WaitingForAddConfirm;
                        }
                    }
                    else
                    {
                        if (addingPackageTimeTotal.Elapsed.TotalSeconds > estimatedTimeToInstall)
                        {
                            if (addTryCount == 1)
                                estimatedTimeToInstall *= 2; //give us more time to retry
                            else
                                updateState = PackageStates.Failed;
                        }

                        string dialogText;
                        if (addTryCount == 1)
                            dialogText = "Installing OpenVR from Unity Package Manager...";
                        else
                            dialogText = "Retrying OpenVR install from Unity Package Manager...";

                        bool cancel = UnityEditor.EditorUtility.DisplayCancelableProgressBar("SteamVR", dialogText, (float)addingPackageTimeTotal.Elapsed.TotalSeconds / estimatedTimeToInstall);
                        if (cancel)
                            updateState = PackageStates.Failed;

                        if (addingPackageTime.Elapsed.TotalSeconds > 10)
                        {
                            Debug.Log("<b>[SteamVR Setup]</b> Waiting for package manager to install OpenVR package...");
                            addingPackageTime.Stop();
                            addingPackageTime.Reset();
                            addingPackageTime.Start();
                        }
                    }
                    break;

                case PackageStates.WaitingForAddConfirm:
                    if (listRequest.IsCompleted)
                    {
                        if (listRequest.Error != null)
                        {
                            updateState = PackageStates.Failed;
                            break;
                        }
                        string packageName = unityOpenVRPackageString;

                        bool hasPackage = listRequest.Result.Any(package => package.name == packageName);

                        if (hasPackage == false)
                        {
                            if (addTryCount == 1)
                            {
                                addRequest = UnityEditor.PackageManager.Client.Add(packageName);
                                updateState = PackageStates.WaitingForAdd;
                                addTryCount++;

                                Debug.Log("<b>[SteamVR Setup]</b> Retrying OpenVR package install...");
                            }
                            else
                            {
                                updateState = PackageStates.Failed;
                            }
                        }
                        else
                        {
                            updateState = PackageStates.Installed; //installed successfully

                            Debug.Log("<b>[SteamVR Setup]</b> Successfully installed OpenVR Desktop package.");
                        }
                    }
                    break;

                case PackageStates.Installed:
                    UnityEditor.BuildTargetGroup currentTarget = UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup;

                    string[] devices = UnityEditorInternal.VR.VREditor.GetVREnabledDevicesOnTargetGroup(currentTarget);

                    bool hasOpenVR = false;
                    bool isFirst = false;

                    if (devices.Length != 0)
                    {
                        int index = Array.FindIndex(devices, device => string.Equals(device, openVRString, System.StringComparison.CurrentCultureIgnoreCase));
                        hasOpenVR = index != -1;
                        isFirst = index == 0;
                    }

                    //list openvr as the first option if it was in the list already
                    List<string> devicesList = new List<string>(devices);
                    if (isFirst == false)
                    {
                        if (hasOpenVR == true)
                            devicesList.Remove(openVRString);

                        devicesList.Insert(0, openVRString);
                        string[] newDevices = devicesList.ToArray();

                        UnityEditorInternal.VR.VREditor.SetVREnabledDevicesOnTargetGroup(currentTarget, newDevices);
                        Debug.Log("<b>[SteamVR Setup]</b> Added OpenVR to supported VR SDKs list.");
                    }

                    End();
                    break;

                case PackageStates.Failed:
                    End();

                    string failtext = "The Unity Package Manager failed to automatically install the OpenVR Desktop package. Please open the Package Manager Window and try to install it manually.";
                    UnityEditor.EditorUtility.DisplayDialog("SteamVR", failtext, "Ok");
                    Debug.Log("<b>[SteamVR Setup]</b> " + failtext);
                    break;
            }
        }
    }
}
#endif