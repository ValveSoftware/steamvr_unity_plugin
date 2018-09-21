using UnityEditor;
using UnityEngine;

using System.CodeDom;
using Microsoft.CSharp;
using System.IO;
using System.CodeDom.Compiler;

using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;
using System;
using UnityEditorInternal;
using Valve.Newtonsoft.Json;

namespace Valve.VR
{
#pragma warning disable 0219 // variable assigned but not used.
    public class SteamVR_Input_EditorWindow : EditorWindow
    {
        [MenuItem("Window/SteamVR Input")]
        public static void ShowWindow()
        {
            GetWindow<SteamVR_Input_EditorWindow>(false, "SteamVR Input", true);
        }

        private const bool defaultOverwriteBuildOption = true;
        private const bool defaultDeleteUnusedOption = true;

        private static void InitializeEditorValues()
        {

            if (EditorPrefs.HasKey(SteamVR_Input_Generator.steamVRInputOverwriteBuildKey) == false)
                EditorPrefs.SetBool(SteamVR_Input_Generator.steamVRInputOverwriteBuildKey, defaultOverwriteBuildOption);

            if (EditorPrefs.HasKey(SteamVR_Input_Generator.steamVRInputDeleteUnusedKey) == false)
                EditorPrefs.SetBool(SteamVR_Input_Generator.steamVRInputDeleteUnusedKey, defaultDeleteUnusedOption);
        }

        private ReorderableList inList;
        private ReorderableList outList;
        private ReorderableList localizationList;

        private int selectedActionIndex = -1;
        private SteamVR_Input_ActionFile_Action selectedAction;
        private int selectedActionSet = -1;
        private int selectedLocalizationIndex = -1;

        private void InitializeLists()
        {
            if (selectedActionSet == -1)
            {
                inList = null;
                outList = null;
                return;
            }


            inList = new ReorderableList(SteamVR_Input.actionFile.action_sets[selectedActionSet].actionsInList, typeof(string), false, true, true, true);
            inList.onAddCallback += OnAddCallback;
            inList.onRemoveCallback += OnRemoveCallback;
            inList.onSelectCallback += OnSelectCallback;
            inList.drawHeaderCallback += DrawHeaderCallbackIn;

            outList = new ReorderableList(SteamVR_Input.actionFile.action_sets[selectedActionSet].actionsOutList, typeof(string), false, true, true, true);
            outList.onAddCallback += OnAddCallback;
            outList.onRemoveCallback += OnRemoveCallback;
            outList.onSelectCallback += OnSelectCallback;
            outList.drawHeaderCallback += DrawHeaderCallbackOut;
        }

        private void DrawHeaderCallbackIn(Rect rect)
        {
            DrawHeaderCallback(rect, "In");
        }

        private void DrawHeaderCallbackOut(Rect rect)
        {
            DrawHeaderCallback(rect, "Out");
        }

        private void DrawHeaderCallback(Rect rect, string name)
        {
            EditorGUI.LabelField(rect, name);
        }

        private void OnSelectCallback(ReorderableList list)
        {
            selectedActionIndex = list.index;

            if (selectedActionIndex != -1)
            {
                selectedAction = (SteamVR_Input_ActionFile_Action)list.list[selectedActionIndex];
            }

            if (inList == list)
            {
                outList.index = -1;
            }
            else if (outList == list)
            {
                inList.index = -1;
            }
        }

        private void OnRemoveCallback(ReorderableList list)
        {
            if (list.index == -1)
                return;

            list.list.RemoveAt(list.index);

            list.index = -1;

            OnSelectCallback(list);
        }

        private void OnAddCallback(ReorderableList list)
        {
            if (selectedActionSet == -1)
            {
                return;
            }

            SteamVR_Input_ActionFile_Action newAction = new SteamVR_Input_ActionFile_Action();
            list.list.Add(newAction);

            string direction = "";

            if (inList == list)
            {
                direction = "in";
                outList.index = -1;
                inList.index = inList.list.Count - 1;
            }
            else if (outList == list)
            {
                direction = "out";
                inList.index = -1;
                outList.index = outList.list.Count - 1;
            }

            newAction.name = newAction.CreateNewName(SteamVR_Input.actionFile.action_sets[selectedActionSet].name, direction);

            OnSelectCallback(list);
        }

        private void InitializeLocalizationArray()
        {
            localizationList = new ReorderableList(SteamVR_Input.actionFile.localizationHelperList, typeof(string), false, true, true, true);
            localizationList.onAddCallback += LocalizationOnAddCallback;
            localizationList.onRemoveCallback += LocalizationOnRemoveCallback;
            localizationList.onSelectCallback += LocalizationOnSelectCallback;
            localizationList.drawHeaderCallback += LocalizationDrawHeaderCallback;
            localizationList.drawElementCallback += LocalizationDrawElementCallback;
        }

        private void LocalizationDrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            SteamVR_Input_ActionFile_LocalizationItem item = ((List<SteamVR_Input_ActionFile_LocalizationItem>)localizationList.list)[index];

            if (localizationList.index == index)
            {
                item.language = EditorGUI.TextField(rect, item.language);
            }
            else
            {
                EditorGUI.LabelField(rect, item.language);
            }
        }

        private void LocalizationDrawHeaderCallback(Rect rect)
        {
            EditorGUI.LabelField(rect, "Languages");
        }

        private void LocalizationOnSelectCallback(ReorderableList list)
        {
            selectedLocalizationIndex = list.index;
        }

        private void LocalizationOnRemoveCallback(ReorderableList list)
        {
            List<SteamVR_Input_ActionFile_LocalizationItem> itemList = ((List<SteamVR_Input_ActionFile_LocalizationItem>)localizationList.list);
            itemList.RemoveAt(list.index);

            selectedLocalizationIndex = -1;
        }

        private void LocalizationOnAddCallback(ReorderableList list)
        {
            List<SteamVR_Input_ActionFile_LocalizationItem> itemList = ((List<SteamVR_Input_ActionFile_LocalizationItem>)localizationList.list);
            SteamVR_Input_ActionFile_LocalizationItem newLanguage = new SteamVR_Input_ActionFile_LocalizationItem("new-language");
            newLanguage.items.Add(selectedAction.name, selectedAction.name);

            itemList.Add(newLanguage);

            selectedLocalizationIndex = list.list.Count - 1;
        }

        private const string progressBarTitle = "SteamVR Input Generation";
        private const string progressBarTextKey = "SteamVR_Input_ProgressBarText";
        private const string progressBarAmountKey = "SteamVR_Input_ProgressBarAmount";
        private static string progressBarText = null;
        private static float progressBarAmount = 0;
        public static void SetProgressBarText(string newText, float newAmount)
        {
            EditorPrefs.SetString(progressBarTextKey, newText);
            EditorPrefs.SetFloat(progressBarAmountKey, newAmount);
            progressBarText = newText;
            progressBarAmount = newAmount;
        }
        public static void ClearProgressBar()
        {
            EditorPrefs.SetString(progressBarTextKey, "");
            EditorPrefs.SetFloat(progressBarAmountKey, 0);
            progressBarText = "";
            progressBarAmount = 0;

            EditorUtility.ClearProgressBar();
        }
        private static void UpdateProgressBarTextFromPrefs()
        {
            if (progressBarText == null)
            {
                if (EditorPrefs.HasKey(progressBarTextKey))
                {
                    progressBarText = EditorPrefs.GetString(progressBarTextKey);
                    progressBarAmount = EditorPrefs.GetFloat(progressBarAmountKey);
                }
                else
                {
                    progressBarText = "";
                    EditorPrefs.SetString(progressBarTextKey, progressBarText);
                }
            }
        }

        void OnInspectorUpdate()
        {
            Repaint();

#if UNITY_2017_1_OR_NEWER
        if (EditorApplication.isCompiling == false) 
            SteamVR_Input_Generator.OnEditorUpdate();
#endif
        }

        private Vector2 scrollPosition;

        private bool initialized = false;

        private void Initialize()
        {
            SteamVR_Input.InitializeFile();
            InitializeEditorValues();
            initialized = true;
        }

        private bool CopyOrClose()
        {
            bool copy = UnityEditor.EditorUtility.DisplayDialog("Copy Examples", "It looks like your project is missing an actions.json. Would you like to use the example file?", "Yes", "No");
            if (copy)
            {
                SteamVR_CopyExampleInputFiles.CopyFiles(true);
                bool initializeSuccess = SteamVR_Input.InitializeFile();
                return initializeSuccess;
            }
            return false;
        }

        private void CheckFileInitialized()
        {
            if (initialized == false)
            {
                Initialize();
            }
            if (SteamVR_Input.actionFile == null)
            {
                bool initializeSuccess = SteamVR_Input.InitializeFile();

                if (initializeSuccess == false)
                {
                    bool copySuccess = CopyOrClose();
                    if (copySuccess == false)
                    {
                        this.Close();
                    }
                }
            }
        }

        private void UpdateProgressBar()
        {

            UpdateProgressBarTextFromPrefs();
            if (string.IsNullOrEmpty(progressBarText) == false)
            {
                bool cancel = EditorUtility.DisplayCancelableProgressBar(progressBarTitle, progressBarText, progressBarAmount);
                if (cancel)
                {
                    SteamVR_Input_Generator.CancelGeneration();
                    ClearProgressBar();
                }
            }
        }

        private void CheckInitialized()
        {
            if (localizationList == null)
                InitializeLocalizationArray();

            if (selectedLocalizationIndex == -1 && localizationList.count > 0)
                selectedLocalizationIndex = 0;

            if (selectedActionSet == -1 && SteamVR_Input.actionFile.action_sets.Count() > 0)
            {
                selectedActionSet = 0;
                InitializeLists();
            }

            if (selectedActionSet != -1 && inList == null)
                InitializeLists();
        }

        private void OnGUI()
        {
            CheckFileInitialized();

            UpdateProgressBar();

            if (Application.isPlaying == false && (SteamVR_Input_Generator.IsGenerating() == true || string.IsNullOrEmpty(progressBarText) == false))
            {
                EditorGUI.LabelField(new Rect(0, 0, 200, 20), "Generating SteamVR Input...");

                bool cancel = GUI.Button(new Rect(50, 20, 100, 20), "Cancel");
                if (cancel)
                {
                    SteamVR_Input_Generator.CancelGeneration();
                }

                return;
            }

#if UNITY_2017_1_OR_NEWER
        if (EditorApplication.isCompiling)
        {
            EditorGUI.LabelField(new Rect(0, 0, 100, 20), "Compiling...");
            return; //ongui gets more fussy after 2017
        }
#endif

            CheckInitialized();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.Space();

            DrawTop();

            EditorGUILayout.Space();

            DrawSets();

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            if (selectedActionSet == -1)
            {
                DrawNoSetSelected();
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
                DrawActions();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical();
                if (selectedActionIndex == -1)
                {
                    DrawNoActionSelected();
                }
                else
                {
                    DrawDetails();
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();

                DrawSave();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawTop()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            DrawRefreshButton();

            GUILayout.FlexibleSpace();

            DrawSettingsButton();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawRefreshButton()
        {
            bool refresh = GUILayout.Button("Refresh");

            if (refresh)
            {
                SteamVR_Input.InitializeFile(true);
                InitializeLists();
            }
        }

        private void DrawSettingsButton()
        {
            bool openSettings = GUILayout.Button("Settings");

            if (openSettings)
            {
                SteamVR_Input_SettingsEditor.ShowWindow();
            }
        }

        private void DrawNoSetSelected()
        {
            //EditorGUILayout.LabelField("No action set selected.");
            EditorGUILayout.LabelField("");
        }

        private void DrawNoActionSelected()
        {
            //EditorGUILayout.LabelField("No action selected.");
            EditorGUILayout.LabelField("");
        }

        private void DrawSave()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            bool save = GUILayout.Button("Save and generate");
            GUILayout.FlexibleSpace();
            bool open = GUILayout.Button("Open binding UI");
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (save)
            {
                SaveFile();
            }

            if (open)
            {
                OpenControllerBindings();
            }
        }

        private void OpenControllerBindings()
        {
            if (HasBeenModified())
            {
                bool saveFirst = EditorUtility.DisplayDialog("Save?", "It looks like you've made changes without saving. Would you like to save before editing the bindings?.", "Save", "Open without saving");
                if (saveFirst)
                {
                    SaveFile();
                }
            }

            SteamVR.ShowBindingsForEditor();
        }

        private bool HasBeenModified()
        {
            SteamVR_Input.actionFile.SaveHelperLists();
            return SteamVR_Input.HasFileInMemoryBeenModified();
        }

        private void OnDestroy()
        {
            if (HasBeenModified())
            {
                bool saveFirst = EditorUtility.DisplayDialog("Save?", "It looks like you've closed the input actions window without saving changes. Would you like to save first?", "Save", "Close");
                if (saveFirst)
                {
                    SaveFile();
                }
            }
        }

        private void DrawActions()
        {
            EditorGUILayout.BeginFadeGroup(1);

            EditorGUILayout.LabelField("Actions");

            EditorGUILayout.Space();

            if (inList != null)
                inList.DoLayoutList();

            EditorGUILayout.Space();

            if (inList != null)
                outList.DoLayoutList();

            EditorGUILayout.EndFadeGroup();
        }


        private void DrawDetails()
        {
            EditorGUILayout.BeginFadeGroup(1);

            EditorGUILayout.LabelField("Action Details");

            EditorGUILayout.Space();


            EditorGUILayout.LabelField("Full Action Path:");
            if (selectedActionIndex != -1)
            {
                EditorGUILayout.LabelField(selectedAction.name);
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Name:");
            if (selectedActionIndex != -1)
            {
                string newName = EditorGUILayout.TextField(selectedAction.shortName);

                if (newName != selectedAction.shortName)
                {
                    selectedAction.name = selectedAction.path + newName;
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Type:");
            if (selectedActionIndex != -1)
            {
                bool directionIn = selectedAction.path.IndexOf("/in/", StringComparison.CurrentCultureIgnoreCase) != -1;

                string[] list;

                if (directionIn)
                    list = SteamVR_Input_ActionFile_ActionTypes.listIn;
                else
                    list = SteamVR_Input_ActionFile_ActionTypes.listOut;


                int selectedType = Array.IndexOf(list, selectedAction.type);

                int newSelectedType = EditorGUILayout.Popup(selectedType, list);

                if (selectedType == -1 && newSelectedType == -1)
                    newSelectedType = 0;

                if (selectedType != newSelectedType && newSelectedType != -1)
                {
                    selectedAction.type = list[newSelectedType];
                }

                if (selectedAction.type == SteamVR_Input_ActionFile_ActionTypes.skeleton)
                {
                    string currentSkeletonPath = selectedAction.skeleton;
                    if (string.IsNullOrEmpty(currentSkeletonPath) == false)
                        currentSkeletonPath = currentSkeletonPath.Replace("/", "\\");

                    int selectedSkeletonType = Array.IndexOf(SteamVR_Input_ActionFile_ActionTypes.listSkeletons, currentSkeletonPath);
                    int newSelectedSkeletonType = EditorGUILayout.Popup(selectedSkeletonType, SteamVR_Input_ActionFile_ActionTypes.listSkeletons);

                    if (selectedSkeletonType == -1)
                        selectedSkeletonType = 0;

                    if (selectedSkeletonType != newSelectedSkeletonType && newSelectedSkeletonType != -1)
                    {
                        selectedAction.skeleton = SteamVR_Input_ActionFile_ActionTypes.listSkeletons[newSelectedSkeletonType].Replace("\\", "/");
                    }
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Required:");
            if (selectedActionIndex != -1)
            {
                int oldRequirement = (int)selectedAction.requirementEnum;
                int newRequirement = GUILayout.SelectionGrid(oldRequirement, SteamVR_Input_ActionFile_Action.requirementValues, 1, EditorStyles.radioButton);

                if (oldRequirement != newRequirement)
                {
                    selectedAction.requirementEnum = (SteamVR_Input_ActionFile_Action_Requirements)newRequirement;
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Localization:");
            localizationList.DoLayoutList();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Localized String:");
            if (selectedLocalizationIndex != -1)
            {
                Dictionary<string, string> localizationItems = SteamVR_Input.actionFile.localizationHelperList[selectedLocalizationIndex].items;
                string oldValue = "";

                if (localizationItems.ContainsKey(selectedAction.name))
                    oldValue = localizationItems[selectedAction.name];

                string newValue = EditorGUILayout.TextField(oldValue);

                if (string.IsNullOrEmpty(newValue))
                {
                    localizationItems.Remove(selectedAction.name);
                }
                else if (oldValue != newValue)
                {
                    if (localizationItems.ContainsKey(selectedAction.name) == false)
                        localizationItems.Add(selectedAction.name, newValue);
                    else
                        localizationItems[selectedAction.name] = newValue;
                }
            }


            EditorGUILayout.EndFadeGroup();
        }

        private void DrawSets()
        {
            EditorGUILayout.LabelField("Action Sets");
            EditorGUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            for (int actionSetIndex = 0; actionSetIndex < SteamVR_Input.actionFile.action_sets.Count; actionSetIndex++)
            {
                if (selectedActionSet == actionSetIndex)
                {
                    EditorGUILayout.BeginVertical();
                    string newName = GUILayout.TextField(SteamVR_Input.actionFile.action_sets[actionSetIndex].shortName);
                    if (newName != SteamVR_Input.actionFile.action_sets[actionSetIndex].shortName)
                    {
                        SteamVR_Input.actionFile.action_sets[actionSetIndex].SetNewShortName(newName);
                    }

                    EditorGUILayout.BeginHorizontal();

                    int selectedUsage = -1;
                    for (int valueIndex = 0; valueIndex < SteamVR_Input_ActionFile_ActionSet_Usages.listValues.Length; valueIndex++)
                    {
                        if (SteamVR_Input_ActionFile_ActionSet_Usages.listValues[valueIndex] == SteamVR_Input.actionFile.action_sets[actionSetIndex].usage)
                        {
                            selectedUsage = valueIndex;
                            break;
                        }
                    }

                    int wasUsage = selectedUsage;
                    if (selectedUsage == -1)
                        selectedUsage = 1;

                    selectedUsage = EditorGUILayout.Popup(selectedUsage, SteamVR_Input_ActionFile_ActionSet_Usages.listDescriptions);

                    if (wasUsage != selectedUsage)
                    {
                        SteamVR_Input.actionFile.action_sets[actionSetIndex].usage = SteamVR_Input_ActionFile_ActionSet_Usages.listValues[selectedUsage];
                    }

                    EditorGUILayout.Space();


                    bool removeSet = GUILayout.Button("-");
                    if (removeSet)
                    {
                        bool confirm = EditorUtility.DisplayDialog("Confirmation", "Are you sure you want to delete this action set and all of its actions?.", "Delete", "Cancel");
                        if (confirm)
                        {
                            //todo: this doesn't work
                            SteamVR_Input.actionFile.action_sets.RemoveAt(selectedActionSet);
                            selectedActionSet = -1;
                            selectedAction = null;

                            InitializeLists();
                            break;
                        }
                    }

                    EditorGUILayout.Space();

                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    bool pressedSet = GUILayout.Button(SteamVR_Input.actionFile.action_sets[actionSetIndex].shortName);

                    if (pressedSet)
                    {
                        selectedActionSet = actionSetIndex;

                        selectedActionIndex = -1;
                        selectedAction = null;

                        InitializeLists();
                    }
                }

                if (actionSetIndex < SteamVR_Input.actionFile.action_sets.Count - 1)
                    GUILayout.FlexibleSpace();
            }

            EditorGUILayout.Space();

            bool addSet = GUILayout.Button("+");

            if (addSet)
            {
                SteamVR_Input_ActionFile_ActionSet newActionSet = new SteamVR_Input_ActionFile_ActionSet();
                newActionSet.name = newActionSet.CreateNewName();

                SteamVR_Input.actionFile.action_sets.Add(newActionSet);

                selectedActionSet = SteamVR_Input.actionFile.action_sets.Count - 1;

                selectedActionIndex = -1;
                selectedAction = null;

                InitializeLists();
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
        }

        private static MemberInfo GetMemberInfo<TModel, TItem>(TModel model, Expression<Func<TModel, TItem>> expr)
        {
            return ((MemberExpression)expr.Body).Member;
        }

        private void SaveFile()
        {
            FileInfo existingActionsFile = new FileInfo(SteamVR_Input.actionsFilePath);
            if (existingActionsFile.Exists)
            {
                existingActionsFile.IsReadOnly = false;
            }

            SteamVR_Input.actionFile.SaveHelperLists();

            //SanitizeActionFile(); //todo: shouldn't we be doing this?

            string json = JsonConvert.SerializeObject(SteamVR_Input.actionFile, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            File.WriteAllText(SteamVR_Input.actionsFilePath, json);

            Debug.Log("[SteamVR Input] Saved actions manifest successfully.");

            SteamVR_Input_Generator.BeginGeneration();
        }

        private void SanitizeActionFile()
        {
            foreach (var action in SteamVR_Input.actionFile.actions)
            {
                if (action.type != SteamVR_Input_ActionFile_ActionTypes.skeleton)
                {
                    if (string.IsNullOrEmpty(action.skeleton) == false)
                    {
                        action.skeleton = null; //todo: shouldn't have skeleton data for non skeleton types I think
                    }
                }
            }
        }
    }

    public class SteamVR_Input_SettingsEditor : EditorWindow
    {
        public static void ShowWindow()
        {
            GetWindow<SteamVR_Input_SettingsEditor>(true, "SteamVR Input Settings", true);
        }

        private void OnGUI()
        {
            SteamVR_Input.InitializeFile();

            EditorGUILayout.LabelField(string.Format("File path: {0}", SteamVR_Input.actionsFilePath));


            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            DrawOverwriteOption();

            DrawDeleteUnusedActions();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            bool delete = GUILayout.Button("Delete input classes");
            GUILayout.FlexibleSpace();
            bool deleteActions = GUILayout.Button("Delete input actions");
            GUILayout.FlexibleSpace();
            bool showSettings = GUILayout.Button("SteamVR Settings");
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (delete)
            {
                bool confirm = EditorUtility.DisplayDialog("Confirmation", "Are you sure you want to delete the input code files? This may make your project unable to compile.", "Delete", "Cancel");
                if (confirm)
                    SteamVR_Input_Generator.DeleteActionClassFiles();
            }

            if (deleteActions)
            {
                bool confirm = EditorUtility.DisplayDialog("Confirmation", "Are you sure you want to delete the input action files? This will remove all references in your project to these actions.", "Delete", "Cancel");
                if (confirm)
                    SteamVR_Input_Generator.DeleteActionObjects("*.asset");
            }

            if (showSettings)
            {
                Selection.activeObject = SteamVR_Settings.instance;
            }
        }

        private void DrawOverwriteOption()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Overwrite actions and bindings json files during build");
            bool overwrite = EditorPrefs.GetBool(SteamVR_Input_Generator.steamVRInputOverwriteBuildKey);
            bool newOverwrite = EditorGUILayout.Toggle(overwrite);

            if (overwrite != newOverwrite)
            {
                EditorPrefs.SetBool(SteamVR_Input_Generator.steamVRInputOverwriteBuildKey, newOverwrite);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawDeleteUnusedActions()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Delete actions that are no longer in the action list during generation");
            bool deleteUnused = EditorPrefs.GetBool(SteamVR_Input_Generator.steamVRInputDeleteUnusedKey);
            bool newDeleteUnused = EditorGUILayout.Toggle(deleteUnused);

            if (deleteUnused != newDeleteUnused)
            {
                EditorPrefs.SetBool(SteamVR_Input_Generator.steamVRInputDeleteUnusedKey, newDeleteUnused);
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}