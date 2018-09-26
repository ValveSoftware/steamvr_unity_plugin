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
    /// In actions are all input type actions. Boolean, Single, Vector2, Vector3, Skeleton, and Pose. 
    /// This class fires onChange and onUpdate events.
    /// </summary>
    public abstract class SteamVR_Action_In : SteamVR_Action
    {
        [NonSerialized]
        protected Dictionary<SteamVR_Input_Sources, float> updateTime = new Dictionary<SteamVR_Input_Sources, float>(new SteamVR_Input_Sources_Comparer());

        [NonSerialized]
        protected Dictionary<SteamVR_Input_Sources, ulong> activeOrigin = new Dictionary<SteamVR_Input_Sources, ulong>(new SteamVR_Input_Sources_Comparer());

        [NonSerialized]
        protected Dictionary<SteamVR_Input_Sources, bool> active = new Dictionary<SteamVR_Input_Sources, bool>(new SteamVR_Input_Sources_Comparer());

        [NonSerialized]
        protected Dictionary<SteamVR_Input_Sources, bool> changed = new Dictionary<SteamVR_Input_Sources, bool>(new SteamVR_Input_Sources_Comparer());

        [NonSerialized]
        protected Dictionary<SteamVR_Input_Sources, Action<SteamVR_Action_In, bool>> onActiveChange = new Dictionary<SteamVR_Input_Sources, Action<SteamVR_Action_In, bool>>(new SteamVR_Input_Sources_Comparer());

        [NonSerialized]
        protected Dictionary<SteamVR_Input_Sources, Action<SteamVR_Action_In>> onChange = new Dictionary<SteamVR_Input_Sources, Action<SteamVR_Action_In>>(new SteamVR_Input_Sources_Comparer());

        [NonSerialized]
        protected Dictionary<SteamVR_Input_Sources, Action<SteamVR_Action_In>> onUpdate = new Dictionary<SteamVR_Input_Sources, Action<SteamVR_Action_In>>(new SteamVR_Input_Sources_Comparer());

        [NonSerialized]
        protected Dictionary<SteamVR_Input_Sources, InputOriginInfo_t> lastInputOriginInfo = new Dictionary<SteamVR_Input_Sources, InputOriginInfo_t>(new SteamVR_Input_Sources_Comparer());

        [NonSerialized]
        protected Dictionary<SteamVR_Input_Sources, float> lastOriginGetFrame = new Dictionary<SteamVR_Input_Sources, float>(new SteamVR_Input_Sources_Comparer());

        [NonSerialized]
        protected Dictionary<SteamVR_Input_Sources, bool> lastActive = new Dictionary<SteamVR_Input_Sources, bool>(new SteamVR_Input_Sources_Comparer());

        [NonSerialized]
        protected static uint inputOriginInfo_size = 0;

        public abstract void UpdateValue(SteamVR_Input_Sources inputSource);

        public override void Initialize()
        {
            base.Initialize();

            if (inputOriginInfo_size == 0)
            {
                InputOriginInfo_t inputOriginInfo = new InputOriginInfo_t();
                inputOriginInfo_size = (uint)Marshal.SizeOf(inputOriginInfo);
            }
        }

        protected override void InitializeDictionaries(SteamVR_Input_Sources source)
        {
            base.InitializeDictionaries(source);

            updateTime.Add(source, -1);
            activeOrigin.Add(source, 0);
            active.Add(source, false);
            changed.Add(source, false);
            onChange.Add(source, null);
            onActiveChange.Add(source, null);
            onUpdate.Add(source, null);
            lastInputOriginInfo.Add(source, new InputOriginInfo_t());
            lastOriginGetFrame.Add(source, -1);
            lastActive.Add(source, false);
        }

        /// <summary>
        /// Returns the component name for the part of the controller that is bound to this action.
        /// </summary>
        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public virtual string GetDeviceComponentName(SteamVR_Input_Sources inputSource)
        {
            if (GetActive(inputSource))
            {
                UpdateOriginTrackedDeviceInfo(inputSource);

                return lastInputOriginInfo[inputSource].rchRenderModelComponentName;
            }

            return null;
        }

        /// <summary>
        /// Gets the full device path for the controller this device is bound to.
        /// </summary>
        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public virtual ulong GetDevicePath(SteamVR_Input_Sources inputSource)
        {
            if (GetActive(inputSource))
            {
                UpdateOriginTrackedDeviceInfo(inputSource);

                return lastInputOriginInfo[inputSource].devicePath;
            }

            return 0;
        }

        /// <summary>
        /// Gets the device index for the controller this action is bound to. This can be used for render models or the pose tracking system.
        /// </summary>
        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public virtual uint GetDeviceIndex(SteamVR_Input_Sources inputSource)
        {
            if (GetActive(inputSource))
            {
                UpdateOriginTrackedDeviceInfo(inputSource);

                return lastInputOriginInfo[inputSource].trackedDeviceIndex;
            }

            return 0;
        }

        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public virtual bool GetChanged(SteamVR_Input_Sources inputSource)
        {
            return changed[inputSource];
        }

        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public virtual bool GetActive(SteamVR_Input_Sources inputSource)
        {
            return active[inputSource];
        }

        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        protected void UpdateOriginTrackedDeviceInfo(SteamVR_Input_Sources inputSource)
        {
            if (lastOriginGetFrame[inputSource] != Time.frameCount) //only get once per frame
            {
                InputOriginInfo_t inputOriginInfo = new InputOriginInfo_t();
                EVRInputError err = OpenVR.Input.GetOriginTrackedDeviceInfo(activeOrigin[inputSource], ref inputOriginInfo, inputOriginInfo_size);

                if (err != EVRInputError.None)
                    Debug.LogError("GetOriginTrackedDeviceInfo error (" + fullPath + "): " + err.ToString() + " handle: " + handle.ToString() + " activeOrigin: " + activeOrigin[inputSource].ToString() + " active: " + active[inputSource]);

                lastInputOriginInfo[inputSource] = inputOriginInfo;
                lastOriginGetFrame[inputSource] = Time.frameCount;
            }
        }

        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public void AddOnActiveChangeListener(Action<SteamVR_Action_In, bool> action, SteamVR_Input_Sources inputSource)
        {
            onActiveChange[inputSource] += action;
        }

        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public void RemoveOnActiveChangeListener(Action<SteamVR_Action_In, bool> action, SteamVR_Input_Sources inputSource)
        {
            onActiveChange[inputSource] -= action;
        }

        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public void AddOnChangeListener(Action<SteamVR_Action_In> action, SteamVR_Input_Sources inputSource)
        {
            onChange[inputSource] += action;
        }

        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public void RemoveOnChangeListener(Action<SteamVR_Action_In> action, SteamVR_Input_Sources inputSource)
        {
            onChange[inputSource] -= action;
        }

        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public void AddOnUpdateListener(Action<SteamVR_Action_In> action, SteamVR_Input_Sources inputSource)
        {
            onUpdate[inputSource] += action;
        }

        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public void RemoveOnUpdateListener(Action<SteamVR_Action_In> action, SteamVR_Input_Sources inputSource)
        {
            onUpdate[inputSource] -= action;
        }
    }
}