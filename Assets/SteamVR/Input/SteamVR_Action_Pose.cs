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
    /// Pose actions represent a position and orientation inside the tracked space. 
    /// SteamVR also keeps a log of past poses so you can retrieve old poses with GetPoseAtTimeOffset or GetVelocitiesAtTimeOffset.
    /// You can also pass in times in the future to these methods for SteamVR's best prediction of where the pose will be at that time.
    /// </summary>
    public class SteamVR_Action_Pose : SteamVR_Action_In
    {
        [NonSerialized]
        protected static ETrackingUniverseOrigin universeOrigin = ETrackingUniverseOrigin.TrackingUniverseRawAndUncalibrated;

        [NonSerialized]
        public float predictedSecondsFromNow = 0;

        [NonSerialized]
        protected Dictionary<SteamVR_Input_Sources, Action<SteamVR_Action_Pose>> onTrackingChanged = new Dictionary<SteamVR_Input_Sources, Action<SteamVR_Action_Pose>>(new SteamVR_Input_Sources_Comparer());

        [NonSerialized]
        protected Dictionary<SteamVR_Input_Sources, Action<SteamVR_Action_Pose>> onValidPoseChanged = new Dictionary<SteamVR_Input_Sources, Action<SteamVR_Action_Pose>>(new SteamVR_Input_Sources_Comparer());

        [NonSerialized]
        protected Dictionary<SteamVR_Input_Sources, Action<SteamVR_Action_Pose>> onDeviceConnectedChanged = new Dictionary<SteamVR_Input_Sources, Action<SteamVR_Action_Pose>>(new SteamVR_Input_Sources_Comparer());

        [NonSerialized]
        protected Dictionary<SteamVR_Input_Sources, InputPoseActionData_t> poseActionData = new Dictionary<SteamVR_Input_Sources, InputPoseActionData_t>(new SteamVR_Input_Sources_Comparer());

        [NonSerialized]
        protected Dictionary<SteamVR_Input_Sources, InputPoseActionData_t> lastPoseActionData = new Dictionary<SteamVR_Input_Sources, InputPoseActionData_t>(new SteamVR_Input_Sources_Comparer());

        [NonSerialized]
        protected Dictionary<SteamVR_Input_Sources, InputPoseActionData_t> lastRecordedPoseActionData = new Dictionary<SteamVR_Input_Sources, InputPoseActionData_t>(new SteamVR_Input_Sources_Comparer());

        [NonSerialized]
        protected Dictionary<SteamVR_Input_Sources, bool> lastRecordedActive = new Dictionary<SteamVR_Input_Sources, bool>(new SteamVR_Input_Sources_Comparer());

        [NonSerialized]
        protected InputPoseActionData_t tempPoseActionData = new InputPoseActionData_t();

        [NonSerialized]
        protected uint poseActionData_size = 0;

        public override void Initialize()
        {
            base.Initialize();
            poseActionData_size = (uint)Marshal.SizeOf(tempPoseActionData);
        }

        /// <param name="inputSource">The device you would like to initialize dictionaries for</param>
        protected override void InitializeDictionaries(SteamVR_Input_Sources source)
        {
            base.InitializeDictionaries(source);

            onTrackingChanged.Add(source, null);
            onValidPoseChanged.Add(source, null);
            onDeviceConnectedChanged.Add(source, null);
            poseActionData.Add(source, new InputPoseActionData_t());
            lastPoseActionData.Add(source, new InputPoseActionData_t());
            lastRecordedPoseActionData.Add(source, new InputPoseActionData_t());
            lastRecordedActive.Add(source, false);
        }

        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public override void UpdateValue(SteamVR_Input_Sources inputSource)
        {
            UpdateValue(inputSource, false);
        }

        protected void ResetLastStates(SteamVR_Input_Sources inputSource)
        {
            lastPoseActionData[inputSource] = lastRecordedPoseActionData[inputSource];
            lastActive[inputSource] = lastRecordedActive[inputSource];
        }

        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public virtual void UpdateValue(SteamVR_Input_Sources inputSource, bool skipStateAndEventUpdates)
        {
            changed[inputSource] = false;
            if (skipStateAndEventUpdates == false)
            {
                ResetLastStates(inputSource);
            }

            EVRInputError err = OpenVR.Input.GetPoseActionData(handle, universeOrigin, predictedSecondsFromNow, ref tempPoseActionData, poseActionData_size, SteamVR_Input_Source.GetHandle(inputSource));
            if (err != EVRInputError.None)
            {
                Debug.LogError("GetPoseActionData error (" + fullPath + "): " + err.ToString() + " handle: " + handle.ToString());
            }
            
            poseActionData[inputSource] = tempPoseActionData;
            active[inputSource] = tempPoseActionData.bActive;
            activeOrigin[inputSource] = tempPoseActionData.activeOrigin;
            updateTime[inputSource] = Time.time;

            if (Vector3.Distance(GetLocalPosition(inputSource), GetLastLocalPosition(inputSource)) > changeTolerance)
            {
                changed[inputSource] = true;
            }
            else if (Mathf.Abs(Quaternion.Angle(GetLocalRotation(inputSource), GetLastLocalRotation(inputSource))) > changeTolerance)
            {
                changed[inputSource] = true;
            }

            if (skipStateAndEventUpdates == false)
            {
                CheckAndSendEvents(inputSource);
            }

            if (changed[inputSource])
                lastChanged[inputSource] = Time.time;

            if (onUpdate[inputSource] != null)
                onUpdate[inputSource].Invoke(this);

            if (skipStateAndEventUpdates == false)
            {
                lastRecordedActive[inputSource] = active[inputSource];
                lastRecordedPoseActionData[inputSource] = poseActionData[inputSource];
            }
        }

        /// <summary>
        /// SteamVR keeps a log of past poses so you can retrieve old poses or estimated poses in the future by passing in a secondsFromNow value that is negative or positive.
        /// </summary>
        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public bool GetVelocitiesAtTimeOffset(SteamVR_Input_Sources inputSource, float secondsFromNow, out Vector3 velocity, out Vector3 angularVelocity)
        {
            EVRInputError err = OpenVR.Input.GetPoseActionData(handle, universeOrigin, secondsFromNow, ref tempPoseActionData, poseActionData_size, SteamVR_Input_Source.GetHandle(inputSource));
            if (err != EVRInputError.None)
            {
                Debug.LogError("GetPoseActionData error (" + fullPath + "): " + err.ToString() + " handle: " + handle.ToString()); //todo: this should be an error
                
                velocity = Vector3.zero;
                angularVelocity = Vector3.zero;
                return false;
            }

            velocity = new Vector3(tempPoseActionData.pose.vVelocity.v0, tempPoseActionData.pose.vVelocity.v1, -tempPoseActionData.pose.vVelocity.v2);
            angularVelocity = new Vector3(-tempPoseActionData.pose.vAngularVelocity.v0, -tempPoseActionData.pose.vAngularVelocity.v1, tempPoseActionData.pose.vAngularVelocity.v2);

            return true;
        }

        /// <summary>
        /// SteamVR keeps a log of past poses so you can retrieve old poses or estimated poses in the future by passing in a secondsFromNow value that is negative or positive.
        /// </summary>
        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public bool GetPoseAtTimeOffset(SteamVR_Input_Sources inputSource, float secondsFromNow, out Vector3 position, out Quaternion rotation, out Vector3 velocity, out Vector3 angularVelocity)
        {
            EVRInputError err = OpenVR.Input.GetPoseActionData(handle, universeOrigin, secondsFromNow, ref tempPoseActionData, poseActionData_size, SteamVR_Input_Source.GetHandle(inputSource));
            if (err != EVRInputError.None)
            {
                if (err == EVRInputError.InvalidHandle)
                {
                    //todo: ignoring this error for now since it throws while the dashboard is up
                }
                else
                {
                    Debug.LogError("GetPoseActionData error (" + fullPath + "): " + err.ToString() + " handle: " + handle.ToString()); //todo: this should be an error
                }

                velocity = Vector3.zero;
                angularVelocity = Vector3.zero;
                position = Vector3.zero;
                rotation = Quaternion.identity;
                return false;
            }

            velocity = new Vector3(tempPoseActionData.pose.vVelocity.v0, tempPoseActionData.pose.vVelocity.v1, -tempPoseActionData.pose.vVelocity.v2);
            angularVelocity = new Vector3(-tempPoseActionData.pose.vAngularVelocity.v0, -tempPoseActionData.pose.vAngularVelocity.v1, tempPoseActionData.pose.vAngularVelocity.v2);
            position = SteamVR_Utils.GetPosition(tempPoseActionData.pose.mDeviceToAbsoluteTracking);
            rotation = SteamVR_Utils.GetRotation(tempPoseActionData.pose.mDeviceToAbsoluteTracking);

            return true;
        }

        /// <summary>
        /// Update a transform's local position and local roation to match the pose.
        /// </summary>
        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        /// <param name="transformToUpdate">The transform of the object to be updated</param>
        public void UpdateTransform(SteamVR_Input_Sources inputSource, Transform transformToUpdate)
        {
            transformToUpdate.localPosition = GetLocalPosition(inputSource);
            transformToUpdate.localRotation = GetLocalRotation(inputSource);
        }

        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        protected void CheckAndSendEvents(SteamVR_Input_Sources inputSource)
        {
            if (poseActionData[inputSource].pose.eTrackingResult != lastPoseActionData[inputSource].pose.eTrackingResult && onTrackingChanged[inputSource] != null)
                onTrackingChanged[inputSource].Invoke(this);

            if (poseActionData[inputSource].pose.bPoseIsValid != lastPoseActionData[inputSource].pose.bPoseIsValid && onValidPoseChanged[inputSource] != null)
                onValidPoseChanged[inputSource].Invoke(this);

            if (poseActionData[inputSource].pose.bDeviceIsConnected != lastPoseActionData[inputSource].pose.bDeviceIsConnected && onDeviceConnectedChanged[inputSource] != null)
                onDeviceConnectedChanged[inputSource].Invoke(this);

            if (changed[inputSource])
            {
                if (onChange[inputSource] != null)
                    onChange[inputSource].Invoke(this);
            }

            if (onActiveChange[inputSource] != null)
            {
                if (lastActive[inputSource] != active[inputSource])
                    onActiveChange[inputSource].Invoke(this, active[inputSource]);
            }
        }
        
        /// <param name="newUniverseOrigin">The origin of the universe. Don't get this wrong.</param>
        public static void SetTrackingUniverseOrigin(ETrackingUniverseOrigin newUniverseOrigin)
        {
            universeOrigin = newUniverseOrigin;
        }

        /// <summary>The local position of the pose relative to the center of the tracked space.</summary>
        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public Vector3 GetLocalPosition(SteamVR_Input_Sources inputSource)
        {
            // Convert the transform from SteamVR's coordinate system to Unity's coordinate system.
            // ie: flip the X axis
            return new Vector3(poseActionData[inputSource].pose.mDeviceToAbsoluteTracking.m3,
                poseActionData[inputSource].pose.mDeviceToAbsoluteTracking.m7,
                -poseActionData[inputSource].pose.mDeviceToAbsoluteTracking.m11);
        }

        /// <summary>The local rotation of the pose relative to the center of the tracked space.</summary>
        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public Quaternion GetLocalRotation(SteamVR_Input_Sources inputSource)
        {
            return SteamVR_Utils.GetRotation(poseActionData[inputSource].pose.mDeviceToAbsoluteTracking);
        }

        /// <summary>The local velocity of the pose relative to the center of the tracked space.</summary>
        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public Vector3 GetVelocity(SteamVR_Input_Sources inputSource)
        {
            return new Vector3(poseActionData[inputSource].pose.vVelocity.v0, poseActionData[inputSource].pose.vVelocity.v1, -poseActionData[inputSource].pose.vVelocity.v2);
        }

        /// <summary>The local angular velocity of the pose relative to the center of the tracked space.</summary>
        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public Vector3 GetAngularVelocity(SteamVR_Input_Sources inputSource)
        {
            return new Vector3(-poseActionData[inputSource].pose.vAngularVelocity.v0, -poseActionData[inputSource].pose.vAngularVelocity.v1, poseActionData[inputSource].pose.vAngularVelocity.v2);
        }

        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public bool GetDeviceIsConnected(SteamVR_Input_Sources inputSource)
        {
            return poseActionData[inputSource].pose.bDeviceIsConnected;
        }

        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public bool GetPoseIsValid(SteamVR_Input_Sources inputSource)
        {
            return poseActionData[inputSource].pose.bPoseIsValid;
        }

        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public ETrackingResult GetTrackingResult(SteamVR_Input_Sources inputSource)
        {
            return poseActionData[inputSource].pose.eTrackingResult;
        }



        /// <summary>The last local position of the pose relative to the center of the tracked space.</summary>
        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public Vector3 GetLastLocalPosition(SteamVR_Input_Sources inputSource)
        {
            return new Vector3(lastPoseActionData[inputSource].pose.mDeviceToAbsoluteTracking.m3,
                lastPoseActionData[inputSource].pose.mDeviceToAbsoluteTracking.m7,
                -lastPoseActionData[inputSource].pose.mDeviceToAbsoluteTracking.m11);
        }

        /// <summary>The last local rotation of the pose relative to the center of the tracked space.</summary>
        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public Quaternion GetLastLocalRotation(SteamVR_Input_Sources inputSource)
        {
            return SteamVR_Utils.GetRotation(lastPoseActionData[inputSource].pose.mDeviceToAbsoluteTracking);
        }

        /// <summary>The last local velocity of the pose relative to the center of the tracked space.</summary>
        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public Vector3 GetLastVelocity(SteamVR_Input_Sources inputSource)
        {
            return new Vector3(lastPoseActionData[inputSource].pose.vVelocity.v0, lastPoseActionData[inputSource].pose.vVelocity.v1, -lastPoseActionData[inputSource].pose.vVelocity.v2);
        }

        /// <summary>The last local angular velocity of the pose relative to the center of the tracked space.</summary>
        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public Vector3 GetLastAngularVelocity(SteamVR_Input_Sources inputSource)
        {
            return new Vector3(-lastPoseActionData[inputSource].pose.vAngularVelocity.v0, -lastPoseActionData[inputSource].pose.vAngularVelocity.v1, lastPoseActionData[inputSource].pose.vAngularVelocity.v2);
        }

        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public bool GetLastDeviceIsConnected(SteamVR_Input_Sources inputSource)
        {
            return lastPoseActionData[inputSource].pose.bDeviceIsConnected;
        }

        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public bool GetLastPoseIsValid(SteamVR_Input_Sources inputSource)
        {
            return lastPoseActionData[inputSource].pose.bPoseIsValid;
        }

        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public ETrackingResult GetLastTrackingResult(SteamVR_Input_Sources inputSource)
        {
            return lastPoseActionData[inputSource].pose.eTrackingResult;
        }


        /// <summary>Fires an event when a device is connected or disconnected.</summary>
        /// <param name="inputSource">The device you would like to add an event to. Any if the action is not device specific.</param>
        /// <param name="action">The method you would like to be called when a device is connected. Should take a SteamVR_Action_Pose as a param</param>
        public void AddOnDeviceConnectedChanged(SteamVR_Input_Sources inputSource, Action<SteamVR_Action_Pose> action)
        {
            onDeviceConnectedChanged[inputSource] += action;
        }
        
        /// <param name="inputSource">The device you would like to remove an event from. Any if the action is not device specific.</param>
        /// <param name="action">The method you would like to stop calling when a device is connected. Should take a SteamVR_Action_Pose as a param</param>
        public void RemoveOnDeviceConnectedChanged(SteamVR_Input_Sources inputSource, Action<SteamVR_Action_Pose> action)
        {
            onDeviceConnectedChanged[inputSource] -= action;
        }


        /// <summary>Fires an event when the tracking of the device has changed</summary>
        /// <param name="inputSource">The device you would like to add an event to. Any if the action is not device specific.</param>
        /// <param name="action">The method you would like to be called when tracking has changed. Should take a SteamVR_Action_Pose as a param</param>
        public void AddOnTrackingChanged(SteamVR_Input_Sources inputSource, Action<SteamVR_Action_Pose> action)
        {
            onTrackingChanged[inputSource] += action;
        }

        /// <param name="inputSource">The device you would like to remove an event from. Any if the action is not device specific.</param>
        /// <param name="action">The method you would like to stop calling when tracking has changed. Should take a SteamVR_Action_Pose as a param</param>
        public void RemoveOnTrackingChanged(SteamVR_Input_Sources inputSource, Action<SteamVR_Action_Pose> action)
        {
            onTrackingChanged[inputSource] -= action;
        }


        /// <summary>Fires an event when the device now has a valid pose or no longer has a valid pose</summary>
        /// <param name="inputSource">The device you would like to add an event to. Any if the action is not device specific.</param>
        /// <param name="action">The method you would like to be called when the pose has become valid or invalid. Should take a SteamVR_Action_Pose as a param</param>
        public void AddOnValidPoseChanged(SteamVR_Input_Sources inputSource, Action<SteamVR_Action_Pose> action)
        {
            onValidPoseChanged[inputSource] += action;
        }

        /// <param name="inputSource">The device you would like to remove an event from. Any if the action is not device specific.</param>
        /// <param name="action">The method you would like to stop calling when the pose has become valid or invalid. Should take a SteamVR_Action_Pose as a param</param>
        public void RemoveOnValidPoseChanged(SteamVR_Input_Sources inputSource, Action<SteamVR_Action_Pose> action)
        {
            onValidPoseChanged[inputSource] -= action;
        }
    }
}