using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using UnityEngine.Events;

public class SteamVR_Input_Vector3 : MonoBehaviour
{
    public SteamVR_Input_Action_Vector3 vector3Action;

    public SteamVR_Input_Input_Sources inputSource;

    public SteamVR_Input_Vector3Event onChange;

    public bool isActive { get { return vector3Action.GetActive(inputSource); } }

    private void OnEnable()
    {
        vector3Action.AddOnChangeListener(ActionChanged, inputSource);
    }

    private void OnDisable()
    {
        vector3Action.RemoveOnChangeListener(ActionChanged, inputSource);
    }

    private void ActionChanged(SteamVR_Input_Action_In action)
    {
        if (onChange != null)
        {
            onChange.Invoke((SteamVR_Input_Action_Vector3)action);
        }
    }
}

[Serializable]
public class SteamVR_Input_Vector3Event : UnityEvent<SteamVR_Input_Action_Vector3> { }