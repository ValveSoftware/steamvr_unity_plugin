using UnityEngine;
using System.Collections;
using System;
using Valve.VR;
using System.Runtime.InteropServices;
using System.Collections.Generic;

public class SteamVR_Input_Action_Boolean : SteamVR_Input_Action_In
{
    
    protected Dictionary<SteamVR_Input_Input_Sources, Action<SteamVR_Input_Action_Boolean>> onStateDown = new Dictionary<SteamVR_Input_Input_Sources, Action<SteamVR_Input_Action_Boolean>>(new SteamVR_Input_Sources_Comparer());
    protected Dictionary<SteamVR_Input_Input_Sources, Action<SteamVR_Input_Action_Boolean>> onStateUp = new Dictionary<SteamVR_Input_Input_Sources, Action<SteamVR_Input_Action_Boolean>>(new SteamVR_Input_Sources_Comparer());
    
    protected Dictionary<SteamVR_Input_Input_Sources, InputDigitalActionData_t> actionData = new Dictionary<SteamVR_Input_Input_Sources, InputDigitalActionData_t>(new SteamVR_Input_Sources_Comparer());
    protected Dictionary<SteamVR_Input_Input_Sources, InputDigitalActionData_t> lastActionData = new Dictionary<SteamVR_Input_Input_Sources, InputDigitalActionData_t>(new SteamVR_Input_Sources_Comparer());
    protected InputDigitalActionData_t tempActionData = new InputDigitalActionData_t();

    [NonSerialized]
    protected uint actionData_size = 0;

    public override void Initialize()
    {
        base.Initialize();
        actionData_size = (uint)Marshal.SizeOf(tempActionData);
    }

    protected override void InitializeDictionaries(SteamVR_Input_Input_Sources source)
    {
        base.InitializeDictionaries(source);

        onStateDown.Add(source, null);
        onStateUp.Add(source, null);
        actionData.Add(source, new InputDigitalActionData_t());
        lastActionData.Add(source, new InputDigitalActionData_t());
    }

    public override void UpdateValue(SteamVR_Input_Input_Sources inputSource)
    {
        lastActionData[inputSource] = actionData[inputSource];

        EVRInputError err = OpenVR.Input.GetDigitalActionData(handle, ref tempActionData, actionData_size, SteamVR_Input_Input_Source.GetHandle(inputSource));
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
    }

    public bool GetStateDown(SteamVR_Input_Input_Sources inputSource = SteamVR_Input_Input_Sources.Any)
    {
        return actionData[inputSource].bState && actionData[inputSource].bChanged;
    }
    public bool GetStateUp(SteamVR_Input_Input_Sources inputSource = SteamVR_Input_Input_Sources.Any)
    {
        return actionData[inputSource].bState == false && actionData[inputSource].bChanged;
    }
    public bool GetState(SteamVR_Input_Input_Sources inputSource = SteamVR_Input_Input_Sources.Any)
    {
        return actionData[inputSource].bState;
    }
    public bool GetLastStateDown(SteamVR_Input_Input_Sources inputSource = SteamVR_Input_Input_Sources.Any)
    {
        return lastActionData[inputSource].bState && lastActionData[inputSource].bChanged;
    }
    public bool GetLastStateUp(SteamVR_Input_Input_Sources inputSource = SteamVR_Input_Input_Sources.Any)
    {
        return lastActionData[inputSource].bState == false && lastActionData[inputSource].bChanged;
    }
    public bool GetLastState(SteamVR_Input_Input_Sources inputSource = SteamVR_Input_Input_Sources.Any)
    {
        return lastActionData[inputSource].bState;
    }
}