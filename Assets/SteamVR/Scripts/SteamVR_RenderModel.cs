//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Render model of associated tracked object
//
//=============================================================================

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Valve.VR;

namespace Valve.VR
{
    [ExecuteInEditMode]
    public class SteamVR_RenderModel : MonoBehaviour
    {
        public SteamVR_TrackedObject.EIndex index = SteamVR_TrackedObject.EIndex.None;
        protected SteamVR_Input_Sources inputSource;

        public const string modelOverrideWarning = "Model override is really only meant to be used in " +
            "the scene view for lining things up; using it at runtime is discouraged.  Use tracked device " +
            "index instead to ensure the correct model is displayed for all users.";

        [Tooltip(modelOverrideWarning)]
        public string modelOverride;

        [Tooltip("Shader to apply to model.")]
        public Shader shader;

        [Tooltip("Enable to print out when render models are loaded.")]
        public bool verbose = false;

        [Tooltip("If available, break down into separate components instead of loading as a single mesh.")]
        public bool createComponents = true;

        [Tooltip("Update transforms of components at runtime to reflect user action.")]
        public bool updateDynamically = true;

        // Additional controller settings for showing scrollwheel, etc.
        public RenderModel_ControllerMode_State_t controllerModeState;

        // Name of the sub-object which represents the "local" coordinate space for each component.
        public const string k_localTransformName = "attach";

        // Cached name of this render model for updating component transforms at runtime.
        public string renderModelName { get; private set; }

        public bool initializedAttachPoints { get; set; }

        private Dictionary<string, Transform> componentAttachPoints = new Dictionary<string, Transform>();

        private List<MeshRenderer> meshRenderers = new List<MeshRenderer>();

        // If someone knows how to keep these from getting cleaned up every time
        // you exit play mode, let me know.  I've tried marking the RenderModel
        // class below as [System.Serializable] and switching to normal public
        // variables for mesh and material to get them to serialize properly,
        // as well as tried marking the mesh and material objects as
        // DontUnloadUnusedAsset, but Unity was still unloading them.
        // The hashtable is preserving its entries, but the mesh and material
        // variables are going null.

        public class RenderModel
        {
            public RenderModel(Mesh mesh, Material material)
            {
                this.mesh = mesh;
                this.material = material;
            }
            public Mesh mesh { get; private set; }
            public Material material { get; private set; }
        }

        public static Hashtable models = new Hashtable();
        public static Hashtable materials = new Hashtable();

        // Helper class to load render models interface on demand and clean up when done.
        public sealed class RenderModelInterfaceHolder : System.IDisposable
        {
            private bool needsShutdown, failedLoadInterface;
            private CVRRenderModels _instance;
            public CVRRenderModels instance
            {
                get
                {
                    if (_instance == null && !failedLoadInterface)
                    {
                        if (!SteamVR.active && !SteamVR.usingNativeSupport)
                        {
                            var error = EVRInitError.None;
                            OpenVR.Init(ref error, EVRApplicationType.VRApplication_Utility);
                            needsShutdown = true;
                        }

                        _instance = OpenVR.RenderModels;
                        if (_instance == null)
                        {
                            Debug.LogError("<b><color=#1b2838>[SteamVR]</color></b> Failed to load IVRRenderModels interface version " + OpenVR.IVRRenderModels_Version);
                            failedLoadInterface = true;
                        }
                    }
                    return _instance;
                }
            }
            public void Dispose()
            {
                if (needsShutdown)
                    OpenVR.Shutdown();
            }
        }

        private void OnModelSkinSettingsHaveChanged(VREvent_t vrEvent)
        {
            if (!string.IsNullOrEmpty(renderModelName))
            {
                renderModelName = "";
                UpdateModel();
            }
        }

        public void SetMeshRendererState(bool state)
        {
            for (int rendererIndex = 0; rendererIndex < meshRenderers.Count; rendererIndex++)
            {
                MeshRenderer renderer = meshRenderers[rendererIndex];
                renderer.enabled = state;
            }
        }

        private void OnHideRenderModels(bool hidden)
        {
            SetMeshRendererState(!hidden);
        }

        private void OnDeviceConnected(int i, bool connected)
        {
            if (i != (int)index)
                return;

            if (connected)
            {
                UpdateModel();
            }
        }

        public void UpdateModel()
        {
            var system = OpenVR.System;
            if (system == null)
                return;

            var error = ETrackedPropertyError.TrackedProp_Success;
            var capacity = system.GetStringTrackedDeviceProperty((uint)index, ETrackedDeviceProperty.Prop_RenderModelName_String, null, 0, ref error);
            if (capacity <= 1)
            {
                Debug.LogError("<b><color=#1b2838>[SteamVR]</color></b> Failed to get render model name for tracked object " + index);
                return;
            }

            var buffer = new System.Text.StringBuilder((int)capacity);
            system.GetStringTrackedDeviceProperty((uint)index, ETrackedDeviceProperty.Prop_RenderModelName_String, buffer, capacity, ref error);

            var s = buffer.ToString();
            if (renderModelName != s)
            {
                StartCoroutine(SetModelAsync(s));
            }
        }

        IEnumerator SetModelAsync(string newRenderModelName)
        {
            meshRenderers.Clear();

            if (string.IsNullOrEmpty(newRenderModelName))
                yield break;

            // Preload all render models before asking for the data to create meshes.
            using (RenderModelInterfaceHolder holder = new RenderModelInterfaceHolder())
            {
                CVRRenderModels renderModels = holder.instance;
                if (renderModels == null)
                    yield break;

                // Gather names of render models to preload.
                string[] renderModelNames;

                uint count = renderModels.GetComponentCount(newRenderModelName);
                if (count > 0)
                {
                    renderModelNames = new string[count];

                    for (int componentIndex = 0; componentIndex < count; componentIndex++)
                    {
                        uint capacity = renderModels.GetComponentName(newRenderModelName, (uint)componentIndex, null, 0);
                        if (capacity == 0)
                            continue;

                        var componentNameStringBuilder = new System.Text.StringBuilder((int)capacity);
                        if (renderModels.GetComponentName(newRenderModelName, (uint)componentIndex, componentNameStringBuilder, capacity) == 0)
                            continue;

                        string componentName = componentNameStringBuilder.ToString();

                        capacity = renderModels.GetComponentRenderModelName(newRenderModelName, componentName, null, 0);
                        if (capacity == 0)
                            continue;

                        var nameStringBuilder = new System.Text.StringBuilder((int)capacity);
                        if (renderModels.GetComponentRenderModelName(newRenderModelName, componentName, nameStringBuilder, capacity) == 0)
                            continue;

                        var s = nameStringBuilder.ToString();

                        // Only need to preload if not already cached.
                        RenderModel model = models[s] as RenderModel;
                        if (model == null || model.mesh == null)
                        {
                            renderModelNames[componentIndex] = s;
                        }
                    }
                }
                else
                {
                    // Only need to preload if not already cached.
                    RenderModel model = models[newRenderModelName] as RenderModel;
                    if (model == null || model.mesh == null)
                    {
                        renderModelNames = new string[] { newRenderModelName };
                    }
                    else
                    {
                        renderModelNames = new string[0];
                    }
                }

                // Keep trying every 100ms until all components finish loading.
                while (true)
                {
                    var loading = false;
                    for (int renderModelNameIndex = 0; renderModelNameIndex < renderModelNames.Length; renderModelNameIndex++)
                    {
                        if (string.IsNullOrEmpty(renderModelNames[renderModelNameIndex]))
                            continue;

                        var pRenderModel = System.IntPtr.Zero;

                        var error = renderModels.LoadRenderModel_Async(renderModelNames[renderModelNameIndex], ref pRenderModel);
						//Debug.Log("<b><color=#1b2838>[SteamVR]</color></b> renderModels.LoadRenderModel_Async(" + renderModelNames[renderModelNameIndex] + ": " + error.ToString());

						if (error == EVRRenderModelError.Loading)
                        {
                            loading = true;
                        }
                        else if (error == EVRRenderModelError.None)
                        {
                            // Preload textures as well.
                            var renderModel = MarshalRenderModel(pRenderModel);

                            // Check the cache first.
                            var material = materials[renderModel.diffuseTextureId] as Material;
                            if (material == null || material.mainTexture == null)
                            {
                                var pDiffuseTexture = System.IntPtr.Zero;

                                error = renderModels.LoadTexture_Async(renderModel.diffuseTextureId, ref pDiffuseTexture);
								//Debug.Log("<b><color=#1b2838>[SteamVR]</color></b> renderModels.LoadRenderModel_Async(" + renderModelNames[renderModelNameIndex] + ": " + error.ToString());

								if (error == EVRRenderModelError.Loading)
                                {
                                    loading = true;
                                }
                            }
                        }
                    }

                    if (loading)
                    {
                        yield return new WaitForSecondsRealtime(0.1f);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            bool success = SetModel(newRenderModelName);
            this.renderModelName = newRenderModelName;
            SteamVR_Events.RenderModelLoaded.Send(this, success);
        }

        private bool SetModel(string renderModelName)
        {
            StripMesh(gameObject);

            using (var holder = new RenderModelInterfaceHolder())
            {
                if (createComponents)
                {
                    componentAttachPoints.Clear();

                    if (LoadComponents(holder, renderModelName))
                    {
                        UpdateComponents(holder.instance);
                        return true;
                    }

                    Debug.Log("<b><color=#1b2838>[SteamVR]</color></b> [" + gameObject.name + "] Render model does not support components, falling back to single mesh.");
                }

                if (!string.IsNullOrEmpty(renderModelName))
                {
                    var model = models[renderModelName] as RenderModel;
                    if (model == null || model.mesh == null)
                    {
                        var renderModels = holder.instance;
                        if (renderModels == null)
                            return false;

                        if (verbose)
                            Debug.Log("<b><color=#1b2838>[SteamVR]</color></b> Loading render model " + renderModelName);

                        model = LoadRenderModel(renderModels, renderModelName, renderModelName);
                        if (model == null)
                            return false;

                        models[renderModelName] = model;
                    }

                    gameObject.AddComponent<MeshFilter>().mesh = model.mesh;
                    MeshRenderer newRenderer = gameObject.AddComponent<MeshRenderer>();
                    newRenderer.sharedMaterial = model.material;
                    meshRenderers.Add(newRenderer);
                    return true;
                }
            }

            return false;
        }

        RenderModel LoadRenderModel(CVRRenderModels renderModels, string renderModelName, string baseName)
        {
            var pRenderModel = System.IntPtr.Zero;

            EVRRenderModelError error;
            while (true)
            {
                error = renderModels.LoadRenderModel_Async(renderModelName, ref pRenderModel);
                if (error != EVRRenderModelError.Loading)
                    break;

                Sleep();
            }

            if (error != EVRRenderModelError.None)
            {
                Debug.LogError(string.Format("Failed to load render model {0} - {1}", renderModelName, error.ToString()));
                return null;
            }

            var renderModel = MarshalRenderModel(pRenderModel);

            var vertices = new Vector3[renderModel.unVertexCount];
            var normals = new Vector3[renderModel.unVertexCount];
            var uv = new Vector2[renderModel.unVertexCount];

            var type = typeof(RenderModel_Vertex_t);
            for (int iVert = 0; iVert < renderModel.unVertexCount; iVert++)
            {
                var ptr = new System.IntPtr(renderModel.rVertexData.ToInt64() + iVert * Marshal.SizeOf(type));
                var vert = (RenderModel_Vertex_t)Marshal.PtrToStructure(ptr, type);

                vertices[iVert] = new Vector3(vert.vPosition.v0, vert.vPosition.v1, -vert.vPosition.v2);
                normals[iVert] = new Vector3(vert.vNormal.v0, vert.vNormal.v1, -vert.vNormal.v2);
                uv[iVert] = new Vector2(vert.rfTextureCoord0, vert.rfTextureCoord1);
            }

            int indexCount = (int)renderModel.unTriangleCount * 3;
            var indices = new short[indexCount];
            Marshal.Copy(renderModel.rIndexData, indices, 0, indices.Length);

            var triangles = new int[indexCount];
            for (int iTri = 0; iTri < renderModel.unTriangleCount; iTri++)
            {
                triangles[iTri * 3 + 0] = (int)indices[iTri * 3 + 2];
                triangles[iTri * 3 + 1] = (int)indices[iTri * 3 + 1];
                triangles[iTri * 3 + 2] = (int)indices[iTri * 3 + 0];
            }

            var mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uv;
            mesh.triangles = triangles;

#if (UNITY_5_4 || UNITY_5_3 || UNITY_5_2 || UNITY_5_1 || UNITY_5_0)
            mesh.Optimize();
#endif
            //mesh.hideFlags = HideFlags.DontUnloadUnusedAsset;

            // Check cache before loading texture.
            var material = materials[renderModel.diffuseTextureId] as Material;
            if (material == null || material.mainTexture == null)
            {
                var pDiffuseTexture = System.IntPtr.Zero;

                while (true)
                {
                    error = renderModels.LoadTexture_Async(renderModel.diffuseTextureId, ref pDiffuseTexture);
                    if (error != EVRRenderModelError.Loading)
                        break;

                    Sleep();
                }

                if (error == EVRRenderModelError.None)
                {
                    var diffuseTexture = MarshalRenderModel_TextureMap(pDiffuseTexture);
                    var texture = new Texture2D(diffuseTexture.unWidth, diffuseTexture.unHeight, TextureFormat.RGBA32, false);
                    if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Direct3D11)
                    {
                        texture.Apply();
                        System.IntPtr texturePointer = texture.GetNativeTexturePtr();
                        while (true)
                        {
                            error = renderModels.LoadIntoTextureD3D11_Async(renderModel.diffuseTextureId, texturePointer);
                            if (error != EVRRenderModelError.Loading)
                                break;

                            Sleep();
                        }
                    }
                    else
                    {
                        var textureMapData = new byte[diffuseTexture.unWidth * diffuseTexture.unHeight * 4]; // RGBA
                        Marshal.Copy(diffuseTexture.rubTextureMapData, textureMapData, 0, textureMapData.Length);

                        var colors = new Color32[diffuseTexture.unWidth * diffuseTexture.unHeight];
                        int iColor = 0;
                        for (int iHeight = 0; iHeight < diffuseTexture.unHeight; iHeight++)
                        {
                            for (int iWidth = 0; iWidth < diffuseTexture.unWidth; iWidth++)
                            {
                                var r = textureMapData[iColor++];
                                var g = textureMapData[iColor++];
                                var b = textureMapData[iColor++];
                                var a = textureMapData[iColor++];
                                colors[iHeight * diffuseTexture.unWidth + iWidth] = new Color32(r, g, b, a);
                            }
                        }

                        texture.SetPixels32(colors);
                        texture.Apply();
                    }

                    material = new Material(shader != null ? shader : Shader.Find("Standard"));
                    material.mainTexture = texture;
                    //material.hideFlags = HideFlags.DontUnloadUnusedAsset;

                    materials[renderModel.diffuseTextureId] = material;

                    renderModels.FreeTexture(pDiffuseTexture);
                }
                else
                {
                    Debug.Log("<b><color=#1b2838>[SteamVR]</color></b> Failed to load render model texture for render model " + renderModelName + ". Error: " + error.ToString());
                }
            }

            // Delay freeing when we can since we'll often get multiple requests for the same model right
            // after another (e.g. two controllers or two basestations).
#if UNITY_EDITOR
            if (!Application.isPlaying)
                renderModels.FreeRenderModel(pRenderModel);
            else
#endif
                StartCoroutine(FreeRenderModel(pRenderModel));

            return new RenderModel(mesh, material);
        }

        IEnumerator FreeRenderModel(System.IntPtr pRenderModel)
        {
            yield return new WaitForSeconds(1.0f);

            using (var holder = new RenderModelInterfaceHolder())
            {
                var renderModels = holder.instance;
                renderModels.FreeRenderModel(pRenderModel);
            }
        }

        public Transform FindTransformByName(string componentName, Transform inTransform = null)
        {
            if (inTransform == null)
                inTransform = this.transform;

            for (int childIndex = 0; childIndex < inTransform.childCount; childIndex++)
            {
                Transform child = inTransform.GetChild(childIndex);
                if (child.name == componentName)
                    return child;
            }

            return null;
        }

        public Transform GetComponentTransform(string componentName)
        {
            if (componentName == null)
                return this.transform;

            if (componentAttachPoints.ContainsKey(componentName))
                return componentAttachPoints[componentName];

            return null;
        }

        private void StripMesh(GameObject go)
        {
            var meshRenderer = go.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
                DestroyImmediate(meshRenderer);

            var meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
                DestroyImmediate(meshFilter);
        }

        private bool LoadComponents(RenderModelInterfaceHolder holder, string renderModelName)
        {
            // Disable existing components (we will re-enable them if referenced by this new model).
            // Also strip mesh filter and renderer since these will get re-added if the new component needs them.
            var t = transform;
            for (int childIndex = 0; childIndex < t.childCount; childIndex++)
            {
                var child = t.GetChild(childIndex);
                child.gameObject.SetActive(false);
                StripMesh(child.gameObject);
            }

            // If no model specified, we're done; return success.
            if (string.IsNullOrEmpty(renderModelName))
                return true;

            var renderModels = holder.instance;
            if (renderModels == null)
                return false;

            var count = renderModels.GetComponentCount(renderModelName);
            if (count == 0)
                return false;

            for (int i = 0; i < count; i++)
            {
                var capacity = renderModels.GetComponentName(renderModelName, (uint)i, null, 0);
                if (capacity == 0)
                    continue;

                System.Text.StringBuilder componentNameStringBuilder = new System.Text.StringBuilder((int)capacity);
                if (renderModels.GetComponentName(renderModelName, (uint)i, componentNameStringBuilder, capacity) == 0)
                    continue;

                string componentName = componentNameStringBuilder.ToString();

                // Create (or reuse) a child object for this component (some components are dynamic and don't have meshes).
                t = FindTransformByName(componentName);
                if (t != null)
                {
                    t.gameObject.SetActive(true);
                    componentAttachPoints[componentName] = FindTransformByName(k_localTransformName, t);
                }
                else
                {
                    t = new GameObject(componentName).transform;
                    t.parent = transform;
                    t.gameObject.layer = gameObject.layer;

                    // Also create a child 'attach' object for attaching things.
                    var attach = new GameObject(k_localTransformName).transform;
                    attach.parent = t;
                    attach.localPosition = Vector3.zero;
                    attach.localRotation = Quaternion.identity;
                    attach.localScale = Vector3.one;
                    attach.gameObject.layer = gameObject.layer;

                    componentAttachPoints[componentName] = attach;
                }

                // Reset transform.
                t.localPosition = Vector3.zero;
                t.localRotation = Quaternion.identity;
                t.localScale = Vector3.one;

                capacity = renderModels.GetComponentRenderModelName(renderModelName, componentName, null, 0);
                if (capacity == 0)
                    continue;

                var componentRenderModelNameStringBuilder = new System.Text.StringBuilder((int)capacity);
                if (renderModels.GetComponentRenderModelName(renderModelName, componentName, componentRenderModelNameStringBuilder, capacity) == 0)
                    continue;

                string componentRenderModelName = componentRenderModelNameStringBuilder.ToString();

                // Check the cache or load into memory.
                var model = models[componentRenderModelName] as RenderModel;
                if (model == null || model.mesh == null)
                {
                    if (verbose)
                        Debug.Log("<b><color=#1b2838>[SteamVR]</color></b> Loading render model " + componentRenderModelName);

                    model = LoadRenderModel(renderModels, componentRenderModelName, renderModelName);
                    if (model == null)
                        continue;

                    models[componentRenderModelName] = model;
                }

                t.gameObject.AddComponent<MeshFilter>().mesh = model.mesh;
                MeshRenderer newRenderer = t.gameObject.AddComponent<MeshRenderer>();
                newRenderer.sharedMaterial = model.material;
                meshRenderers.Add(newRenderer);
            }

            return true;
        }

        SteamVR_Events.Action deviceConnectedAction, hideRenderModelsAction, modelSkinSettingsHaveChangedAction;

        SteamVR_RenderModel()
        {
            deviceConnectedAction = SteamVR_Events.DeviceConnectedAction(OnDeviceConnected);
            hideRenderModelsAction = SteamVR_Events.HideRenderModelsAction(OnHideRenderModels);
            modelSkinSettingsHaveChangedAction = SteamVR_Events.SystemAction(EVREventType.VREvent_ModelSkinSettingsHaveChanged, OnModelSkinSettingsHaveChanged);
        }

        void OnEnable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return;
#endif
            if (!string.IsNullOrEmpty(modelOverride))
            {
                Debug.Log(modelOverrideWarning);
                enabled = false;
                return;
            }

            var system = OpenVR.System;
            if (system != null && system.IsTrackedDeviceConnected((uint)index))
            {
                UpdateModel();
            }

            deviceConnectedAction.enabled = true;
            hideRenderModelsAction.enabled = true;
            modelSkinSettingsHaveChangedAction.enabled = true;
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return;
#endif
            deviceConnectedAction.enabled = false;
            hideRenderModelsAction.enabled = false;
            modelSkinSettingsHaveChangedAction.enabled = false;
        }

#if UNITY_EDITOR
        Hashtable values;
#endif
        void Update()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // See if anything has changed since this gets called whenever anything gets touched.
                var fields = GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

                bool modified = false;

                if (values == null)
                {
                    modified = true;
                }
                else
                {
                    foreach (var f in fields)
                    {
                        if (!values.Contains(f))
                        {
                            modified = true;
                            break;
                        }

                        var v0 = values[f];
                        var v1 = f.GetValue(this);
                        if (v1 != null)
                        {
                            if (!v1.Equals(v0))
                            {
                                modified = true;
                                break;
                            }
                        }
                        else if (v0 != null)
                        {
                            modified = true;
                            break;
                        }
                    }
                }

                if (modified)
                {
                    if (renderModelName != modelOverride)
                    {
                        renderModelName = modelOverride;
                        SetModel(modelOverride);
                    }

                    values = new Hashtable();
                    foreach (var f in fields)
                        values[f] = f.GetValue(this);
                }

                return; // Do not update transforms (below) when not playing in Editor (to avoid keeping OpenVR running all the time).
            }
#endif
            // Update component transforms dynamically.
            if (updateDynamically)
                UpdateComponents(OpenVR.RenderModels);
        }

        Dictionary<int, string> nameCache;

        public void UpdateComponents(CVRRenderModels renderModels)
        {
            if (renderModels == null)
                return;

            if (transform.childCount == 0)
                return;

            if (nameCache == null)
                nameCache = new Dictionary<int, string>();

            for (int childIndex = 0; childIndex < transform.childCount; childIndex++)
            {
                Transform child = transform.GetChild(childIndex);

                // Cache names since accessing an object's name allocate memory.
                string componentName;
                if (!nameCache.TryGetValue(child.GetInstanceID(), out componentName))
                {
                    componentName = child.name;
                    nameCache.Add(child.GetInstanceID(), componentName);
                }

                var componentState = new RenderModel_ComponentState_t();
                if (!renderModels.GetComponentStateForDevicePath(renderModelName, componentName, SteamVR_Input_Source.GetHandle(inputSource), ref controllerModeState, ref componentState))
                    continue;

                child.localPosition = SteamVR_Utils.GetPosition(componentState.mTrackingToComponentRenderModel);
                child.localRotation = SteamVR_Utils.GetRotation(componentState.mTrackingToComponentRenderModel);

                Transform attach = null;
                for (int childChildIndex = 0; childChildIndex < child.childCount; childChildIndex++)
                {
                    Transform childChild = child.GetChild(childChildIndex);
                    int childInstanceID = childChild.GetInstanceID();
                    string childName;
                    if (!nameCache.TryGetValue(childInstanceID, out childName))
                    {
                        childName = childChild.name;
                        nameCache.Add(childInstanceID, componentName);
                    }

                    if (childName == SteamVR_RenderModel.k_localTransformName)
                        attach = childChild;
                }

                if (attach != null)
                {
                    attach.position = transform.TransformPoint(SteamVR_Utils.GetPosition(componentState.mTrackingToComponentLocal));
                    attach.rotation = transform.rotation * SteamVR_Utils.GetRotation(componentState.mTrackingToComponentLocal);

                    initializedAttachPoints = true;
                }

                bool visible = (componentState.uProperties & (uint)EVRComponentProperty.IsVisible) != 0;
                if (visible != child.gameObject.activeSelf)
                {
                    child.gameObject.SetActive(visible);
                }
            }
        }

        public void SetDeviceIndex(int newIndex)
        {
            this.index = (SteamVR_TrackedObject.EIndex)newIndex;

            modelOverride = "";

            if (enabled)
            {
                UpdateModel();
            }
        }

        public void SetInputSource(SteamVR_Input_Sources newInputSource)
        {
            inputSource = newInputSource;
        }

        private static void Sleep()
        {
#if !UNITY_METRO
            //System.Threading.Thread.SpinWait(1); //faster napping
            System.Threading.Thread.Sleep(1);
#endif
        }

        /// <summary>
        /// Helper function to handle the inconvenient fact that the packing for RenderModel_t is 
        /// different on Linux/OSX (4) than it is on Windows (8)
        /// </summary>
        /// <param name="pRenderModel">native pointer to the RenderModel_t</param>
        /// <returns></returns>
        private RenderModel_t MarshalRenderModel(System.IntPtr pRenderModel)
        {
            if ((System.Environment.OSVersion.Platform == System.PlatformID.MacOSX) ||
                (System.Environment.OSVersion.Platform == System.PlatformID.Unix))
            {
                var packedModel = (RenderModel_t_Packed)Marshal.PtrToStructure(pRenderModel, typeof(RenderModel_t_Packed));
                RenderModel_t model = new RenderModel_t();
                packedModel.Unpack(ref model);
                return model;
            }
            else
            {
                return (RenderModel_t)Marshal.PtrToStructure(pRenderModel, typeof(RenderModel_t));
            }
        }

        /// <summary>
        /// Helper function to handle the inconvenient fact that the packing for RenderModel_TextureMap_t is 
        /// different on Linux/OSX (4) than it is on Windows (8)
        /// </summary>
        /// <param name="pRenderModel">native pointer to the RenderModel_TextureMap_t</param>
        /// <returns></returns>
        private RenderModel_TextureMap_t MarshalRenderModel_TextureMap(System.IntPtr pRenderModel)
        {
            if ((System.Environment.OSVersion.Platform == System.PlatformID.MacOSX) ||
                (System.Environment.OSVersion.Platform == System.PlatformID.Unix))
            {
                var packedModel = (RenderModel_TextureMap_t_Packed)Marshal.PtrToStructure(pRenderModel, typeof(RenderModel_TextureMap_t_Packed));
                RenderModel_TextureMap_t model = new RenderModel_TextureMap_t();
                packedModel.Unpack(ref model);
                return model;
            }
            else
            {
                return (RenderModel_TextureMap_t)Marshal.PtrToStructure(pRenderModel, typeof(RenderModel_TextureMap_t));
            }
        }
    }
}