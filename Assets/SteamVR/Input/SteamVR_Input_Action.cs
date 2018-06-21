using UnityEngine;
using System.Collections;
using System;
using Valve.VR;
using System.Runtime.InteropServices;
using System.Collections.Generic;

public abstract class SteamVR_Input_Action : ScriptableObject
{
    public float changeTolerance = 0.000001f;

    public string fullPath;
    
    protected ulong handle;
    
    public SteamVR_Input_ActionSet actionSet;
    
    public SteamVR_Input_ActionDirections direction;

    [NonSerialized]
    protected Dictionary<SteamVR_Input_Input_Sources, float> lastChanged = new Dictionary<SteamVR_Input_Input_Sources, float>(new SteamVR_Input_Sources_Comparer());

    public float GetTimeLastChanged(SteamVR_Input_Input_Sources inputSource = SteamVR_Input_Input_Sources.Any)
    {
        return lastChanged[inputSource];
    }

    public virtual void Initialize()
    {
        handle = 0;

        EVRInputError err = OpenVR.Input.GetActionHandle(fullPath.ToLower(), ref handle); 

        if (err != EVRInputError.None)
            Debug.LogError("GetActionHandle (" + fullPath + ") error: " + err.ToString());

        var sources = SteamVR_Input_Input_Source.GetUpdateSources();
        for (int sourceIndex = 0; sourceIndex < sources.Length; sourceIndex++)
        {
            InitializeDictionaries(sources[sourceIndex]);
        }
    }

    protected virtual void InitializeDictionaries(SteamVR_Input_Input_Sources source)
    {
        lastChanged.Add(source, 0);
    }

    [NonSerialized]
    private string cachedShortName;
    public string GetShortName()
    {
        if (cachedShortName == null)
        {
            cachedShortName = SteamVR_Input_ActionFile.GetShortName(fullPath);
        }

        return cachedShortName;
    }
}