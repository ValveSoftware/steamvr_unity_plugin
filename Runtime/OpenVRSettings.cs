using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

#if UNITY_XR_MANAGEMENT
using UnityEngine.XR.Management;
#endif

namespace Unity.XR.OpenVR
{
#if UNITY_XR_MANAGEMENT
    [XRConfigurationData("OpenVR", "Unity.XR.OpenVR.Settings")]
#endif
    [System.Serializable]
    public class OpenVRSettings : ScriptableObject
    {
        public enum StereoRenderingModes
        {
            MultiPass = 0,
            SinglePassInstanced
        }

        public enum InitializationTypes
        {
            Scene = 1,
            Overlay = 2,
        }

        public enum MirrorViewModes
        {
            None = 0,
            Left,
            Right,
            OpenVR,
        }

        [SerializeField, Tooltip("Set the Stereo Rendering Method")]
        public StereoRenderingModes StereoRenderingMode = StereoRenderingModes.SinglePassInstanced;

        [SerializeField, Tooltip("Most applications initialize as type Scene")]
        public InitializationTypes InitializationType = InitializationTypes.Scene;

        [SerializeField, Tooltip("A generated unique identifier for your application while in the editor")]
        public string EditorAppKey = null;

        [SerializeField, HideInInspector, Tooltip("Internal value that tells the system if this is a legacy app or steamvr input app")]
        public bool IsLegacy = true;

        [SerializeField, HideInInspector, Tooltip("Internal value that tells the system what the relative path is to the manifest")]
        public string ActionManifestFileRelativeFilePath;

        [SerializeField, Tooltip("Which eye to use when rendering the headset view to the main window (none, left, right, or a composite of both + OpenVR overlays)")]
        public MirrorViewModes MirrorViewMode = MirrorViewModes.OpenVR;

        public const string StreamingAssetsFolderName = "SteamVR";
        public const string ActionManifestFileName = "legacy_manifest.json";
        public static string GetStreamingSteamVRPath(bool create = true)
        {
            string path = System.IO.Path.Combine(Application.streamingAssetsPath, StreamingAssetsFolderName);

            if (create)
            {
                CreateDirectory(new DirectoryInfo(path));
            }

            return path;
        }

        private static void CreateDirectory(DirectoryInfo directory)
        {
            if (directory.Parent.Exists == false)
                CreateDirectory(directory.Parent);

            if (directory.Exists == false)
                directory.Create();
        }

        [SerializeField, Tooltip("Internal value that tells the system if we have copied the default binding files yet.")]
        public bool HasCopiedDefaults = false;

        public ushort GetStereoRenderingMode()
        {
            return (ushort)StereoRenderingMode;
        }

        public ushort GetInitializationType()
        {
            return (ushort)InitializationType;
        }

        public MirrorViewModes GetMirrorViewMode()
        {
            return MirrorViewMode;
        }

        /// <summary>
        /// Sets the mirror view mode (left, right, composite of both + openvr overlays) at runtime.
        /// </summary>
        /// <param name="newMode">left, right, composite of both + openvr overlays</param>
        public void SetMirrorViewMode(MirrorViewModes newMode)
        {
            MirrorViewMode = newMode;
            SetMirrorViewMode((ushort)newMode);
        }

        public void SetIsLegacy(bool state)
        {
            IsLegacy = state; 
        }

        public string GenerateEditorAppKey()
        {
            return string.Format("application.generated.unity.{0}.{1}.{2}.exe", GetInputTypeString(), CleanProductName(), ((int)(UnityEngine.Random.value * int.MaxValue)).ToString());
        }

        public string GetInputTypeString()
        {
            if (this.IsLegacy)
                return "legacy";
            else
                return "steamvrinput";
        }

        private static string CleanProductName()
        {
            string productName = Application.productName;
            if (string.IsNullOrEmpty(productName))
                productName = "unnamed_product";
            else
            {
                productName = System.Text.RegularExpressions.Regex.Replace(Application.productName, "[^\\w\\._]", "");
                productName = productName.ToLower();
            }

            return productName;
        }

        public static OpenVRSettings GetSettings(bool create = true)
        {
            OpenVRSettings settings = null;
#if UNITY_EDITOR
            UnityEditor.EditorBuildSettings.TryGetConfigObject<OpenVRSettings>("Unity.XR.OpenVR.Settings", out settings);
#else
            settings = OpenVRSettings.s_Settings;
#endif

            if (settings == null && create)
                settings = OpenVRSettings.CreateInstance<OpenVRSettings>();

            return settings;
        }

        [DllImport("XRSDKOpenVR", CharSet = CharSet.Auto)]
        public static extern void SetMirrorViewMode(ushort mirrorViewMode);

        public bool InitializeActionManifestFileRelativeFilePath()
        {
            string temp = ActionManifestFileRelativeFilePath;

            if (IsLegacy)
            {
                ActionManifestFileRelativeFilePath = System.IO.Path.Combine(OpenVRSettings.GetStreamingSteamVRPath(false), OpenVRSettings.ActionManifestFileName);
            }
            else
            {
                ActionManifestFileRelativeFilePath = System.IO.Path.Combine(OpenVRSettings.GetStreamingSteamVRPath(false), OpenVRHelpers.GetActionManifestNameFromPlugin());
            }

            #if UNITY_EDITOR
            if (temp != ActionManifestFileRelativeFilePath)
            {
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.AssetDatabase.SaveAssets();
                return true;
            }
            #endif
            return false;
        }

#if UNITY_EDITOR
        public void Awake()
        {
            if (string.IsNullOrEmpty(this.EditorAppKey))
            {
                this.EditorAppKey = this.GenerateEditorAppKey();
            }

            this.InitializeActionManifestFileRelativeFilePath();
        }
#else
        public static OpenVRSettings s_Settings;

		public void Awake()
		{
			s_Settings = this;
		}
#endif
    }
}
