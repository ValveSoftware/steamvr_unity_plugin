#if UNITY_XR_MANAGEMENT
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Experimental.XR;
using UnityEngine.XR.Management;
using System.IO;

#if UNITY_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.XR;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.XR.OpenVR
{
#if UNITY_INPUT_SYSTEM
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    static class InputLayoutLoader
    {
        static InputLayoutLoader()
        {
            RegisterInputLayouts();
        }

        public static void RegisterInputLayouts()
        {
            InputSystem.RegisterLayout<Unity.XR.OpenVR.OpenVRHMD>("OpenVRHMD",
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithProduct(@"^(OpenVR Headset)|^(Vive Pro)")
            );

            InputSystem.RegisterLayout<Unity.XR.OpenVR.OpenVRControllerWMR>("OpenVRControllerWMR",
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithProduct(@"^(OpenVR Controller\(WindowsMR)")
            );

            InputSystem.RegisterLayout<Unity.XR.OpenVR.ViveWand>("ViveWand",
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithManufacturer("HTC")
                    .WithProduct(@"^(OpenVR Controller\(((Vive Controller)|(VIVE Controller)))")
            );

            InputSystem.RegisterLayout<Unity.XR.OpenVR.OpenVRViveCosmosController>("OpenVRViveCosmosController",
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithManufacturer("HTC")
                    .WithProduct(@"^(OpenVR Controller\(((VIVE Cosmos Controller)|(Vive Cosmos Controller)|(vive_cosmos_controller)))")
            );

            InputSystem.RegisterLayout<Unity.XR.OpenVR.OpenVRControllerIndex>("OpenVRControllerIndex",
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithManufacturer("Valve")
                    .WithProduct(@"^(OpenVR Controller\(Knuckles)")
            );

            InputSystem.RegisterLayout<Unity.XR.OpenVR.OpenVROculusTouchController>("OpenVROculusTouchController",
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithManufacturer("Oculus")
                    .WithProduct(@"^(OpenVR Controller\(Oculus)")
            );

            InputSystem.RegisterLayout<Unity.XR.OpenVR.HandedViveTracker>("HandedViveTracker",
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithManufacturer("HTC")
                    .WithProduct(@"^(OpenVR Controller\(((Vive Tracker)|(VIVE Tracker)).+ - ((Left)|(Right)))")
            );

            InputSystem.RegisterLayout<Unity.XR.OpenVR.ViveTracker>("ViveTracker",
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithManufacturer("HTC")
                    .WithProduct(@"^(OpenVR Controller\(((Vive Tracker)|(VIVE Tracker)).+\)(?! - Left| - Right))")
            );

            InputSystem.RegisterLayout<Unity.XR.OpenVR.ViveTracker>("ViveTracker",
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithManufacturer("HTC")
                    .WithProduct(@"^(OpenVR Tracked Device\(((Vive Tracker)|(VIVE Tracker)).+\)(?! - Left| - Right))")
            );

            InputSystem.RegisterLayout<Unity.XR.OpenVR.LogitechStylus>("LogitechStylus",
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithManufacturer("Logitech")
                    .WithProduct(@"(OpenVR Controller\(.+stylus)")
            );

            InputSystem.RegisterLayout<Unity.XR.OpenVR.ViveLighthouse>("ViveLighthouse",
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithManufacturer("HTC")
                    .WithProduct(@"^(OpenVR Tracking Reference\()")
            );

            InputSystem.RegisterLayout<Unity.XR.OpenVR.ValveLighthouse>("ValveLighthouse",
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithManufacturer("Valve Corporation")
                    .WithProduct(@"^(OpenVR Tracking Reference\()")
            );
        }
    }
#endif

    public class OpenVRLoader : XRLoaderHelper
#if UNITY_EDITOR
    , IXRLoaderPreInit
#endif
    {
        private static List<XRDisplaySubsystemDescriptor> s_DisplaySubsystemDescriptors = new List<XRDisplaySubsystemDescriptor>();
        private static List<XRInputSubsystemDescriptor> s_InputSubsystemDescriptors = new List<XRInputSubsystemDescriptor>();
        

        public XRDisplaySubsystem displaySubsystem
        {
            get
            {
                return GetLoadedSubsystem<XRDisplaySubsystem>();
            }
        }

        public XRInputSubsystem inputSubsystem
        {
            get
            {
                return GetLoadedSubsystem<XRInputSubsystem>();
            }
        }

        public override bool Initialize()
        {
#if UNITY_INPUT_SYSTEM
            InputLayoutLoader.RegisterInputLayouts();
#endif


//this only works at the right time in editor. In builds we use a different method (reading the asset manually)
#if UNITY_EDITOR
            OpenVRSettings settings = OpenVRSettings.GetSettings();
            if (settings != null)
            {
                if (string.IsNullOrEmpty(settings.EditorAppKey))
                {
                    settings.EditorAppKey = settings.GenerateEditorAppKey();
                }

                UserDefinedSettings userDefinedSettings;
                userDefinedSettings.stereoRenderingMode = (ushort)settings.GetStereoRenderingMode();
                userDefinedSettings.initializationType = (ushort)settings.GetInitializationType();
                userDefinedSettings.applicationName = null;
                userDefinedSettings.editorAppKey = null;
                userDefinedSettings.isSteamVRLegacyInput = settings.IsLegacy;
                userDefinedSettings.mirrorViewMode = (ushort)settings.GetMirrorViewMode();

                if (OpenVRHelpers.IsUsingSteamVRInput())
                {
                    userDefinedSettings.isSteamVRLegacyInput = false;
                    settings.IsLegacy = false;
                }

                userDefinedSettings.editorAppKey = settings.EditorAppKey; //only set the key if we're in the editor. Otherwise let steamvr set the key.

                if (OpenVRHelpers.IsUsingSteamVRInput())
                {
                    userDefinedSettings.editorAppKey = OpenVRHelpers.GetEditorAppKeyFromPlugin();
                }

                userDefinedSettings.applicationName = string.Format("[Testing] {0}", GetEscapedApplicationName());
                settings.InitializeActionManifestFileRelativeFilePath();

                userDefinedSettings.actionManifestPath = settings.ActionManifestFileRelativeFilePath;


                //only set the path if the file exists
                FileInfo actionManifestFileInfo = new FileInfo(userDefinedSettings.actionManifestPath);
                if (actionManifestFileInfo.Exists)
                    userDefinedSettings.actionManifestPath = actionManifestFileInfo.FullName.Replace("\\", "\\\\");
                else
                    userDefinedSettings.actionManifestPath = null;


                SetUserDefinedSettings(userDefinedSettings); 
            }
#endif
            
            CreateSubsystem<XRDisplaySubsystemDescriptor, XRDisplaySubsystem>(s_DisplaySubsystemDescriptors, "OpenVR Display");
            CreateSubsystem<XRInputSubsystemDescriptor, XRInputSubsystem>(s_InputSubsystemDescriptors, "OpenVR Input");

            OpenVREvents.Initialize();
            TickCallbackDelegate callback = TickCallback;
            RegisterTickCallback(callback);
            callback(0);

            return displaySubsystem != null && inputSubsystem != null;
        }

        private string GetEscapedApplicationName()
        {
            if (string.IsNullOrEmpty(Application.productName))
                return "";

            return Application.productName.Replace("\\", "\\\\").Replace("\"", "\\\""); //replace \ with \\ and replace " with \"  for json escaping
        }

        public override bool Start()
        {
            StartSubsystem<XRDisplaySubsystem>();
            StartSubsystem<XRInputSubsystem>();

            API.EVRInitError result = GetInitializationResult();

            if (result != API.EVRInitError.None)
            {
                Debug.LogError("<b>[OpenVR]</b> Could not initialize OpenVR. Error code: " + result.ToString());
                return false;
            }

            return true;
        }

        private UnityEngine.Events.UnityEvent[] events;

        public override bool Stop()
        {
            StopSubsystem<XRInputSubsystem>();
            StopSubsystem<XRDisplaySubsystem>(); //display actually does vrshutdown

            return true;
        }

        public override bool Deinitialize()
        {
            DestroySubsystem<XRInputSubsystem>();
            DestroySubsystem<XRDisplaySubsystem>();

            return true;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto )]
        struct UserDefinedSettings
        {
            [MarshalAs(UnmanagedType.U1)] public bool isSteamVRLegacyInput;
            public ushort stereoRenderingMode;
            public ushort initializationType;
            public ushort mirrorViewMode;
            [MarshalAs(UnmanagedType.LPStr)] public string editorAppKey;
            [MarshalAs(UnmanagedType.LPStr)] public string actionManifestPath;
            [MarshalAs(UnmanagedType.LPStr)] public string applicationName;
        }

        [DllImport("XRSDKOpenVR", CharSet = CharSet.Auto)]
        private static extern void SetUserDefinedSettings(UserDefinedSettings settings);

        [DllImport("XRSDKOpenVR", CharSet = CharSet.Auto)]
        static extern API.EVRInitError GetInitializationResult();

        [DllImport("XRSDKOpenVR", CharSet = CharSet.Auto)]
        static extern void RegisterTickCallback([MarshalAs(UnmanagedType.FunctionPtr)] TickCallbackDelegate callbackPointer);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate void TickCallbackDelegate(int value);

        private void TickCallback(int value)
        {
            OpenVREvents.Update();
        }

#if UNITY_EDITOR
        public string GetPreInitLibraryName(BuildTarget buildTarget, BuildTargetGroup buildTargetGroup)
        {
            return "XRSDKOpenVR";
        }
#endif
    }
}
#endif