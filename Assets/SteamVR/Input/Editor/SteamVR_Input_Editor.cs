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

[CustomEditor(typeof(SteamVR_Input))]
public class SteamVR_Input_Editor : Editor
{
    private const string liveUpdateKey = "SteamVR_Input_LiveUpdates";

    private double lastOnSceneGUI;

    void OnSceneGUI()
    {
        Repaint();
        lastOnSceneGUI = EditorApplication.timeSinceStartup;
    }

    private GUIStyle labelStyle;

    private bool? liveUpdate;

    public override void OnInspectorGUI()
    {
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(EditorStyles.textField);
            labelStyle.normal.background = Texture2D.whiteTexture;
        }

        Color defaultColor = GUI.backgroundColor;

        if (liveUpdate == null)
        {
            if (EditorPrefs.HasKey(liveUpdateKey))
                liveUpdate = EditorPrefs.GetBool(liveUpdateKey);
            else
                liveUpdate = true;
        }

        bool newLiveUpdate = EditorGUILayout.Toggle("Live view of input action states", liveUpdate.Value);

        if (liveUpdate.Value != newLiveUpdate)
        {
            liveUpdate = newLiveUpdate;
            EditorPrefs.SetBool(liveUpdateKey, newLiveUpdate);
        }

        bool sceneViewOpen = ((EditorApplication.timeSinceStartup - lastOnSceneGUI) < 0.5f);

        if (Application.isPlaying == false)
        {
            bool showSettings = GUILayout.Button("SteamVR Settings");

            if (showSettings)
            {
                Selection.activeObject = SteamVR_Settings.instance;
            }
        }
        else if (liveUpdate.Value)
        {
            if (sceneViewOpen)
            {
                SteamVR_Input_Input_Sources[] sources = SteamVR_Input_Input_Source.GetUpdateSources();
                for (int sourceIndex = 0; sourceIndex < sources.Length; sourceIndex++)
                {
                    SteamVR_Input_Input_Sources source = sources[sourceIndex];
                    EditorGUILayout.LabelField(source.ToString());

                    for (int actionSetIndex = 0; actionSetIndex < SteamVR_Input.actionSets.Length; actionSetIndex++)
                    {
                        SteamVR_Input_ActionSet set = SteamVR_Input.actionSets[actionSetIndex];
                        string activeText = set.IsActive() ? "Active" : "Inactive";
                        float setLastChanged = set.GetTimeLastChanged();

                        if (setLastChanged != -1)
                        {
                            float timeSinceLastChanged = Time.time - setLastChanged;
                            if (timeSinceLastChanged < 1)
                            {
                                Color setColor = Color.Lerp(Color.green, defaultColor, timeSinceLastChanged);
                                GUI.backgroundColor = setColor;
                            }
                        }

                        EditorGUILayout.LabelField(set.GetShortName(), activeText, labelStyle);
                        GUI.backgroundColor = defaultColor;

                        EditorGUI.indentLevel++;

                        for (int actionIndex = 0; actionIndex < set.allActions.Length; actionIndex++)
                        {
                            SteamVR_Input_Action action = set.allActions[actionIndex];

                            float actionLastChanged = action.GetTimeLastChanged(source);

                            string actionText = "";

                            float timeSinceLastChanged = -1;

                            if (actionLastChanged != -1)
                            {
                                timeSinceLastChanged = Time.time - actionLastChanged;

                                if (timeSinceLastChanged < 1)
                                {
                                    Color setColor = Color.Lerp(Color.green, defaultColor, timeSinceLastChanged);
                                    GUI.backgroundColor = setColor;
                                }
                            }


                            if (action is SteamVR_Input_Action_Boolean)
                            {
                                SteamVR_Input_Action_Boolean actionBoolean = (SteamVR_Input_Action_Boolean)action;
                                actionText = actionBoolean.GetState(source).ToString();
                            }
                            else if (action is SteamVR_Input_Action_Single)
                            {
                                SteamVR_Input_Action_Single actionSingle = (SteamVR_Input_Action_Single)action;
                                actionText = actionSingle.GetAxis(source).ToString("0.0000");
                            }
                            else if (action is SteamVR_Input_Action_Vector2)
                            {
                                SteamVR_Input_Action_Vector2 actionVector2 = (SteamVR_Input_Action_Vector2)action;
                                actionText = string.Format("({0:0.0000}, {1:0.0000})", actionVector2.GetAxis(source).x, actionVector2.GetAxis(source).y);
                            }
                            else if (action is SteamVR_Input_Action_Vector3)
                            {
                                SteamVR_Input_Action_Vector3 actionVector3 = (SteamVR_Input_Action_Vector3)action;
                                Vector3 axis = actionVector3.GetAxis(source);
                                actionText = string.Format("({0:0.0000}, {1:0.0000}, {2:0.0000})", axis.x, axis.y, axis.z);
                            }
                            else if (action is SteamVR_Input_Action_Pose)
                            {
                                SteamVR_Input_Action_Pose actionPose = (SteamVR_Input_Action_Pose)action;
                                Vector3 position = actionPose.GetLocalPosition(source);
                                Quaternion rotation = actionPose.GetLocalRotation(source);
                                actionText = string.Format("({0:0.0000}, {1:0.0000}, {2:0.0000}) : ({3:0.0000}, {4:0.0000}, {5:0.0000}, {6:0.0000})",
                                    position.x, position.y, position.z,
                                    rotation.x, rotation.y, rotation.z, rotation.w);
                            }
                            else if (action is SteamVR_Input_Action_Skeleton)
                            {
                                SteamVR_Input_Action_Skeleton actionSkeleton = (SteamVR_Input_Action_Skeleton)action;
                                Vector3 position = actionSkeleton.GetLocalPosition(source);
                                Quaternion rotation = actionSkeleton.GetLocalRotation(source);
                                actionText = string.Format("({0:0.0000}, {1:0.0000}, {2:0.0000}) : ({3:0.0000}, {4:0.0000}, {5:0.0000}, {6:0.0000})",
                                    position.x, position.y, position.z,
                                    rotation.x, rotation.y, rotation.z, rotation.w);
                            }
                            else if (action is SteamVR_Input_Action_Vibration)
                            {
                                //SteamVR_Input_Action_Vibration actionVibration = (SteamVR_Input_Action_Vibration)action;

                                if (timeSinceLastChanged == -1)
                                    actionText = "never used";

                                actionText = string.Format("{0:0} seconds since last used", timeSinceLastChanged);
                            }

                            EditorGUILayout.LabelField(action.GetShortName(), actionText, labelStyle);
                            GUI.backgroundColor = defaultColor;
                        }

                        EditorGUILayout.Space();
                    }


                    EditorGUI.indentLevel--;
                }
            }
            else
            {
                EditorGUILayout.LabelField("Scene view must be visible for live view to function.");
            }
        }
        else
        {
            EditorGUILayout.LabelField("Live view of action states is disabled.");
        }
    }


    [MenuItem("Assets/Create/YourClass")]
    public static void CreateAsset()
    {
        CreateAsset<SteamVR_Settings>();
    }
    public static void CreateAsset<T>() where T : ScriptableObject
    {
        T asset = ScriptableObject.CreateInstance<T>();

        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (path == "")
        {
            path = "Assets";
        }
        else if (Path.GetExtension(path) != "")
        {
            path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
        }

        string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/New " + typeof(T).ToString() + ".asset");

        AssetDatabase.CreateAsset(asset, assetPathAndName);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
    }
}