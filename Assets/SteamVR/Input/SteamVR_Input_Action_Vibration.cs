using UnityEngine;
using System.Collections;
using System;
using Valve.VR;
using System.Runtime.InteropServices;

public class SteamVR_Input_Action_Vibration : SteamVR_Input_Action_Out
{
    public void Execute(float secondsFromNow, float durationSeconds, float frequency, float amplitude, SteamVR_Input_Input_Sources inputSource = SteamVR_Input_Input_Sources.Any)
    {
        lastChanged[inputSource] = Time.time;

        EVRInputError err = OpenVR.Input.TriggerHapticVibrationAction(handle, secondsFromNow, durationSeconds, frequency, amplitude, SteamVR_Input_Input_Source.GetHandle(inputSource));

        //Debug.Log(string.Format("haptic: {5}: {0}, {1}, {2}, {3}, {4}", secondsFromNow, durationSeconds, frequency, amplitude, inputSource, this.GetShortName()));

        if (err != EVRInputError.None)
            Debug.LogError("TriggerHapticVibrationAction (" + fullPath + ") error: " + err.ToString() + " handle: " + handle.ToString());
    }
}