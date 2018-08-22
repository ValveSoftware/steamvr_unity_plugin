//======= Copyright (c) Valve Corporation, All rights reserved. ===============

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using UnityEngine.Events;

namespace Valve.VR
{
    public class SteamVR_Behaviour_Vector3 : MonoBehaviour
    {
        /// <summary>The vector3 action to get data from</summary>
        public SteamVR_Action_Vector3 vector3Action;

        /// <summary>The device this action applies to. Any if the action is not device specific.</summary>
        [Tooltip("The device this action should apply to. Any if the action is not device specific.")]
        public SteamVR_Input_Sources inputSource;

        /// <summary>Fires whenever the action's value has changed since the last update.</summary>
        [Tooltip("Fires whenever the action's value has changed since the last update.")]
        public SteamVR_Behaviour_Vector3Event onChange;

        /// <summary>Returns whether this action is bound and the action set is active</summary>
        public bool isActive { get { return vector3Action.GetActive(inputSource); } }

        private void OnEnable()
        {
            vector3Action.AddOnChangeListener(ActionChanged, inputSource);
        }

        private void OnDisable()
        {
            vector3Action.RemoveOnChangeListener(ActionChanged, inputSource);
        }

        private void ActionChanged(SteamVR_Action_In action)
        {
            if (onChange != null)
            {
                onChange.Invoke((SteamVR_Action_Vector3)action);
            }
        }
    }

    [Serializable]
    public class SteamVR_Behaviour_Vector3Event : UnityEvent<SteamVR_Action_Vector3> { }
}