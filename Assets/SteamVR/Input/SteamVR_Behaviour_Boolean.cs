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
    /// This component simplifies using boolean actions. 
    /// <para>Provides editor accessible events: OnPress, OnPressDown, OnPressUp, OnChange, and OnUpdate.</para>
    /// <para>Provides script accessible events: OnPressEvent, OnPressDownEvent, OnPressUpEvent, OnChangeEvent, and OnUpdateEvent.</para>
    /// </summary>
    public class SteamVR_Behaviour_Boolean : MonoBehaviour
    {
        [Tooltip("The SteamVR boolean action that this component should use")]
        public SteamVR_Action_Boolean booleanAction;

        [Tooltip("The device this action should apply to. Any if the action is not device specific.")]
        public SteamVR_Input_Sources inputSource;

        /// <summary>This UnityEvent fires whenever a change happens in the action</summary>
        public SteamVR_Behaviour_BooleanEvent onChange;

        /// <summary>This C# event fires whenever a change happens in the action</summary>
        public event SteamVR_Action_Boolean.ChangeHandler onChangeEvent { add { booleanAction[inputSource].onChange += value; } remove { booleanAction[inputSource].onChange -= value; } }

        /// <summary>This UnityEvent fires whenever the action is updated</summary>
        public SteamVR_Behaviour_BooleanEvent onUpdate;

        /// <summary>This C# event fires whenever the action is updated</summary>
        public event SteamVR_Action_Boolean.UpdateHandler onUpdateEvent { add { booleanAction[inputSource].onUpdate += value; } remove { booleanAction[inputSource].onUpdate -= value; } }

        /// <summary>This UnityEvent will fire whenever the boolean action is true and gets updated</summary>
        public SteamVR_Behaviour_BooleanEvent onPress;

        /// <summary>This C# event will fire whenever the boolean action is true and gets updated</summary>
        public event SteamVR_Action_Boolean.StateHandler onPressEvent { add { booleanAction[inputSource].onState += value; } remove { booleanAction[inputSource].onState -= value; } }

        /// <summary>This UnityEvent will fire whenever the boolean action has changed from false to true in the last update</summary>
        public SteamVR_Behaviour_BooleanEvent onPressDown;

        /// <summary>This C# event will fire whenever the boolean action has changed from false to true in the last update</summary>
        public event SteamVR_Action_Boolean.StateDownHandler onPressDownEvent { add { booleanAction[inputSource].onStateDown += value; } remove { booleanAction[inputSource].onStateDown -= value; } }

        /// <summary>This UnityEvent will fire whenever the boolean action has changed from true to false in the last update</summary>
        public SteamVR_Behaviour_BooleanEvent onPressUp;

        /// <summary>This C# event will fire whenever the boolean action has changed from true to false in the last update</summary>
        public event SteamVR_Action_Boolean.StateUpHandler onPressUpEvent { add { booleanAction[inputSource].onStateUp += value; } remove { booleanAction[inputSource].onStateUp -= value; } }

        /// <summary>Returns true if this action is currently bound and its action set is active</summary>
        public bool isActive { get { return booleanAction[inputSource].active; } }

        /// <summary>Returns the action set that this action is in.</summary>
        public SteamVR_ActionSet actionSet { get { if (booleanAction != null) return booleanAction.actionSet; else return null; } }
        


        protected virtual void OnEnable()
        {
            if (booleanAction == null)
            {
                Debug.LogError("[SteamVR] Boolean action not set.", this);
                return;
            }

            AddHandlers();
        }

        protected virtual void OnDisable()
        {
            RemoveHandlers();
        }

        protected void AddHandlers()
        {
            booleanAction[inputSource].onUpdate += SteamVR_Behaviour_Boolean_OnUpdate;
            booleanAction[inputSource].onChange += SteamVR_Behaviour_Boolean_OnChange;
            booleanAction[inputSource].onState += SteamVR_Behaviour_Boolean_OnState;
            booleanAction[inputSource].onStateDown += SteamVR_Behaviour_Boolean_OnStateDown;
            booleanAction[inputSource].onStateUp += SteamVR_Behaviour_Boolean_OnStateUp;
        }

        protected void RemoveHandlers()
        {

            if (booleanAction != null)
            {
                booleanAction[inputSource].onUpdate -= SteamVR_Behaviour_Boolean_OnUpdate;
                booleanAction[inputSource].onChange -= SteamVR_Behaviour_Boolean_OnChange;
                booleanAction[inputSource].onState -= SteamVR_Behaviour_Boolean_OnState;
                booleanAction[inputSource].onStateDown -= SteamVR_Behaviour_Boolean_OnStateDown;
                booleanAction[inputSource].onStateUp -= SteamVR_Behaviour_Boolean_OnStateUp;
            }
        }

        private void SteamVR_Behaviour_Boolean_OnStateUp(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
        {
            if (onPressUp != null)
            {
                onPressUp.Invoke(fromAction, fromSource, false);
            }
        }

        private void SteamVR_Behaviour_Boolean_OnStateDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
        {
            if (onPressDown != null)
            {
                onPressDown.Invoke(fromAction, fromSource, true);
            }
        }

        private void SteamVR_Behaviour_Boolean_OnState(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
        {
            if (onPress != null)
            {
                onPress.Invoke(fromAction, fromSource, true);
            }
        }

        private void SteamVR_Behaviour_Boolean_OnUpdate(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, bool newState)
        {
            if (onUpdate != null)
            {
                onUpdate.Invoke(fromAction, fromSource, newState);
            }
        }

        private void SteamVR_Behaviour_Boolean_OnChange(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, bool newState)
        {
            if (onChange != null)
            {
                onChange.Invoke(fromAction, fromSource, newState);
            }
        }

        /// <summary>
        /// Gets the localized name of the device that the action corresponds to. 
        /// </summary>
        /// <param name="localizedParts">
        /// <list type="bullet">
        /// <item><description>VRInputString_Hand - Which hand the origin is in. E.g. "Left Hand"</description></item>
        /// <item><description>VRInputString_ControllerType - What kind of controller the user has in that hand.E.g. "Vive Controller"</description></item>
        /// <item><description>VRInputString_InputSource - What part of that controller is the origin. E.g. "Trackpad"</description></item>
        /// <item><description>VRInputString_All - All of the above. E.g. "Left Hand Vive Controller Trackpad"</description></item>
        /// </list>
        /// </param>
        public string GetLocalizedName(params EVRInputStringBits[] localizedParts)
        {
            if (booleanAction != null)
                return booleanAction.GetLocalizedOriginPart(inputSource, localizedParts);
            return null;
        }
    }

    [Serializable]
    public class SteamVR_Behaviour_BooleanEvent : UnityEvent<SteamVR_Action_Boolean, SteamVR_Input_Sources, bool> { }
}