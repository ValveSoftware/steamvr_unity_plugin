using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using UnityEngine.Events;

public class SteamVR_Input_Single : MonoBehaviour
{
    public SteamVR_Input_Action_Single singleAction;

    public SteamVR_Input_Input_Sources inputSource;

    public SteamVR_Input_SingleEvent onChange;

    public bool isActive { get { return singleAction.GetActive(inputSource); } }

    private void OnEnable()
    {
        singleAction.AddOnChangeListener(ActionChanged, inputSource);
    }

    private void OnDisable()
    {
        singleAction.RemoveOnChangeListener(ActionChanged, inputSource);
    }

    private void ActionChanged(SteamVR_Input_Action_In action)
    {
        if (onChange != null)
        {
            onChange.Invoke((SteamVR_Input_Action_Single)action);
        }
    }
}

[Serializable]
public class SteamVR_Input_SingleEvent : UnityEvent<SteamVR_Input_Action_Single> { }