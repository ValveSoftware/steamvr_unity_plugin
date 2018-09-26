//======= Copyright (c) Valve Corporation, All rights reserved. ===============

using UnityEngine;
using System.Collections;
using System;
using Valve.VR;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Valve.VR
{
    /// <summary>
    /// Boolean actions are either true or false. There is an onStateUp and onStateDown event for the rising and falling edge.
    /// </summary>
    public class SteamVR_Action_Boolean : SteamVR_Action_In
    {
        [NonSerialized]
        protected Dictionary<SteamVR_Input_Sources, Action<SteamVR_Action_Boolean>> onStateDown = new Dictionary<SteamVR_Input_Sources, Action<SteamVR_Action_Boolean>>(new SteamVR_Input_Sources_Comparer());

        [NonSerialized]
        protected Dictionary<SteamVR_Input_Sources, Action<SteamVR_Action_Boolean>> onStateUp = new Dictionary<SteamVR_Input_Sources, Action<SteamVR_Action_Boolean>>(new SteamVR_Input_Sources_Comparer());

        [NonSerialized]
        protected Dictionary<SteamVR_Input_Sources, InputDigitalActionData_t> actionData = new Dictionary<SteamVR_Input_Sources, InputDigitalActionData_t>(new SteamVR_Input_Sources_Comparer());

        [NonSerialized]
        protected Dictionary<SteamVR_Input_Sources, InputDigitalActionData_t> lastActionData = new Dictionary<SteamVR_Input_Sources, InputDigitalActionData_t>(new SteamVR_Input_Sources_Comparer());

        [NonSerialized]
        protected InputDigitalActionData_t tempActionData = new InputDigitalActionData_t();

        [NonSerialized]
        protected uint actionData_size = 0;

        public override void Initialize()
        {
            base.Initialize();
            actionData_size = (uint)Marshal.SizeOf(tempActionData);
        }

        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        protected override void InitializeDictionaries(SteamVR_Input_Sources source)
        {
            base.InitializeDictionaries(source);

            onStateDown.Add(source, null);
            onStateUp.Add(source, null);
            actionData.Add(source, new InputDigitalActionData_t());
            lastActionData.Add(source, new InputDigitalActionData_t());
        }

        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public override void UpdateValue(SteamVR_Input_Sources inputSource)
        {
            lastActionData[inputSource] = actionData[inputSource];
            lastActive[inputSource] = active[inputSource];

            EVRInputError err = OpenVR.Input.GetDigitalActionData(handle, ref tempActionData, actionData_size, SteamVR_Input_Source.GetHandle(inputSource));
            if (err != EVRInputError.None)
                Debug.LogError("GetDigitalActionData error (" + fullPath + "): " + err.ToString() + " handle: " + handle.ToString());

            actionData[inputSource] = tempActionData;
            changed[inputSource] = tempActionData.bChanged;
            active[inputSource] = tempActionData.bActive;
            activeOrigin[inputSource] = tempActionData.activeOrigin;
            updateTime[inputSource] = tempActionData.fUpdateTime;

            if (changed[inputSource])
                lastChanged[inputSource] = Time.time;


            if (onStateDown[inputSource] != null && GetStateDown(inputSource))
                onStateDown[inputSource].Invoke(this);

            if (onStateUp[inputSource] != null && GetStateUp(inputSource))
                onStateUp[inputSource].Invoke(this);

            if (onChange[inputSource] != null && GetChanged(inputSource))
                onChange[inputSource].Invoke(this);

            if (onUpdate[inputSource] != null)
                onUpdate[inputSource].Invoke(this);

            if (onActiveChange[inputSource] != null && lastActive[inputSource] != active[inputSource])
                onActiveChange[inputSource].Invoke(this, active[inputSource]);
        }

        /// <summary>Returns true if the value of the action has been set to true (from false) in the most recent update.</summary>
        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public bool GetStateDown(SteamVR_Input_Sources inputSource)
        {
            return actionData[inputSource].bState && actionData[inputSource].bChanged;
        }

        /// <summary>Returns true if the value of the action has been set to false (from true) in the most recent update.</summary>
        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public bool GetStateUp(SteamVR_Input_Sources inputSource)
        {
            return actionData[inputSource].bState == false && actionData[inputSource].bChanged;
        }

        /// <summary>Returns true if the value of the action is currently true</summary>
        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public bool GetState(SteamVR_Input_Sources inputSource)
        {
            return actionData[inputSource].bState;
        }

        /// <summary>Returns true if the value of the action has been set to true (from false) in the previous update.</summary>
        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public bool GetLastStateDown(SteamVR_Input_Sources inputSource)
        {
            return lastActionData[inputSource].bState && lastActionData[inputSource].bChanged;
        }

        /// <summary>Returns true if the value of the action has been set to false (from true) in the previous update.</summary>
        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public bool GetLastStateUp(SteamVR_Input_Sources inputSource)
        {
            return lastActionData[inputSource].bState == false && lastActionData[inputSource].bChanged;
        }

        /// <summary>Returns true if the value of the action was true in the previous update.</summary>
        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public bool GetLastState(SteamVR_Input_Sources inputSource)
        {
            return lastActionData[inputSource].bState;
        }
    }
}