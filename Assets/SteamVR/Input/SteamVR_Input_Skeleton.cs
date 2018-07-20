//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: For controlling in-game objects with tracked devices.
//
//=============================================================================

using System.Collections;
using UnityEngine;
using Valve.VR;

public class SteamVR_Input_Skeleton : MonoBehaviour
{
    [DefaultInputAction("Skeleton", "default", "inputSource")]
    public SteamVR_Input_Action_Skeleton skeletonAction;

    public SteamVR_Input_Input_Sources inputSource;

    public EVRSkeletalMotionRange rangeOfMotion = EVRSkeletalMotionRange.WithoutController;

    public EVRSkeletalMotionRange? temporaryRangeOfMotion = null;

    [Tooltip("This needs to be in the order of: root -> thumb, index, middle, ring, pinky")]
    public Transform skeletonRoot;

    [Tooltip("If not set, relative to parent")]
    public Transform origin;

    [Tooltip("Set to true if you want this script to update its position and rotation. False if this will be handled elsewhere")]
    public bool updatePose = true;

    [Range(0, 1)]
    [Tooltip("Modify this to blend between animations setup on the hand")]
    public float skeletonBlend = 1f;

    [Tooltip("Is this rendermodel a mirror of another one?")]
    public MirrorType mirroring;

    public bool isActive { get { return skeletonAction.GetActive(inputSource); } }

    protected Transform[] bones;

    public Transform root { get { return bones[SteamVR_Input_SkeletonJointIndexes.root]; } }
    public Transform wrist { get { return bones[SteamVR_Input_SkeletonJointIndexes.wrist]; } }
    public Transform indexMetacarpal { get { return bones[SteamVR_Input_SkeletonJointIndexes.indexMetacarpal]; } }
    public Transform indexProximal { get { return bones[SteamVR_Input_SkeletonJointIndexes.indexProximal]; } }
    public Transform indexMiddle { get { return bones[SteamVR_Input_SkeletonJointIndexes.indexMiddle]; } }
    public Transform indexDistal { get { return bones[SteamVR_Input_SkeletonJointIndexes.indexDistal]; } }
    public Transform indexTip { get { return bones[SteamVR_Input_SkeletonJointIndexes.indexTip]; } }
    public Transform middleMetacarpal { get { return bones[SteamVR_Input_SkeletonJointIndexes.middleMetacarpal]; } }
    public Transform middleProximal { get { return bones[SteamVR_Input_SkeletonJointIndexes.middleProximal]; } }
    public Transform middleMiddle { get { return bones[SteamVR_Input_SkeletonJointIndexes.middleMiddle]; } }
    public Transform middleDistal { get { return bones[SteamVR_Input_SkeletonJointIndexes.middleDistal]; } }
    public Transform middleTip { get { return bones[SteamVR_Input_SkeletonJointIndexes.middleTip]; } }
    public Transform pinkyMetacarpal { get { return bones[SteamVR_Input_SkeletonJointIndexes.pinkyMetacarpal]; } }
    public Transform pinkyProximal { get { return bones[SteamVR_Input_SkeletonJointIndexes.pinkyProximal]; } }
    public Transform pinkyMiddle { get { return bones[SteamVR_Input_SkeletonJointIndexes.pinkyMiddle]; } }
    public Transform pinkyDistal { get { return bones[SteamVR_Input_SkeletonJointIndexes.pinkyDistal]; } }
    public Transform pinkyTip { get { return bones[SteamVR_Input_SkeletonJointIndexes.pinkyTip]; } }
    public Transform ringMetacarpal { get { return bones[SteamVR_Input_SkeletonJointIndexes.ringMetacarpal]; } }
    public Transform ringProximal { get { return bones[SteamVR_Input_SkeletonJointIndexes.ringProximal]; } }
    public Transform ringMiddle { get { return bones[SteamVR_Input_SkeletonJointIndexes.ringMiddle]; } }
    public Transform ringDistal { get { return bones[SteamVR_Input_SkeletonJointIndexes.ringDistal]; } }
    public Transform ringTip { get { return bones[SteamVR_Input_SkeletonJointIndexes.ringTip]; } }
    public Transform thumbMetacarpal { get { return bones[SteamVR_Input_SkeletonJointIndexes.thumbMetacarpal]; } } //doesn't exist - mapped to proximal
    public Transform thumbProximal { get { return bones[SteamVR_Input_SkeletonJointIndexes.thumbProximal]; } }
    public Transform thumbMiddle { get { return bones[SteamVR_Input_SkeletonJointIndexes.thumbMiddle]; } }
    public Transform thumbDistal { get { return bones[SteamVR_Input_SkeletonJointIndexes.thumbDistal]; } }
    public Transform thumbTip { get { return bones[SteamVR_Input_SkeletonJointIndexes.thumbTip]; } }
    public Transform thumbAux { get { return bones[SteamVR_Input_SkeletonJointIndexes.thumbAux]; } }
    public Transform indexAux { get { return bones[SteamVR_Input_SkeletonJointIndexes.indexAux]; } }
    public Transform middleAux { get { return bones[SteamVR_Input_SkeletonJointIndexes.middleAux]; } }
    public Transform ringAux { get { return bones[SteamVR_Input_SkeletonJointIndexes.ringAux]; } }
    public Transform pinkyAux { get { return bones[SteamVR_Input_SkeletonJointIndexes.pinkyAux]; } }

    public Transform[] proximals { get; protected set; }
    public Transform[] tips { get; protected set; }
    public Transform[] aux { get; protected set; }

    protected Coroutine blendRoutine;
    protected Coroutine rangeOfMotionBlendRoutine;

    public bool isBlending
    {
        get
        {
            return blendRoutine != null;
        }
    }

    protected virtual void Awake()
    {
        bones = skeletonRoot.GetComponentsInChildren<Transform>();
        proximals = new Transform[] { indexProximal, middleProximal, pinkyProximal, ringProximal, thumbProximal };
        tips = new Transform[] { indexTip, middleTip, pinkyTip, ringTip, thumbTip };
        aux = new Transform[] { thumbAux, indexAux, middleAux, ringAux, pinkyAux };
    }

    protected virtual void OnEnable()
    {
        SteamVR_Input.OnSkeletonsUpdated += SteamVR_Input_OnSkeletonsUpdated;
    }

    protected virtual void OnDisable()
    {
        SteamVR_Input.OnSkeletonsUpdated -= SteamVR_Input_OnSkeletonsUpdated;
    }

    protected virtual void SteamVR_Input_OnSkeletonsUpdated(bool obj)
    {
        UpdateSkeleton();
    }

    protected virtual void UpdateSkeleton()
    {
        if (skeletonAction == null || skeletonAction.GetActive(inputSource) == false)
            return;

        if (updatePose)
            UpdatePose();

        if (rangeOfMotionBlendRoutine == null)
        {
            if (temporaryRangeOfMotion != null)
                skeletonAction.SetRangeOfMotion(inputSource, temporaryRangeOfMotion.Value);
            else
                skeletonAction.SetRangeOfMotion(inputSource, rangeOfMotion); //this may be a frame behind

            UpdateSkeletonTransforms();
        }
    }

    public void SetTemporaryRangeOfMotion(EVRSkeletalMotionRange newRangeOfMotion, float blendOverSeconds = 0.1f)
    {
        if (rangeOfMotion != newRangeOfMotion || temporaryRangeOfMotion != newRangeOfMotion)
        {
            TemporaryRangeOfMotionBlend(newRangeOfMotion, blendOverSeconds);
        }
    }

    public void ResetTemporaryRangeOfMotion(float blendOverSeconds = 0.1f)
    {
        ResetTemporaryRangeOfMotionBlend(blendOverSeconds);
    }

    public void SetRangeOfMotion(EVRSkeletalMotionRange newRangeOfMotion, float blendOverSeconds = 0.1f)
    {
        if (rangeOfMotion != newRangeOfMotion)
        {
            RangeOfMotionBlend(newRangeOfMotion, blendOverSeconds);
        }
    }

    public void BlendToSkeleton(float overTime = 0.1f)
    {
        BlendTo(1, overTime);
    }

    public void BlendToAnimation(float overTime = 0.1f)
    {
        BlendTo(0, overTime);
    }

    public void BlendTo(float blendToAmount, float overTime)
    {
        if (blendRoutine != null)
            StopCoroutine(blendRoutine);

        if (this.gameObject.activeInHierarchy)
            blendRoutine = StartCoroutine(DoBlendRoutine(blendToAmount, overTime));
    }

    protected IEnumerator DoBlendRoutine(float blendToAmount, float overTime)
    {
        float startTime = Time.time;
        float endTime = startTime + overTime;

        float startAmount = skeletonBlend;

        while (Time.time < endTime)
        {
            yield return null;
            skeletonBlend = Mathf.Lerp(startAmount, blendToAmount, (Time.time - startTime) / overTime);
        }

        skeletonBlend = blendToAmount;
        blendRoutine = null;
    }

    protected void RangeOfMotionBlend(EVRSkeletalMotionRange newRangeOfMotion, float blendOverSeconds)
    {
        if (rangeOfMotionBlendRoutine != null)
            StopCoroutine(rangeOfMotionBlendRoutine);

        EVRSkeletalMotionRange oldRangeOfMotion = rangeOfMotion;
        rangeOfMotion = newRangeOfMotion;

        if (this.gameObject.activeInHierarchy)
            rangeOfMotionBlendRoutine = StartCoroutine(DoRangeOfMotionBlend(oldRangeOfMotion, newRangeOfMotion, blendOverSeconds));
    }

    protected void TemporaryRangeOfMotionBlend(EVRSkeletalMotionRange newRangeOfMotion, float blendOverSeconds)
    {
        if (rangeOfMotionBlendRoutine != null)
            StopCoroutine(rangeOfMotionBlendRoutine);

        EVRSkeletalMotionRange oldRangeOfMotion = rangeOfMotion;
        if (temporaryRangeOfMotion != null)
            oldRangeOfMotion = temporaryRangeOfMotion.Value;

        temporaryRangeOfMotion = newRangeOfMotion;

        if (this.gameObject.activeInHierarchy)
            rangeOfMotionBlendRoutine = StartCoroutine(DoRangeOfMotionBlend(oldRangeOfMotion, newRangeOfMotion, blendOverSeconds));
    }

    protected void ResetTemporaryRangeOfMotionBlend(float blendOverSeconds)
    {
        if (temporaryRangeOfMotion != null)
        {
            if (rangeOfMotionBlendRoutine != null)
                StopCoroutine(rangeOfMotionBlendRoutine);

            EVRSkeletalMotionRange oldRangeOfMotion = temporaryRangeOfMotion.Value;

            EVRSkeletalMotionRange newRangeOfMotion = rangeOfMotion;

            temporaryRangeOfMotion = null;

            if (this.gameObject.activeInHierarchy)
                rangeOfMotionBlendRoutine = StartCoroutine(DoRangeOfMotionBlend(oldRangeOfMotion, newRangeOfMotion, blendOverSeconds));
        }
    }

    protected IEnumerator DoRangeOfMotionBlend(EVRSkeletalMotionRange oldRangeOfMotion, EVRSkeletalMotionRange newRangeOfMotion, float overTime)
    {
        float startTime = Time.time;
        float endTime = startTime + overTime;

        Vector3[] oldBonePositions;
        Quaternion[] oldBoneRotations;

        Vector3[] newBonePositions;
        Quaternion[] newBoneRotations;

        while (Time.time < endTime)
        {
            yield return null;
            float lerp = (Time.time - startTime) / overTime;

            if (skeletonBlend > 0)
            {
                skeletonAction.SetRangeOfMotion(inputSource, oldRangeOfMotion);
                skeletonAction.UpdateValue(inputSource, true);
                oldBonePositions = (Vector3[])GetBonePositions(inputSource).Clone();
                oldBoneRotations = (Quaternion[])GetBoneRotations(inputSource).Clone();

                skeletonAction.SetRangeOfMotion(inputSource, newRangeOfMotion);
                skeletonAction.UpdateValue(inputSource, true);
                newBonePositions = GetBonePositions(inputSource);
                newBoneRotations = GetBoneRotations(inputSource);

                for (int boneIndex = 0; boneIndex < bones.Length; boneIndex++)
                {
                    if (SteamVR_Utils.IsValid(newBoneRotations[boneIndex]) == false || SteamVR_Utils.IsValid(oldBoneRotations[boneIndex]) == false)
                    {
                        break;
                    }

                    Vector3 blendedRangeOfMotionPosition = Vector3.Lerp(oldBonePositions[boneIndex], newBonePositions[boneIndex], lerp);
                    Quaternion blendedRangeOfMotionRotation = Quaternion.Lerp(oldBoneRotations[boneIndex], newBoneRotations[boneIndex], lerp);

                    if (skeletonBlend < 1)
                    {
                        bones[boneIndex].localPosition = Vector3.Lerp(bones[boneIndex].localPosition, blendedRangeOfMotionPosition, skeletonBlend);
                        bones[boneIndex].localRotation = Quaternion.Lerp(bones[boneIndex].localRotation, blendedRangeOfMotionRotation, skeletonBlend);
                    }
                    else
                    {
                        bones[boneIndex].localPosition = blendedRangeOfMotionPosition;
                        bones[boneIndex].localRotation = blendedRangeOfMotionRotation;
                    }
                }
            }
        }


        rangeOfMotionBlendRoutine = null;
    }

    protected virtual void UpdateSkeletonTransforms()
    {
        if (skeletonBlend <= 0)
            return;

        Vector3[] bonePositions = GetBonePositions(inputSource);
        Quaternion[] boneRotations = GetBoneRotations(inputSource);



        if (skeletonBlend >= 1)
        {
            for (int boneIndex = 0; boneIndex < bones.Length; boneIndex++)
            {
                bones[boneIndex].localPosition = bonePositions[boneIndex];
                bones[boneIndex].localRotation = boneRotations[boneIndex];
            }
        }
        else
        {
            for (int boneIndex = 0; boneIndex < bones.Length; boneIndex++)
            {
                bones[boneIndex].localPosition = Vector3.Lerp(bones[boneIndex].localPosition, bonePositions[boneIndex], skeletonBlend);
                bones[boneIndex].localRotation = Quaternion.Lerp(bones[boneIndex].localRotation, boneRotations[boneIndex], skeletonBlend);
            }
        }
    }
    
    protected Vector3[] GetBonePositions(SteamVR_Input_Input_Sources inputSource)
    {
        Vector3[] rawSkeleton = skeletonAction.GetBonePositions(inputSource);
        if (mirroring == MirrorType.LeftToRight || mirroring == MirrorType.RightToLeft)
        {
            for (int boneIndex = 0; boneIndex < rawSkeleton.Length; boneIndex++)
            {
                if (boneIndex == SteamVR_Input_SkeletonJointIndexes.wrist || IsMetacarpal(boneIndex))
                {
                    rawSkeleton[boneIndex].Scale(new Vector3(-1, 1, 1));
                }
                else if (boneIndex != SteamVR_Input_SkeletonJointIndexes.root)
                {
                    rawSkeleton[boneIndex] = rawSkeleton[boneIndex] * -1;
                }
            }
        }

        return rawSkeleton;
    }

    protected Quaternion rightFlipAngle = Quaternion.AngleAxis(180, Vector3.right);
    protected Quaternion[] GetBoneRotations(SteamVR_Input_Input_Sources inputSource)
    {
        Quaternion[] rawSkeleton = skeletonAction.GetBoneRotations(inputSource);
        if (mirroring == MirrorType.LeftToRight || mirroring == MirrorType.RightToLeft)
        {
            for (int boneIndex = 0; boneIndex < rawSkeleton.Length; boneIndex++)
            {
                if (boneIndex == SteamVR_Input_SkeletonJointIndexes.wrist)
                {
                    rawSkeleton[boneIndex].y = rawSkeleton[boneIndex].y * -1;
                    rawSkeleton[boneIndex].z = rawSkeleton[boneIndex].z * -1;
                }

                if (IsMetacarpal(boneIndex))
                {
                    rawSkeleton[boneIndex] = rightFlipAngle * rawSkeleton[boneIndex];
                }
            }
        }

        return rawSkeleton;
    }

    protected virtual void UpdatePose()
    {
        if (skeletonAction == null)
            return;

        if (origin == null)
            skeletonAction.UpdateTransform(inputSource, this.transform);
        else
        {
            this.transform.position = origin.TransformPoint(skeletonAction.GetLocalPosition(inputSource));
            this.transform.eulerAngles = origin.TransformDirection(skeletonAction.GetLocalRotation(inputSource).eulerAngles);
        }
    }

    public enum MirrorType
    {
        None,
        LeftToRight,
        RightToLeft
    }

    protected bool IsMetacarpal(int boneIndex)
    {
        return (boneIndex == SteamVR_Input_SkeletonJointIndexes.indexMetacarpal || 
            boneIndex == SteamVR_Input_SkeletonJointIndexes.middleMetacarpal || 
            boneIndex == SteamVR_Input_SkeletonJointIndexes.ringMetacarpal || 
            boneIndex == SteamVR_Input_SkeletonJointIndexes.pinkyMetacarpal || 
            boneIndex == SteamVR_Input_SkeletonJointIndexes.thumbMetacarpal);
    }
}



public class SteamVR_Input_SkeletonJointIndexes
{
    public const int root = 0;
    public const int wrist = 1;
    public const int thumbMetacarpal = 2;
    public const int thumbProximal = 2;
    public const int thumbMiddle = 3;
    public const int thumbDistal = 4;
    public const int thumbTip = 5;
    public const int indexMetacarpal = 6;
    public const int indexProximal = 7;
    public const int indexMiddle = 8;
    public const int indexDistal = 9;
    public const int indexTip = 10;
    public const int middleMetacarpal = 11;
    public const int middleProximal = 12;
    public const int middleMiddle = 13;
    public const int middleDistal = 14;
    public const int middleTip = 15;
    public const int ringMetacarpal = 16;
    public const int ringProximal = 17;
    public const int ringMiddle = 18;
    public const int ringDistal = 19;
    public const int ringTip = 20;
    public const int pinkyMetacarpal = 21;
    public const int pinkyProximal = 22;
    public const int pinkyMiddle = 23;
    public const int pinkyDistal = 24;
    public const int pinkyTip = 25;
    public const int thumbAux = 26;
    public const int indexAux = 27;
    public const int middleAux = 28;
    public const int ringAux = 29;
    public const int pinkyAux = 30;
}