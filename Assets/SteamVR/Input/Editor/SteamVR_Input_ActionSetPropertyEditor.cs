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


[CustomPropertyDrawer(typeof(SteamVR_Input_ActionSet))]
public class SteamVR_Input_ActionSetPropertyEditor : PropertyDrawer
{
    protected SteamVR_Input_ActionSet[] actionSets;
    protected string[] enumItems;
    protected int selected = -1;

    protected void Awake()
    {
        actionSets = SteamVR_Input.GetActionSets();
        if (actionSets != null && actionSets.Length > 0)
        {
            List<string> enumList = actionSets.Select(actionSet => actionSet.fullPath).ToList();

            //replace forward slashes with backslack instead
            for (int index = 0; index < enumList.Count; index++)
                enumList[index] = enumList[index].Replace('/', '\\');

            enumList.Add("Add...");
            enumItems = enumList.ToArray();
        }
        else
        {
            enumItems = new string[] { "Add..." };
        }

        /*
        //keep sub menus:
        for (int index = 0; index < enumItems.Length; index++)
            if (enumItems[index][0] == '/')
                enumItems[index] = enumItems[index].Substring(1);
        */

    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return base.GetPropertyHeight(property, label) * 2;
    }

    // Draw the property inside the given rect
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (enumItems == null || enumItems.Length == 0)
        {
            Awake();
        }

        // Using BeginProperty / EndProperty on the parent property means that
        // prefab override logic works on the entire property.
        EditorGUI.BeginProperty(position, label, property);

        // Don't make child fields be indented
        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        if (property.objectReferenceValue != null)
        {
            SteamVR_Input_ActionSet action = property.objectReferenceValue as SteamVR_Input_ActionSet;
            // Calculate rects
            //var nameRect = new Rect(position.x + 90, position.y, position.width - 90, position.height);

            if (string.IsNullOrEmpty(action.fullPath) == false)
            {
                for (int enumIndex = 0; enumIndex < enumItems.Length - 1; enumIndex++)
                {
                    if (actionSets[enumIndex].fullPath == action.fullPath)
                    {
                        selected = enumIndex;
                        break;
                    }
                }
            }
        }

        // Draw fields - passs GUIContent.none to each so they are drawn without labels
        //EditorGUI.PropertyField(nameRect, nameProperty, GUIContent.none);
        EditorGUI.PropertyField(position, property, label, true);

        position.y += GetPropertyHeight(property, label) / 2;
        position.height /= 2;

        EditorGUI.indentLevel = indent + 2;

        int wasSelected = selected;
        selected = EditorGUI.Popup(position, selected, enumItems);
        if (selected != wasSelected)
        {
            if (selected == enumItems.Length - 1)
            {
                selected = wasSelected;

                SteamVR_Input_EditorWindow.ShowWindow();
            }
            else
            {
                property.objectReferenceValue = actionSets[selected];
            }
        }

        // Set indent back to what it was
        EditorGUI.indentLevel = indent;
        
        EditorGUI.EndProperty();
    }
}