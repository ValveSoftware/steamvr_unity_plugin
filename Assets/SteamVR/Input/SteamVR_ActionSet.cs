//======= Copyright (c) Valve Corporation, All rights reserved. ===============

using UnityEngine;
using System.Collections;
using System;
using Valve.VR;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Valve.VR
{
    /// <summary>
    /// Action sets are logical groupings of actions. Multiple sets can be active at one time.
    /// </summary>
    public class SteamVR_ActionSet : ScriptableObject
    {
        [NonSerialized]
        protected static VRActiveActionSet_t[] activeActionSets;

        [NonSerialized]
        protected static List<VRActiveActionSet_t> activeActionSetsList = new List<VRActiveActionSet_t>();

        [NonSerialized]
        protected VRActiveActionSet_t actionSet = new VRActiveActionSet_t();

        /// <summary>All actions within this set (including out actions)</summary>
        public SteamVR_Action[] allActions;

        /// <summary>All IN actions within this set that are NOT pose or skeleton actions</summary>
        public SteamVR_Action_In[] nonVisualInActions;

        /// <summary>All pose and skeleton actions within this set</summary>
        public SteamVR_Action_In[] visualActions;

        /// <summary>All pose actions within this set</summary>
        public SteamVR_Action_Pose[] poseActions;

        /// <summary>All skeleton actions within this set</summary>
        public SteamVR_Action_Skeleton[] skeletonActions;

        /// <summary>All out actions within this set</summary>
        public SteamVR_Action_Out[] outActionArray;


        /// <summary>The full path to this action set (ex: /actions/in/default)</summary>
        public string fullPath;
        public string usage;

        [NonSerialized]
        public ulong handle;

        [NonSerialized]
        protected bool setIsActive = false;

        [NonSerialized]
        protected float lastChanged = -1;

        [NonSerialized]
        protected static uint activeActionSetSize;

        public void Initialize()
        {
            EVRInputError err = OpenVR.Input.GetActionSetHandle(fullPath.ToLower(), ref handle);

            if (err != EVRInputError.None)
                Debug.LogError("GetActionSetHandle (" + fullPath + ") error: " + err.ToString());

            activeActionSetSize = (uint)(Marshal.SizeOf(typeof(VRActiveActionSet_t)));
        }

        /// <summary>
        /// Returns whether the set is currently active or not.
        /// </summary>
        public bool IsActive()
        {
            return setIsActive;
        }

        /// <summary>
        /// Returns the last time this action set was changed (set to active or inactive)
        /// </summary>
        /// <returns></returns>
        public float GetTimeLastChanged()
        {
            return lastChanged;
        }

        /// <summary>
        /// Activate this set as a primary action set so its actions can be called
        /// </summary>
        /// <param name="disableAllOtherActionSets">Disable all other action sets at the same time</param>
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

        /// <summary>
        /// Activate this set as a secondary action set so its actions can be called
        /// </summary>
        /// <param name="disableAllOtherActionSets">Disable all other action sets at the same time</param>
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

        /// <summary>
        /// Deactivate the action set so its actions can no longer be called
        /// </summary>
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

        /// <summary>
        /// Disable all known action sets.
        /// </summary>
        public static void DisableAllActionSets()
        {
            for (int actionSetIndex = 0; actionSetIndex < SteamVR_Input.actionSets.Length; actionSetIndex++)
            {
                SteamVR_ActionSet set = SteamVR_Input.actionSets[actionSetIndex];
                set.Deactivate();
            }
        }
        
        protected static void UpdateActionSetArray()
        {
            activeActionSets = activeActionSetsList.ToArray();
        }

        [NonSerialized]
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
                    //else Debug.Log("Action sets activated: " + activeActionSets.Length);
                }
                else
                {
                    //Debug.LogWarning("No sets active");
                }
            }
        }

        [NonSerialized]
        private string cachedShortName;

        /// <summary>Gets the last part of the path for this action. Removes "actions" and direction.</summary>
        public string GetShortName()
        {
            if (cachedShortName == null)
            {
                cachedShortName = SteamVR_Input_ActionFile.GetShortName(fullPath);
            }

            return cachedShortName;
        }
    }
}