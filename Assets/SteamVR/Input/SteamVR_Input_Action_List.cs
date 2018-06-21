using UnityEngine;
using System.Collections;
using System;
using Valve.VR;
using System.Runtime.InteropServices;

public abstract class SteamVR_Input_Action_List : ScriptableObject
{
    public SteamVR_Input_ActionSet actionSet;
    public SteamVR_Input_ActionDirections listDirection;
    public SteamVR_Input_Action[] actions;
}
