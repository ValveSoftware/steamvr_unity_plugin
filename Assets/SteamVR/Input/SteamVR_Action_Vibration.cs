//======= Copyright (c) Valve Corporation, All rights reserved. ===============

using UnityEngine;
using System.Collections;
using System;
using Valve.VR;
using System.Runtime.InteropServices;

namespace Valve.VR
{
    /// <summary>
    /// Vibration actions are used to trigger haptic feedback in vr controllers.
    /// </summary>
    public class SteamVR_Action_Vibration : SteamVR_Action_Out
    {
        /// <summary>
        /// Trigger the haptics at a certain time for a certain length
        /// </summary>
        /// <param name="secondsFromNow">How long from the current time to execute the action (in seconds - can be 0)</param>
        /// <param name="durationSeconds">How long the haptic action should last (in seconds)</param>
        /// <param name="frequency">How often the haptic motor should bounce (0 - 320 in hz. The lower end being more useful)</param>
        /// <param name="amplitude">How intense the haptic action should be (0 - 1)</param>
        /// <param name="inputSource">The device you would like to execute the haptic action. Any if the action is not device specific.</param>
        public void Execute(float secondsFromNow, float durationSeconds, float frequency, float amplitude, SteamVR_Input_Sources inputSource)
        {
            lastChanged[inputSource] = Time.time;

            EVRInputError err = OpenVR.Input.TriggerHapticVibrationAction(handle, secondsFromNow, durationSeconds, frequency, amplitude, SteamVR_Input_Source.GetHandle(inputSource));

            //Debug.Log(string.Format("haptic: {5}: {0}, {1}, {2}, {3}, {4}", secondsFromNow, durationSeconds, frequency, amplitude, inputSource, this.GetShortName()));

            if (err != EVRInputError.None)
                Debug.LogError("TriggerHapticVibrationAction (" + fullPath + ") error: " + err.ToString() + " handle: " + handle.ToString());
        }
    }
}