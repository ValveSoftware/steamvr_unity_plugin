//======= Copyright (c) Valve Corporation, All rights reserved. ===============

using UnityEngine;
using Valve.VR;
using System.IO;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Valve.Newtonsoft.Json;

namespace Valve.VR
{
    public partial class SteamVR_Input
    {
        public const string defaultInputGameObjectName = "[SteamVR Input]";
        private const string localizationKeyName = "localization";
        public static string actionsFilePath;

        /// <summary>True if the actions file has been initialized</summary>
        public static bool fileInitialized = false;

        /// <summary>True if the steamvr input system initialization process has completed successfully</summary>
        public static bool initialized = false;

        /// <summary>True if the preinitialization process (setting up dictionaries, etc) has completed successfully</summary>
        public static bool preInitialized = false;

        /// <summary>The serialized version of the actions file we're currently using (only used in editor)</summary>
        public static SteamVR_Input_ActionFile actionFile;

        /// <summary>The hash of the current action file on disk</summary>
        public static string actionFileHash;

        /// <summary>A reference to the method that has been generated to initialize action sets</summary>
        public static Action InitializeActionSets;

        /// <summary>A reference to the method that has been generated to initialize actions</summary>
        public static Action InitializeActions;

        /// <summary>A reference to the method that has been generated to initialize the references to the action set objects</summary>
        public static Action InitializeInstanceActionSets;

        /// <summary>A reference to the method that has been generated to initialize the references to the action objects</summary>
        public static Action InitializeInstanceActions;

        /// <summary>An event that fires when the non visual actions (everything except poses / skeletons) have been updated</summary>
        public static event Action OnNonVisualActionsUpdated;

        /// <summary>An event that fires when the pose actions have been updated</summary>
        public static event Action<bool> OnPosesUpdated;

        /// <summary>An event that fires when the skeleton actions have been updated</summary>
        public static event Action<bool> OnSkeletonsUpdated;
        

        protected static Type inputType = typeof(SteamVR_Input);

        protected static bool initializing = false;

        #region array accessors
        /// <summary>An array of all action sets</summary>
        public static SteamVR_ActionSet[] actionSets;

        /// <summary>An array of all actions (in all action sets)</summary>
        public static SteamVR_Action[] actions;

        /// <summary>An array of all input actions</summary>
        public static SteamVR_Action_In[] actionsIn;

        /// <summary>An array of all output actions (haptic)</summary>
        public static SteamVR_Action_Out[] actionsOut;

        /// <summary>An array of all the boolean actions</summary>
        public static SteamVR_Action_Boolean[] actionsBoolean;

        /// <summary>An array of all the single actions</summary>
        public static SteamVR_Action_Single[] actionsSingle;

        /// <summary>An array of all the vector2 actions</summary>
        public static SteamVR_Action_Vector2[] actionsVector2;

        /// <summary>An array of all the vector3 actions</summary>
        public static SteamVR_Action_Vector3[] actionsVector3;

        /// <summary>An array of all the pose actions</summary>
        public static SteamVR_Action_Pose[] actionsPose;

        /// <summary>An array of all the skeleton actions</summary>
        public static SteamVR_Action_Skeleton[] actionsSkeleton;

        /// <summary>An array of all the vibration (haptic) actions</summary>
        public static SteamVR_Action_Vibration[] actionsVibration;

        /// <summary>An array of all the input actions that are not pose or skeleton actions (boolean, single, vector2, vector3)</summary>
        public static SteamVR_Action_In[] actionsNonPoseNonSkeletonIn;

        protected static Dictionary<string, SteamVR_ActionSet> actionSetsByPath = new Dictionary<string, SteamVR_ActionSet>();
        protected static Dictionary<string, SteamVR_Action> actionsByPath = new Dictionary<string, SteamVR_Action>();

        protected static Dictionary<string, SteamVR_ActionSet> actionSetsByPathCache = new Dictionary<string, SteamVR_ActionSet>();
        protected static Dictionary<string, SteamVR_Action> actionsByPathCache = new Dictionary<string, SteamVR_Action>();
        #endregion

        /// <summary>Tell SteamVR that we're using the actions file at the path defined in SteamVR_Settings.</summary>
        public static void IdentifyActionsFile()
        {
            string currentPath = Application.dataPath;
            int lastIndex = currentPath.LastIndexOf('/');
            currentPath = currentPath.Remove(lastIndex, currentPath.Length - lastIndex);

            string fullPath = System.IO.Path.Combine(currentPath, SteamVR_Settings.instance.actionsFilePath);
            fullPath = fullPath.Replace("\\", "/");

            if (File.Exists(fullPath))
            {
                Debug.Log("[SteamVR] Loading actions file: " + fullPath);

                var err = OpenVR.Input.SetActionManifestPath(fullPath);
                if (err != EVRInputError.None)
                    Debug.LogError("[SteamVR] Error loading action manifest into SteamVR: " + err.ToString());
                else
                    Debug.Log("[SteamVR] Successfully loaded action manifest into SteamVR");
            }
            else
            {
                Debug.LogError("[SteamVR] Could not find actions file at: " + fullPath);
            }
        }

        /// <summary>Set up the dictionaries and references to methods we'll use for SteamVR Input</summary>
        public static void PreInitialize()
        {
            if (initialized || preInitialized)
                return;

#if UNITY_EDITOR
            CheckSetup();
#endif

            InitializeInstanceActionSets = GetMethod<Action>(SteamVR_Input_Generator_Names.initializeInstanceActionSetsMethodName) as Action;
            InitializeInstanceActions = GetMethod<Action>(SteamVR_Input_Generator_Names.initializeInstanceActionsMethodName) as Action;

            InitializeInstanceActionSets();
            InitializeInstanceActions();

            for (int actionIndex = 0; actionIndex < actions.Length; actionIndex++)
            {
                actions[actionIndex].PreInitialize();
            }

            preInitialized = true;
        }

        /// <summary>
        /// Get all the handles for actions and action sets. 
        /// Initialize our dictionaries of action / action set names. 
        /// Setup the tracking space universe origin
        /// </summary>
        public static void Initialize()
        {
            if (initialized)
                return;

            Debug.Log("Initializing steamvr input...");
            initializing = true;

            SteamVR_Input_Source.Initialize();

            InitializeActionSets = GetMethod<Action>(SteamVR_Input_Generator_Names.initializeActionSetsMethodName) as Action;
            InitializeActions = GetMethod<Action>(SteamVR_Input_Generator_Names.initializeActionsMethodName) as Action;

            InitializeActionSets();

            if (SteamVR.settings.activateFirstActionSetOnStart)
            {
                if (actionSets != null)
                    actionSets[0].ActivatePrimary();
                else
                {
                    Debug.LogError("No action sets.");
                }
            }

            InitializeActions();

            SteamVR_Action_Pose.SetTrackingUniverseOrigin(SteamVR_Settings.instance.trackingSpace);

            InitializeDictionaries();

            initialized = true;

            initializing = false;
            Debug.Log("Steamvr input initialization complete.");
        }


        protected static void InitializeDictionaries()
        {
            actionsByPath.Clear();
            actionSetsByPath.Clear();
            actionsByPathCache.Clear();
            actionSetsByPathCache.Clear();

            for (int actionIndex = 0; actionIndex < actions.Length; actionIndex++)
            {
                SteamVR_Action action = actions[actionIndex];
                actionsByPath.Add(action.fullPath.ToLower(), action);
            }

            for (int actionSetIndex = 0; actionSetIndex < actionSets.Length; actionSetIndex++)
            {
                SteamVR_ActionSet set = actionSets[actionSetIndex];
                actionSetsByPath.Add(set.fullPath.ToLower(), set);
            }
        }

        /// <summary>
        /// Get an action by the full path to that action. Action paths are in the format /actions/[actionSet]/[direction]/[actionName]
        /// </summary>
        /// <typeparam name="T">The type of action you're expecting to get back</typeparam>
        /// <param name="path">The full path to the action you want (Action paths are in the format /actions/[actionSet]/[direction]/[actionName])</param>
        /// <param name="caseSensitive">case sensitive searches are faster</param>
        public static T GetActionFromPath<T>(string path, bool caseSensitive = false) where T : SteamVR_Action
        {
            if (caseSensitive)
            {
                if (actionsByPath.ContainsKey(path))
                {
                    return (T)actionsByPath[path];
                }
            }
            else
            {
                if (actionsByPathCache.ContainsKey(path))
                {
                    return (T)actionsByPathCache[path];
                }
                else
                {
                    string loweredPath = path.ToLower();
                    if (actionsByPath.ContainsKey(loweredPath))
                    {
                        actionsByPathCache.Add(path, actionsByPath[loweredPath]);
                        return (T)actionsByPath[loweredPath];
                    }
                    else
                    {
                        actionsByPathCache.Add(path, null);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Get an action by the full path to that action. Action paths are in the format /actions/[actionSet]/[direction]/[actionName]
        /// </summary>
        /// <param name="path">The full path to the action you want (Action paths are in the format /actions/[actionSet]/[direction]/[actionName])</param>
        public static SteamVR_Action GetActionFromPath(string path)
        {
            return GetActionFromPath<SteamVR_Action>(path);
        }

        /// <summary>
        /// Get an action set by the full path to that action set. Action set paths are in the format /actions/[actionSet]
        /// </summary>
        /// <typeparam name="T">The type of action set you're expecting to get back</typeparam>
        /// <param name="path">The full path to the action set you want (Action paths are in the format /actions/[actionSet])</param>
        /// <param name="caseSensitive">case sensitive searches are faster</param>
        public static T GetActionSetFromPath<T>(string path, bool caseSensitive = false) where T : SteamVR_ActionSet
        {
            if (caseSensitive)
            {
                if (actionSetsByPath.ContainsKey(path))
                {
                    return (T)actionSetsByPath[path];
                }
            }
            else
            {
                if (actionSetsByPathCache.ContainsKey(path))
                {
                    return (T)actionSetsByPathCache[path];
                }
                else
                {
                    string loweredPath = path.ToLower();
                    if (actionSetsByPath.ContainsKey(loweredPath))
                    {
                        actionSetsByPathCache.Add(path, actionSetsByPath[loweredPath]);
                        return (T)actionSetsByPath[loweredPath];
                    }
                    else
                    {
                        actionsByPathCache.Add(path, null);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Get an action set by the full path to that action set. Action set paths are in the format /actions/[actionSet]
        /// </summary>
        /// <param name="path">The full path to the action set you want (Action paths are in the format /actions/[actionSet])</param>
        public static SteamVR_ActionSet GetActionSetFromPath(string path)
        {
            return GetActionSetFromPath<SteamVR_ActionSet>(path);
        }

        /// <summary>Gets called by SteamVR_Behaviour every Update and updates actions if the steamvr settings are configured to update then.</summary>
        public static void Update()
        {
            if (initialized == false)
                return;

            if (SteamVR.settings.IsInputUpdateMode(SteamVR_UpdateModes.OnUpdate))
            {
                UpdateNonVisualActions();
            }
            if (SteamVR.settings.IsPoseUpdateMode(SteamVR_UpdateModes.OnUpdate))
            {
                UpdateVisualActions();
            }
        }

        /// <summary>
        /// Gets called by SteamVR_Behaviour every LateUpdate and updates actions if the steamvr settings are configured to update then. 
        /// Also updates skeletons regardless of settings are configured to so we can account for animations on the skeletons.
        /// </summary>
        public static void LateUpdate()
        {
            if (initialized == false)
                return;

            if (SteamVR.settings.IsInputUpdateMode(SteamVR_UpdateModes.OnLateUpdate))
            {
                UpdateNonVisualActions();
            }

            if (SteamVR.settings.IsPoseUpdateMode(SteamVR_UpdateModes.OnLateUpdate))
            {
                //update poses and skeleton
                UpdateVisualActions();
            }
            else
            {
                //force skeleton update so animation blending sticks
                UpdateSkeletonActions(true);
            }
        }

        /// <summary>Gets called by SteamVR_Behaviour every FixedUpdate and updates actions if the steamvr settings are configured to update then.</summary>
        public static void FixedUpdate()
        {
            if (initialized == false)
                return;

            if (SteamVR.settings.IsInputUpdateMode(SteamVR_UpdateModes.OnFixedUpdate))
            {
                UpdateNonVisualActions();
            }

            if (SteamVR.settings.IsPoseUpdateMode(SteamVR_UpdateModes.OnFixedUpdate))
            {
                UpdateVisualActions();
            }
        }

        /// <summary>Gets called by SteamVR_Behaviour every OnPreCull and updates actions if the steamvr settings are configured to update then.</summary>
        public static void OnPreCull()
        {
            if (initialized == false)
                return;

            if (SteamVR.settings.IsInputUpdateMode(SteamVR_UpdateModes.OnPreCull))
            {
                UpdateNonVisualActions();
            }
            if (SteamVR.settings.IsPoseUpdateMode(SteamVR_UpdateModes.OnPreCull))
            {
                UpdateVisualActions();
            }
        }

        /// <summary>
        /// Updates the states of all the visual actions (pose / skeleton)
        /// </summary>
        /// <param name="skipStateAndEventUpdates">Controls whether or not events are fired from this update call</param>
        public static void UpdateVisualActions(bool skipStateAndEventUpdates = false)
        {
            if (initialized == false)
                return;

            SteamVR_ActionSet.UpdateActionSetsState();

            UpdatePoseActions(skipStateAndEventUpdates);

            UpdateSkeletonActions(skipStateAndEventUpdates);
        }

        /// <summary>
        /// Updates the states of all the pose actions
        /// </summary>
        /// <param name="skipStateAndEventUpdates">Controls whether or not events are fired from this update call</param>
        public static void UpdatePoseActions(bool skipStateAndEventUpdates = false)
        {
            if (initialized == false)
                return;

            var sources = SteamVR_Input_Source.GetUpdateSources();

            for (int sourceIndex = 0; sourceIndex < sources.Length; sourceIndex++)
            {
                UpdatePoseActions(sources[sourceIndex], skipStateAndEventUpdates);
            }

            if (OnPosesUpdated != null)
                OnPosesUpdated(false);
        }

        /// <summary>
        /// Updates the states of all the pose actions for a specific input source (left hand / right hand / any)
        /// </summary>
        /// <param name="skipStateAndEventUpdates">Controls whether or not events are fired from this update call</param>
        protected static void UpdatePoseActions(SteamVR_Input_Sources inputSource, bool skipStateAndEventUpdates = false)
        {
            if (initialized == false)
                return;

            for (int actionIndex = 0; actionIndex < actionsPose.Length; actionIndex++)
            {
                SteamVR_Action_Pose action = actionsPose[actionIndex] as SteamVR_Action_Pose;

                if (action != null)
                {
                    if (action.actionSet.IsActive())
                    {
                        action.UpdateValue(inputSource, skipStateAndEventUpdates);
                    }
                }
            }
        }


        /// <summary>
        /// Updates the states of all the skeleton actions
        /// </summary>
        /// <param name="skipStateAndEventUpdates">Controls whether or not events are fired from this update call</param>
        public static void UpdateSkeletonActions(bool skipStateAndEventUpdates = false)
        {
            if (initialized == false)
                return;

            var sources = SteamVR_Input_Source.GetUpdateSources();

            for (int sourceIndex = 0; sourceIndex < sources.Length; sourceIndex++)
            {
                UpdateSkeletonActions(sources[sourceIndex], skipStateAndEventUpdates);
            }

            if (OnSkeletonsUpdated != null)
                OnSkeletonsUpdated(skipStateAndEventUpdates);
        }


        /// <summary>
        /// Updates the states of all the skeleton actions for a specific input source (left hand / right hand / any)
        /// </summary>
        /// <param name="skipStateAndEventUpdates">Controls whether or not events are fired from this update call</param>
        protected static void UpdateSkeletonActions(SteamVR_Input_Sources inputSource, bool skipStateAndEventUpdates = false)
        {
            if (initialized == false)
                return;

            for (int actionIndex = 0; actionIndex < actionsSkeleton.Length; actionIndex++)
            {
                SteamVR_Action_Skeleton action = actionsSkeleton[actionIndex] as SteamVR_Action_Skeleton;

                if (action != null)
                {
                    if (action.actionSet.IsActive())
                    {
                        action.UpdateValue(inputSource, skipStateAndEventUpdates);
                    }
                }
            }
        }


        /// <summary>
        /// Updates the states of all the non visual actions (boolean, single, vector2, vector3)
        /// </summary>
        public static void UpdateNonVisualActions()
        {
            if (initialized == false)
                return;

            SteamVR_ActionSet.UpdateActionSetsState();

            var sources = SteamVR_Input_Source.GetUpdateSources();

            for (int sourceIndex = 0; sourceIndex < sources.Length; sourceIndex++)
            {
                UpdateNonVisualActions(sources[sourceIndex]);
            }

            if (OnNonVisualActionsUpdated != null)
                OnNonVisualActionsUpdated();
        }

        /// <summary>
        /// Updates the states of all the non visual actions (boolean, single, vector2, vector3) for a given input source (left hand / right hand / any)
        /// </summary>
        /// <param name="inputSource"></param>
        public static void UpdateNonVisualActions(SteamVR_Input_Sources inputSource)
        {
            if (initialized == false)
                return;

            for (int actionSetIndex = 0; actionSetIndex < actionSets.Length; actionSetIndex++)
            {
                SteamVR_ActionSet set = actionSets[actionSetIndex];

                if (set.IsActive())
                {
                    for (int actionIndex = 0; actionIndex < set.nonVisualInActions.Length; actionIndex++)
                    {
                        SteamVR_Action_In actionIn = set.nonVisualInActions[actionIndex] as SteamVR_Action_In;

                        if (actionIn != null)
                        {
                            actionIn.UpdateValue(inputSource);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns all of the action sets. If we're in the editor, doesn't rely on the actionSets field being filled.
        /// </summary>
        public static SteamVR_ActionSet[] GetActionSets()
        {
            if (Application.isPlaying)
            {
                return actionSets;
            }
            else
            {
#if UNITY_EDITOR
                string[] assetGuids = UnityEditor.AssetDatabase.FindAssets("t:" + typeof(SteamVR_ActionSet).FullName);
                if (assetGuids.Length > 0)
                {
                    SteamVR_ActionSet[] assets = new SteamVR_ActionSet[assetGuids.Length];
                    for (int assetIndex = 0; assetIndex < assets.Length; assetIndex++)
                    {
                        string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(assetGuids[assetIndex]);
                        assets[assetIndex] = UnityEditor.AssetDatabase.LoadAssetAtPath<SteamVR_ActionSet>(assetPath);
                    }
                    return assets;
                }
                return new SteamVR_ActionSet[0];
#else
            return null;
#endif
            }
        }

        /// <summary>
        /// Returns all of the actions of the specified type. If we're in the editor, doesn't rely on the arrays being filled.
        /// </summary>
        /// <typeparam name="T">The type of actions you want to get</typeparam>
        public static T[] GetActions<T>() where T : SteamVR_Action
        {
            Type type = typeof(T);

            if (Application.isPlaying)
            {
                if (type == typeof(SteamVR_Action))
                {
                    return actions as T[];
                }
                else if (type == typeof(SteamVR_Action_In))
                {
                    return actionsIn as T[];
                }
                else if (type == typeof(SteamVR_Action_Out))
                {
                    return actionsOut as T[];
                }
                else if (type == typeof(SteamVR_Action_Boolean))
                {
                    return actionsBoolean as T[];
                }
                else if (type == typeof(SteamVR_Action_Single))
                {
                    return actionsSingle as T[];
                }
                else if (type == typeof(SteamVR_Action_Vector2))
                {
                    return actionsVector2 as T[];
                }
                else if (type == typeof(SteamVR_Action_Vector3))
                {
                    return actionsVector3 as T[];
                }
                else if (type == typeof(SteamVR_Action_Pose))
                {
                    return actionsPose as T[];
                }
                else if (type == typeof(SteamVR_Action_Skeleton))
                {
                    return actionsSkeleton as T[];
                }
                else if (type == typeof(SteamVR_Action_Vibration))
                {
                    return actionsVibration as T[];
                }
                else
                {
                    Debug.Log("Wrong type.");
                }
            }
            else
            {
#if UNITY_EDITOR
                string[] assetGuids = UnityEditor.AssetDatabase.FindAssets("t:" + type.FullName);
                if (assetGuids.Length > 0)
                {
                    T[] assets = new T[assetGuids.Length];
                    for (int assetIndex = 0; assetIndex < assets.Length; assetIndex++)
                    {
                        string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(assetGuids[assetIndex]);
                        assets[assetIndex] = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetPath);
                    }
                    return assets;
                }
                return new T[0];
#endif
            }

            return null;
        }

        protected static Delegate GetMethod<T>(string methodName)
        {
            MethodInfo methodInfo = inputType.GetMethod(methodName);

            return Delegate.CreateDelegate(typeof(T), methodInfo);
        }

        /// <summary>
        /// Does the actions file in memory differ from the one on disk as determined by a md5 hash
        /// </summary>
        public static bool HasFileInMemoryBeenModified()
        {
            string projectPath = Application.dataPath;
            int lastIndex = projectPath.LastIndexOf("/");
            projectPath = projectPath.Remove(lastIndex, projectPath.Length - lastIndex);
            actionsFilePath = Path.Combine(projectPath, SteamVR_Settings.instance.actionsFilePath);

            string jsonText = null;

            if (File.Exists(actionsFilePath))
            {
                jsonText = System.IO.File.ReadAllText(actionsFilePath);
            }
            else
            {
                return true;
            }

            string newHashFromFile = SteamVR_Utils.GetBadMD5Hash(jsonText);

            string newJSON = JsonConvert.SerializeObject(SteamVR_Input.actionFile, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            string newHashFromMemory = SteamVR_Utils.GetBadMD5Hash(newJSON);

            return newHashFromFile != newHashFromMemory;
        }

        /// <summary>
        /// Load from disk and deserialize the actions file
        /// </summary>
        /// <param name="force">Force a refresh of this file from disk</param>
        public static bool InitializeFile(bool force = false)
        {
            string projectPath = Application.dataPath;
            int lastIndex = projectPath.LastIndexOf("/");
            projectPath = projectPath.Remove(lastIndex, projectPath.Length - lastIndex);
            actionsFilePath = Path.Combine(projectPath, SteamVR_Settings.instance.actionsFilePath);

            string jsonText = null;

            if (File.Exists(actionsFilePath))
            {
                jsonText = System.IO.File.ReadAllText(actionsFilePath);
            }
            else
            {
                Debug.LogErrorFormat("[SteamVR] Actions file does not exist in project root: {0}", actionsFilePath);
                return false;
            }

            if (fileInitialized == true || (fileInitialized == true && force == false))
            {
                string newHash = SteamVR_Utils.GetBadMD5Hash(jsonText);

                if (newHash == actionFileHash)
                {
                    return true;
                }

                actionFileHash = newHash;
            }

            actionFile = Valve.Newtonsoft.Json.JsonConvert.DeserializeObject<SteamVR_Input_ActionFile>(jsonText);
            actionFile.InitializeHelperLists();
            fileInitialized = true;
            return true;
        }

#if UNITY_EDITOR
        public static string GetResourcesFolderPath(bool fromAssetsDirectory = false)
        {
            string inputFolder = string.Format("Assets/{0}", SteamVR_Settings.instance.steamVRInputPath);

            string path = Path.Combine(inputFolder, "Resources");

            bool createdDirectory = false;
            if (Directory.Exists(inputFolder) == false)
            {
                Directory.CreateDirectory(inputFolder);
                createdDirectory = true;
            }


            if (Directory.Exists(path) == false)
            {
                Directory.CreateDirectory(path);
                createdDirectory = true;
            }

            if (createdDirectory)
                UnityEditor.AssetDatabase.Refresh();

            if (fromAssetsDirectory == false)
                return path.Replace("Assets/", "");
            else
                return path;
        }

        private static bool checkingSetup = false;
        private static void CheckSetup()
        {
            if (checkingSetup == false && (SteamVR_Input_References.instance.actionSetObjects == null || SteamVR_Input_References.instance.actionSetObjects.Length == 0 || SteamVR_Input_References.instance.actionSetObjects.Any(set => set != null) == false))
            {
                checkingSetup = true;
                Debug.Break();

                bool open = UnityEditor.EditorUtility.DisplayDialog("[SteamVR]", "It looks like you haven't generated actions for SteamVR Input yet. Would you like to open the SteamVR Input window?", "Yes", "No");
                if (open)
                {
                    UnityEditor.EditorApplication.isPlaying = false;
                    Type editorWindowType = FindType("Valve.VR.SteamVR_Input_EditorWindow");
                    if (editorWindowType != null)
                    {
                        var window = UnityEditor.EditorWindow.GetWindow(editorWindowType, false, "SteamVR Input", true);
                        if (window != null)
                            window.Show();
                    }
                }
                checkingSetup = false;
            }
        }

        private static Type FindType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null) return type;
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = a.GetType(typeName);
                if (type != null)
                    return type;
            }
            return null;
        }
#endif
    }
}