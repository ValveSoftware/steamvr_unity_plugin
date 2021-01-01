//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Prompt developers to use settings most compatible with SteamVR.
//
//=============================================================================

#if (UNITY_2019_1_OR_NEWER)

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;

using Valve.VR.InteractionSystem;
using UnityEditor.Callbacks;

#if OPENVR_XR_API
using UnityEditor.XR.Management.Metadata;
using UnityEngine.XR.Management;
using UnityEditor.XR.Management;
#endif

#pragma warning disable CS0618
#pragma warning disable CS0219
#pragma warning disable CS0414


namespace Valve.VR
{
#if (UNITY_2019_1_OR_NEWER && !UNITY_2020_1_OR_NEWER)
    public class SteamVR_AutoEnableVR_2019to2020
    {
        [DidReloadScripts]
        private static void OnReload()
        {
#if !OPENVR_XR_API
            //if we don't have xr installed, check to see if we have vr installed. if we don't have vr installed, ask which they do want to install.
            SteamVR_AutoEnableVR_2019.CheckAndAsk();
#else
            //since we already have xr installed, we know we just want to enable it
            SteamVR_AutoEnableVR_UnityXR.EnableUnityXR();
#endif
        }
    }
#endif

#if (UNITY_2020_1_OR_NEWER)
    public class SteamVR_AutoEnableVR_2020Plus
    {
        [DidReloadScripts]
        private static void OnReload()
        {
#if !OPENVR_XR_API
            SteamVR_AutoEnableVR_UnityXR.InstallAndEnableUnityXR();
#else
            //since we already have xr installed, we know we just want to enable it
            SteamVR_AutoEnableVR_UnityXR.EnableUnityXR();
#endif
        }
    }
#endif

#if (UNITY_2019_1_OR_NEWER && !UNITY_2020_1_OR_NEWER)
    public class SteamVR_AutoEnableVR_2019
    {
        public static void CheckAndAsk()
        {
            EditorApplication.update += Update;
        }

        protected const string openVRString = "OpenVR";
        protected const string unityOpenVRPackageString = "com.unity.xr.openvr.standalone";
        protected const string valveOpenVRPackageString = "com.valvesoftware.unity.openvr";

        private enum PackageStates
        {
            None,
            WaitingForList,
            Complete,
            Failed,
        }

        private static UnityEditor.PackageManager.Requests.ListRequest listRequest;
        private static PackageStates packageState = PackageStates.None;

        private static void End()
        {
            packageState = PackageStates.None;
            UnityEditor.EditorUtility.ClearProgressBar();
            EditorApplication.update -= Update;
        }

        private static void ShowDialog()
        {
            int shouldInstall = UnityEditor.EditorUtility.DisplayDialogComplex("SteamVR", "The SteamVR Unity Plugin can be used with the legacy Unity VR API (Unity 5.4 - 2019) or with the Unity XR API (2019+). Would you like to install in legacy VR mode or for Unity XR?", "Legacy VR", "Cancel", "Unity XR");

            switch (shouldInstall)
            {
                case 0: //legacy vr
                    SteamVR_AutoEnableVR_UnityPackage.InstallAndEnableUnityVR();
                    break;
                case 1: //cancel
                    break;
                case 2: //unity xr
                    SteamVR_AutoEnableVR_UnityXR.InstallAndEnableUnityXR();
                    break;
            }

            End();
        }

        public static void Update()
        {
            if (!SteamVR_Settings.instance.autoEnableVR || Application.isPlaying)
                End();

            if (UnityEditor.PlayerSettings.virtualRealitySupported == false)
            {
                ShowDialog();
                return;
            }

            switch (packageState)
            {
                case PackageStates.None:
                    //see if we have the package
                    listRequest = UnityEditor.PackageManager.Client.List(true);
                    packageState = PackageStates.WaitingForList;
                    break;

                case PackageStates.WaitingForList:
                    if (listRequest.IsCompleted)
                    {
                        if (listRequest.Error != null || listRequest.Status == UnityEditor.PackageManager.StatusCode.Failure)
                        {
                            packageState = PackageStates.Failed;
                            break;
                        }

                        string packageName = unityOpenVRPackageString;

                        bool hasPackage = listRequest.Result.Any(package => package.name == packageName);

                        if (hasPackage == false)
                            ShowDialog();
                        else //if we do have the package, do nothing
                            End();
                    }
                    break;

                case PackageStates.Failed:
                    End();

                    string failtext = "The Unity Package Manager failed to verify the OpenVR package. If you were trying to install it you may need to open the Package Manager Window and try to install it manually.";
                    UnityEditor.EditorUtility.DisplayDialog("SteamVR", failtext, "Ok");
                    Debug.Log("<b>[SteamVR Setup]</b> " + failtext);
                    break;
            }
        }
    }
#endif

    // todo: split the below into an install and an enable section

    public class SteamVR_AutoEnableVR_UnityXR
    {
        public static void InstallAndEnableUnityXR()
        {
            StartXRInstaller();
            EditorApplication.update += Update;
        }
        public static void EnableUnityXR()
        {
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
        private static PackageStates packageState = PackageStates.None;
        private static System.Diagnostics.Stopwatch addingPackageTime = new System.Diagnostics.Stopwatch();
        private static System.Diagnostics.Stopwatch addingPackageTimeTotal = new System.Diagnostics.Stopwatch();
        private static float estimatedTimeToInstall = 80;
        private static int addTryCount = 0;

        private static string enabledLoaderKey = null;

        private static MethodInfo isLoaderAssigned;
        private static MethodInfo installPackageAndAssignLoaderForBuildTarget;

        private static Type[] isLoaderAssignedMethodParameters;
        private static object[] isLoaderAssignedCallParameters;

        private static void End()
        {
            addingPackageTime.Stop();
            addingPackageTimeTotal.Stop();
            UnityEditor.EditorUtility.ClearProgressBar();
            EditorApplication.update -= Update;
        }

        public static void Update()
        {
            if (!SteamVR_Settings.instance.autoEnableVR)
                End();

#if OPENVR_XR_API
            EnableLoader();
#endif
        }

#if OPENVR_XR_API

        private static EditorWindow settingsWindow = null;
        private static int skipEditorFrames = 5;
        public static void EnableLoader()
        {
            if (skipEditorFrames > 0)
            {
                skipEditorFrames--;
                return;
            }

            if (enabledLoaderKey == null)
                enabledLoaderKey = string.Format(valveEnabledLoaderKeyTemplate, SteamVR_Settings.instance.editorAppKey);

            if (EditorPrefs.HasKey(enabledLoaderKey) == false)
            {
                if (UnityEditor.PlayerSettings.virtualRealitySupported == true)
                {
                    UnityEditor.PlayerSettings.virtualRealitySupported = false;
                    Debug.Log("<b>[SteamVR Setup]</b> Disabled virtual reality support in Player Settings. <b>Because you're using XR Manager. Make sure OpenVR Loader is enabled in XR Manager UI.</b> (you can disable this by unchecking Assets/SteamVR/SteamVR_Settings.autoEnableVR)");
                }

                var generalSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
                if (generalSettings == null)
                {
                    if (settingsWindow == null)
                    {
                        settingsWindow = SettingsService.OpenProjectSettings("Project/XR Plug-in Management");
                        settingsWindow.Repaint();
                        return;
                    }

                    if (settingsWindow == null || generalSettings == null)
                    {
                        Debug.LogWarning("<b>[SteamVR Setup]</b> Unable to access standalone xr settings while trying to enable OpenVR Loader. <b>You may need to manually enable OpenVR Loader in XR Plug-in Management (Project Settings).</b> (you can disable this by unchecking Assets/SteamVR/SteamVR_Settings.autoEnableVR)");
                        return;
                    }
                }

                if (generalSettings.AssignedSettings == null)
                {
                    var assignedSettings = ScriptableObject.CreateInstance<XRManagerSettings>() as XRManagerSettings;
                    generalSettings.AssignedSettings = assignedSettings;
                    EditorUtility.SetDirty(generalSettings);
                }

                bool existing = generalSettings.AssignedSettings.loaders.Any(loader => loader.name == valveOpenVRLoaderType);

                foreach (var loader in generalSettings.AssignedSettings.loaders)
                {
                    Debug.Log("loader: " + loader.name);
                }

                if (!existing)
                {
                    bool status = XRPackageMetadataStore.AssignLoader(generalSettings.AssignedSettings, valveOpenVRLoaderType, BuildTargetGroup.Standalone);
                    if (status)
                        Debug.Log("<b>[SteamVR Setup]</b> Enabled OpenVR Loader in XR Management");
                    else
                        Debug.LogError("<b>[SteamVR Setup]</b> Failed to enable enable OpenVR Loader in XR Management. You may need to manually open the XR Plug-in Management tab in project settings and check the OpenVR Loader box.");
                }

                EditorPrefs.SetBool(enabledLoaderKey, true);

            }

            End();
        }
#endif

        protected const string valveEnabledLoaderKeyTemplate = "valve.enabledxrloader.{0}";
        protected const string valveOpenVRLoaderType = "Unity.XR.OpenVR.OpenVRLoader";

        private static void StartXRInstaller() 
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int assemblyIndex = 0; assemblyIndex < assemblies.Length; assemblyIndex++)
            {
                Assembly assembly = assemblies[assemblyIndex];
                Type type = assembly.GetType("Unity.XR.OpenVR.OpenVRPackageInstaller");
                if (type != null)
                {
                    MethodInfo preinitMethodInfo = type.GetMethod("Start");
                    if (preinitMethodInfo != null)
                    {
                        preinitMethodInfo.Invoke(null, new object[] { true });
                        return;
                    }
                }
            }
        }
    }
}
#endif