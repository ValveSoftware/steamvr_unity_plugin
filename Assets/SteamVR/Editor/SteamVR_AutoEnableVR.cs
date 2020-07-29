//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Prompt developers to use settings most compatible with SteamVR.
//
//=============================================================================

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;

using Valve.VR.InteractionSystem;

#if OPENVR_XR_API
using UnityEditor.XR.Management.Metadata;
using UnityEngine.XR.Management;
#endif

#if UNITY_2018_2_OR_NEWER
#pragma warning disable CS0618
#pragma warning disable CS0219
#pragma warning disable CS0414
#endif

namespace Valve.VR
{
    [InitializeOnLoad]
    public class SteamVR_AutoEnableVR
    {
        static SteamVR_AutoEnableVR()
        {
            EditorApplication.update += Update;
        }

        protected const string openVRString = "OpenVR";
        protected const string unityOpenVRPackageString = "com.unity.xr.openvr.standalone";
        protected const string valveOpenVRPackageString = "com.valvesoftware.unity.openvr";

#if UNITY_2020_1_OR_NEWER || OPENVR_XR_API
        protected const string valveEnabledLoaderKeyTemplate = "valve.enablexddrloader.{0}";
        protected const string valveOpenVRLoaderType = "Unity.XR.OpenVR.OpenVRLoader";
#endif

#if UNITY_2018_2_OR_NEWER
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
#endif

        public static void Update()
        {
            if (SteamVR_Settings.instance.autoEnableVR)
            {
                bool enabledVR = false;

#if UNITY_2020_1_OR_NEWER || OPENVR_XR_API
#if !UNITY_2020_2_OR_NEWER
                if (UnityEditor.PlayerSettings.virtualRealitySupported == true)
                {
                    UnityEditor.PlayerSettings.virtualRealitySupported = false;
                    enabledVR = true;
                    Debug.Log("<b>[SteamVR Setup]</b> Disabled virtual reality support in Player Settings. <b>Because you're using XR Manager. Make sure OpenVR Loader is enabled in XR Manager UI.</b> (you can disable this by unchecking Assets/SteamVR/SteamVR_Settings.autoEnableVR)");
                }
#endif

#if OPENVR_XR_API
                //little hacky, but the public methods weren't working.

                if (isLoaderAssignedMethodParameters == null)
                    isLoaderAssignedMethodParameters = new Type[] { typeof(string), typeof(BuildTargetGroup) };
                if (isLoaderAssignedCallParameters == null)
                    isLoaderAssignedCallParameters = new object[] { valveOpenVRLoaderType, BuildTargetGroup.Standalone };
                if (isLoaderAssigned == null)
                    isLoaderAssigned = GetMethod("IsLoaderAssigned", isLoaderAssignedMethodParameters);

                if (installPackageAndAssignLoaderForBuildTarget == null)
                    installPackageAndAssignLoaderForBuildTarget = GetMethod("InstallPackageAndAssignLoaderForBuildTarget");

                if (isLoaderAssigned != null && installPackageAndAssignLoaderForBuildTarget != null)
                {
                    bool isAssigned = (bool)isLoaderAssigned.Invoke(null, isLoaderAssignedCallParameters);

                    if (isAssigned == false)
                    {
                        if (enabledLoaderKey == null)
                            enabledLoaderKey = string.Format(valveEnabledLoaderKeyTemplate, SteamVR_Settings.instance.editorAppKey);

                        if (EditorPrefs.HasKey(enabledLoaderKey) == false)
                        {
                            installPackageAndAssignLoaderForBuildTarget.Invoke(null, new object[] { valveOpenVRPackageString, valveOpenVRLoaderType, BuildTargetGroup.Standalone });

                            isAssigned = (bool)isLoaderAssigned.Invoke(null, isLoaderAssignedCallParameters);
                            if (isAssigned)
                            {
                                EditorPrefs.SetBool(enabledLoaderKey, true);

                                Debug.Log("<b>[SteamVR Setup]</b> Enabled OpenVR Loader in XR Management");
                                UnityEditor.EditorApplication.update -= Update;
                            }
                        }
                        else
                        {
                            UnityEditor.EditorApplication.update -= Update;
                        }

                    }
                    else
                    {
                        UnityEditor.EditorApplication.update -= Update;
                    }
                }

#elif UNITY_2020_1_OR_NEWER
                StartAutoUpdater();
#endif
#else
                if (UnityEditor.PlayerSettings.virtualRealitySupported == false)
                {
                    UnityEditor.PlayerSettings.virtualRealitySupported = true;
                    enabledVR = true;
                    Debug.Log("<b>[SteamVR Setup]</b> Enabled virtual reality support in Player Settings. (you can disable this by unchecking Assets/SteamVR/SteamVR_Settings.autoEnableVR)");
                }

                UnityEditor.BuildTargetGroup currentTarget = UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup;

#if (UNITY_5_4 || UNITY_5_3 || UNITY_5_2 || UNITY_5_1 || UNITY_5_0)
                string[] devices = UnityEditorInternal.VR.VREditor.GetVREnabledDevices(currentTarget);
#else
                string[] devices = UnityEditorInternal.VR.VREditor.GetVREnabledDevicesOnTargetGroup(currentTarget);
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

#if (UNITY_5_6 || UNITY_5_4 || UNITY_5_3 || UNITY_5_2 || UNITY_5_1 || UNITY_5_0)
                    UnityEditorInternal.VR.VREditor.SetVREnabledDevices(currentTarget, newDevices);
#else
                    UnityEditorInternal.VR.VREditor.SetVREnabledDevicesOnTargetGroup(currentTarget, newDevices);
#endif
                    Debug.Log("<b>[SteamVR Setup]</b> Added OpenVR to supported VR SDKs list.");

                }

#if UNITY_2018_2_OR_NEWER
                //2018+ requires us to manually add the OpenVR package
                //2020.1+ has a separate script that does installs

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

                            string packageName = null;

                            packageName = unityOpenVRPackageString;

                            bool hasPackage = listRequest.Result.Any(package => package.name == packageName);

                            if (hasPackage == false)
                            {
                                //if we don't have the package - then install it
                                addRequest = UnityEditor.PackageManager.Client.Add(packageName);
                                packageState = PackageStates.WaitingForAdd;
                                addTryCount++;

                                Debug.Log("<b>[SteamVR Setup]</b> Installing OpenVR package...");
                                addingPackageTime.Start();
                                addingPackageTimeTotal.Start();
                            }
                            else
                            {
                                //if we do have the package, do nothing
                                packageState = PackageStates.Installed; //already installed
                            }
                        }
                        break;

                    case PackageStates.WaitingForAdd:
                        if (addRequest.IsCompleted)
                        {
                            if (addRequest.Error != null || addRequest.Status == UnityEditor.PackageManager.StatusCode.Failure)
                            {
                                packageState = PackageStates.Failed;
                                break;
                            }
                            else
                            {
                                //if the package manager says we added it then confirm that with the list
                                listRequest = UnityEditor.PackageManager.Client.List(true);
                                packageState = PackageStates.WaitingForAddConfirm;
                            }
                        }
                        else
                        {
                            if (addingPackageTimeTotal.Elapsed.TotalSeconds > estimatedTimeToInstall)
                                estimatedTimeToInstall *= 2; // :)

                            string dialogText;
                            if (addTryCount == 1)
                                dialogText = "Installing OpenVR from Unity Package Manager...";
                            else
                                dialogText = "Retrying OpenVR install from Unity Package Manager...";

                            bool cancel = UnityEditor.EditorUtility.DisplayCancelableProgressBar("SteamVR", dialogText, (float)addingPackageTimeTotal.Elapsed.TotalSeconds / estimatedTimeToInstall);
                            if (cancel)
                                packageState = PackageStates.Failed;

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
                                packageState = PackageStates.Failed;
                                break;
                            }
                            string packageName = unityOpenVRPackageString;

                            bool hasPackage = listRequest.Result.Any(package => package.name == packageName);

                            if (hasPackage == false)
                            {
                                if (addTryCount == 1)
                                {
                                    addRequest = UnityEditor.PackageManager.Client.Add(packageName);
                                    packageState = PackageStates.WaitingForAdd;
                                    addTryCount++;

                                    Debug.Log("<b>[SteamVR Setup]</b> Retrying OpenVR package install...");
                                }
                                else
                                {
                                    packageState = PackageStates.Failed;
                                }
                            }
                            else
                            {
                                packageState = PackageStates.Installed; //installed successfully

                                Debug.Log("<b>[SteamVR Setup]</b> Successfully installed OpenVR package.");
                            }
                        }
                        break;
                }

                if (packageState == PackageStates.Failed || packageState == PackageStates.Installed)
                {
                    addingPackageTime.Stop();
                    addingPackageTimeTotal.Stop();
                    UnityEditor.EditorUtility.ClearProgressBar();
                    UnityEditor.EditorApplication.update -= Update; //we're done trying to auto-enable vr

                    if (packageState == PackageStates.Failed)
                    {
                        string failtext = "The Unity Package Manager failed to automatically install the OpenVR package. Please open the Package Manager Window and try to install it manually.";
                        UnityEditor.EditorUtility.DisplayDialog("SteamVR", failtext, "Ok");
                        Debug.Log("<b>[SteamVR Setup]</b> " + failtext);
                    }
                }
#else
                UnityEditor.EditorApplication.update -= Update;
#endif
#endif
            }
        }
        private static void StartAutoUpdater()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int assemblyIndex = 0; assemblyIndex < assemblies.Length; assemblyIndex++)
            {
                Assembly assembly = assemblies[assemblyIndex];
                Type type = assembly.GetType("Unity.XR.OpenVR.OpenVRAutoUpdater");
                if (type != null)
                {
                    MethodInfo preinitMethodInfo = type.GetMethod("Start");
                    if (preinitMethodInfo != null)
                    {
                        preinitMethodInfo.Invoke(null, null);
                        return;
                    }
                }
            }
        }

        private static Type xrMetadataStoreType;
        private static MethodInfo GetMethod(string methodName, Type[] parameters = null)
        {
            if (xrMetadataStoreType == null)
                xrMetadataStoreType = SteamVR_Utils.FindType("UnityEditor.XR.Management.Metadata.XRPackageMetadataStore");

            if (parameters == null)
                return xrMetadataStoreType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            else
                return xrMetadataStoreType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic, null, parameters, null);
        }
    }
}