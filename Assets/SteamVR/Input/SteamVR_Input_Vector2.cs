using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using UnityEngine.Events;

public class SteamVR_Input_Vector2 : MonoBehaviour
{
    public SteamVR_Input_Action_Vector2 vector2Action;

    public SteamVR_Input_Input_Sources inputSource;

    public SteamVR_Input_Vector2Event onChange;

    public bool isActive { get { return vector2Action.GetActive(inputSource); } }

    private void OnEnable()
    {
        vector2Action.AddOnChangeListener(ActionChanged, inputSource);
    }

    private void OnDisable()
    {
        vector2Action.RemoveOnChangeListener(ActionChanged, inputSource);
    }

    private void ActionChanged(SteamVR_Input_Action_In action)
    {
        if (onChange != null)
        {
            onChange.Invoke((SteamVR_Input_Action_Vector2)action);
        }
    }
}

[Serializable]
public class SteamVR_Input_Vector2Event : UnityEvent<SteamVR_Input_Action_Vector2> { }