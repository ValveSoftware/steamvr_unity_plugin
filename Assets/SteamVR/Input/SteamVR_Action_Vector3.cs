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
    /// Vector3 actions are relatively uncommon. If you're looking for an action to get a 3d position from you probably want a Pose action.
    /// </summary>
    public class SteamVR_Action_Vector3 : SteamVR_Action_In
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

        protected override void InitializeDictionaries(SteamVR_Input_Sources source)
        {
            base.InitializeDictionaries(source);

            actionData.Add(source, new InputAnalogActionData_t());
            lastActionData.Add(source, new InputAnalogActionData_t());
        }

        public override void UpdateValue(SteamVR_Input_Sources inputSource)
        {
            lastActionData[inputSource] = actionData[inputSource];
            lastActive[inputSource] = active[inputSource];

            EVRInputError err = OpenVR.Input.GetAnalogActionData(handle, ref tempActionData, actionData_size, SteamVR_Input_Source.GetHandle(inputSource));
            if (err != EVRInputError.None)
                Debug.LogError("Vector3 GetAnalogActionData error (" + fullPath + "): " + err.ToString() + " handle: " + handle.ToString());

            active[inputSource] = tempActionData.bActive;
            activeOrigin[inputSource] = tempActionData.activeOrigin;
            updateTime[inputSource] = tempActionData.fUpdateTime;
            changed[inputSource] = false;
            actionData[inputSource] = tempActionData;

            if (GetAxisDelta(inputSource).magnitude > changeTolerance)
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

        /// <summary>The three axis value from the latest update</summary>
        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public Vector3 GetAxis(SteamVR_Input_Sources inputSource)
        {
            return new Vector3(actionData[inputSource].x, actionData[inputSource].y, actionData[inputSource].z);
        }

        /// <summary>The three axis delta of the value from the latest update</summary>
        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public Vector3 GetAxisDelta(SteamVR_Input_Sources inputSource)
        {
            return new Vector3(actionData[inputSource].deltaX, actionData[inputSource].deltaY, actionData[inputSource].deltaZ);
        }

        /// <summary>The three axis value from the previous update</summary>
        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public Vector3 GetLastAxis(SteamVR_Input_Sources inputSource)
        {
            return new Vector3(lastActionData[inputSource].x, lastActionData[inputSource].y, lastActionData[inputSource].z);
        }

        /// <summary>The three axis delta of the value from the previous update</summary>
        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public Vector3 GetLastAxisDelta(SteamVR_Input_Sources inputSource)
        {
            return new Vector3(lastActionData[inputSource].deltaX, lastActionData[inputSource].deltaY, lastActionData[inputSource].deltaZ);
        }
    }
}