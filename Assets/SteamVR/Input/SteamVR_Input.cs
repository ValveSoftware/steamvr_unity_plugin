//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Access to SteamVR system (hmd) and compositor (distort) interfaces.
//
//=============================================================================

using UnityEngine;
using Valve.VR;
using System.IO;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Valve.Newtonsoft.Json;

public partial class SteamVR_Input
{
    public const string defaultInputGameObjectName = "[SteamVR Input]";
    private const string localizationKeyName = "localization";
    public static string actionsFilePath;

    public static bool fileInitialized = false;
    public static bool initialized = false;

    public static SteamVR_Input_ActionFile actionFile;
    public static string actionFileHash;

    public static Action InitializeActionSets;
    public static Action InitializeActions;

    public static Action InitializeInstanceActionSets;
    public static Action InitializeInstanceActions;

    public static event Action OnNonVisualActionsUpdated;
    public static event Action<bool> OnPosesUpdated;
    public static event Action<bool> OnSkeletonsUpdated;

    protected static Type inputType = typeof(SteamVR_Input);

    protected static bool initializing = false;

    #region array accessors
    public static SteamVR_Input_ActionSet[] actionSets;

    public static SteamVR_Input_Action[] actions;

    public static SteamVR_Input_Action_In[] actionsIn;

    public static SteamVR_Input_Action_Out[] actionsOut;

    public static SteamVR_Input_Action_Boolean[] actionsBoolean;

    public static SteamVR_Input_Action_Single[] actionsSingle;

    public static SteamVR_Input_Action_Vector2[] actionsVector2;

    public static SteamVR_Input_Action_Vector3[] actionsVector3;

    public static SteamVR_Input_Action_Pose[] actionsPose;

    public static SteamVR_Input_Action_Skeleton[] actionsSkeleton;

    public static SteamVR_Input_Action_Vibration[] actionsVibration;

    public static SteamVR_Input_Action_In[] actionsNonPoseNonSkeletonIn;

    protected static Dictionary<string, SteamVR_Input_ActionSet> actionSetsByPath = new Dictionary<string, SteamVR_Input_ActionSet>();
    protected static Dictionary<string, SteamVR_Input_Action> actionsByPath = new Dictionary<string, SteamVR_Input_Action>();

    protected static Dictionary<string, SteamVR_Input_ActionSet> actionSetsByPathCache = new Dictionary<string, SteamVR_Input_ActionSet>();
    protected static Dictionary<string, SteamVR_Input_Action> actionsByPathCache = new Dictionary<string, SteamVR_Input_Action>();
    #endregion

    public static void IdentifyActionsFile()
    {
        string currentPath = Application.dataPath;
        int lastIndex = currentPath.LastIndexOf('/');
        currentPath = currentPath.Remove(lastIndex, currentPath.Length - lastIndex);

        string fullPath = System.IO.Path.Combine(currentPath, SteamVR_Settings.instance.actionsFilePath);
        fullPath = fullPath.Replace("\\", "/");

        Debug.Log("Loading actions file: " + fullPath);

        var err = OpenVR.Input.SetActionManifestPath(fullPath);
        if (err != EVRInputError.None)
            Debug.LogError("Error loading action manifest into SteamVR: " + err.ToString());
        else
            Debug.Log("Successfully loaded action manifest into SteamVR");
    }

    public static void Initialize()
    {
        if (initialized)
            return;

        Debug.Log("Initializing steamvr input...");
        initializing = true;

#if UNITY_EDITOR
        CheckSetup();
#endif

        SteamVR_Input_Input_Source.Initialize();

        InitializeActionSets = GetMethod<Action>(SteamVR_Input_Generator_Names.initializeActionSetsMethodName) as Action;
        InitializeActions = GetMethod<Action>(SteamVR_Input_Generator_Names.initializeActionsMethodName) as Action;

        InitializeInstanceActionSets = GetMethod<Action>(SteamVR_Input_Generator_Names.initializeInstanceActionSetsMethodName) as Action;
        InitializeInstanceActions = GetMethod<Action>(SteamVR_Input_Generator_Names.initializeInstanceActionsMethodName) as Action;

        InitializeInstanceActionSets();
        InitializeInstanceActions();

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

        SteamVR_Input_Action_Pose.SetTrackingUniverseOrigin(SteamVR_Settings.instance.trackingSpace);

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
            SteamVR_Input_Action action = actions[actionIndex];
            actionsByPath.Add(action.fullPath.ToLower(), action);
        }

        for (int actionSetIndex = 0; actionSetIndex < actionSets.Length; actionSetIndex++)
        {
            SteamVR_Input_ActionSet set = actionSets[actionSetIndex];
            actionSetsByPath.Add(set.fullPath.ToLower(), set);
        }
    }

    public static T GetActionFromPath<T>(string path, bool caseSensitive = false) where T : SteamVR_Input_Action
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
    public static SteamVR_Input_Action GetActionFromPath(string path)
    {
        return GetActionFromPath<SteamVR_Input_Action>(path);
    }
    public static T GetActionSetFromPath<T>(string path, bool caseSensitive = false) where T : SteamVR_Input_ActionSet
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
    public static SteamVR_Input_ActionSet GetActionSetFromPath(string path)
    {
        return GetActionSetFromPath<SteamVR_Input_ActionSet>(path);
    }

    public static Func<T> GetPropertyGetter<T>(string propertyName)
    {
        PropertyInfo property = inputType.GetProperty(propertyName, BindingFlags.Static | BindingFlags.Public);
        MethodInfo getMethod = property.GetGetMethod(true);

        return (Func<T>)Delegate.CreateDelegate(typeof(Func<T>), getMethod);
    }

    public static Delegate GetMethod<T>(string methodName)
    {
        MethodInfo methodInfo = inputType.GetMethod(methodName);

        return Delegate.CreateDelegate(typeof(T), methodInfo);
    }

    public static void UpdateVisualActions(bool skipStateAndEventUpdates = false)
    {
        if (initialized == false)
            return;

        SteamVR_Input_ActionSet.UpdateActionSetsState();
        
        UpdatePoseActions(skipStateAndEventUpdates);

        UpdateSkeletonActions(skipStateAndEventUpdates);
    }

    public static void Update()
    {
        if (initialized == false)
            return;
        
        if (SteamVR.settings.IsInputUpdateMode(SteamVR_UpdateModes.OnUpdate))
        {
            UpdateNonVisualActions();
        }
    }

    public static void LateUpdate()
    {
        if (initialized == false)
            return;

        if (SteamVR.settings.IsInputUpdateMode(SteamVR_UpdateModes.OnLateUpdate))
        {
            UpdateNonVisualActions();
        }
    }

    public static void FixedUpdate()
    {
        if (initialized == false)
            return;

        if (SteamVR.settings.IsInputUpdateMode(SteamVR_UpdateModes.OnFixedUpdate))
        {
            UpdateNonVisualActions();
        }
    }

    public static void UpdatePoseActions(bool skipStateAndEventUpdates = false)
    {
        if (initialized == false)
            return;

        var sources = SteamVR_Input_Input_Source.GetUpdateSources();

        for (int sourceIndex = 0; sourceIndex < sources.Length; sourceIndex++)
        {
            UpdatePoseActions(sources[sourceIndex], skipStateAndEventUpdates);
        }

        if (OnPosesUpdated != null)
            OnPosesUpdated(false);
    }

    protected static void UpdatePoseActions(SteamVR_Input_Input_Sources inputSource, bool skipStateAndEventUpdates = false)
    {
        if (initialized == false)
            return;

        for (int actionIndex = 0; actionIndex < actionsPose.Length; actionIndex++)
        {
            SteamVR_Input_Action_Pose action = actionsPose[actionIndex] as SteamVR_Input_Action_Pose;

            if (action != null)
            {
                if (action.actionSet.IsActive())
                {
                    action.UpdateValue(inputSource, skipStateAndEventUpdates);
                }
            }
        }
    }

    public static void UpdateSkeletonActions(bool skipStateAndEventUpdates = false)
    {
        if (initialized == false)
            return;

        var sources = SteamVR_Input_Input_Source.GetUpdateSources();

        for (int sourceIndex = 0; sourceIndex < sources.Length; sourceIndex++)
        {
            UpdateSkeletonActions(sources[sourceIndex], skipStateAndEventUpdates);
        }

        if (OnSkeletonsUpdated != null)
            OnSkeletonsUpdated(false);
    }

    protected static void UpdateSkeletonActions(SteamVR_Input_Input_Sources inputSource, bool skipStateAndEventUpdates = false)
    {
        if (initialized == false)
            return;

        for (int actionIndex = 0; actionIndex < actionsSkeleton.Length; actionIndex++)
        {
            SteamVR_Input_Action_Skeleton action = actionsSkeleton[actionIndex] as SteamVR_Input_Action_Skeleton;

            if (action != null)
            {
                if (action.actionSet.IsActive())
                {
                    action.UpdateValue(inputSource, skipStateAndEventUpdates);
                }
            }
        }
    }

    public static void UpdateNonVisualActions()
    {
        if (initialized == false)
            return;

        var sources = SteamVR_Input_Input_Source.GetUpdateSources();

        for (int sourceIndex = 0; sourceIndex < sources.Length; sourceIndex++)
        {
            UpdateNonVisualActions(sources[sourceIndex]);
        }

        if (OnNonVisualActionsUpdated != null)
            OnNonVisualActionsUpdated();
    }

    public static void UpdateNonVisualActions(SteamVR_Input_Input_Sources inputSource)
    {
        if (initialized == false)
            return;

        SteamVR_Input_ActionSet.UpdateActionSetsState();


        for (int actionSetIndex = 0; actionSetIndex < actionSets.Length; actionSetIndex++)
        {
            SteamVR_Input_ActionSet set = actionSets[actionSetIndex];

            if (set.IsActive())
            {
                for (int actionIndex = 0; actionIndex < set.nonVisualInActions.Length; actionIndex++)
                {
                    SteamVR_Input_Action_In actionIn = set.nonVisualInActions[actionIndex] as SteamVR_Input_Action_In;

                    if (actionIn != null)
                    {
                        actionIn.UpdateValue(inputSource);
                    }
                }
            }
        }
    }

    public static SteamVR_Input_ActionSet[] GetActionSets()
    {
        if (Application.isPlaying)
        {
            return actionSets;
        }
        else
        {
            #if UNITY_EDITOR
            string[] assetGuids = UnityEditor.AssetDatabase.FindAssets("t:" + typeof(SteamVR_Input_ActionSet).FullName);
            if (assetGuids.Length > 0)
            {
                SteamVR_Input_ActionSet[] assets = new SteamVR_Input_ActionSet[assetGuids.Length];
                for (int assetIndex = 0; assetIndex < assets.Length; assetIndex++)
                {
                    string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(assetGuids[assetIndex]);
                    assets[assetIndex] = UnityEditor.AssetDatabase.LoadAssetAtPath<SteamVR_Input_ActionSet>(assetPath);
                }
                return assets;
            }
            return new SteamVR_Input_ActionSet[0];
            #else
            return null;
            #endif
        }
    }

    public static T[] GetActions<T>() where T : SteamVR_Input_Action
    {
        Type type = typeof(T);

        if (Application.isPlaying)
        {
            if (type == typeof(SteamVR_Input_Action))
            {
                return actions as T[];
            }
            else if (type == typeof(SteamVR_Input_Action_In))
            {
                return actionsIn as T[];
            }
            else if (type == typeof(SteamVR_Input_Action_Out))
            {
                return actionsOut as T[];
            }
            else if (type == typeof(SteamVR_Input_Action_Boolean))
            {
                return actionsBoolean as T[];
            }
            else if (type == typeof(SteamVR_Input_Action_Single))
            {
                return actionsSingle as T[];
            }
            else if (type == typeof(SteamVR_Input_Action_Vector2))
            {
                return actionsVector2 as T[];
            }
            else if (type == typeof(SteamVR_Input_Action_Vector3))
            {
                return actionsVector3 as T[];
            }
            else if (type == typeof(SteamVR_Input_Action_Pose))
            {
                return actionsPose as T[];
            }
            else if (type == typeof(SteamVR_Input_Action_Skeleton))
            {
                return actionsSkeleton as T[];
            }
            else if (type == typeof(SteamVR_Input_Action_Vibration))
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
            Debug.LogFormat("[SteamVR] Actions file does not exist in project root: ", actionsFilePath);
                //todo: copy a default file here?
                //todo: move to streaming assets?
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

    private static void CheckSetup()
    {
        if (SteamVR_Input_References.instance.actionSetObjects == null || SteamVR_Input_References.instance.actionSetObjects.Length == 0 || SteamVR_Input_References.instance.actionSetObjects.Any(set => set != null) == false)
        {
            Debug.Break();
            bool open = UnityEditor.EditorUtility.DisplayDialog("[SteamVR]", "It looks like you haven't generated actions for SteamVR Input yet. Would you like to open the SteamVR Input window?", "Yes", "No");
            if (open)
            {
                UnityEditor.EditorApplication.isPlaying = false;
                Type editorWindowType = FindType("SteamVR_Input_EditorWindow");
                if (editorWindowType != null)
                {
                    var window = UnityEditor.EditorWindow.GetWindow(editorWindowType, false, "SteamVR Input", true);
                    if (window != null)
                        window.Show();
                }
            }
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