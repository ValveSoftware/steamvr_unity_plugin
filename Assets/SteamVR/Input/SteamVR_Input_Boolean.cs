using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using UnityEngine.Events;

public class SteamVR_Input_Boolean : MonoBehaviour
{
    public SteamVR_Input_Action_Boolean booleanAction;

    public SteamVR_Input_Input_Sources inputSource;

    public SteamVR_Input_BooleanEvent onChange;

    public SteamVR_Input_BooleanEvent onPress;
    public SteamVR_Input_BooleanEvent onPressDown;
    public SteamVR_Input_BooleanEvent onPressUp;

    public bool isActive { get { return booleanAction.GetActive(inputSource); } }
    
    private void OnEnable()
    {
        booleanAction.AddOnChangeListener(ActionUpdated, inputSource);
        booleanAction.AddOnUpdateListener(ActionUpdated, inputSource);
    }

    private void OnDisable()
    {
        booleanAction.RemoveOnChangeListener(ActionUpdated, inputSource);
    }


    private void ActionUpdated(SteamVR_Input_Action_In action)
    {
        SteamVR_Input_Action_Boolean booleanAction = (SteamVR_Input_Action_Boolean)action;

        if (onChange != null && booleanAction.GetChanged(inputSource))
        {
            onChange.Invoke(booleanAction);
        }

        if (onPressDown != null && booleanAction.GetStateDown(inputSource))
        {
            onPressDown.Invoke(booleanAction);
        }

        if (onPress != null && booleanAction.GetState(inputSource))
        {
            onPress.Invoke(booleanAction);
        }

        if (onPressUp != null && booleanAction.GetStateUp(inputSource))
        {
            onPressUp.Invoke(booleanAction);
        }
    }
}

[Serializable]
public class SteamVR_Input_BooleanEvent : UnityEvent<SteamVR_Input_Action_Boolean> { }