//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: For controlling in-game objects with tracked devices.
//
//=============================================================================

using UnityEngine;
using Valve.VR;

public class SteamVR_Input_SkeletonCustom : MonoBehaviour
{
    public SteamVR_Input_Action_Skeleton skeletonAction;

    public SteamVR_Input_Input_Sources inputSource;

    public bool isActive { get { return skeletonAction.GetActive(inputSource); } }

    public Transform wrist;
    public Transform thumbMetacarpal;
    public Transform thumbProximal;
    public Transform thumbMiddle;
    public Transform thumbDistal;
    public Transform thumbTip;
    public Transform indexMetacarpal;
    public Transform indexProximal;
    public Transform indexMiddle;
    public Transform indexDistal;
    public Transform indexTip;
    public Transform middleMetacarpal;
    public Transform middleProximal;
    public Transform middleMiddle;
    public Transform middleDistal;
    public Transform middleTip;
    public Transform ringMetacarpal;
    public Transform ringProximal;
    public Transform ringMiddle;
    public Transform ringDistal;
    public Transform ringTip;
    public Transform pinkyMetacarpal;
    public Transform pinkyProximal;
    public Transform pinkyMiddle;
    public Transform pinkyDistal;
    public Transform pinkyTip;

    public Transform[] proximals { get; protected set; }
    public Transform[] tips { get; protected set; }

    protected virtual void Awake()
    {
        proximals = new Transform[] { indexProximal, middleProximal, pinkyProximal, ringProximal, thumbProximal };
        tips = new Transform[] { indexTip, middleTip, pinkyTip, ringTip, thumbTip };
    }

    private void OnEnable()
    {
        SteamVR_Input.OnSkeletonsUpdated += SteamVR_Input_OnSkeletonsUpdated;
    }

    private void OnDisable()
    {
        SteamVR_Input.OnSkeletonsUpdated -= SteamVR_Input_OnSkeletonsUpdated;
    }

    private void SteamVR_Input_OnSkeletonsUpdated(bool obj)
    {
        UpdateSkeleton();
    }

    protected virtual void UpdateSkeleton()
    {
        if (skeletonAction == null)
            return;

        Vector3[] bonePositions = skeletonAction.GetBonePositions(inputSource);
        Quaternion[] boneRotations = skeletonAction.GetBoneRotations(inputSource);

        if (wrist != null)
            SetTransform(wrist, SteamVR_Input_SkeletonJointIndexes.wrist, bonePositions, boneRotations);

        if (indexMetacarpal != null)
            SetTransform(indexMetacarpal, SteamVR_Input_SkeletonJointIndexes.indexMetacarpal, bonePositions, boneRotations);

        if (indexProximal != null)
            SetTransform(indexProximal, SteamVR_Input_SkeletonJointIndexes.indexProximal, bonePositions, boneRotations);

        if (indexMiddle != null)
            SetTransform(indexMiddle, SteamVR_Input_SkeletonJointIndexes.indexMiddle, bonePositions, boneRotations);

        if (indexDistal != null)
            SetTransform(indexDistal, SteamVR_Input_SkeletonJointIndexes.indexDistal, bonePositions, boneRotations);

        if (indexTip != null)
            SetTransform(indexTip, SteamVR_Input_SkeletonJointIndexes.indexTip, bonePositions, boneRotations);

        if (middleMetacarpal != null)
            SetTransform(middleMetacarpal, SteamVR_Input_SkeletonJointIndexes.middleMetacarpal, bonePositions, boneRotations);

        if (middleProximal != null)
            SetTransform(middleProximal, SteamVR_Input_SkeletonJointIndexes.middleProximal, bonePositions, boneRotations);

        if (middleMiddle != null)
            SetTransform(middleMiddle, SteamVR_Input_SkeletonJointIndexes.middleMiddle, bonePositions, boneRotations);

        if (middleDistal != null)
            SetTransform(middleDistal, SteamVR_Input_SkeletonJointIndexes.middleDistal, bonePositions, boneRotations);

        if (middleTip != null)
            SetTransform(middleTip, SteamVR_Input_SkeletonJointIndexes.middleTip, bonePositions, boneRotations);

        if (pinkyMetacarpal != null)
            SetTransform(pinkyMetacarpal, SteamVR_Input_SkeletonJointIndexes.pinkyMetacarpal, bonePositions, boneRotations);

        if (pinkyProximal != null)
            SetTransform(pinkyProximal, SteamVR_Input_SkeletonJointIndexes.pinkyProximal, bonePositions, boneRotations);

        if (pinkyMiddle != null)
            SetTransform(pinkyMiddle, SteamVR_Input_SkeletonJointIndexes.pinkyMiddle, bonePositions, boneRotations);

        if (pinkyDistal != null)
            SetTransform(pinkyDistal, SteamVR_Input_SkeletonJointIndexes.pinkyDistal, bonePositions, boneRotations);

        if (ringMetacarpal != null)
            SetTransform(ringMetacarpal, SteamVR_Input_SkeletonJointIndexes.ringMetacarpal, bonePositions, boneRotations);

        if (ringProximal != null)
            SetTransform(ringProximal, SteamVR_Input_SkeletonJointIndexes.ringProximal, bonePositions, boneRotations);

        if (ringMiddle != null)
            SetTransform(ringMiddle, SteamVR_Input_SkeletonJointIndexes.ringMiddle, bonePositions, boneRotations);

        if (ringDistal != null)
            SetTransform(ringDistal, SteamVR_Input_SkeletonJointIndexes.ringDistal, bonePositions, boneRotations);

        if (ringTip != null)
            SetTransform(ringTip, SteamVR_Input_SkeletonJointIndexes.ringTip, bonePositions, boneRotations);

        if (thumbMetacarpal != null)
            SetTransform(thumbMetacarpal, SteamVR_Input_SkeletonJointIndexes.thumbMetacarpal, bonePositions, boneRotations);

        if (thumbProximal != null)
            SetTransform(thumbProximal, SteamVR_Input_SkeletonJointIndexes.thumbProximal, bonePositions, boneRotations);

        if (thumbMiddle != null)
            SetTransform(thumbMiddle, SteamVR_Input_SkeletonJointIndexes.thumbMiddle, bonePositions, boneRotations);

        if (thumbDistal != null)
            SetTransform(thumbDistal, SteamVR_Input_SkeletonJointIndexes.thumbDistal, bonePositions, boneRotations);

        if (thumbTip != null)
            SetTransform(thumbTip, SteamVR_Input_SkeletonJointIndexes.thumbTip, bonePositions, boneRotations);
    }

    protected virtual void SetTransform(Transform toSetTransform, int boneIndex, Vector3[] bonePositions, Quaternion[] boneRotations)
    {
        toSetTransform.localPosition = bonePositions[boneIndex];
        toSetTransform.localRotation = boneRotations[boneIndex];
    }
}