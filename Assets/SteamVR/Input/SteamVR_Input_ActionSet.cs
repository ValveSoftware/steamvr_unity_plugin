using UnityEngine;
using System.Collections;
using System;
using Valve.VR;
using System.Runtime.InteropServices;
using System.Collections.Generic;

public class SteamVR_Input_ActionSet : ScriptableObject
{
    protected static VRActiveActionSet_t[] activeActionSets;
    protected static List<VRActiveActionSet_t> activeActionSetsList = new List<VRActiveActionSet_t>();
    protected VRActiveActionSet_t actionSet = new VRActiveActionSet_t();

    public SteamVR_Input_Action[] allActions;
    public SteamVR_Input_Action_In[] nonVisualInActions;
    public SteamVR_Input_Action_In[] visualActions;
    public SteamVR_Input_Action_Pose[] poseActions;
    public SteamVR_Input_Action_Skeleton[] skeletonActions;
    public SteamVR_Input_Action_Out[] outActionArray;

    public string fullPath;
    public string usage;

    [NonSerialized]
    public ulong handle;

    [NonSerialized]
    protected bool setIsActive = false;

    [NonSerialized]
    protected float lastChanged = -1;

    protected static uint activeActionSetSize;

    public void Initialize()
    {
        EVRInputError err = OpenVR.Input.GetActionSetHandle(fullPath.ToLower(), ref handle);

        if (err != EVRInputError.None)
            Debug.LogError("GetActionSetHandle (" + fullPath + ") error: " + err.ToString());

        activeActionSetSize = (uint)(Marshal.SizeOf(typeof(VRActiveActionSet_t)));
    }

    public bool IsActive()
    {
        return setIsActive;
    }

    public float GetTimeLastChanged()
    {
        return lastChanged;
    }

    public void ActivatePrimary(bool disableAllOtherActionSets = false)
    {
        if (disableAllOtherActionSets)
            DisableAllActionSets();

        actionSet.ulActionSet = handle;

        if (activeActionSetsList.Contains(actionSet) == false)
            activeActionSetsList.Add(actionSet);

        setIsActive = true;
        lastChanged = Time.time;

        UpdateActionSetArray();
    }

    public void ActivateSecondary(bool disableAllOtherActionSets = false)
    {
        if (disableAllOtherActionSets)
            DisableAllActionSets();

        actionSet.ulSecondaryActionSet = handle;

        if (activeActionSetsList.Contains(actionSet) == false)
            activeActionSetsList.Add(actionSet);

        setIsActive = true;
        lastChanged = Time.time;

        UpdateActionSetArray();
    }

    public void Deactivate()
    {
        setIsActive = false;
        lastChanged = Time.time;

        if (actionSet.ulActionSet == handle)
            actionSet.ulActionSet = 0;
        if (actionSet.ulSecondaryActionSet == handle)
            actionSet.ulActionSet = 0;

        if (actionSet.ulActionSet == 0 && actionSet.ulSecondaryActionSet == 0)
        {
            activeActionSetsList.Remove(actionSet);

            UpdateActionSetArray();
        }
    }

    public static void DisableAllActionSets()
    {
        for (int actionSetIndex = 0; actionSetIndex < SteamVR_Input.actionSets.Length; actionSetIndex++)
        {
            SteamVR_Input_ActionSet set = SteamVR_Input.actionSets[actionSetIndex];
            set.Deactivate();
        }
    }

    protected static void UpdateActionSetArray()
    {
        activeActionSets = activeActionSetsList.ToArray();
    }

    protected static int lastFrameUpdated;
    public static void UpdateActionSetsState(bool force = false)
    {
        if (force || Time.frameCount != lastFrameUpdated)
        {
            lastFrameUpdated = Time.frameCount;

            if (activeActionSets != null && activeActionSets.Length > 0)
            {
                EVRInputError err = OpenVR.Input.UpdateActionState(activeActionSets, activeActionSetSize);
                if (err != EVRInputError.None)
                    Debug.LogError("UpdateActionState error: " + err.ToString());
            }
            else
            {
                Debug.LogWarning("No sets active");
            }
        }
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