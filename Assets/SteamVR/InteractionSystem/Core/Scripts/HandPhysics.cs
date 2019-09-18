//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: handles the physics of hands colliding with the world
//
//=============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Valve.VR.InteractionSystem
{
    public class HandPhysics : MonoBehaviour
    {
        [Tooltip("Hand collider prefab to instantiate")]
        public HandCollider handColliderPrefab;
        [HideInInspector]
        public HandCollider handCollider;

        [Tooltip("Layers to consider when checking if an area is clear")]
        public LayerMask clearanceCheckMask;

        [HideInInspector]
        public Hand hand;

        // distance at which hand will teleport back to controller
        const float handResetDistance = 0.6f;

        const float collisionReenableClearanceRadius = 0.1f;

        private bool initialized = false;

        private bool collisionsEnabled = true;


        private void Start()
        {
            hand = GetComponent<Hand>();
            //spawn hand collider and link it to us
            
            handCollider = ((GameObject)Instantiate(handColliderPrefab.gameObject)).GetComponent<HandCollider>();
            Vector3 localPosition = handCollider.transform.localPosition;
            Quaternion localRotation = handCollider.transform.localRotation;

            handCollider.transform.parent = Player.instance.transform;
            handCollider.transform.localPosition = localPosition;
            handCollider.transform.localRotation = localRotation;
            handCollider.hand = this;

            GetComponent<SteamVR_Behaviour_Pose>().onTransformUpdated.AddListener(UpdateHand);
        }

        // cached transformations
        Matrix4x4 wristToRoot;
        Matrix4x4 rootToArmature;
        Matrix4x4 wristToArmature;

        Vector3 targetPosition = Vector3.zero;
        Quaternion targetRotation = Quaternion.identity;

        //bones
        const int wristBone = SteamVR_Skeleton_JointIndexes.wrist;
        const int rootBone = SteamVR_Skeleton_JointIndexes.root;

        private void FixedUpdate()
        {
            if (hand.skeleton == null) return;
            initialized = true;

            UpdateCenterPoint();

            handCollider.MoveTo(targetPosition, targetRotation);

            if ((handCollider.transform.position - targetPosition).sqrMagnitude > handResetDistance * handResetDistance)
                handCollider.TeleportTo(targetPosition, targetRotation);

            UpdateFingertips();
        }

        private void UpdateCenterPoint()
        {
            Vector3 offset = hand.skeleton.GetBonePosition(SteamVR_Skeleton_JointIndexes.middleProximal) - hand.skeleton.GetBonePosition(SteamVR_Skeleton_JointIndexes.root);
            if (hand.HasSkeleton())
            {
                handCollider.SetCenterPoint(hand.skeleton.transform.position + offset);
            }
        }

        Collider[] clearanceBuffer = new Collider[1];

        private void UpdatePositions()
        {
            // disable collisions when holding something
            if (hand.currentAttachedObject != null)
            {
                collisionsEnabled = false;
            }
            else
            {
                // wait for area to become clear before reenabling collisions
                if (!collisionsEnabled)
                {
                    clearanceBuffer[0] = null;
                    Physics.OverlapSphereNonAlloc(hand.objectAttachmentPoint.position, collisionReenableClearanceRadius, clearanceBuffer);
                    // if we don't find anything in the vicinity, reenable collisions!
                    if (clearanceBuffer[0] == null)
                    {
                        collisionsEnabled = true;
                    }
                }
            }

            handCollider.SetCollisionDetectionEnabled(collisionsEnabled);

            if (hand.skeleton == null) return;
            initialized = true;

            // get the desired pose of the wrist in world space. Can't get the wrist bone transform, as this is affected by the resulting physics.

            wristToRoot = Matrix4x4.TRS(ProcessPos(wristBone, hand.skeleton.GetBone(wristBone).localPosition),
                ProcessRot(wristBone, hand.skeleton.GetBone(wristBone).localRotation),
                Vector3.one).inverse;

            rootToArmature = Matrix4x4.TRS(ProcessPos(rootBone, hand.skeleton.GetBone(rootBone).localPosition),
                ProcessRot(rootBone, hand.skeleton.GetBone(rootBone).localRotation),
                Vector3.one).inverse;

            wristToArmature = (wristToRoot * rootToArmature).inverse;

            // step up through virtual transform hierarchy and into world space
            targetPosition = transform.TransformPoint(wristToArmature.MultiplyPoint3x4(Vector3.zero));

            targetRotation = transform.rotation * wristToArmature.GetRotation();


            //bypass physics when game paused
            if (Time.timeScale == 0)
            {
                handCollider.TeleportTo(targetPosition, targetRotation);
            }
        }

        Transform wrist;

        const int thumbBone = SteamVR_Skeleton_JointIndexes.thumbDistal;
        const int indexBone = SteamVR_Skeleton_JointIndexes.indexDistal;
        const int middleBone = SteamVR_Skeleton_JointIndexes.middleDistal;
        const int ringBone = SteamVR_Skeleton_JointIndexes.ringDistal;
        const int pinkyBone = SteamVR_Skeleton_JointIndexes.pinkyDistal;

        void UpdateFingertips()
        {
            wrist = hand.skeleton.GetBone(SteamVR_Skeleton_JointIndexes.wrist);

            // set finger tip positions in wrist space

            for(int finger = 0; finger < 5; finger++)
            {
                int tip = SteamVR_Skeleton_JointIndexes.GetBoneForFingerTip(finger);
                int bone = tip;
                for(int i = 0; i < handCollider.fingerColliders[finger].Length; i++)
                {
                    bone = tip - 1 - i; // start at distal and go down
                    if (handCollider.fingerColliders[finger][i] != null)
                        handCollider.fingerColliders[finger][i].localPosition = wrist.InverseTransformPoint(hand.skeleton.GetBone(bone).position);
                }
            }
            /*
            if(handCollider.tip_thumb != null)
                handCollider.tip_thumb.localPosition = wrist.InverseTransformPoint(hand.skeleton.GetBone(thumbBone).position);

            if(handCollider.tip_index != null)
            handCollider.tip_index.localPosition = wrist.InverseTransformPoint(hand.skeleton.GetBone(indexBone).position);

            if(handCollider.tip_middle != null)
            handCollider.tip_middle.localPosition = wrist.InverseTransformPoint(hand.skeleton.GetBone(middleBone).position);

            if(handCollider.tip_ring != null)
            handCollider.tip_ring.localPosition = wrist.InverseTransformPoint(hand.skeleton.GetBone(ringBone).position);

            if (handCollider.tip_pinky != null)
            handCollider.tip_pinky.localPosition = wrist.InverseTransformPoint(hand.skeleton.GetBone(pinkyBone).position);
            */
        }

        void UpdateHand(SteamVR_Behaviour_Pose pose, SteamVR_Input_Sources inputSource)
        {
            if (!initialized) return;

            UpdateCenterPoint();

            UpdatePositions();

            Quaternion offsetRotation = handCollider.transform.rotation * wristToArmature.inverse.GetRotation();

            hand.mainRenderModel.transform.rotation = offsetRotation;

            Vector3 offsetPosition = handCollider.transform.TransformPoint(wristToArmature.inverse.MultiplyPoint3x4(Vector3.zero));

            hand.mainRenderModel.transform.position = offsetPosition;

            /*
            Vector3 wristPointInArmatureSpace = transform.InverseTransformPoint(handCollider.transform.position);

            Vector3 handTargetPosition =

            hand.mainRenderModel.transform.position = handTargetPosition;

            //Quaternion handTargetRotation = transform.rotation * (wristToArmature.inverse.rotation * (Quaternion.Inverse(transform.rotation) * handCollider.transform.rotation));

            //hand.mainRenderModel.transform.rotation = handTargetRotation;
            */
        }

        Vector3 ProcessPos(int boneIndex, Vector3 pos)
        {
            if(hand.skeleton.mirroring != SteamVR_Behaviour_Skeleton.MirrorType.None)
            {
                return SteamVR_Behaviour_Skeleton.MirrorPosition(boneIndex, pos);
            }

            return pos;
        }

        Quaternion ProcessRot(int boneIndex, Quaternion rot)
        {
            if (hand.skeleton.mirroring != SteamVR_Behaviour_Skeleton.MirrorType.None)
            {
                return SteamVR_Behaviour_Skeleton.MirrorRotation(boneIndex, rot);
            }

            return rot;
        }


    }
}