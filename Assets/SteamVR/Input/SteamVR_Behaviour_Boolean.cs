using System;
using System.Collections.Generic;
using System.Linq;
//======= Copyright (c) Valve Corporation, All rights reserved. ===============

using System.Text;

using UnityEngine;
using UnityEngine.Events;

namespace Valve.VR
{
    /// <summary>
    /// This component simplifies using boolean actions. Provides editor accessible events: onPress, onPressDown, onPressUp, onChange, and onUpdate.
    /// </summary>
    public class SteamVR_Behaviour_Boolean : MonoBehaviour
    {
        public SteamVR_Action_Boolean booleanAction;

        [Tooltip("The device this action should apply to. Any if the action is not device specific.")]
        public SteamVR_Input_Sources inputSource;

        /// <summary>This event fires whenever a change happens in the action</summary>
        public SteamVR_Behaviour_BooleanEvent onChange;

        /// <summary>This event fires whenever the action is updated</summary>
        public SteamVR_Behaviour_BooleanEvent onUpdate;

        /// <summary>This event will fire whenever the boolean action is true and gets updated</summary>
        public SteamVR_Behaviour_BooleanEvent onPress;

        /// <summary>This event will fire whenever the boolean action has changed from false to true in the last update</summary>
        public SteamVR_Behaviour_BooleanEvent onPressDown;

        /// <summary>This event will fire whenever the boolean action has changed from true to false in the last update</summary>
        public SteamVR_Behaviour_BooleanEvent onPressUp;

        /// <summary>Returns true if this action is currently bound and its action set is active</summary>
        public bool isActive { get { return booleanAction.GetActive(inputSource); } }

        /// <summary>Returns the action set that this action is in.</summary>
        public SteamVR_ActionSet actionSet { get { if (booleanAction != null) return booleanAction.actionSet; else return null; } }

        protected virtual void OnEnable()
        {
            booleanAction.AddOnUpdateListener(ActionUpdated, inputSource);
        }

        protected virtual void OnDisable()
        {
            booleanAction.RemoveOnUpdateListener(ActionUpdated, inputSource);
        }


        protected virtual void ActionUpdated(SteamVR_Action_In action)
        {
            SteamVR_Action_Boolean booleanAction = (SteamVR_Action_Boolean)action;

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

            if (onUpdate != null)
            {
                onUpdate.Invoke(booleanAction);
            }
        }
    }

    [Serializable]
    public class SteamVR_Behaviour_BooleanEvent : UnityEvent<SteamVR_Action_Boolean> { }
}