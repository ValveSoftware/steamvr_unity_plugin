//======= Copyright (c) Valve Corporation, All rights reserved. ===============

using System.Collections;
using UnityEngine;
using Valve.VR;

namespace Valve.VR
{
    public class SteamVR_Behaviour_Skeleton : MonoBehaviour
    {
        /// <summary>The action this component will use to update the model. Must be a Skeleton type action.</summary>
        [SteamVR_DefaultAction("Skeleton", "default", "inputSource")]
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

        /// <summary>Can be set to mirror the bone data across the x axis</summary>
        [Tooltip("Is this rendermodel a mirror of another one?")]
        public MirrorType mirroring;

        /// <summary>Returns whether this action is bound and the action set is active</summary>
        public bool isActive { get { return skeletonAction.GetActive(inputSource); } }

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

        protected virtual void Awake()
        {
            AssignBonesArray();

            proximals = new Transform[] { thumbProximal, indexProximal, middleProximal, ringProximal, pinkyProximal };
            middles = new Transform[] { thumbMiddle, indexMiddle, middleMiddle, ringMiddle, pinkyMiddle };
            distals = new Transform[] { thumbDistal, indexDistal, middleDistal, ringDistal, pinkyDistal };
            tips = new Transform[] { thumbTip, indexTip, middleTip, ringTip, pinkyTip };
            auxs = new Transform[] { thumbAux, indexAux, middleAux, ringAux, pinkyAux };
        }

        protected virtual void AssignBonesArray()
        {
            bones = skeletonRoot.GetComponentsInChildren<Transform>();
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
        public void BlendToSkeleton(float overTime = 0.1f)
        {
            BlendTo(1, overTime);
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
                            SetBonePosition(boneIndex, Vector3.Lerp(bones[boneIndex].localPosition, blendedRangeOfMotionPosition, skeletonBlend));
                            SetBoneRotation(boneIndex, Quaternion.Lerp(bones[boneIndex].localRotation, blendedRangeOfMotionRotation, skeletonBlend));
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

                    SetBonePosition(boneIndex, Vector3.Lerp(bones[boneIndex].localPosition, bonePositions[boneIndex], skeletonBlend));
                    SetBoneRotation(boneIndex, Quaternion.Lerp(bones[boneIndex].localRotation, boneRotations[boneIndex], skeletonBlend));
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

        protected Vector3[] GetBonePositions(SteamVR_Input_Sources inputSource)
        {
            Vector3[] rawSkeleton = skeletonAction.GetBonePositions(inputSource);
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
        protected Quaternion[] GetBoneRotations(SteamVR_Input_Sources inputSource)
        {
            Quaternion[] rawSkeleton = skeletonAction.GetBoneRotations(inputSource);
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
            return (boneIndex == SteamVR_Skeleton_JointIndexes.indexMetacarpal ||
                boneIndex == SteamVR_Skeleton_JointIndexes.middleMetacarpal ||
                boneIndex == SteamVR_Skeleton_JointIndexes.ringMetacarpal ||
                boneIndex == SteamVR_Skeleton_JointIndexes.pinkyMetacarpal ||
                boneIndex == SteamVR_Skeleton_JointIndexes.thumbMetacarpal);
        }
    }


    /// <summary>The order of the joints that SteamVR Skeleton Input is expecting.</summary>
    public class SteamVR_Skeleton_JointIndexes
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

    public enum SteamVR_Skeleton_JointIndexEnum
    {
        root = SteamVR_Skeleton_JointIndexes.root,
        wrist = SteamVR_Skeleton_JointIndexes.wrist,
        thumbMetacarpal = SteamVR_Skeleton_JointIndexes.thumbMetacarpal,
        thumbProximal = SteamVR_Skeleton_JointIndexes.thumbProximal,
        thumbMiddle = SteamVR_Skeleton_JointIndexes.thumbMiddle,
        thumbDistal = SteamVR_Skeleton_JointIndexes.thumbDistal,
        thumbTip = SteamVR_Skeleton_JointIndexes.thumbTip,
        indexMetacarpal = SteamVR_Skeleton_JointIndexes.indexMetacarpal,
        indexProximal = SteamVR_Skeleton_JointIndexes.indexProximal,
        indexMiddle = SteamVR_Skeleton_JointIndexes.indexMiddle,
        indexDistal = SteamVR_Skeleton_JointIndexes.indexDistal,
        indexTip = SteamVR_Skeleton_JointIndexes.indexTip,
        middleMetacarpal = SteamVR_Skeleton_JointIndexes.middleMetacarpal,
        middleProximal = SteamVR_Skeleton_JointIndexes.middleProximal,
        middleMiddle = SteamVR_Skeleton_JointIndexes.middleMiddle,
        middleDistal = SteamVR_Skeleton_JointIndexes.middleDistal,
        middleTip = SteamVR_Skeleton_JointIndexes.middleTip,
        ringMetacarpal = SteamVR_Skeleton_JointIndexes.ringMetacarpal,
        ringProximal = SteamVR_Skeleton_JointIndexes.ringProximal,
        ringMiddle = SteamVR_Skeleton_JointIndexes.ringMiddle,
        ringDistal = SteamVR_Skeleton_JointIndexes.ringDistal,
        ringTip = SteamVR_Skeleton_JointIndexes.ringTip,
        pinkyMetacarpal = SteamVR_Skeleton_JointIndexes.pinkyMetacarpal,
        pinkyProximal = SteamVR_Skeleton_JointIndexes.pinkyProximal,
        pinkyMiddle = SteamVR_Skeleton_JointIndexes.pinkyMiddle,
        pinkyDistal = SteamVR_Skeleton_JointIndexes.pinkyDistal,
        pinkyTip = SteamVR_Skeleton_JointIndexes.pinkyTip,
        thumbAux = SteamVR_Skeleton_JointIndexes.thumbAux,
        indexAux = SteamVR_Skeleton_JointIndexes.indexAux,
        middleAux = SteamVR_Skeleton_JointIndexes.middleAux,
        ringAux = SteamVR_Skeleton_JointIndexes.ringAux,
        pinkyAux = SteamVR_Skeleton_JointIndexes.pinkyAux,
    }
}