//======= Copyright (c) Valve Corporation, All rights reserved. ===============

using System;
using System.Collections;
using UnityEngine;
using Valve.VR;

namespace Valve.VR
{
    public class SteamVR_Behaviour_Skeleton : MonoBehaviour
    {
        [Tooltip("If not set, will try to auto assign this based on 'Skeleton' + inputSource")]
        /// <summary>The action this component will use to update the model. Must be a Skeleton type action.</summary>
        public SteamVR_Action_Skeleton skeletonAction;

        /// <summary>The device this action should apply to. Any if the action is not device specific.</summary>
        [Tooltip("The device this action should apply to. Any if the action is not device specific.")]
        public SteamVR_Input_Sources inputSource;

        /// <summary>The range of motion you'd like the hand to move in. With controller is the best estimate of the fingers wrapped around a controller. Without is from a flat hand to a fist.</summary>
        [Tooltip("The range of motion you'd like the hand to move in. With controller is the best estimate of the fingers wrapped around a controller. Without is from a flat hand to a fist.")]
        public EVRSkeletalMotionRange rangeOfMotion = EVRSkeletalMotionRange.WithoutController;

        /// <summary>The root Transform of the skeleton. Needs to have a child (wrist) then wrist should have children in the order thumb, index, middle, ring, pinky</summary>
        [Tooltip("This needs to be in the order of: root -> wrist -> thumb, index, middle, ring, pinky")]
        public Transform skeletonRoot;

        /// <summary>The transform this transform should be relative to</summary>
        [Tooltip("If not set, relative to parent")]
        public Transform origin;

        /// <summary>Whether or not to update this transform's position and rotation inline with the skeleton transforms or if this is handled in another script</summary>
        [Tooltip("Set to true if you want this script to update its position and rotation. False if this will be handled elsewhere")]
        public bool updatePose = true;

        /// <summary>Check this to not set the positions of the bones. This is helpful for differently scaled skeletons.</summary>
        [Tooltip("Check this to not set the positions of the bones. This is helpful for differently scaled skeletons.")]
        public bool onlySetRotations = false;

        /// <summary>
        /// How much of a blend to apply to the transform positions and rotations. 
        /// Set to 0 for the transform orientation to be set by an animation. 
        /// Set to 1 for the transform orientation to be set by the skeleton action.
        /// </summary>
        [Range(0, 1)]
        [Tooltip("Modify this to blend between animations setup on the hand")]
        public float skeletonBlend = 1f;

        /// <summary>
        /// How much of a blend to apply to the transform positions and rotations between the normal position/rotation and the transform's
        /// Set to 0 for the transform orientation to be set to the skeleton position/rotation
        /// Set to 1 for the transform orientation to be set by the attached transform's position/rotation
        /// </summary>
        [Range(0, 1)]
        [Tooltip("Modify this to blend between skeleton root orientation and attached transform root orientation")]
        public float attachedTransformBlend = 0f;

        public Transform attachedToTransform;

        protected SteamVR_Skeleton_Pose_Hand blendToPose;


        /// <summary>Can be set to mirror the bone data across the x axis</summary>
        [Tooltip("Is this rendermodel a mirror of another one?")]
        public MirrorType mirroring;

        /// <summary>Returns whether this action is bound and the action set is active</summary>
        public bool isActive { get { return skeletonAction.GetActive(); } }


        /// <summary>An array of five 0-1 values representing how curled a finger is. 0 being straight, 1 being fully curled. Index 0 being thumb, index 4 being pinky</summary>
        public float[] fingerCurls { get { return skeletonAction.GetFingerCurls(); } }

        /// <summary>An 0-1 value representing how curled a finger is. 0 being straight, 1 being fully curled.</summary>
        public float thumbCurl { get { return skeletonAction.GetFingerCurl(SteamVR_Skeleton_FingerIndexEnum.thumb); } }

        /// <summary>An 0-1 value representing how curled a finger is. 0 being straight, 1 being fully curled.</summary>
        public float indexCurl { get { return skeletonAction.GetFingerCurl(SteamVR_Skeleton_FingerIndexEnum.index); } }

        /// <summary>An 0-1 value representing how curled a finger is. 0 being straight, 1 being fully curled.</summary>
        public float middleCurl { get { return skeletonAction.GetFingerCurl(SteamVR_Skeleton_FingerIndexEnum.middle); } }

        /// <summary>An 0-1 value representing how curled a finger is. 0 being straight, 1 being fully curled.</summary>
        public float ringCurl { get { return skeletonAction.GetFingerCurl(SteamVR_Skeleton_FingerIndexEnum.ring); } }

        /// <summary>An 0-1 value representing how curled a finger is. 0 being straight, 1 being fully curled.</summary>
        public float pinkyCurl { get { return skeletonAction.GetFingerCurl(SteamVR_Skeleton_FingerIndexEnum.pinky); } }


        public Transform root { get { return bones[SteamVR_Skeleton_JointIndexes.root]; } }
        public Transform wrist { get { return bones[SteamVR_Skeleton_JointIndexes.wrist]; } }
        public Transform indexMetacarpal { get { return bones[SteamVR_Skeleton_JointIndexes.indexMetacarpal]; } }
        public Transform indexProximal { get { return bones[SteamVR_Skeleton_JointIndexes.indexProximal]; } }
        public Transform indexMiddle { get { return bones[SteamVR_Skeleton_JointIndexes.indexMiddle]; } }
        public Transform indexDistal { get { return bones[SteamVR_Skeleton_JointIndexes.indexDistal]; } }
        public Transform indexTip { get { return bones[SteamVR_Skeleton_JointIndexes.indexTip]; } }
        public Transform middleMetacarpal { get { return bones[SteamVR_Skeleton_JointIndexes.middleMetacarpal]; } }
        public Transform middleProximal { get { return bones[SteamVR_Skeleton_JointIndexes.middleProximal]; } }
        public Transform middleMiddle { get { return bones[SteamVR_Skeleton_JointIndexes.middleMiddle]; } }
        public Transform middleDistal { get { return bones[SteamVR_Skeleton_JointIndexes.middleDistal]; } }
        public Transform middleTip { get { return bones[SteamVR_Skeleton_JointIndexes.middleTip]; } }
        public Transform pinkyMetacarpal { get { return bones[SteamVR_Skeleton_JointIndexes.pinkyMetacarpal]; } }
        public Transform pinkyProximal { get { return bones[SteamVR_Skeleton_JointIndexes.pinkyProximal]; } }
        public Transform pinkyMiddle { get { return bones[SteamVR_Skeleton_JointIndexes.pinkyMiddle]; } }
        public Transform pinkyDistal { get { return bones[SteamVR_Skeleton_JointIndexes.pinkyDistal]; } }
        public Transform pinkyTip { get { return bones[SteamVR_Skeleton_JointIndexes.pinkyTip]; } }
        public Transform ringMetacarpal { get { return bones[SteamVR_Skeleton_JointIndexes.ringMetacarpal]; } }
        public Transform ringProximal { get { return bones[SteamVR_Skeleton_JointIndexes.ringProximal]; } }
        public Transform ringMiddle { get { return bones[SteamVR_Skeleton_JointIndexes.ringMiddle]; } }
        public Transform ringDistal { get { return bones[SteamVR_Skeleton_JointIndexes.ringDistal]; } }
        public Transform ringTip { get { return bones[SteamVR_Skeleton_JointIndexes.ringTip]; } }
        public Transform thumbMetacarpal { get { return bones[SteamVR_Skeleton_JointIndexes.thumbMetacarpal]; } } //doesn't exist - mapped to proximal
        public Transform thumbProximal { get { return bones[SteamVR_Skeleton_JointIndexes.thumbProximal]; } }
        public Transform thumbMiddle { get { return bones[SteamVR_Skeleton_JointIndexes.thumbMiddle]; } }
        public Transform thumbDistal { get { return bones[SteamVR_Skeleton_JointIndexes.thumbDistal]; } }
        public Transform thumbTip { get { return bones[SteamVR_Skeleton_JointIndexes.thumbTip]; } }
        public Transform thumbAux { get { return bones[SteamVR_Skeleton_JointIndexes.thumbAux]; } }
        public Transform indexAux { get { return bones[SteamVR_Skeleton_JointIndexes.indexAux]; } }
        public Transform middleAux { get { return bones[SteamVR_Skeleton_JointIndexes.middleAux]; } }
        public Transform ringAux { get { return bones[SteamVR_Skeleton_JointIndexes.ringAux]; } }
        public Transform pinkyAux { get { return bones[SteamVR_Skeleton_JointIndexes.pinkyAux]; } }

        /// <summary>An array of all the finger proximal joint transforms</summary>
        public Transform[] proximals { get; protected set; }

        /// <summary>An array of all the finger middle joint transforms</summary>
        public Transform[] middles { get; protected set; }

        /// <summary>An array of all the finger distal joint transforms</summary>
        public Transform[] distals { get; protected set; }

        /// <summary>An array of all the finger tip transforms</summary>
        public Transform[] tips { get; protected set; }

        /// <summary>An array of all the finger aux transforms</summary>
        public Transform[] auxs { get; protected set; }

        protected Coroutine blendRoutine;
        protected Coroutine rangeOfMotionBlendRoutine;
        protected Coroutine attachRoutine;

        protected Transform[] bones;

        /// <summary>The range of motion that is set temporarily (call ResetTemporaryRangeOfMotion to reset to rangeOfMotion)</summary>
        protected EVRSkeletalMotionRange? temporaryRangeOfMotion = null;

        /// <summary>Returns true if we are in the process of blending the skeletonBlend field (between animation and bone data)</summary>
        public bool isBlending
        {
            get
            {
                return blendRoutine != null;
            }
        }

        public float predictedSecondsFromNow
        {
            get
            {
                return skeletonAction.predictedSecondsFromNow;
            }

            set
            {
                skeletonAction.predictedSecondsFromNow = value;
            }
        }

        public SteamVR_ActionSet actionSet
        {
            get
            {
                return skeletonAction.actionSet;
            }
        }

        public SteamVR_ActionDirections direction
        {
            get
            {
                return skeletonAction.direction;
            }
        }

        protected virtual void Awake()
        {
            AssignBonesArray();

            proximals = new Transform[] { thumbProximal, indexProximal, middleProximal, ringProximal, pinkyProximal };
            middles = new Transform[] { thumbMiddle, indexMiddle, middleMiddle, ringMiddle, pinkyMiddle };
            distals = new Transform[] { thumbDistal, indexDistal, middleDistal, ringDistal, pinkyDistal };
            tips = new Transform[] { thumbTip, indexTip, middleTip, ringTip, pinkyTip };
            auxs = new Transform[] { thumbAux, indexAux, middleAux, ringAux, pinkyAux };

            CheckSkeletonAction();
        }

        protected virtual void CheckSkeletonAction()
        {
            if (skeletonAction == null)
                skeletonAction = SteamVR_Input.GetAction<SteamVR_Action_Skeleton>("Skeleton" + inputSource.ToString());
        }

        protected virtual void AssignBonesArray()
        {
            bones = skeletonRoot.GetComponentsInChildren<Transform>();
        }

        protected virtual void OnEnable()
        {
            SteamVR_Input.onSkeletonsUpdated += SteamVR_Input_OnSkeletonsUpdated;
        }

        protected virtual void OnDisable()
        {
            SteamVR_Input.onSkeletonsUpdated -= SteamVR_Input_OnSkeletonsUpdated;
        }

        protected virtual void SteamVR_Input_OnSkeletonsUpdated(bool skipSendingEvents)
        {
            UpdateSkeleton();
        }

        protected virtual void UpdateSkeleton()
        {
            if (skeletonAction == null || skeletonAction.active == false)
                return;

            if (updatePose)
                UpdatePose();

            if (rangeOfMotionBlendRoutine == null)
            {
                if (temporaryRangeOfMotion != null)
                    skeletonAction.SetRangeOfMotion(temporaryRangeOfMotion.Value);
                else
                    skeletonAction.SetRangeOfMotion(rangeOfMotion); //this may be a frame behind

                UpdateSkeletonTransforms();
            }
        }

        /// <summary>
        /// Sets a temporary range of motion for this action that can easily be reset (using ResetTemporaryRangeOfMotion).
        /// This is useful for short range of motion changes, for example picking up a controller shaped object
        /// </summary>
        /// <param name="newRangeOfMotion">The new range of motion you want to apply (temporarily)</param>
        /// <param name="blendOverSeconds">How long you want the blend to the new range of motion to take (in seconds)</param>
        public void SetTemporaryRangeOfMotion(EVRSkeletalMotionRange newRangeOfMotion, float blendOverSeconds = 0.1f)
        {
            if (rangeOfMotion != newRangeOfMotion || temporaryRangeOfMotion != newRangeOfMotion)
            {
                TemporaryRangeOfMotionBlend(newRangeOfMotion, blendOverSeconds);
            }
        }

        /// <summary>
        /// Resets the previously set temporary range of motion. 
        /// Will return to the range of motion defined by the rangeOfMotion field.
        /// </summary>
        /// <param name="blendOverSeconds">How long you want the blend to the standard range of motion to take (in seconds)</param>
        public void ResetTemporaryRangeOfMotion(float blendOverSeconds = 0.1f)
        {
            ResetTemporaryRangeOfMotionBlend(blendOverSeconds);
        }

        /// <summary>
        /// Permanently sets the range of motion for this component.
        /// </summary>
        /// <param name="newRangeOfMotion">
        /// The new range of motion to be set. 
        /// WithController being the best estimation of where fingers are wrapped around the controller (pressing buttons, etc). 
        /// WithoutController being a range between a flat hand and a fist.</param>
        /// <param name="blendOverSeconds">How long you want the blend to the new range of motion to take (in seconds)</param>
        public void SetRangeOfMotion(EVRSkeletalMotionRange newRangeOfMotion, float blendOverSeconds = 0.1f)
        {
            if (rangeOfMotion != newRangeOfMotion)
            {
                RangeOfMotionBlend(newRangeOfMotion, blendOverSeconds);
            }
        }

        /// <summary>
        /// Blend from the current skeletonBlend amount to full bone data. (skeletonBlend = 1)
        /// </summary>
        /// <param name="overTime">How long you want the blend to take (in seconds)</param>
        public void BlendToSkeleton(float overTime = 0.1f, bool detachTransform = false)
        {
            BlendTo(1, overTime);
        }

        /// <summary>
        /// Blend from the current skeletonBlend amount to pose animation. (skeletonBlend = 0)
        /// Note: This will ignore the root position and rotation of the pose.
        /// </summary>
        /// <param name="overTime">How long you want the blend to take (in seconds)</param>
        public void BlendToPose(SteamVR_Skeleton_Pose pose, float overTime = 0.1f)
        {
            blendToPose = pose.GetHand(inputSource);
            BlendTo(0, overTime);
        }

        /// <summary>
        /// Blend from the current skeletonBlend amount to pose animation. (skeletonBlend = 0)
        /// Note: This will ignore the root position and rotation of the pose.
        /// </summary>
        /// <param name="overTime">How long you want the blend to take (in seconds)</param>
        /// <param name="attachToTransform">If you have a positiona and rotation offset for your pose you can attach it to a particular transform</param>
        public void BlendToPose(SteamVR_Skeleton_Pose pose, Transform attachToTransform, float overTime = 0.1f)
        {
            blendToPose = pose.GetHand(inputSource);
            BlendTo(0, overTime);
        }

        /// <summary>
        /// Blend from the current skeletonBlend amount to full animation data (no bone data. skeletonBlend = 0)
        /// </summary>
        /// <param name="overTime">How long you want the blend to take (in seconds)</param>
        public void BlendToAnimation(float overTime = 0.1f)
        {
            BlendTo(0, overTime);
        }

        /// <summary>
        /// Blend from the current skeletonBlend amount to a specified new amount.
        /// </summary>
        /// <param name="blendToAmount">The amount of blend you want to apply. 
        /// 0 being fully set by animations, 1 being fully set by bone data from the action.</param>
        /// <param name="overTime">How long you want the blend to take (in seconds)</param>
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
            {
                rangeOfMotionBlendRoutine = StartCoroutine(DoRangeOfMotionBlend(oldRangeOfMotion, newRangeOfMotion, blendOverSeconds));
            }
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
            {
                rangeOfMotionBlendRoutine = StartCoroutine(DoRangeOfMotionBlend(oldRangeOfMotion, newRangeOfMotion, blendOverSeconds));
            }
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
                {
                    rangeOfMotionBlendRoutine = StartCoroutine(DoRangeOfMotionBlend(oldRangeOfMotion, newRangeOfMotion, blendOverSeconds));
                }
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
                    skeletonAction.SetRangeOfMotion(oldRangeOfMotion);
                    skeletonAction.UpdateValueWithoutEvents();
                    oldBonePositions = (Vector3[])GetBonePositions().Clone();
                    oldBoneRotations = (Quaternion[])GetBoneRotations().Clone();

                    skeletonAction.SetRangeOfMotion(newRangeOfMotion);
                    skeletonAction.UpdateValueWithoutEvents();
                    newBonePositions = GetBonePositions();
                    newBoneRotations = GetBoneRotations();

                    for (int boneIndex = 0; boneIndex < bones.Length; boneIndex++)
                    {
                        if (bones[boneIndex] == null)
                            continue;

                        if (SteamVR_Utils.IsValid(newBoneRotations[boneIndex]) == false || SteamVR_Utils.IsValid(oldBoneRotations[boneIndex]) == false)
                        {
                            continue;
                        }

                        Vector3 blendedRangeOfMotionPosition = Vector3.Lerp(oldBonePositions[boneIndex], newBonePositions[boneIndex], lerp);
                        Quaternion blendedRangeOfMotionRotation = Quaternion.Lerp(oldBoneRotations[boneIndex], newBoneRotations[boneIndex], lerp);

                        if (skeletonBlend < 1)
                        {
                            if (blendToPose != null)
                            {
                                SetBonePosition(boneIndex, Vector3.Lerp(blendToPose.bonePositions[boneIndex], blendedRangeOfMotionPosition, skeletonBlend));
                                SetBoneRotation(boneIndex, Quaternion.Lerp(GetBlendPoseForBone(boneIndex, blendedRangeOfMotionRotation), blendedRangeOfMotionRotation, skeletonBlend));
                            }
                            else
                            {
                                SetBonePosition(boneIndex, Vector3.Lerp(bones[boneIndex].localPosition, blendedRangeOfMotionPosition, skeletonBlend));
                                SetBoneRotation(boneIndex, Quaternion.Lerp(bones[boneIndex].localRotation, blendedRangeOfMotionRotation, skeletonBlend));
                            }
                        }
                        else
                        {
                            SetBonePosition(boneIndex, blendedRangeOfMotionPosition);
                            SetBoneRotation(boneIndex, blendedRangeOfMotionRotation);
                        }
                    }
                }
            }

            rangeOfMotionBlendRoutine = null;
        }

        protected virtual Quaternion GetBlendPoseForBone(int boneIndex, Quaternion skeletonRotation)
        {
            SteamVR_Skeleton_FingerExtensionTypes movementType = blendToPose.GetMovementTypeForBone(boneIndex);
            Quaternion poseRotation = blendToPose.boneRotations[boneIndex];

            if (movementType == SteamVR_Skeleton_FingerExtensionTypes.Free)
                return skeletonRotation;
            else if (movementType == SteamVR_Skeleton_FingerExtensionTypes.Static)
                return poseRotation;
            else
            {
                Vector3 localForward = bones[boneIndex].localRotation * Vector3.forward;
                //z rotation is curl
                Vector3 poseForward = poseRotation * localForward;
                Vector3 skeletonForward = skeletonRotation * localForward;

                float poseAngle = Mathf.Atan2(poseForward.x, poseForward.y) * Mathf.Rad2Deg;
                float skeletonAngle = Mathf.Atan2(skeletonForward.x, skeletonForward.y) * Mathf.Rad2Deg;

                float angleDifference = Mathf.DeltaAngle(poseAngle, skeletonAngle);

                if (movementType == SteamVR_Skeleton_FingerExtensionTypes.Contract)
                {
                    if (angleDifference > 0)
                        return poseRotation * Quaternion.Euler(0, 0, -angleDifference);
                    else
                        return poseRotation;
                }
                else if (movementType == SteamVR_Skeleton_FingerExtensionTypes.Extend)
                {
                    if (angleDifference < 0)
                        return poseRotation * Quaternion.Euler(0, 0, -angleDifference);
                    else
                        return poseRotation;
                }
            }

            return Quaternion.identity;
        }

        protected virtual void UpdateSkeletonTransforms()
        {
            Vector3[] bonePositions = GetBonePositions();
            Quaternion[] boneRotations = GetBoneRotations();

            if (skeletonBlend <= 0)
            {
                if (blendToPose != null)
                {
                    for (int boneIndex = 0; boneIndex < bones.Length; boneIndex++)
                    {
                        if (bones[boneIndex] == null)
                            continue;

                        if ((boneIndex == SteamVR_Skeleton_JointIndexes.wrist && blendToPose.ignoreWristPoseData) ||
                            (boneIndex == SteamVR_Skeleton_JointIndexes.root && blendToPose.ignoreRootPoseData))
                        {
                            SetBonePosition(boneIndex, bonePositions[boneIndex]);
                            SetBoneRotation(boneIndex, boneRotations[boneIndex]);
                        }
                        else
                        {
                            Quaternion poseRotation = GetBlendPoseForBone(boneIndex, boneRotations[boneIndex]);

                            SetBonePosition(boneIndex, blendToPose.bonePositions[boneIndex]);
                            SetBoneRotation(boneIndex, poseRotation);
                        }
                    }
                }
            }
            else if (skeletonBlend >= 1)
            {
                for (int boneIndex = 0; boneIndex < bones.Length; boneIndex++)
                {
                    if (bones[boneIndex] == null)
                        continue;

                    SetBonePosition(boneIndex, bonePositions[boneIndex]);
                    SetBoneRotation(boneIndex, boneRotations[boneIndex]);
                }
            }
            else
            {
                for (int boneIndex = 0; boneIndex < bones.Length; boneIndex++)
                {
                    if (bones[boneIndex] == null)
                        continue;
                    
                    if (blendToPose != null)
                    {
                        if ((boneIndex == SteamVR_Skeleton_JointIndexes.wrist && blendToPose.ignoreWristPoseData) ||
                            (boneIndex == SteamVR_Skeleton_JointIndexes.root && blendToPose.ignoreRootPoseData))
                        {
                            SetBonePosition(boneIndex, bonePositions[boneIndex]);
                            SetBoneRotation(boneIndex, boneRotations[boneIndex]);
                        }
                        else
                        {
                            Quaternion poseRotation = GetBlendPoseForBone(boneIndex, boneRotations[boneIndex]);

                            SetBonePosition(boneIndex, Vector3.Lerp(blendToPose.bonePositions[boneIndex], bonePositions[boneIndex], skeletonBlend));
                            SetBoneRotation(boneIndex, Quaternion.Lerp(poseRotation, boneRotations[boneIndex], skeletonBlend));
                        }
                    }
                    else
                    {
                        SetBonePosition(boneIndex, Vector3.Lerp(bones[boneIndex].localPosition, bonePositions[boneIndex], skeletonBlend));
                        SetBoneRotation(boneIndex, Quaternion.Lerp(bones[boneIndex].localRotation, boneRotations[boneIndex], skeletonBlend));
                    }
                }
            }
        }

        protected virtual void SetBonePosition(int boneIndex, Vector3 localPosition)
        {
            if (onlySetRotations == false) //ignore position sets if we're only setting rotations
                bones[boneIndex].localPosition = localPosition;
        }

        protected virtual void SetBoneRotation(int boneIndex, Quaternion localRotation)
        {
            bones[boneIndex].localRotation = localRotation;
        }

        /// <summary>
        /// Gets the transform for a bone by the joint index. Joint indexes specified in: SteamVR_Skeleton_JointIndexes 
        /// </summary>
        /// <param name="joint">The joint index of the bone. Specified in SteamVR_Skeleton_JointIndexes</param>
        public virtual Transform GetBone(int joint)
        {
            if (bones == null || bones.Length == 0)
                Awake();

            return bones[joint];
        }


        /// <summary>
        /// Gets the position of the transform for a bone by the joint index. Joint indexes specified in: SteamVR_Skeleton_JointIndexes 
        /// </summary>
        /// <param name="joint">The joint index of the bone. Specified in SteamVR_Skeleton_JointIndexes</param>
        /// <param name="local">true to get the localspace position for the joint (position relative to this joint's parent)</param>
        public Vector3 GetBonePosition(int joint, bool local = false)
        {
            if (local)
                return bones[joint].localPosition;
            else
                return bones[joint].position;
        }

        /// <summary>
        /// Gets the rotation of the transform for a bone by the joint index. Joint indexes specified in: SteamVR_Skeleton_JointIndexes 
        /// </summary>
        /// <param name="joint">The joint index of the bone. Specified in SteamVR_Skeleton_JointIndexes</param>
        /// <param name="local">true to get the localspace rotation for the joint (rotation relative to this joint's parent)</param>
        public Quaternion GetBoneRotation(int joint, bool local = false)
        {
            if (local)
                return bones[joint].localRotation;
            else
                return bones[joint].rotation;
        }

        protected Vector3[] GetBonePositions()
        {
            Vector3[] rawSkeleton = skeletonAction.GetBonePositions();
            if (mirroring == MirrorType.LeftToRight || mirroring == MirrorType.RightToLeft)
            {
                for (int boneIndex = 0; boneIndex < rawSkeleton.Length; boneIndex++)
                {
                    if (boneIndex == SteamVR_Skeleton_JointIndexes.wrist || IsMetacarpal(boneIndex))
                    {
                        rawSkeleton[boneIndex].Scale(new Vector3(-1, 1, 1));
                    }
                    else if (boneIndex != SteamVR_Skeleton_JointIndexes.root)
                    {
                        rawSkeleton[boneIndex] = rawSkeleton[boneIndex] * -1;
                    }
                }
            }

            return rawSkeleton;
        }

        protected Quaternion rightFlipAngle = Quaternion.AngleAxis(180, Vector3.right);
        protected Quaternion[] GetBoneRotations()
        {
            Quaternion[] rawSkeleton = skeletonAction.GetBoneRotations();
            if (mirroring == MirrorType.LeftToRight || mirroring == MirrorType.RightToLeft)
            {
                for (int boneIndex = 0; boneIndex < rawSkeleton.Length; boneIndex++)
                {
                    if (boneIndex == SteamVR_Skeleton_JointIndexes.wrist)
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

        /*
        protected virtual void UpdatePose()
        {
            if (skeletonAction == null)
                return;

            Vector3 skeletonPosition = skeletonAction.GetLocalPosition();
            Quaternion skeletonRotation = skeletonAction.GetLocalRotation();
            if (origin == null)
            {
                if (this.transform.parent != null)
                {
                    skeletonPosition = this.transform.parent.TransformPoint(skeletonPosition);
                    skeletonRotation = this.transform.parent.rotation * skeletonRotation;
                }
            }
            else
            {
                skeletonPosition = origin.TransformPoint(skeletonPosition);
                skeletonRotation = origin.rotation * skeletonRotation;
            }

            if (attachedTransformBlend <= 0)
            {
                this.transform.position = skeletonPosition;
                this.transform.rotation = skeletonRotation;
            }
            else
            {
                Vector3 attachedTransformPosition = attachedToTransform.position;
                Quaternion attachedTransformRotation = attachedToTransform.rotation;

                if (blendToPose != null)
                {
                    attachedTransformPosition = attachedToTransform.TransformPoint(blendToPose.position);
                    attachedTransformRotation = attachedTransformRotation * blendToPose.rotation;
                }

                if (attachedTransformBlend >= 1)
                {
                    this.transform.position = attachedTransformPosition;
                    this.transform.rotation = attachedTransformRotation;
                }
                else
                {
                    Vector3 targetPosition = Vector3.Lerp(skeletonPosition, attachedTransformPosition, attachedTransformBlend);
                    Quaternion targetRotation = Quaternion.Lerp(skeletonRotation, attachedTransformRotation, attachedTransformBlend);

                    if (attachRoutine != null)
                    {
                        this.transform.position = Vector3.Lerp(attachingStartPosition, targetPosition, attachedTransformBlend);
                        this.transform.rotation = Quaternion.Lerp(attachingStartRotation, targetRotation, attachedTransformBlend);
                    }
                    else
                    {
                        this.transform.position = targetPosition;
                        this.transform.rotation = targetRotation;
                    }
                }
            }

            attaching = attachRoutine != null;
        }
        */
        protected virtual void UpdatePose()
        {
            if (skeletonAction == null)
                return;

            Vector3 skeletonPosition = skeletonAction.GetLocalPosition();
            Quaternion skeletonRotation = skeletonAction.GetLocalRotation();
            if (origin == null)
            {
                if (this.transform.parent != null)
                {
                    skeletonPosition = this.transform.parent.TransformPoint(skeletonPosition);
                    skeletonRotation = this.transform.parent.rotation * skeletonRotation;
                }
            }
            else
            {
                skeletonPosition = origin.TransformPoint(skeletonPosition);
                skeletonRotation = origin.rotation * skeletonRotation;
            }

            this.transform.position = skeletonPosition;
            this.transform.rotation = skeletonRotation;
        }

        /// <summary>
        /// Returns an array of positions/rotations that represent the state of each bone in a reference pose.
        /// </summary>
        /// <param name="referencePose">Which reference pose to return</param>
        public void ForceToReferencePose(EVRSkeletalReferencePose referencePose)
        {
            bool temporarySession = false;
            if (Application.isEditor && Application.isPlaying == false)
            {
                temporarySession = SteamVR.InitializeTemporarySession(true);
                Awake();
                //skeletonAction.Initialize(true);
                //skeletonAction.actionSet.Initialize(true);

#if UNITY_EDITOR
                //gotta wait a bit for steamvr input to startup //todo: implement steamvr_input.isready
                string title = "SteamVR";
                string text = "Getting reference pose...";
                float msToWait = 3000;
                float increment = 100;
                for (float timer = 0; timer < msToWait; timer += increment)
                {
                    bool cancel = UnityEditor.EditorUtility.DisplayCancelableProgressBar(title, text, timer / msToWait);
                    if (cancel)
                    {
                        UnityEditor.EditorUtility.ClearProgressBar();

                        if (temporarySession)
                            SteamVR.ExitTemporarySession();
                        return;
                    }
                    System.Threading.Thread.Sleep((int)increment);
                }
                UnityEditor.EditorUtility.ClearProgressBar();
#endif

                skeletonAction.actionSet.Activate();

                SteamVR_ActionSet_Manager.UpdateActionStates(true);

                skeletonAction.UpdateValueWithoutEvents();
            }

            SteamVR_Utils.RigidTransform[] transforms = skeletonAction.GetReferenceTransforms(EVRSkeletalTransformSpace.Parent, referencePose);

            if (transforms == null || transforms.Length == 0)
            {
                Debug.LogError("<b>[SteamVR Input</b> Unable to get the reference transform for " + inputSource.ToString() + ". Please make sure SteamVR is open and both controllers are connected.");
            }

            for (int boneIndex = 0; boneIndex < transforms.Length; boneIndex++)
            {
                bones[boneIndex].localPosition = transforms[boneIndex].pos;
                bones[boneIndex].localRotation = transforms[boneIndex].rot;
            }

            if (temporarySession)
                SteamVR.ExitTemporarySession();
        }

        public enum MirrorType
        {
            None,
            LeftToRight,
            RightToLeft
        }

        protected bool IsMetacarpal(int boneIndex)
        {
            return (boneIndex == SteamVR_Skeleton_JointIndexes.indexMetacarpal ||
                boneIndex == SteamVR_Skeleton_JointIndexes.middleMetacarpal ||
                boneIndex == SteamVR_Skeleton_JointIndexes.ringMetacarpal ||
                boneIndex == SteamVR_Skeleton_JointIndexes.pinkyMetacarpal ||
                boneIndex == SteamVR_Skeleton_JointIndexes.thumbMetacarpal);
        }
    }
}