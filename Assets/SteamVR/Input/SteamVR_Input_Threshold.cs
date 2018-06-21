using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class SteamVR_Input_Threshold
{
    public SteamVR_Input_Action_Single singleAction;

    public SteamVR_Input_Input_Sources inputSource;

    public bool isActive { get { return singleAction.GetActive(inputSource); } }

    public float engageThreshold = 0.01f;

    [NonSerialized]
    public bool engaged;

    [NonSerialized]
    public bool wasEngaged;

    [NonSerialized]
    public bool engageStarting;

    [NonSerialized]
    public bool engageEnding;

    public float axis { get { return singleAction.GetAxis(inputSource); } }

    public void Update()
    {
        if (isActive)
        {
            wasEngaged = engaged;
            engaged = axis >= engageThreshold;

            engageStarting = wasEngaged == false && engaged == true;
            engageEnding = wasEngaged == false && engaged == true;
        }
    }
}