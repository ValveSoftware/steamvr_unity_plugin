//======= Copyright (c) Valve Corporation, All rights reserved. ===============

using UnityEngine;
using System.Collections;
using System;
using Valve.VR;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Valve.VR
{
    /// <summary>An analog action with a value generally from 0 to 1. Also provides a delta since the last update.</summary>
    public class SteamVR_Action_Single : SteamVR_Action_In
    {
        [NonSerialized]
        protected Dictionary<SteamVR_Input_Sources, InputAnalogActionData_t> actionData = new Dictionary<SteamVR_Input_Sources, InputAnalogActionData_t>(new SteamVR_Input_Sources_Comparer());

        [NonSerialized]
        protected Dictionary<SteamVR_Input_Sources, InputAnalogActionData_t> lastActionData = new Dictionary<SteamVR_Input_Sources, InputAnalogActionData_t>(new SteamVR_Input_Sources_Comparer());

        [NonSerialized]
        protected InputAnalogActionData_t tempActionData = new InputAnalogActionData_t();

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

            actionData.Add(source, new InputAnalogActionData_t());
            lastActionData.Add(source, new InputAnalogActionData_t());
        }

        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public override void UpdateValue(SteamVR_Input_Sources inputSource)
        {
            lastActionData[inputSource] = actionData[inputSource];
            lastActive[inputSource] = active[inputSource];

            EVRInputError err = OpenVR.Input.GetAnalogActionData(handle, ref tempActionData, actionData_size, SteamVR_Input_Source.GetHandle(inputSource));
            if (err != EVRInputError.None)
                Debug.LogError("GetAnalogActionData error (" + fullPath + "): " + err.ToString() + " handle: " + handle.ToString());

            active[inputSource] = tempActionData.bActive;
            activeOrigin[inputSource] = tempActionData.activeOrigin;
            updateTime[inputSource] = tempActionData.fUpdateTime;
            changed[inputSource] = false;
            actionData[inputSource] = tempActionData;

            if (Mathf.Abs(GetAxisDelta(inputSource)) > changeTolerance)
            {
                changed[inputSource] = true;
                lastChanged[inputSource] = Time.time;

                if (onChange[inputSource] != null)
                    onChange[inputSource].Invoke(this);
            }

            if (onUpdate[inputSource] != null)
            {
                onUpdate[inputSource].Invoke(this);
            }

            if (onActiveChange[inputSource] != null && lastActive[inputSource] != active[inputSource])
                onActiveChange[inputSource].Invoke(this, active[inputSource]);
        }

        /// <summary>The analog value</summary>
        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public float GetAxis(SteamVR_Input_Sources inputSource)
        {
            return actionData[inputSource].x;
        }

        /// <summary>The delta from the analog value</summary>
        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public float GetAxisDelta(SteamVR_Input_Sources inputSource)
        {
            return actionData[inputSource].deltaX;
        }

        /// <summary>The previous analog value</summary>
        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public float GetLastAxis(SteamVR_Input_Sources inputSource)
        {
            return lastActionData[inputSource].x;
        }

        /// <summary>The previous delta from the analog value</summary>
        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public float GetLastAxisDelta(SteamVR_Input_Sources inputSource)
        {
            return lastActionData[inputSource].deltaX;
        }
    }
}