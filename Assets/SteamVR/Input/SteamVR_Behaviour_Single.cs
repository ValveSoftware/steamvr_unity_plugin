//======= Copyright (c) Valve Corporation, All rights reserved. ===============

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using UnityEngine.Events;


namespace Valve.VR
{
    /// <summary>
    /// SteamVR_Behaviour_Single simplifies the use of single actions. It gives an event to subscribe to for when the action has changed.
    /// </summary>
    public class SteamVR_Behaviour_Single : MonoBehaviour
    {
        /// <summary>The single action to get data from.</summary>
        public SteamVR_Action_Single singleAction;

        /// <summary>The device this action applies to. Any if the action is not device specific.</summary>
        [Tooltip("The device this action should apply to. Any if the action is not device specific.")]
        public SteamVR_Input_Sources inputSource;

        /// <summary>Fires whenever the action's value has changed since the last update.</summary>
        [Tooltip("Fires whenever the action's value has changed since the last update.")]
        public SteamVR_Behaviour_SingleEvent onChange;

        /// <summary>Returns whether this action is bound and the action set is active</summary>
        public bool isActive { get { return singleAction.GetActive(inputSource); } }

        private void OnEnable()
        {
            singleAction.AddOnChangeListener(ActionChanged, inputSource);
        }

        private void OnDisable()
        {
            singleAction.RemoveOnChangeListener(ActionChanged, inputSource);
        }

        private void ActionChanged(SteamVR_Action_In action)
        {
            if (onChange != null)
            {
                onChange.Invoke((SteamVR_Action_Single)action);
            }
        }
    }

    [Serializable]
    public class SteamVR_Behaviour_SingleEvent : UnityEvent<SteamVR_Action_Single> { }
}