using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#else
using XRSettings = UnityEngine.VR.VRSettings;
using XRDevice = UnityEngine.VR.VRDevice;
#endif

#if UNITY_2019_3_OR_NEWER && OPENVR_XR_API
using UnityEngine.XR.Management;
#endif

namespace Valve.VR
{
    public class SteamVR_Behaviour : MonoBehaviour
    {
        private const string openVRDeviceName = "OpenVR";
        public static bool forcingInitialization = false;

        private static SteamVR_Behaviour _instance;
        public static SteamVR_Behaviour instance
        {
            get
            {
                if (_instance == null)
                {
                    Initialize(false);
                }

                return _instance;
            }
        }

        public bool initializeSteamVROnAwake = true;

        public bool doNotDestroy = true;

        [HideInInspector]
        public SteamVR_Render steamvr_render;

        internal static bool isPlaying = false;

        private static bool initializing = false;
        public static void Initialize(bool forceUnityVRToOpenVR = false)
        {
            if (_instance == null && initializing == false)
            {
                initializing = true;
                GameObject steamVRObject = null;

                if (forceUnityVRToOpenVR)
                    forcingInitialization = true;

#if UNITY_2023_1_OR_NEWER
                SteamVR_Render renderInstance = GameObject.FindFirstObjectByType<SteamVR_Render>();
#else
                SteamVR_Render renderInstance = GameObject.FindObjectOfType<SteamVR_Render>();
#endif
                if (renderInstance != null)
                    steamVRObject = renderInstance.gameObject;

#if UNITY_2023_1_OR_NEWER
                SteamVR_Behaviour behaviourInstance = GameObject.FindFirstObjectByType<SteamVR_Behaviour>();
#else
                SteamVR_Behaviour behaviourInstance = GameObject.FindObjectOfType<SteamVR_Behaviour>();
#endif
                if (behaviourInstance != null)
                    steamVRObject = behaviourInstance.gameObject;

                if (steamVRObject == null)
                {
                    GameObject objectInstance = new GameObject("[SteamVR]");
                    _instance = objectInstance.AddComponent<SteamVR_Behaviour>();
                    _instance.steamvr_render = objectInstance.AddComponent<SteamVR_Render>();
                }
                else
                {
                    behaviourInstance = steamVRObject.GetComponent<SteamVR_Behaviour>();
                    if (behaviourInstance == null)
                        behaviourInstance = steamVRObject.AddComponent<SteamVR_Behaviour>();

                    if (renderInstance != null)
                        behaviourInstance.steamvr_render = renderInstance;
                    else
                    {
                        behaviourInstance.steamvr_render = steamVRObject.GetComponent<SteamVR_Render>();
                        if (behaviourInstance.steamvr_render == null)
                            behaviourInstance.steamvr_render = steamVRObject.AddComponent<SteamVR_Render>();
                    }

                    _instance = behaviourInstance;
                }

                if (_instance != null && _instance.doNotDestroy)
                    GameObject.DontDestroyOnLoad(_instance.transform.root.gameObject);

                initializing = false;
            }
        }

        protected void Awake()
        {
            isPlaying = true;

            if (initializeSteamVROnAwake && forcingInitialization == false)
                InitializeSteamVR();
        }



#if UNITY_2019_3_OR_NEWER && OPENVR_XR_API
        public void InitializeSteamVR(bool forceUnityVRToOpenVR = false)
        {
            if (forceUnityVRToOpenVR)
            {
                forcingInitialization = true;
                if (initializeCoroutine != null)
                    StopCoroutine(initializeCoroutine);

                if (!IsOpenVRLoaded())
                {
                    initializeCoroutine = StartCoroutine(DoInitializeSteamVR(forceUnityVRToOpenVR));
                }
            }
            else
            {
                SteamVR.Initialize(false);
            }
        }

        private Coroutine initializeCoroutine;
        private IEnumerator DoInitializeSteamVR(bool forceUnityVRToOpenVR = false)
        {
            XRManagerSettings xrManager = XRGeneralSettings.Instance ? XRGeneralSettings.Instance.Manager : null;
            if (xrManager == null)
            {
                Debug.LogError("<b>[SteamVR]</b> XRGeneralSettings.Instance is null");
                yield break;
            }

            if (forceUnityVRToOpenVR)
            {
                // Stop current loader if one is active
                if (xrManager.isInitializationComplete)
                {
                    xrManager.StopSubsystems();
                    xrManager.DeinitializeLoader();
                    yield return null;
                }

                // Find and initialize OpenVR loader specifically
                XRLoader openVRLoader = null;
                foreach (var loader in xrManager.activeLoaders)
                {
                    if (loader.name.Contains("OpenVR") || loader.GetType().Name.Contains("OpenVR"))
                    {
                        openVRLoader = loader;
                        break;
                    }
                }

                if (openVRLoader == null)
                {
                    Debug.LogError("<b>[SteamVR]</b> OpenVR Loader not found in XR Management settings");
                    yield break;
                }

                bool initSuccess = openVRLoader.Initialize();
                if (!initSuccess)
                {
                    Debug.LogError("<b>[SteamVR]</b> Failed to initialize OpenVR loader");
                    yield break;
                }

                bool startSuccess = openVRLoader.Start();
                if (!startSuccess)
                {
                    Debug.LogError("<b>[SteamVR]</b> Failed to start OpenVR loader");
                    openVRLoader.Deinitialize();
                    yield break;
                }
            }
            else
            {
                // Use default initialization
                if (!xrManager.isInitializationComplete)
                {
                    yield return xrManager.InitializeLoader();
                }

                if (xrManager.activeLoader != null)
                {
                    xrManager.StartSubsystems();
                }
            }

            // Wait for subsystems to be ready
            if (xrManager.activeLoader != null)
            {
                XRDisplaySubsystem displaySubsystem = null;
                XRInputSubsystem inputSubsystem = null;

                float timeout = 10f;
                float startTime = Time.time;

                while (Time.time - startTime < timeout)
                {
                    displaySubsystem = xrManager.activeLoader.GetLoadedSubsystem<XRDisplaySubsystem>();
                    inputSubsystem = xrManager.activeLoader.GetLoadedSubsystem<XRInputSubsystem>();

                    if (displaySubsystem != null && displaySubsystem.running &&
                        inputSubsystem != null && inputSubsystem.running)
                    {
                        break;
                    }

                    yield return null;
                }

                if (displaySubsystem == null || !displaySubsystem.running ||
                    inputSubsystem == null || !inputSubsystem.running)
                {
                    Debug.LogError("<b>[SteamVR]</b> Timeout exceeded waiting for XR subsystems to start");
                }
            }
            else
            {
                Debug.LogError("<b>[SteamVR]</b> No active XR loader");
            }

            initializeCoroutine = null;
            forcingInitialization = false;
        }

        private bool IsOpenVRLoaded()
        {
            XRManagerSettings xrManager = XRGeneralSettings.Instance ? XRGeneralSettings.Instance.Manager : null;
            XRDisplaySubsystem displaySubsystem = xrManager.activeLoader ? xrManager.activeLoader.GetLoadedSubsystem<XRDisplaySubsystem>() : null;
            XRInputSubsystem inputSubsystem = xrManager.activeLoader ? xrManager.activeLoader.GetLoadedSubsystem<XRInputSubsystem>() : null;

            return (displaySubsystem != null && displaySubsystem.running && inputSubsystem != null && inputSubsystem.running);
        }
#elif UNITY_2018_3_OR_NEWER
        public void InitializeSteamVR(bool forceUnityVRToOpenVR = false)
        {
            if (forceUnityVRToOpenVR)
            {
                forcingInitialization = true;

                if (initializeCoroutine != null)
                    StopCoroutine(initializeCoroutine);

                if (XRSettings.loadedDeviceName == openVRDeviceName)
                    EnableOpenVR();
                else
                    initializeCoroutine = StartCoroutine(DoInitializeSteamVR(forceUnityVRToOpenVR));
            }
            else
            {
                SteamVR.Initialize(false);
            }
        }

        private Coroutine initializeCoroutine;
        private bool loadedOpenVRDeviceSuccess = false;
        private IEnumerator DoInitializeSteamVR(bool forceUnityVRToOpenVR = false)
        {
            XRDevice.deviceLoaded += XRDevice_deviceLoaded;
            XRSettings.LoadDeviceByName(openVRDeviceName);
            while (loadedOpenVRDeviceSuccess == false)
            {
                yield return null;
            }
            XRDevice.deviceLoaded -= XRDevice_deviceLoaded;
            EnableOpenVR();
        }

        private void XRDevice_deviceLoaded(string deviceName)
        {
            if (deviceName == openVRDeviceName)
            {
                loadedOpenVRDeviceSuccess = true;
            }
            else
            {
                Debug.LogError("<b>[SteamVR]</b> Tried to async load: " + openVRDeviceName + ". Loaded: " + deviceName, this);
                loadedOpenVRDeviceSuccess = true; //try anyway
            }
        }

        private void EnableOpenVR()
        {
            XRSettings.enabled = true;
            SteamVR.Initialize(false);
            initializeCoroutine = null;
            forcingInitialization = false;
        }
#else
        private Coroutine initializeCoroutine;
        public void InitializeSteamVR(bool forceUnityVRToOpenVR = false)
        {
            if (forceUnityVRToOpenVR)
            {
                forcingInitialization = true;

                if (initializeCoroutine != null)
                    StopCoroutine(initializeCoroutine);

                if (XRSettings.loadedDeviceName == openVRDeviceName)
                    EnableOpenVR();
                else
                    initializeCoroutine = StartCoroutine(DoInitializeSteamVR(forceUnityVRToOpenVR));
            }
            else
            {
                SteamVR.Initialize(false);
            }
        }
        private IEnumerator DoInitializeSteamVR(bool forceUnityVRToOpenVR = false)
        {
            XRSettings.LoadDeviceByName(openVRDeviceName);
            yield return null;
            EnableOpenVR();
        }

        private void EnableOpenVR()
        {
            XRSettings.enabled = true;
            SteamVR.Initialize(false);
            initializeCoroutine = null;
            forcingInitialization = false;
        }
#endif

#if UNITY_EDITOR
        //only stop playing if the unity editor is running
        private void OnDestroy()
        {
            isPlaying = false;
        }
#endif

#if UNITY_2017_1_OR_NEWER
        protected void OnEnable()
        {
		    Application.onBeforeRender += OnBeforeRender;
            SteamVR_Events.System(EVREventType.VREvent_Quit).Listen(OnQuit);
        }
        protected void OnDisable()
        {
		    Application.onBeforeRender -= OnBeforeRender;
            SteamVR_Events.System(EVREventType.VREvent_Quit).Remove(OnQuit);
        }
	    protected void OnBeforeRender()
        {
            PreCull();
        }
#else
        protected void OnEnable()
        {
            Camera.onPreCull += OnCameraPreCull;
            SteamVR_Events.System(EVREventType.VREvent_Quit).Listen(OnQuit);
        }
        protected void OnDisable()
        {
            Camera.onPreCull -= OnCameraPreCull;
            SteamVR_Events.System(EVREventType.VREvent_Quit).Remove(OnQuit);
        }
        protected void OnCameraPreCull(Camera cam)
        {
            if (!cam.stereoEnabled)
                return;

            PreCull();
        }
#endif

        protected static int lastFrameCount = -1;
        protected void PreCull()
        {
            if (OpenVR.Input != null)
            {
                // Only update poses on the first camera per frame.
                if (Time.frameCount != lastFrameCount)
                {
                    lastFrameCount = Time.frameCount;

                    SteamVR_Input.OnPreCull();
                }
            }
        }

        protected void FixedUpdate()
        {
            if (OpenVR.Input != null)
            {
                SteamVR_Input.FixedUpdate();
            }
        }

        protected void LateUpdate()
        {
            if (OpenVR.Input != null)
            {
                SteamVR_Input.LateUpdate();
            }
        }

        protected void Update()
        {
            if (OpenVR.Input != null)
            {
                SteamVR_Input.Update();
            }
        }

        protected void OnQuit(VREvent_t vrEvent)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
		    Application.Quit();
#endif
        }
    }
}
