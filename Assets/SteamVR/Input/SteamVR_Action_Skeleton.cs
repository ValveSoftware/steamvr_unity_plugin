//======= Copyright (c) Valve Corporation, All rights reserved. ===============

using UnityEngine;
using System.Collections;
using System;

using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Valve.VR
{
    /// <summary>
    /// Skeleton Actions are our best approximation of where your hands are while holding vr controllers and pressing buttons. We give you 31 bones to help you animate hand models.
    /// For more information check out this blog post: https://steamcommunity.com/games/250820/announcements/detail/1690421280625220068
    /// </summary>
    public class SteamVR_Action_Skeleton : SteamVR_Action_Pose
    {
        public const int numBones = 31;

        [NonSerialized]
        protected List<Vector3[]> bonePositions = new List<Vector3[]>();

        [NonSerialized]
        protected List<Quaternion[]> boneRotations = new List<Quaternion[]>();

        [NonSerialized]
        protected List<Vector3[]> lastBonePositions = new List<Vector3[]>();

        [NonSerialized]
        protected List<Quaternion[]> lastBoneRotations = new List<Quaternion[]>();

        [NonSerialized]
        protected Dictionary<SteamVR_Input_Sources, EVRSkeletalMotionRange> rangeOfMotion = new Dictionary<SteamVR_Input_Sources, EVRSkeletalMotionRange>(new SteamVR_Input_Sources_Comparer());

        [NonSerialized]
        protected VRBoneTransform_t[] tempBoneTransforms = new VRBoneTransform_t[numBones];

        [NonSerialized]
        protected InputSkeletalActionData_t tempSkeletonActionData = new InputSkeletalActionData_t();

        [NonSerialized]
        protected uint skeletonActionData_size = 0;

        [NonSerialized]
        protected Dictionary<SteamVR_Input_Sources, EVRSkeletalTransformSpace> skeletalTransformSpace = new Dictionary<SteamVR_Input_Sources, EVRSkeletalTransformSpace>(new SteamVR_Input_Sources_Comparer());

        public override void Initialize()
        {
            base.Initialize();
            skeletonActionData_size = (uint)Marshal.SizeOf(tempSkeletonActionData);
        }

        protected override void InitializeDictionaries(SteamVR_Input_Sources source)
        {
            base.InitializeDictionaries(source);

            bonePositions.Add(new Vector3[numBones]);
            boneRotations.Add(new Quaternion[numBones]);
            lastBonePositions.Add(new Vector3[numBones]);
            lastBoneRotations.Add(new Quaternion[numBones]);
            rangeOfMotion.Add(source, EVRSkeletalMotionRange.WithController);
            skeletalTransformSpace.Add(source, EVRSkeletalTransformSpace.Parent);
        }

        public override void UpdateValue(SteamVR_Input_Sources inputSource)
        {
            UpdateValue(inputSource, false);
        }

        public override void UpdateValue(SteamVR_Input_Sources inputSource, bool skipStateAndEventUpdates)
        {
            if (skipStateAndEventUpdates == false)
                base.ResetLastStates(inputSource);

            base.UpdateValue(inputSource, true);
            bool poseChanged = base.changed[inputSource];

            int inputSourceInt = (int)inputSource;

            if (skipStateAndEventUpdates == false)
            {
                changed[inputSource] = false;

                for (int boneIndex = 0; boneIndex < numBones; boneIndex++)
                {
                    lastBonePositions[inputSourceInt][boneIndex] = bonePositions[inputSourceInt][boneIndex];
                    lastBoneRotations[inputSourceInt][boneIndex] = boneRotations[inputSourceInt][boneIndex];
                }
            }

            EVRInputError err = OpenVR.Input.GetSkeletalActionData(handle, ref tempSkeletonActionData, skeletonActionData_size, SteamVR_Input_Source.GetHandle(inputSource));
            if (err != EVRInputError.None)
            {
                Debug.LogError("GetSkeletalActionData error (" + fullPath + "): " + err.ToString() + " handle: " + handle.ToString()); 
                active[inputSource] = false;
                return;
            }

            active[inputSource] = active[inputSource] && tempSkeletonActionData.bActive; //anding from the pose active state
            activeOrigin[inputSource] = tempSkeletonActionData.activeOrigin;

            if (active[inputSource])
            {
                err = OpenVR.Input.GetSkeletalBoneData(handle, skeletalTransformSpace[inputSource], rangeOfMotion[inputSource], tempBoneTransforms, SteamVR_Input_Source.GetHandle(inputSource));
                if (err != EVRInputError.None)
                    Debug.LogError("GetSkeletalBoneData error (" + fullPath + "): " + err.ToString() + " handle: " + handle.ToString());

                for (int boneIndex = 0; boneIndex < tempBoneTransforms.Length; boneIndex++)
                {
                    // SteamVR's coordinate system is right handed, and Unity's is left handed.  The FBX data has its
                    // X axis flipped when Unity imports it, so here we need to flip the X axis as well
                    bonePositions[inputSourceInt][boneIndex].x = -tempBoneTransforms[boneIndex].position.v0;
                    bonePositions[inputSourceInt][boneIndex].y = tempBoneTransforms[boneIndex].position.v1;
                    bonePositions[inputSourceInt][boneIndex].z = tempBoneTransforms[boneIndex].position.v2;

                    boneRotations[inputSourceInt][boneIndex].x = tempBoneTransforms[boneIndex].orientation.x;
                    boneRotations[inputSourceInt][boneIndex].y = -tempBoneTransforms[boneIndex].orientation.y;
                    boneRotations[inputSourceInt][boneIndex].z = -tempBoneTransforms[boneIndex].orientation.z;
                    boneRotations[inputSourceInt][boneIndex].w = tempBoneTransforms[boneIndex].orientation.w;
                }

                // Now that we're in the same handedness as Unity, rotate the root bone around the Y axis
                // so that forward is facing down +Z
                Quaternion qFixUpRot = Quaternion.AngleAxis(Mathf.PI * Mathf.Rad2Deg, Vector3.up);

                boneRotations[inputSourceInt][0] = qFixUpRot * boneRotations[inputSourceInt][0];
            }

            changed[inputSource] = changed[inputSource] || poseChanged;

            if (skipStateAndEventUpdates == false)
            {
                for (int boneIndex = 0; boneIndex < tempBoneTransforms.Length; boneIndex++)
                {
                    if (Vector3.Distance(lastBonePositions[inputSourceInt][boneIndex], bonePositions[inputSourceInt][boneIndex]) > changeTolerance)
                    {
                        changed[inputSource] |= true;
                        break;
                    }

                    if (Mathf.Abs(Quaternion.Angle(lastBoneRotations[inputSourceInt][boneIndex], boneRotations[inputSourceInt][boneIndex])) > changeTolerance)
                    {
                        changed[inputSource] |= true;
                        break;
                    }
                }

                base.CheckAndSendEvents(inputSource);
            }

            if (changed[inputSource])
                lastChanged[inputSource] = Time.time;

            if (skipStateAndEventUpdates == false)
            {
                lastRecordedActive[inputSource] = active[inputSource];
                lastRecordedPoseActionData[inputSource] = poseActionData[inputSource];
            }
        }

        /// <summary>
        /// Gets the bone positions in local space
        /// </summary>
        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public Vector3[] GetBonePositions(SteamVR_Input_Sources inputSource)
        {
            return bonePositions[(int)inputSource];
        }

        /// <summary>
        /// Gets the bone rotations in local space
        /// </summary>
        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public Quaternion[] GetBoneRotations(SteamVR_Input_Sources inputSource)
        {
            return boneRotations[(int)inputSource];
        }

        /// <summary>
        /// Gets the bone positions in local space from the previous update
        /// </summary>
        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public Vector3[] GetLastBonePositions(SteamVR_Input_Sources inputSource)
        {
            return lastBonePositions[(int)inputSource];
        }

        /// <summary>
        /// Gets the bone rotations in local space from the previous update
        /// </summary>
        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public Quaternion[] GetLastBoneRotations(SteamVR_Input_Sources inputSource)
        {
            return lastBoneRotations[(int)inputSource];
        }

        /// <summary>
        /// Set the range of the motion of the bones in this skeleton. Options are "With Controller" as if your hand is holding your VR controller. 
        /// Or "Without Controller" as if your hand is empty.
        /// </summary>
        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        public void SetRangeOfMotion(SteamVR_Input_Sources inputSource, EVRSkeletalMotionRange range)
        {
            rangeOfMotion[inputSource] = range;
        }

        /// <summary>
        /// Sets the space that you'll get bone data back in. Options are relative to the Model, relative to the Parent bone, and Additive.
        /// </summary>
        /// <param name="inputSource">The device you would like to get data from. Any if the action is not device specific.</param>
        /// <param name="space">the space that you'll get bone data back in. Options are relative to the Model, relative to the Parent bone, and Additive.</param>
        public void SetSkeletalTransformSpace(SteamVR_Input_Sources inputSource, EVRSkeletalTransformSpace space)
        {
            skeletalTransformSpace[inputSource] = space;
        }
    }

    /// <summary>
    /// The change in range of the motion of the bones in the skeleton. Options are "With Controller" as if your hand is holding your VR controller. 
    /// Or "Without Controller" as if your hand is empty.
    /// </summary>
    public enum SkeletalMotionRangeChange
    {
        None = -1,

        /// <summary>Estimation of bones in hand while holding a controller</summary>
        WithController = 0,

        /// <summary>Estimation of bones in hand while hand is empty (allowing full fist)</summary>
        WithoutController = 1,
    }
}