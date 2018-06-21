//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: For controlling in-game objects with tracked devices.
//
//=============================================================================

using UnityEngine;
using Valve.VR;

public class SteamVR_Input_TrackedObject : MonoBehaviour
{
    [DefaultInputAction("Pose", null, "inputSource")]
    public SteamVR_Input_Action_Pose poseAction;

    public SteamVR_Input_Input_Sources inputSource;

    [Tooltip("If not set, relative to parent")]
    public Transform origin;

    public bool isValid { get { return poseAction.GetPoseIsValid(inputSource); } }

    protected int deviceIndex = -1;

    protected virtual void Start()
    {
        CheckDeviceIndex();
        poseAction.AddOnDeviceConnectedChanged(inputSource, OnDeviceConnectedChanged);
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

    protected virtual void OnEnable()
    {
        SteamVR_Input.OnPosesUpdated += SteamVR_Input_OnPosesUpdated;
    }

    protected virtual void OnDisable()
    {
        SteamVR_Input.OnPosesUpdated -= SteamVR_Input_OnPosesUpdated;
    }

    private void SteamVR_Input_OnPosesUpdated(bool obj)
    {
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
    }
}

