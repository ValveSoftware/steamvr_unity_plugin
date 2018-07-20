//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: For controlling in-game objects with tracked devices.
//
//=============================================================================

using System;
using System.Threading;
using UnityEngine;
using Valve.VR;

public class SteamVR_Input_TrackedObject : MonoBehaviour
{
    [DefaultInputAction("Pose")]
    public SteamVR_Input_Action_Pose poseAction;

    public SteamVR_Input_Input_Sources inputSource;

    [Tooltip("If not set, relative to parent")]
    public Transform origin;

    public bool enableAdvancedVelocityEstimation = true;

    public bool isValid { get { return poseAction.GetPoseIsValid(inputSource); } }
    public bool isActive { get { return poseAction.GetActive(inputSource); } }

    public Action onTransformUpdated;

    protected int deviceIndex = -1;

    protected SteamVR_HistoryBuffer historyBuffer = new SteamVR_HistoryBuffer(30);

    protected virtual void Start()
    {
        CheckDeviceIndex();
        poseAction.AddOnDeviceConnectedChanged(inputSource, OnDeviceConnectedChanged);

        if (origin == null)
            origin = this.transform.parent;
    }

    protected virtual void OnDeviceConnectedChanged(SteamVR_Input_Action_Pose changedAction)
    {
        CheckDeviceIndex();
    }

    protected virtual void CheckDeviceIndex()
    {
        if (poseAction.GetActive(inputSource))
        {
            if (poseAction.GetDeviceIsConnected(inputSource))
            {
                int currentDeviceIndex = (int)poseAction.GetDeviceIndex(inputSource);

                if (deviceIndex != currentDeviceIndex)
                {
                    deviceIndex = currentDeviceIndex;
                    this.gameObject.BroadcastMessage("SetDeviceIndex", (int)deviceIndex, SendMessageOptions.DontRequireReceiver);
                }
            }
        }
    }

    public int GetDeviceIndex()
    {
        if (deviceIndex == -1)
            CheckDeviceIndex();

        return deviceIndex;
    }

    protected virtual void OnEnable()
    {
        SteamVR_Input.OnPosesUpdated += SteamVR_Input_OnPosesUpdated;
    }

    protected virtual void OnDisable()
    {
        SteamVR_Input.OnPosesUpdated -= SteamVR_Input_OnPosesUpdated;

        historyBuffer.Clear();
    }

    public Vector3 GetVelocity()
    {
        return poseAction.GetVelocity(inputSource);
    }

    public Vector3 GetAngularVelocity()
    {
        return poseAction.GetAngularVelocity(inputSource);
    }

    public bool GetVelocitiesAtTimeOffset(float secondsFromNow, out Vector3 velocity, out Vector3 angularVelocity)
    {
        return poseAction.GetVelocitiesAtTimeOffset(inputSource, secondsFromNow, out velocity, out angularVelocity);
    }

    protected void UpdateHistoryBuffer()
    {
        historyBuffer.Update(poseAction.GetLocalPosition(inputSource), poseAction.GetLocalRotation(inputSource), poseAction.GetVelocity(inputSource), poseAction.GetAngularVelocity(inputSource));
    }

    public void GetEstimatedPeakVelocities(out Vector3 velocity, out Vector3 angularVelocity)
    {
        int top = historyBuffer.GetTopVelocity(10, 1);

        historyBuffer.GetAverageVelocities(out velocity, out angularVelocity, 2, top);
    }

    private void SteamVR_Input_OnPosesUpdated(bool obj)
    {
        UpdateHistoryBuffer();
        Update();
    }

    protected virtual void Update()
    {
        if (poseAction == null)
            return;
        
        CheckDeviceIndex();

        if (origin != null)
        {
            transform.position = origin.transform.TransformPoint(poseAction.GetLocalPosition(inputSource));
            transform.rotation = origin.rotation * poseAction.GetLocalRotation(inputSource);
        }
        else
        {
            transform.localPosition = poseAction.GetLocalPosition(inputSource);
            transform.localRotation = poseAction.GetLocalRotation(inputSource);
        }

        if (onTransformUpdated != null)
            onTransformUpdated.Invoke();
    }
}