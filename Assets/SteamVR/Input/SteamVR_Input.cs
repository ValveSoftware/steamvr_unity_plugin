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

public partial class SteamVR_Input : MonoBehaviour
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

    public static event Action OnNonVisualActionsUpdated;
    public static event Action<bool> OnPosesUpdated;
    public static event Action<bool> OnSkeletonsUpdated;

    protected static SteamVR_Input instance = null;

    protected static Type inputType = typeof(SteamVR_Input);

    protected static bool initializing = false;

    #region instance and static array accessors
    public SteamVR_Input_ActionSet[] instance_actionSets;
    public static SteamVR_Input_ActionSet[] actionSets
    {
        get
        {
            return SteamVR_Input.instance.instance_actionSets;
        }
    }

    public SteamVR_Input_Action[] instance_actions;

    public SteamVR_Input_Action_In[] instance_actionsIn;

    public SteamVR_Input_Action_Out[] instance_actionsOut;

    public SteamVR_Input_Action_Boolean[] instance_actionsBoolean;

    public SteamVR_Input_Action_Single[] instance_actionsSingle;

    public SteamVR_Input_Action_Vector2[] instance_actionsVector2;

    public SteamVR_Input_Action_Vector3[] instance_actionsVector3;

    public SteamVR_Input_Action_Pose[] instance_actionsPose;

    public SteamVR_Input_Action_Skeleton[] instance_actionsSkeleton;

    public SteamVR_Input_Action_In[] instance_actionsNonPoseNonSkeletonIn;

    public static SteamVR_Input_Action[] actions
    {
        get
        {
            return SteamVR_Input.instance.instance_actions;
        }
    }

    public static SteamVR_Input_Action_In[] actionsIn
    {
        get
        {
            return SteamVR_Input.instance.instance_actionsIn;
        }
    }

    public static SteamVR_Input_Action_Out[] actionsOut
    {
        get
        {
            return SteamVR_Input.instance.instance_actionsOut;
        }
    }

    public static SteamVR_Input_Action_Boolean[] actionsBoolean
    {
        get
        {
            return SteamVR_Input.instance.instance_actionsBoolean;
        }
    }

    public static SteamVR_Input_Action_Single[] actionsSingle
    {
        get
        {
            return SteamVR_Input.instance.instance_actionsSingle;
        }
    }

    public static SteamVR_Input_Action_Vector2[] actionsVector2
    {
        get
        {
            return SteamVR_Input.instance.instance_actionsVector2;
        }
    }

    public static SteamVR_Input_Action_Vector3[] actionsVector3
    {
        get
        {
            return SteamVR_Input.instance.instance_actionsVector3;
        }
    }

    public static SteamVR_Input_Action_Pose[] actionsPose
    {
        get
        {
            return SteamVR_Input.instance.instance_actionsPose;
        }
    }

    public static SteamVR_Input_Action_Skeleton[] actionsSkeleton
    {
        get
        {
            return SteamVR_Input.instance.instance_actionsSkeleton;
        }
    }

    public static SteamVR_Input_Action_In[] actionsNonPoseNonSkeletonIn
    {
        get
        {
            return SteamVR_Input.instance.instance_actionsNonPoseNonSkeletonIn;
        }
    }

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

    public static void InitializeMapping()
    {
        CheckSetup();

        Type actionSetType = typeof(SteamVR_Input_ActionSet);
        Type actionType = typeof(SteamVR_Input_Action);

        FieldInfo[] fields = inputType.GetFields(BindingFlags.Instance | BindingFlags.Public);
        FieldInfo[] actionSetFields = fields.Where(field => actionSetType.IsAssignableFrom(field.FieldType)).ToArray();
        FieldInfo[] actionFields = fields.Where(field => actionType.IsAssignableFrom(field.FieldType)).ToArray();

        for (int actionSetIndex = 0; actionSetIndex < actionSetFields.Length; actionSetIndex++)
        {
            string fieldName = actionSetFields[actionSetIndex].Name;

            int setReferenceIndex = 0;
            for (setReferenceIndex = 0; setReferenceIndex < SteamVR_Input_References.instance.actionSetNames.Length; setReferenceIndex++)
            {
                string currentName = SteamVR_Input_References.instance.actionSetNames[setReferenceIndex];
                if (currentName == fieldName)
                {
                    break;
                }
            }

            actionSetFields[actionSetIndex].SetValue(instance, SteamVR_Input_References.instance.actionSetObjects[setReferenceIndex]);
        }


        for (int actionIndex = 0; actionIndex < actionFields.Length; actionIndex++)
        {
            string fieldName = actionFields[actionIndex].Name;

            int actionReferenceIndex = 0;
            for (actionReferenceIndex = 0; actionReferenceIndex < SteamVR_Input_References.instance.actionNames.Length; actionReferenceIndex++)
            {
                string currentName = SteamVR_Input_References.instance.actionNames[actionReferenceIndex];
                if (currentName == fieldName)
                {
                    break;
                }
            }

            actionFields[actionIndex].SetValue(instance, SteamVR_Input_References.instance.actionObjects[actionReferenceIndex]);
        }
    }

    public static void Initialize(GameObject inputObject)
    {
        Debug.Log("Initializing steamvr input...");
        initializing = true;
        //use the new instance of SteamVR_Input
        SteamVR_Input newInstance = inputObject.GetComponent<SteamVR_Input>();
        if (newInstance == null)
            newInstance = inputObject.AddComponent<SteamVR_Input>();

        if (instance != null && instance != newInstance)
            Destroy(instance);
        instance = newInstance;

        InitializeMapping();

        if (initialized == true && actions != null && actionSets != null)
        {
            Debug.LogError("Already initialized steamvr input. Continuing...");
            return;
        }

        SteamVR_Input_Input_Source.Initialize();

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


        SteamVR_Input_Action_Pose.SetTrackingUniverseOrigin(SteamVR_Settings.instance.trackingSpace);
        

        InitializeDictionaries();

        initializing = false;
        initialized = true;
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

    private void Update()
    {
        if (initialized == false)
            return;
        
        if (SteamVR.settings.IsInputUpdateMode(SteamVR_UpdateModes.OnUpdate))
        {
            UpdateNonVisualActions();
        }
    }

    private void LateUpdate()
    {
        if (initialized == false)
            return;

        if (SteamVR.settings.IsInputUpdateMode(SteamVR_UpdateModes.OnLateUpdate))
        {
            UpdateNonVisualActions();
        }
    }

    private void OnDestroy()
    {
        initialized = false;
    }

    private void FixedUpdate()
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
            if (instance != null)
                return actionSets;
            else
                return null;
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
            if (instance == null)
                return null;

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
                return actionsOut.Cast<SteamVR_Input_Action_Vibration>() as T[];
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

    private static void CheckSetup()
    {
#if UNITY_EDITOR
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
#endif
    }

#if UNITY_EDITOR
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

    private void Reset()
    {
        if (Application.isPlaying == false && initializing == false)
        {
            DestroyImmediate(this);
            UnityEditor.EditorUtility.DisplayDialog("[SteamVR]", "This component gets added automatically at runtime.", "Ok");
        }
    }
#endif
}

public enum SteamVR_UpdateModes
{
    Nothing = (1 << 0),
    OnUpdate = (1 << 1),
    OnFixedUpdate = (1 << 2),
    OnPreCull = (1 << 3),
    OnLateUpdate = (1 << 4),
}

public enum SteamVR_Input_ActionDirections
{
    In,
    Out,
}

public enum SteamVR_Input_ActionScopes
{
    ActionSet,
    Application,
    Global,
}

public enum SteamVR_Input_ActionSetUsages
{
    LeftRight,
    Single,
    Hidden,
}

public class SteamVR_Input_Generator_Names
{
    public const string initializeActionSetsMethodName = "Dynamic_InitializeActionSets";
    public const string initializeActionsMethodName = "Dynamic_InitializeActions";
    public const string updateActionsMethodName = "Dynamic_UpdateActions";
    public const string updateNonPoseNonSkeletonActionsMethodName = "Dynamic_UpdateNonPoseNonSkeletonActions";
    public const string updatePoseActionsMethodName = "Dynamic_UpdatePoseActions";
    public const string updateSkeletonActionsMethodName = "Dynamic_UpdateSkeletalActions";

    public const string actionsFieldName = "actions";
    public const string actionsInFieldName = "actionsIn";
    public const string actionsOutFieldName = "actionsOut";
    public const string actionsPoseFieldName = "actionsPose";
    public const string actionsBooleanFieldName = "actionsBoolean";
    public const string actionsSingleFieldName = "actionsSingle";
    public const string actionsVector2FieldName = "actionsVector2";
    public const string actionsVector3FieldName = "actionsVector3";
    public const string actionsSkeletonFieldName = "actionsSkeleton";
    public const string actionsNonPoseNonSkeletonIn = "actionsNonPoseNonSkeletonIn";
    public const string actionSetsFieldName = "actionSets";
}