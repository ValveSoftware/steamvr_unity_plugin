using System;
using UnityEngine;
using Valve.VR;

public class SteamVR_FirstPersonOverlay : MonoBehaviour
{
    private ulong handle = OpenVR.k_ulOverlayHandleInvalid;
    private RenderTexture renderTexture;
    private Texture_t textureData;

    private void Start()
    {
        if (OpenVR.Overlay == null)
        {
            Debug.LogError("Failed to retrieve OpenVR.Overlay.");
            return;
        }
        handle = OpenVR.k_ulOverlayHandleInvalid;
        EVROverlayError error;

        error = OpenVR.Overlay.CreateOverlay("FirstPersonOverlayKey", "FirstPersonOverlay", ref handle);
        if (error != EVROverlayError.None)
        {
            Debug.LogError("OpenVR.Overlay.CreateOverlay() failed with error: " + error.ToString());
            return;
        }

        VRTextureBounds_t bounds = new VRTextureBounds_t
        {
            uMin = 0,
            uMax = 1,
            vMin = 1,
            vMax = 0
        };

        error = OpenVR.Overlay.SetOverlayTextureBounds(handle, ref bounds);
        if (error != EVROverlayError.None)
        {
            Debug.LogError("OpenVR.Overlay.SetOverlayTextureBounds() failed with error: " + error.ToString());
            return;
        }

        error = OpenVR.Overlay.SetOverlayWidthInMeters(handle, 0.5f);
        if (error != EVROverlayError.None)
        {
            Debug.LogError("OpenVR.Overlay.SetOverlayWidthInMeters() failed with error: " + error.ToString());
            return;
        }

        Camera camera = Camera.main;
        renderTexture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGBFloat);
        camera.targetTexture = renderTexture;

        IntPtr nativeTexturePtr = renderTexture.GetNativeTexturePtr();
        textureData = new Texture_t
        {
            eColorSpace = EColorSpace.Auto,
            eType = GetTextureType(),
            handle = nativeTexturePtr
        };


        error = OpenVR.Overlay.SetOverlayTexture(handle, ref textureData);
        if (error != EVROverlayError.None)
        {
            Debug.LogError("OpenVR.Overlay.SetOverlayTexture() failed with error: " + error.ToString());
            return;
        }

        HmdMatrix34_t matrix = new SteamVR_Utils.RigidTransform(new Vector3(0, 0, 1), Quaternion.identity).ToHmdMatrix34();
        error = OpenVR.Overlay.SetOverlayTransformTrackedDeviceRelative(handle, 0, ref matrix); //tracked device 0 is the hmd
        if (error != EVROverlayError.None)
        {
            Debug.LogError("OpenVR.Overlay.SetOverlayTransformTrackedDeviceRelative() failed with error: " + error.ToString());
            return;
        }

        error = OpenVR.Overlay.ShowOverlay(handle);
        if (error != EVROverlayError.None)
        {
            Debug.LogError("OpenVR.Overlay.ShowOverlay() failed with error: " + error.ToString());
            return;
        }
    }

    private EVROverlayError setTextureError;
    private void Update()
    {
        if (OpenVR.Overlay != null)
        {
            setTextureError = OpenVR.Overlay.SetOverlayTexture(handle, ref textureData);
            if (setTextureError != EVROverlayError.None)
            {
                Debug.LogError("OpenVR.Overlay.SetOverlayTexture() failed with error: " + setTextureError.ToString());
            }
        }
    }

    private void OnDisable()
    {
        if (OpenVR.Overlay != null && handle != OpenVR.k_ulOverlayHandleInvalid)
        {
            EVROverlayError error = OpenVR.Overlay.DestroyOverlay(handle);
            if (error != EVROverlayError.None)
            {
                Debug.LogError("OpenVR.Overlay.DestroyOverlay() failed with error: " + error.ToString());
            }
            else
            {
                handle = OpenVR.k_ulOverlayHandleInvalid;
            }
        }
    }

    private ETextureType GetTextureType()
    {
        switch (SystemInfo.graphicsDeviceType)
        {
            case UnityEngine.Rendering.GraphicsDeviceType.Direct3D11:
                return ETextureType.DirectX;
            case UnityEngine.Rendering.GraphicsDeviceType.Direct3D12:
                return ETextureType.DirectX12;
            case UnityEngine.Rendering.GraphicsDeviceType.Vulkan:
                return ETextureType.Vulkan;
            case UnityEngine.Rendering.GraphicsDeviceType.OpenGLCore:
                return ETextureType.OpenGL;

            default:
                Debug.LogError("Unsupported graphics device: " + SystemInfo.graphicsDeviceType.ToString());
                return ETextureType.Invalid;
        }
    }


#if UNITY_EDITOR
    private void OnEnable()
    {
#if !UNITY_2019_1_OR_NEWER || !OPENVR_XR_API
        Debug.LogError("This example requires Unity 2019+ and the OpenVR plugin for Unity XR.");

        UnityEditor.EditorApplication.isPlaying = false;
        this.enabled = false;
        return;
#endif
#if OPENVR_XR_API
        if (Unity.XR.OpenVR.OpenVRSettings.GetSettings().InitializationType != Unity.XR.OpenVR.OpenVRSettings.InitializationTypes.Overlay)
        {
            Debug.LogError("You must set the OpenVR Application Type to Overlay for this example to work. (Project Settings -> XR Plug-in Management -> OpenVR -> Application Type)");

            this.enabled = false;
            UnityEditor.EditorApplication.isPlaying = false;
        }
#endif
    }
#endif
}