//======= Copyright (c) Valve Corporation, All rights reserved. ===============

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
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

        /// <summary>This Unity event will fire whenever the position or rotation of the bones are updated.</summary>
        public SteamVR_Behaviour_SkeletonEvent onBoneTransformsUpdated;

        /// <summary>This Unity event will fire whenever the position or rotation of this transform is updated.</summary>
        public SteamVR_Behaviour_SkeletonEvent onTransformUpdated;

        /// <summary>This Unity event will fire whenever the position or rotation of this transform is changed.</summary>
        public SteamVR_Behaviour_SkeletonEvent onTransformChanged;

        /// <summary>This Unity event will fire whenever the device is connected or disconnected</summary>
        public SteamVR_Behaviour_Skeleton_ConnectedChangedEvent onConnectedChanged;

        /// <summary>This Unity event will fire whenever the device's tracking state changes</summary>
        public SteamVR_Behaviour_Skeleton_TrackingChangedEvent onTrackingChanged;


        /// <summary>This C# event will fire whenever the position or rotation of this transform is updated.</summary>
        public UpdateHandler onBoneTransformsUpdatedEvent;

        /// <summary>This C# event will fire whenever the position or rotation of this transform is updated.</summary>
        public UpdateHandler onTransformUpdatedEvent;

        /// <summary>This C# event will fire whenever the position or rotation of this transform is changed.</summary>
        public ChangeHandler onTransformChangedEvent;

        /// <summary>This C# event will fire whenever the device is connected or disconnected</summary>
        public DeviceConnectedChangeHandler onConnectedChangedEvent;

        /// <summary>This C# event will fire whenever the device's tracking state changes</summary>
        public TrackingChangeHandler onTrackingChangedEvent;

        /// <summary>Can be set to mirror the bone data across the x axis</summary>
        [Tooltip("Is this rendermodel a mirror of another one?")]
        public MirrorType mirroring;



        [Header("No Skeleton - Fallback")]


        [Tooltip("The fallback SkeletonPoser to drive hand animation when no skeleton data is available")]
        /// <summary>The fallback SkeletonPoser to drive hand animation when no skeleton data is available</summary>
        public SteamVR_Skeleton_Poser fallbackPoser;

        [Tooltip("The fallback action to drive finger curl values when no skeleton data is available")]
        /// <summary>The fallback SkeletonPoser to drive hand animation when no skeleton data is available</summary>
        public SteamVR_Action_Single fallbackCurlAction;

        /// <summary>
        /// Is the skeleton action bound?
        /// </summary>
        public bool skeletonAvailable { get { return skeletonAction.activeBinding; } }







        /// <summary>The current skeletonPoser we're getting pose data from</summary>
        protected SteamVR_Skeleton_Poser blendPoser;
        /// <summary>The current pose snapshot</summary>
        protected SteamVR_Skeleton_PoseSnapshot blendSnapshot = null;


        /// <summary>Returns whether this action is bound and the action set is active</summary>
        public bool isActive { get { return skeletonAction.GetActive(); } }


        /// <summary>An array of five 0-1 values representing how curled a finger is. 0 being straight, 1 being fully curled. 0 being thumb, 4 being pinky</summary>
        public float[] fingerCurls
        {
            get
            {
                if (skeletonAvailable)
                {
                    return skeletonAction.GetFingerCurls();
                }
                else
                {
                    //fallback, return array where each finger curl is just the fallback curl action value
                    float[] curls = new float[5];
                    for (int i = 0; i < 5; i++)
                    {
                        curls[i] = fallbackCurlAction.GetAxis(inputSource);
                    }
                    return curls;
                }
            }
        }

        /// <summary>An 0-1 value representing how curled a finger is. 0 being straight, 1 being fully curled.</summary>
        public float thumbCurl
        {
            get
            {
                if (skeletonAvailable)
                    return skeletonAction.GetFingerCurl(SteamVR_Skeleton_FingerIndexEnum.thumb);
                else
                    return fallbackCurlAction.GetAxis(inputSource);
            }
        }

        /// <summary>An 0-1 value representing how curled a finger is. 0 being straight, 1 being fully curled.</summary>
        public float indexCurl
        {
            get
            {
                if (skeletonAvailable)
                    return skeletonAction.GetFingerCurl(SteamVR_Skeleton_FingerIndexEnum.index);
                else
                    return fallbackCurlAction.GetAxis(inputSource);
            }
        }

        /// <summary>An 0-1 value representing how curled a finger is. 0 being straight, 1 being fully curled.</summary>
        public float middleCurl
        {
            get
            {
                if (skeletonAvailable)
                    return skeletonAction.GetFingerCurl(SteamVR_Skeleton_FingerIndexEnum.middle);
                else
                    return fallbackCurlAction.GetAxis(inputSource);
            }
        }

        /// <summary>An 0-1 value representing how curled a finger is. 0 being straight, 1 being fully curled.</summary>
        public float ringCurl
        {
            get
            {
                if (skeletonAvailable)
                    return skeletonAction.GetFingerCurl(SteamVR_Skeleton_FingerIndexEnum.ring);
                else
                    return fallbackCurlAction.GetAxis(inputSource);
            }
        }

        /// <summary>An 0-1 value representing how curled a finger is. 0 being straight, 1 being fully curled.</summary>
        public float pinkyCurl
        {
            get
            {
                if (skeletonAvailable)
                    return skeletonAction.GetFingerCurl(SteamVR_Skeleton_FingerIndexEnum.pinky);
                else
                    return fallbackCurlAction.GetAxis(inputSource);
            }
        }



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

        /// <summary>
        /// Get the accuracy level of the skeletal tracking data.
        /// <para/>* Estimated: Body part location can’t be directly determined by the device. Any skeletal pose provided by the device is estimated based on the active buttons, triggers, joysticks, or other input sensors. Examples include the Vive Controller and gamepads.
        /// <para/>* Partial: Body part location can be measured directly but with fewer degrees of freedom than the actual body part.Certain body part positions may be unmeasured by the device and estimated from other input data.Examples include Knuckles or gloves that only measure finger curl
        /// <para/>* Full: Body part location can be measured directly throughout the entire range of motion of the body part.Examples include hi-end mocap systems, or gloves that measure the rotation of each finger segment.
        /// </summary>
        public EVRSkeletalTrackingLevel skeletalTrackingLevel
        {
            get
            {
                if (skeletonAvailable)
                {
                    return skeletonAction.skeletalTrackingLevel;
                }
                else
                {
                    return EVRSkeletalTrackingLevel.VRSkeletalTracking_Estimated;
                }
            }
        }

        /// <summary>Returns true if we are in the process of blending the skeletonBlend field (between animation and bone data)</summary>
        public bool isBlending
        {
            get
            {
                return blendRoutine != null;
            }
        }

        /*
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
        */
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
            SteamVR.Initialize();

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
            CheckSkeletonAction();
            SteamVR_Input.onSkeletonsUpdated += SteamVR_Input_OnSkeletonsUpdated;

            if (skeletonAction != null)
            {
                skeletonAction.onDeviceConnectedChanged += OnDeviceConnectedChanged;
                skeletonAction.onTrackingChanged += OnTrackingChanged;
            }
        }

        protected virtual void OnDisable()
        {
            SteamVR_Input.onSkeletonsUpdated -= SteamVR_Input_OnSkeletonsUpdated;

            if (skeletonAction != null)
            {
                skeletonAction.onDeviceConnectedChanged -= OnDeviceConnectedChanged;
                skeletonAction.onTrackingChanged -= OnTrackingChanged;
            }
        }

        private void OnDeviceConnectedChanged(SteamVR_Action_Skeleton fromAction, bool deviceConnected)
        {
            if (onConnectedChanged != null)
                onConnectedChanged.Invoke(this, inputSource, deviceConnected);
            if (onConnectedChangedEvent != null)
                onConnectedChangedEvent.Invoke(this, inputSource, deviceConnected);
        }

        private void OnTrackingChanged(SteamVR_Action_Skeleton fromAction, ETrackingResult trackingState)
        {
            if (onTrackingChanged != null)
                onTrackingChanged.Invoke(this, inputSource, trackingState);
            if (onTrackingChangedEvent != null)
                onTrackingChangedEvent.Invoke(this, inputSource, trackingState);
        }

        protected virtual void SteamVR_Input_OnSkeletonsUpdated(bool skipSendingEvents)
        {
            UpdateSkeleton();
        }

        protected virtual void UpdateSkeleton()
        {
            if (skeletonAction == null)
                return;

            if (updatePose)
                UpdatePose();

            if (blendPoser != null && skeletonBlend < 1)
            {
                if (blendSnapshot == null) blendSnapshot = blendPoser.GetBlendedPose(this);
                blendSnapshot = blendPoser.GetBlendedPose(this);
            }

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
        public void BlendToSkeleton(float overTime = 0.1f)
        {
            if (blendPoser != null)
                blendSnapshot = blendPoser.GetBlendedPose(this);
            blendPoser = null;
            BlendTo(1, overTime);
        }

        /// <summary>
        /// Blend from the current skeletonBlend amount to pose animation. (skeletonBlend = 0)
        /// Note: This will ignore the root position and rotation of the pose.
        /// </summary>
        /// <param name="overTime">How long you want the blend to take (in seconds)</param>
        public void BlendToPoser(SteamVR_Skeleton_Poser poser, float overTime = 0.1f)
        {
            if (poser == null)
                return;

            blendPoser = poser;
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
                            if (blendPoser != null)
                            {

                                SetBonePosition(boneIndex, Vector3.Lerp(blendSnapshot.bonePositions[boneIndex], blendedRangeOfMotionPosition, skeletonBlend));
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

                if (onBoneTransformsUpdated != null)
                    onBoneTransformsUpdated.Invoke(this, inputSource);
                if (onBoneTransformsUpdatedEvent != null)
                    onBoneTransformsUpdatedEvent.Invoke(this, inputSource);

            }

            rangeOfMotionBlendRoutine = null;
        }

        //why does this exist
        protected virtual Quaternion GetBlendPoseForBone(int boneIndex, Quaternion skeletonRotation)
        {
            Quaternion poseRotation = blendSnapshot.boneRotations[boneIndex];
            return poseRotation;
        }

        public virtual void UpdateSkeletonTransforms()
        {
            Vector3[] bonePositions = GetBonePositions();
            Quaternion[] boneRotations = GetBoneRotations();

            if (skeletonBlend <= 0)
            {
                if (blendPoser != null)
                {
                    SteamVR_Skeleton_Pose_Hand mainPose = blendPoser.skeletonMainPose.GetHand(inputSource);
                    for (int boneIndex = 0; boneIndex < bones.Length; boneIndex++)
                    {
                        if (bones[boneIndex] == null)
                            continue;

                        if ((boneIndex == SteamVR_Skeleton_JointIndexes.wrist && mainPose.ignoreWristPoseData) ||
                            (boneIndex == SteamVR_Skeleton_JointIndexes.root && mainPose.ignoreRootPoseData))
                        {
                            SetBonePosition(boneIndex, bonePositions[boneIndex]);
                            SetBoneRotation(boneIndex, boneRotations[boneIndex]);
                        }
                        else
                        {
                            Quaternion poseRotation = GetBlendPoseForBone(boneIndex, boneRotations[boneIndex]);

                            SetBonePosition(boneIndex, blendSnapshot.bonePositions[boneIndex]);
                            SetBoneRotation(boneIndex, poseRotation);
                        }
                    }
                }
                else
                {
                    for (int boneIndex = 0; boneIndex < bones.Length; boneIndex++)
                    {

                        Quaternion poseRotation = GetBlendPoseForBone(boneIndex, boneRotations[boneIndex]);

                        SetBonePosition(boneIndex, blendSnapshot.bonePositions[boneIndex]);
                        SetBoneRotation(boneIndex, poseRotation);

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

                    if (blendPoser != null)
                    {
                        SteamVR_Skeleton_Pose_Hand mainPose = blendPoser.skeletonMainPose.GetHand(inputSource);

                        if ((boneIndex == SteamVR_Skeleton_JointIndexes.wrist && mainPose.ignoreWristPoseData) ||
                            (boneIndex == SteamVR_Skeleton_JointIndexes.root && mainPose.ignoreRootPoseData))
                        {
                            SetBonePosition(boneIndex, bonePositions[boneIndex]);
                            SetBoneRotation(boneIndex, boneRotations[boneIndex]);
                        }
                        else
                        {
                            //Quaternion poseRotation = GetBlendPoseForBone(boneIndex, boneRotations[boneIndex]);

                            SetBonePosition(boneIndex, Vector3.Lerp(blendSnapshot.bonePositions[boneIndex], bonePositions[boneIndex], skeletonBlend));
                            SetBoneRotation(boneIndex, Quaternion.Lerp(blendSnapshot.boneRotations[boneIndex], boneRotations[boneIndex], skeletonBlend));
                            //SetBoneRotation(boneIndex, GetBlendPoseForBone(boneIndex, boneRotations[boneIndex]));
                        }
                    }
                    else
                    {
                        if (blendSnapshot == null)
                        {
                            SetBonePosition(boneIndex, Vector3.Lerp(bones[boneIndex].localPosition, bonePositions[boneIndex], skeletonBlend));
                            SetBoneRotation(boneIndex, Quaternion.Lerp(bones[boneIndex].localRotation, boneRotations[boneIndex], skeletonBlend));
                        }
                        else
                        {
                            SetBonePosition(boneIndex, Vector3.Lerp(blendSnapshot.bonePositions[boneIndex], bonePositions[boneIndex], skeletonBlend));
                            SetBoneRotation(boneIndex, Quaternion.Lerp(blendSnapshot.boneRotations[boneIndex], boneRotations[boneIndex], skeletonBlend));
                        }
                    }
                }
            }


            if (onBoneTransformsUpdated != null)
                onBoneTransformsUpdated.Invoke(this, inputSource);
            if (onBoneTransformsUpdatedEvent != null)
                onBoneTransformsUpdatedEvent.Invoke(this, inputSource);
        }

        public virtual void SetBonePosition(int boneIndex, Vector3 localPosition)
        {
            if (onlySetRotations == false) //ignore position sets if we're only setting rotations
                bones[boneIndex].localPosition = localPosition;
        }

        public virtual void SetBoneRotation(int boneIndex, Quaternion localRotation)
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
            if (skeletonAvailable)
            {
                Vector3[] rawSkeleton = skeletonAction.GetBonePositions();
                if (mirroring == MirrorType.LeftToRight || mirroring == MirrorType.RightToLeft)
                {
                    for (int boneIndex = 0; boneIndex < rawSkeleton.Length; boneIndex++)
                    {
                        rawSkeleton[boneIndex] = MirrorPosition(boneIndex, rawSkeleton[boneIndex]);
                    }
                }

                return rawSkeleton;
            }
            else
            {
                //fallback to getting skeleton pose from skeletonPoser
                if (fallbackPoser != null)
                {
                    return fallbackPoser.GetBlendedPose(skeletonAction, inputSource).bonePositions;
                }
                else
                {
                    Debug.LogError("Skeleton Action is not bound, and you have not provided a fallback SkeletonPoser. Please create one to drive hand animation when no skeleton data is available.", this);
                    return null;
                }
            }
        }

        protected static readonly Quaternion rightFlipAngle = Quaternion.AngleAxis(180, Vector3.right);
        protected Quaternion[] GetBoneRotations()
        {
            if (skeletonAvailable)
            {
                Quaternion[] rawSkeleton = skeletonAction.GetBoneRotations();
                if (mirroring == MirrorType.LeftToRight || mirroring == MirrorType.RightToLeft)
                {
                    for (int boneIndex = 0; boneIndex < rawSkeleton.Length; boneIndex++)
                    {
                        rawSkeleton[boneIndex] = MirrorRotation(boneIndex, rawSkeleton[boneIndex]);
                    }
                }

                return rawSkeleton;

            }
            else
            {
                //fallback to getting skeleton pose from skeletonPoser
                if (fallbackPoser != null)
                {
                    return fallbackPoser.GetBlendedPose(skeletonAction, inputSource).boneRotations;
                }
                else
                {
                    Debug.LogError("Skeleton Action is not bound, and you have not provided a fallback SkeletonPoser. Please create one to drive hand animation when no skeleton data is available.", this);
                    return null;
                }
            }
        }

        public static Vector3 MirrorPosition(int boneIndex, Vector3 rawPosition)
        {
            if (boneIndex == SteamVR_Skeleton_JointIndexes.wrist || IsMetacarpal(boneIndex))
            {
                rawPosition.Scale(new Vector3(-1, 1, 1));
            }
            else if (boneIndex != SteamVR_Skeleton_JointIndexes.root)
            {
                rawPosition = rawPosition * -1;
            }

            return rawPosition;
        }

        public static Quaternion MirrorRotation(int boneIndex, Quaternion rawRotation)
        {
            if (boneIndex == SteamVR_Skeleton_JointIndexes.wrist)
            {
                rawRotation.y = rawRotation.y * -1;
                rawRotation.z = rawRotation.z * -1;
            }

            if (IsMetacarpal(boneIndex))
            {
                rawRotation = rightFlipAngle * rawRotation;
            }

            return rawRotation;
        }

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

            if (skeletonAction.poseChanged)
            {
                if (onTransformChanged != null)
                    onTransformChanged.Invoke(this, inputSource);
                if (onTransformChangedEvent != null)
                    onTransformChangedEvent.Invoke(this, inputSource);
            }

            this.transform.position = skeletonPosition;
            this.transform.rotation = skeletonRotation;

            if (onTransformUpdated != null)
                onTransformUpdated.Invoke(this, inputSource);
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

            if (skeletonAction.active == false)
            {
                Debug.LogError("<b>[SteamVR Input]</b> Please turn on your " + inputSource.ToString() + " controller and ensure SteamVR is open.", this);
                return;
            }

            SteamVR_Utils.RigidTransform[] transforms = skeletonAction.GetReferenceTransforms(EVRSkeletalTransformSpace.Parent, referencePose);

            if (transforms == null || transforms.Length == 0)
            {
                Debug.LogError("<b>[SteamVR Input]</b> Unable to get the reference transform for " + inputSource.ToString() + ". Please make sure SteamVR is open and both controllers are connected.", this);
            }

            if (mirroring == MirrorType.LeftToRight || mirroring == MirrorType.RightToLeft)
            {
                for (int boneIndex = 0; boneIndex < transforms.Length; boneIndex++)
                {
                    bones[boneIndex].localPosition = MirrorPosition(boneIndex, transforms[boneIndex].pos);
                    bones[boneIndex].localRotation = MirrorRotation(boneIndex, transforms[boneIndex].rot);
                }
            }
            else
            {
                for (int boneIndex = 0; boneIndex < transforms.Length; boneIndex++)
                {
                    bones[boneIndex].localPosition = transforms[boneIndex].pos;
                    bones[boneIndex].localRotation = transforms[boneIndex].rot;
                }
            }

            if (temporarySession)
                SteamVR.ExitTemporarySession();
        }

        protected static bool IsMetacarpal(int boneIndex)
        {
            return (boneIndex == SteamVR_Skeleton_JointIndexes.indexMetacarpal ||
                boneIndex == SteamVR_Skeleton_JointIndexes.middleMetacarpal ||
                boneIndex == SteamVR_Skeleton_JointIndexes.ringMetacarpal ||
                boneIndex == SteamVR_Skeleton_JointIndexes.pinkyMetacarpal ||
                boneIndex == SteamVR_Skeleton_JointIndexes.thumbMetacarpal);
        }

        public enum MirrorType
        {
            None,
            LeftToRight,
            RightToLeft
        }

        public delegate void ActiveChangeHandler(SteamVR_Behaviour_Skeleton fromAction, SteamVR_Input_Sources inputSource, bool active);
        public delegate void ChangeHandler(SteamVR_Behaviour_Skeleton fromAction, SteamVR_Input_Sources inputSource);
        public delegate void UpdateHandler(SteamVR_Behaviour_Skeleton fromAction, SteamVR_Input_Sources inputSource);
        public delegate void TrackingChangeHandler(SteamVR_Behaviour_Skeleton fromAction, SteamVR_Input_Sources inputSource, ETrackingResult trackingState);
        public delegate void ValidPoseChangeHandler(SteamVR_Behaviour_Skeleton fromAction, SteamVR_Input_Sources inputSource, bool validPose);
        public delegate void DeviceConnectedChangeHandler(SteamVR_Behaviour_Skeleton fromAction, SteamVR_Input_Sources inputSource, bool deviceConnected);
    }
}