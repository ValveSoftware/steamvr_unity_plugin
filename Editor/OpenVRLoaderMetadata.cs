#if XR_MGMT_GTE_320

using System.Collections;
using System.Collections.Generic;
using Unity.XR.OpenVR;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEditor.XR.Management.Metadata;

class OpenVRPackage : IXRPackage
{
    private class OpenVRLoaderMetadata : IXRLoaderMetadata
    {
        public string loaderName { get; set; }
        public string loaderType { get; set; }
        public List<BuildTargetGroup> supportedBuildTargets { get; set; }
    }

    private class OpenVRPackageMetadata : IXRPackageMetadata
    {
        public string packageName { get; set; }
        public string packageId { get; set; }
        public string settingsType { get; set; }
        public List<IXRLoaderMetadata> loaderMetadata { get; set; }
    }

    private static IXRPackageMetadata s_Metadata = new OpenVRPackageMetadata()
    {
        packageName = "OpenVR XR Plugin",
        packageId = "com.valve.openvr",
        settingsType = "Unity.XR.OpenVR.OpenVRSettings",
        loaderMetadata = new List<IXRLoaderMetadata>() {
                new OpenVRLoaderMetadata() {
                        loaderName = "OpenVR Loader",
                        loaderType = "Unity.XR.OpenVR.OpenVRLoader",
                        supportedBuildTargets = new List<BuildTargetGroup>() {
                            BuildTargetGroup.Standalone
                        }
                    },
                }
    };

    public IXRPackageMetadata metadata => s_Metadata;

    public bool PopulateNewSettingsInstance(ScriptableObject obj)
    {
        OpenVRSettings packageSettings = obj as OpenVRSettings;
        if (packageSettings != null)
        {
            
            // Do something here if you need to...
        }
        return false;

    }
}

#endif