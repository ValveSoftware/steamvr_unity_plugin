using UnityEngine;
using System.Collections;
using System;
using Valve.VR;
using System.Runtime.InteropServices;
using System.Collections.Generic;

public class SteamVR_Input_Action_Pose : SteamVR_Input_Action_In
{
    protected static ETrackingUniverseOrigin universeOrigin = ETrackingUniverseOrigin.TrackingUniverseRawAndUncalibrated;

    [NonSerialized]
    public float predictedSecondsFromNow = 0;

    [NonSerialized]
    protected Dictionary<SteamVR_Input_Input_Sources, Action<SteamVR_Input_Action_Pose>>  onTrackingChanged = new Dictionary<SteamVR_Input_Input_Sources, Action<SteamVR_Input_Action_Pose>>(new SteamVR_Input_Sources_Comparer());

    [NonSerialized]
    protected Dictionary<SteamVR_Input_Input_Sources, Action<SteamVR_Input_Action_Pose>> onValidPoseChanged = new Dictionary<SteamVR_Input_Input_Sources, Action<SteamVR_Input_Action_Pose>>(new SteamVR_Input_Sources_Comparer());

    [NonSerialized]
    protected Dictionary<SteamVR_Input_Input_Sources, Action<SteamVR_Input_Action_Pose>> onDeviceConnectedChanged = new Dictionary<SteamVR_Input_Input_Sources, Action<SteamVR_Input_Action_Pose>>(new SteamVR_Input_Sources_Comparer());

    protected Dictionary<SteamVR_Input_Input_Sources, InputPoseActionData_t> poseActionData = new Dictionary<SteamVR_Input_Input_Sources, InputPoseActionData_t>(new SteamVR_Input_Sources_Comparer());
    protected Dictionary<SteamVR_Input_Input_Sources, InputPoseActionData_t> lastPoseActionData = new Dictionary<SteamVR_Input_Input_Sources, InputPoseActionData_t>(new SteamVR_Input_Sources_Comparer());

    protected InputPoseActionData_t tempPoseActionData = new InputPoseActionData_t();

    [NonSerialized]
    protected uint poseActionData_size = 0;

    public override void Initialize()
    {
        base.Initialize();
        poseActionData_size = (uint)Marshal.SizeOf(tempPoseActionData);
    }

    protected override void InitializeDictionaries(SteamVR_Input_Input_Sources source)
    {
        base.InitializeDictionaries(source);

        onTrackingChanged.Add(source, null);
        onValidPoseChanged.Add(source, null);
        onDeviceConnectedChanged.Add(source, null);
        poseActionData.Add(source, new InputPoseActionData_t());
        lastPoseActionData.Add(source, new InputPoseActionData_t());
    }

    public override void UpdateValue(SteamVR_Input_Input_Sources inputSource)
    {
        UpdateValue(inputSource, false);
    }

    protected void ResetLastStates(SteamVR_Input_Input_Sources inputSource)
    {
        lastPoseActionData[inputSource] = poseActionData[inputSource];
    }

    public virtual void UpdateValue(SteamVR_Input_Input_Sources inputSource, bool skipStateAndEventUpdates)
    {
        changed[inputSource] = false;
        if (skipStateAndEventUpdates == false)
        {
            ResetLastStates(inputSource);
        }

        EVRInputError err = OpenVR.Input.GetPoseActionData(handle, universeOrigin, predictedSecondsFromNow, ref tempPoseActionData, poseActionData_size, SteamVR_Input_Input_Source.GetHandle(inputSource));
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
            active[inputSource] = false;
            return;
        }

        if (tempPoseActionData.bActive && tempPoseActionData.activeOrigin != 0 && poseActionData[inputSource].activeOrigin != tempPoseActionData.activeOrigin)
            Debug.Log("GetPoseActionData active origin changed. Action: " + fullPath + "::" + inputSource.ToString() + "(" + SteamVR_Input_Input_Source.GetHandle(inputSource) + ")" + " new origin: " + tempPoseActionData.activeOrigin.ToString());

        poseActionData[inputSource] = tempPoseActionData;
        active[inputSource] = tempPoseActionData.bActive;
        activeOrigin[inputSource] = tempPoseActionData.activeOrigin;

        if (tempPoseActionData.bActive && tempPoseActionData.activeOrigin == 0)
            Debug.LogError("GetPoseActionData active origin 0. Action: " + fullPath + "::" + inputSource.ToString() + "(" + SteamVR_Input_Input_Source.GetHandle(inputSource) + ")" + " handle: " + handle.ToString());

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
    }

    public bool GetVelocitiesAtTimeOffset(SteamVR_Input_Input_Sources inputSource, float secondsFromNow, out Vector3 velocity, out Vector3 angularVelocity)
    {
        EVRInputError err = OpenVR.Input.GetPoseActionData(handle, universeOrigin, secondsFromNow, ref tempPoseActionData, poseActionData_size, SteamVR_Input_Input_Source.GetHandle(inputSource));
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
            return false;
        }

        velocity = new Vector3(tempPoseActionData.pose.vVelocity.v0, tempPoseActionData.pose.vVelocity.v1, -tempPoseActionData.pose.vVelocity.v2);
        angularVelocity = new Vector3(-tempPoseActionData.pose.vAngularVelocity.v0, -tempPoseActionData.pose.vAngularVelocity.v1, tempPoseActionData.pose.vAngularVelocity.v2);

        return true;
    }

    public bool GetPoseAtTimeOffset(SteamVR_Input_Input_Sources inputSource, float secondsFromNow, out Vector3 position, out Quaternion rotation, out Vector3 velocity, out Vector3 angularVelocity)
    {
        EVRInputError err = OpenVR.Input.GetPoseActionData(handle, universeOrigin, secondsFromNow, ref tempPoseActionData, poseActionData_size, SteamVR_Input_Input_Source.GetHandle(inputSource));
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

    public void UpdateTransform(SteamVR_Input_Input_Sources inputSource, Transform transformToUpdate)
    {
        transformToUpdate.localPosition = GetLocalPosition(inputSource);
        transformToUpdate.localRotation = GetLocalRotation(inputSource);
    }

    protected void CheckAndSendEvents(SteamVR_Input_Input_Sources inputSource)
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
    }

    public static void SetTrackingUniverseOrigin(ETrackingUniverseOrigin newUniverseOrigin)
    {
        universeOrigin = newUniverseOrigin;
    }


    public Vector3 GetLocalPosition(SteamVR_Input_Input_Sources inputSource)
    {
        // Convert the transform from SteamVR's coordinate system to Unity's coordinate system.
        // ie: flip the X axis
        return new Vector3(poseActionData[inputSource].pose.mDeviceToAbsoluteTracking.m3, 
            poseActionData[inputSource].pose.mDeviceToAbsoluteTracking.m7, 
            -poseActionData[inputSource].pose.mDeviceToAbsoluteTracking.m11);
    }
    public Quaternion GetLocalRotation(SteamVR_Input_Input_Sources inputSource)
    {
        return SteamVR_Utils.GetRotation(poseActionData[inputSource].pose.mDeviceToAbsoluteTracking);
    }
    public Vector3 GetVelocity(SteamVR_Input_Input_Sources inputSource)
    {
        return new Vector3(poseActionData[inputSource].pose.vVelocity.v0, poseActionData[inputSource].pose.vVelocity.v1, -poseActionData[inputSource].pose.vVelocity.v2);
    }
    public Vector3 GetAngularVelocity(SteamVR_Input_Input_Sources inputSource)
    {
        return new Vector3(-poseActionData[inputSource].pose.vAngularVelocity.v0, -poseActionData[inputSource].pose.vAngularVelocity.v1, poseActionData[inputSource].pose.vAngularVelocity.v2);
    }
    public bool GetDeviceIsConnected(SteamVR_Input_Input_Sources inputSource)
    {
        return poseActionData[inputSource].pose.bDeviceIsConnected;
    }
    public bool GetPoseIsValid(SteamVR_Input_Input_Sources inputSource)
    {
        return poseActionData[inputSource].pose.bPoseIsValid;
    }
    public ETrackingResult GetTrackingResult(SteamVR_Input_Input_Sources inputSource)
    {
        return poseActionData[inputSource].pose.eTrackingResult;
    }

    
    public Vector3 GetLastLocalPosition(SteamVR_Input_Input_Sources inputSource)
    {
        return new Vector3(lastPoseActionData[inputSource].pose.mDeviceToAbsoluteTracking.m3,
            lastPoseActionData[inputSource].pose.mDeviceToAbsoluteTracking.m7,
            -lastPoseActionData[inputSource].pose.mDeviceToAbsoluteTracking.m11);
    }
    public Quaternion GetLastLocalRotation(SteamVR_Input_Input_Sources inputSource)
    {
        return SteamVR_Utils.GetRotation(lastPoseActionData[inputSource].pose.mDeviceToAbsoluteTracking);
    }
    public Vector3 GetLastVelocity(SteamVR_Input_Input_Sources inputSource)
    {
        return new Vector3(lastPoseActionData[inputSource].pose.vVelocity.v0, lastPoseActionData[inputSource].pose.vVelocity.v1, -lastPoseActionData[inputSource].pose.vVelocity.v2);
    }
    public Vector3 GetLastAngularVelocity(SteamVR_Input_Input_Sources inputSource)
    {
        return new Vector3(-lastPoseActionData[inputSource].pose.vAngularVelocity.v0, -lastPoseActionData[inputSource].pose.vAngularVelocity.v1, lastPoseActionData[inputSource].pose.vAngularVelocity.v2);
    }
    public bool GetLastDeviceIsConnected(SteamVR_Input_Input_Sources inputSource)
    {
        return lastPoseActionData[inputSource].pose.bDeviceIsConnected;
    }
    public bool GetLastPoseIsValid(SteamVR_Input_Input_Sources inputSource)
    {
        return lastPoseActionData[inputSource].pose.bPoseIsValid;
    }
    public ETrackingResult GetLastTrackingResult(SteamVR_Input_Input_Sources inputSource)
    {
        return lastPoseActionData[inputSource].pose.eTrackingResult;
    }


    public void AddOnDeviceConnectedChanged(SteamVR_Input_Input_Sources inputSource, Action<SteamVR_Input_Action_Pose> action)
    {
        onDeviceConnectedChanged[inputSource] += action;
    }
    public void RemoveOnDeviceConnectedChanged(SteamVR_Input_Input_Sources inputSource, Action<SteamVR_Input_Action_Pose> action)
    {
        onDeviceConnectedChanged[inputSource] -= action;
    }

    public void AddOnTrackingChanged(SteamVR_Input_Input_Sources inputSource, Action<SteamVR_Input_Action_Pose> action)
    {
        onTrackingChanged[inputSource] += action;
    }
    public void RemoveOnTrackingChanged(SteamVR_Input_Input_Sources inputSource, Action<SteamVR_Input_Action_Pose> action)
    {
        onTrackingChanged[inputSource] -= action;
    }

    public void AddOnValidPoseChanged(SteamVR_Input_Input_Sources inputSource, Action<SteamVR_Input_Action_Pose> action)
    {
        onValidPoseChanged[inputSource] += action;
    }
    public void RemoveOnValidPoseChanged(SteamVR_Input_Input_Sources inputSource, Action<SteamVR_Input_Action_Pose> action)
    {
        onValidPoseChanged[inputSource] -= action;
    }
}