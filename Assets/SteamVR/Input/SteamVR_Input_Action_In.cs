using UnityEngine;
using System.Collections;
using System;
using Valve.VR;
using System.Runtime.InteropServices;
using System.Collections.Generic;

public abstract class SteamVR_Input_Action_In : SteamVR_Input_Action
{
    protected Dictionary<SteamVR_Input_Input_Sources, float> updateTime = new Dictionary<SteamVR_Input_Input_Sources, float>(new SteamVR_Input_Sources_Comparer());

    protected Dictionary<SteamVR_Input_Input_Sources, ulong> activeOrigin = new Dictionary<SteamVR_Input_Input_Sources, ulong>(new SteamVR_Input_Sources_Comparer());

    protected Dictionary<SteamVR_Input_Input_Sources, bool> active = new Dictionary<SteamVR_Input_Input_Sources, bool>(new SteamVR_Input_Sources_Comparer());

    protected Dictionary<SteamVR_Input_Input_Sources, bool> changed = new Dictionary<SteamVR_Input_Input_Sources, bool>(new SteamVR_Input_Sources_Comparer());

    protected Dictionary<SteamVR_Input_Input_Sources, Action<SteamVR_Input_Action_In>> onChange = new Dictionary<SteamVR_Input_Input_Sources, Action<SteamVR_Input_Action_In>>(new SteamVR_Input_Sources_Comparer());

    protected Dictionary<SteamVR_Input_Input_Sources, Action<SteamVR_Input_Action_In>> onUpdate = new Dictionary<SteamVR_Input_Input_Sources, Action<SteamVR_Input_Action_In>>(new SteamVR_Input_Sources_Comparer());

    public abstract void UpdateValue(SteamVR_Input_Input_Sources inputSource);

    protected Dictionary<SteamVR_Input_Input_Sources, InputOriginInfo_t> lastInputOriginInfo = new Dictionary<SteamVR_Input_Input_Sources, InputOriginInfo_t>(new SteamVR_Input_Sources_Comparer());

    protected override void InitializeDictionaries(SteamVR_Input_Input_Sources source)
    {
        base.InitializeDictionaries(source);

        updateTime.Add(source, -1);
        activeOrigin.Add(source, 0);
        active.Add(source, false);
        changed.Add(source, false);
        onChange.Add(source, null);
        onUpdate.Add(source, null);
        lastInputOriginInfo.Add(source, new InputOriginInfo_t());
        lastOriginGetFrame.Add(source, -1);
    }

    public virtual string GetDeviceComponentName(SteamVR_Input_Input_Sources inputSource = SteamVR_Input_Input_Sources.Any)
    {
        if (GetActive(inputSource))
        {
            UpdateOriginTrackedDeviceInfo(inputSource);

            return lastInputOriginInfo[inputSource].rchRenderModelComponentName;
        }

        return null;
    }

    public virtual ulong GetDevicePath(SteamVR_Input_Input_Sources inputSource = SteamVR_Input_Input_Sources.Any)
    {
        if (GetActive(inputSource))
        {
            UpdateOriginTrackedDeviceInfo(inputSource);

            return lastInputOriginInfo[inputSource].devicePath;
        }

        return 0;
    }

    public virtual uint GetDeviceIndex(SteamVR_Input_Input_Sources inputSource = SteamVR_Input_Input_Sources.Any)
    {
        if (GetActive(inputSource))
        {
            UpdateOriginTrackedDeviceInfo(inputSource);

            return lastInputOriginInfo[inputSource].trackedDeviceIndex;
        }

        return 0;
    }

    public virtual bool GetChanged(SteamVR_Input_Input_Sources inputSource = SteamVR_Input_Input_Sources.Any)
    {
        return changed[inputSource];
    }

    public virtual bool GetActive(SteamVR_Input_Input_Sources inputSource = SteamVR_Input_Input_Sources.Any)
    {
        return active[inputSource];
    }

    protected Dictionary<SteamVR_Input_Input_Sources, float> lastOriginGetFrame = new Dictionary<SteamVR_Input_Input_Sources, float>(new SteamVR_Input_Sources_Comparer());
    protected void UpdateOriginTrackedDeviceInfo(SteamVR_Input_Input_Sources inputSource = SteamVR_Input_Input_Sources.Any)
    {
        if (lastOriginGetFrame[inputSource] != Time.frameCount)
        {
            InputOriginInfo_t inputOriginInfo = new InputOriginInfo_t();
            EVRInputError err = OpenVR.Input.GetOriginTrackedDeviceInfo(activeOrigin[inputSource], ref inputOriginInfo, (uint)Marshal.SizeOf(lastInputOriginInfo[inputSource]));

            if (err != EVRInputError.None)
                Debug.LogError("GetOriginTrackedDeviceInfo error (" + fullPath + "): " + err.ToString() + " handle: " + handle.ToString() + " activeOrigin: " + activeOrigin[inputSource].ToString() + " active: " + active[inputSource]);

            lastInputOriginInfo[inputSource] = inputOriginInfo;
            lastOriginGetFrame[inputSource] = Time.frameCount;
        }
    }

    public void AddOnChangeListener(Action<SteamVR_Input_Action_In> action, SteamVR_Input_Input_Sources inputSource = SteamVR_Input_Input_Sources.Any)
    {
        onChange[inputSource] += action;
    }

    public void RemoveOnChangeListener(Action<SteamVR_Input_Action_In> action, SteamVR_Input_Input_Sources inputSource = SteamVR_Input_Input_Sources.Any)
    {
        onChange[inputSource] -= action;
    }

    public void AddOnUpdateListener(Action<SteamVR_Input_Action_In> action, SteamVR_Input_Input_Sources inputSource = SteamVR_Input_Input_Sources.Any)
    {
        onUpdate[inputSource] += action;
    }

    public void RemoveOnUpdateListener(Action<SteamVR_Input_Action_In> action, SteamVR_Input_Input_Sources inputSource = SteamVR_Input_Input_Sources.Any)
    {
        onUpdate[inputSource] -= action;
    }
}
