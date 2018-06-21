using UnityEngine;
using System.Collections;
using System;
using Valve.VR;
using System.Runtime.InteropServices;
using System.Collections.Generic;

public class SteamVR_Input_Action_Skeleton : SteamVR_Input_Action_Pose
{
    public const int numBones = 31;

    protected List<Vector3[]> bonePositions = new List<Vector3[]>();
    protected List<Quaternion[]> boneRotations = new List<Quaternion[]>();
    protected List<Vector3[]> lastBonePositions = new List<Vector3[]>();
    protected List<Quaternion[]> lastBoneRotations = new List<Quaternion[]>();

    protected Dictionary<SteamVR_Input_Input_Sources, EVRSkeletalMotionRange> rangeOfMotion = new Dictionary<SteamVR_Input_Input_Sources, EVRSkeletalMotionRange>(new SteamVREnumEqualityComparer<SteamVR_Input_Input_Sources>());

    protected VRBoneTransform_t[] tempBoneTransforms = new VRBoneTransform_t[numBones];
    protected InputSkeletalActionData_t tempSkeletonActionData = new InputSkeletalActionData_t();
    protected uint skeletonActionData_size = 0;

    protected Dictionary<SteamVR_Input_Input_Sources, EVRSkeletalTransformSpace> skeletalTransformSpace = new Dictionary<SteamVR_Input_Input_Sources, EVRSkeletalTransformSpace>(new SteamVREnumEqualityComparer<SteamVR_Input_Input_Sources>());

    public override void Initialize()
    {
        base.Initialize();
        skeletonActionData_size = (uint)Marshal.SizeOf(tempSkeletonActionData);
    }

    protected override void InitializeDictionaries(SteamVR_Input_Input_Sources source)
    {
        base.InitializeDictionaries(source);

        bonePositions.Add(new Vector3[numBones]);
        boneRotations.Add(new Quaternion[numBones]);
        lastBonePositions.Add(new Vector3[numBones]);
        lastBoneRotations.Add(new Quaternion[numBones]);
        rangeOfMotion.Add(source, EVRSkeletalMotionRange.WithController);
        skeletalTransformSpace.Add(source, EVRSkeletalTransformSpace.Parent);
    }

    public override void UpdateValue(SteamVR_Input_Input_Sources inputSource)
    {
        UpdateValue(inputSource, false);
    }

    public override void UpdateValue(SteamVR_Input_Input_Sources inputSource, bool skipStateAndEventUpdates)
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

        EVRInputError err = OpenVR.Input.GetSkeletalActionData(handle, ref tempSkeletonActionData, skeletonActionData_size, SteamVR_Input_Input_Source.GetHandle(inputSource));
        if (err != EVRInputError.None)
            Debug.LogError("GetSkeletalActionData error (" + fullPath + "): " + err.ToString() + " handle: " + handle.ToString());

        active[inputSource] = tempSkeletonActionData.bActive;
        activeOrigin[inputSource] = tempSkeletonActionData.activeOrigin;

        if (active[inputSource])
        {
            err = OpenVR.Input.GetSkeletalBoneData(handle, skeletalTransformSpace[inputSource], rangeOfMotion[inputSource], tempBoneTransforms, SteamVR_Input_Input_Source.GetHandle(inputSource));
            if (err != EVRInputError.None)
                Debug.LogError("GetSkeletalBoneData error (" + fullPath + "): " + err.ToString() + " handle: " + handle.ToString());

            for (int boneIndex = 0; boneIndex < tempBoneTransforms.Length; boneIndex++)
            {
                // Convert the transform from SteamVR's coordinate system to Unity's coordinate system.
                // ie: flip the X axis
                bonePositions[inputSourceInt][boneIndex].x = -tempBoneTransforms[boneIndex].position.v0;
                bonePositions[inputSourceInt][boneIndex].y = tempBoneTransforms[boneIndex].position.v1;
                bonePositions[inputSourceInt][boneIndex].z = tempBoneTransforms[boneIndex].position.v2;

                boneRotations[inputSourceInt][boneIndex].x = tempBoneTransforms[boneIndex].orientation.x;
                boneRotations[inputSourceInt][boneIndex].y = -tempBoneTransforms[boneIndex].orientation.y;
                boneRotations[inputSourceInt][boneIndex].z = -tempBoneTransforms[boneIndex].orientation.z;
                boneRotations[inputSourceInt][boneIndex].w = tempBoneTransforms[boneIndex].orientation.w;
            }
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
    }


    public Vector3[] GetBonePositions(SteamVR_Input_Input_Sources inputSource = SteamVR_Input_Input_Sources.Any)
    {
        return bonePositions[(int)inputSource];
    }
    public Quaternion[] GetBoneRotations(SteamVR_Input_Input_Sources inputSource = SteamVR_Input_Input_Sources.Any)
    {
        return boneRotations[(int)inputSource];
    }
    public Vector3[] GetLastBonePositions(SteamVR_Input_Input_Sources inputSource = SteamVR_Input_Input_Sources.Any)
    {
        return lastBonePositions[(int)inputSource];
    }
    public Quaternion[] GetLastBoneRotations(SteamVR_Input_Input_Sources inputSource = SteamVR_Input_Input_Sources.Any)
    {
        return lastBoneRotations[(int)inputSource];
    }

    public void SetRangeOfMotion(SteamVR_Input_Input_Sources inputSource, EVRSkeletalMotionRange range)
    {
        rangeOfMotion[inputSource] = range;
    }
    public void SetSkeletalTransformSpace(SteamVR_Input_Input_Sources inputSource, EVRSkeletalTransformSpace space)
    {
        skeletalTransformSpace[inputSource] = space;
    }
}