using UnityEngine;
using System.Collections;
using System;
using System.Reflection;
using System.Text;
using System.Linq;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
public class DefaultInputAction : System.Attribute
{
    public string actionName;
    public string actionSetName;
    public string inputSourceFieldName;
    public SteamVR_Input_ActionDirections? direction;
    public bool overrideExistingOnGeneration;

    public DefaultInputAction(string defaultActionName, bool overrideExistingActionDuringGeneration = false)
    {
        actionName = defaultActionName;
        overrideExistingOnGeneration = overrideExistingActionDuringGeneration;
    }

    public DefaultInputAction(string defaultActionName, SteamVR_Input_ActionDirections defaultActionDirection, bool overrideExistingActionDuringGeneration = false)
    {
        actionName = defaultActionName;
        direction = defaultActionDirection;
        overrideExistingOnGeneration = overrideExistingActionDuringGeneration;
    }

    public DefaultInputAction(string defaultActionName, string defaultActionSetName, bool overrideExistingActionDuringGeneration = false)
    {
        actionName = defaultActionName;
        actionSetName = defaultActionSetName;
        overrideExistingOnGeneration = overrideExistingActionDuringGeneration;
    }

    public DefaultInputAction(string defaultActionName, string defaultActionSetName, SteamVR_Input_ActionDirections defaultActionDirection, bool overrideExistingActionDuringGeneration = false)
    {
        actionName = defaultActionName;
        actionSetName = defaultActionSetName;
        direction = defaultActionDirection;
        overrideExistingOnGeneration = overrideExistingActionDuringGeneration;
    }

    public DefaultInputAction(string defaultActionName, string defaultActionSetName, string inputSourceFieldName, bool overrideExistingActionDuringGeneration = false)
    {
        actionName = defaultActionName;
        actionSetName = defaultActionSetName;
        this.inputSourceFieldName = inputSourceFieldName;
        overrideExistingOnGeneration = overrideExistingActionDuringGeneration;
    }

    public DefaultInputAction(string defaultActionName, string defaultActionSetName, string inputSourceFieldName, SteamVR_Input_ActionDirections defaultActionDirection, bool overrideExistingActionDuringGeneration = false)
    {
        actionName = defaultActionName;
        actionSetName = defaultActionSetName;
        this.inputSourceFieldName = inputSourceFieldName;
        direction = defaultActionDirection;
        overrideExistingOnGeneration = overrideExistingActionDuringGeneration;
    }

    public void AssignDefault(FieldInfo field, object onObject)
    {
        SteamVR_Input_Action action = GetAction((MonoBehaviour)onObject);

        if (action != null)
        {
            var currentAction = field.GetValue(onObject);

            if (currentAction == null || overrideExistingOnGeneration)
                field.SetValue(onObject, action);
        }
    }

    public void AssignDefault(PropertyInfo property, object onObject)
    {
        SteamVR_Input_Action action = GetAction((MonoBehaviour)onObject);

        if (action != null)
        {
            var currentAction = property.GetValue(onObject, null);

            if (currentAction == null || overrideExistingOnGeneration)
                property.SetValue(onObject, action, null);
        }
    }

    private SteamVR_Input_Action GetAction(MonoBehaviour monobehaviour)
    {
        string inputSource = GetInputSource(monobehaviour, inputSourceFieldName);
        string regex = GetRegex(inputSource);

        var action = SteamVR_Input_References.instance.actionObjects.FirstOrDefault(matchAction => System.Text.RegularExpressions.Regex.IsMatch(matchAction.fullPath, regex, System.Text.RegularExpressions.RegexOptions.IgnoreCase));

        if (action == null)
            Debug.Log("[SteamVR Input] Could not find action matching path: " + regex.Replace("\\", "").Replace(".+", "*"));

        return action;
    }

    private string GetInputSource(MonoBehaviour monoBehaviour, string inputSourceFieldName)
    {
        if (inputSourceFieldName != null)
        {
            Type monoBehaviourType = monoBehaviour.GetType();
            FieldInfo inputSourceField = monoBehaviourType.GetField(inputSourceFieldName);

            if (inputSourceField != null)
            {
                SteamVR_Input_Input_Sources source = (SteamVR_Input_Input_Sources)inputSourceField.GetValue(monoBehaviour);
                return source.ToString();
            }
        }
        return null;
    }

    private string GetRegex(string inputSource)
    {
        string regex = "\\/actions\\/";

        if (actionSetName != null)
            regex += actionSetName;
        else
            regex += ".+";

        regex += "\\/";

        if (direction != null)
            regex += direction.ToString();
        else
            regex += ".+";

        regex += "\\/" + actionName;

        if (inputSource != null)
        {
            regex += inputSource;
        }

        return regex;
    }
}