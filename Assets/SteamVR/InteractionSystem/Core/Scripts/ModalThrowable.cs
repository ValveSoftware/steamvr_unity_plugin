//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Basic throwable object
//
//=============================================================================

using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace Valve.VR.InteractionSystem
{
    public class ModalThrowable : Throwable
    {
        [Tooltip("The local point which acts as a positional and rotational offset to use while held with a grip type grab")]
        public Transform gripOffset;

        [Tooltip("The local point which acts as a positional and rotational offset to use while held with a pinch type grab")]
        public Transform pinchOffset;

        protected override void HandHoverUpdate(Hand hand)
        {
            GrabTypes startingGrabType = hand.GetGrabStarting();

            if (startingGrabType != GrabTypes.None)
            {
                if (startingGrabType == GrabTypes.Pinch)
                {
                    hand.AttachObject(gameObject, startingGrabType, attachmentFlags, pinchOffset);
                }
                else if (startingGrabType == GrabTypes.Grip)
                {
                    hand.AttachObject(gameObject, startingGrabType, attachmentFlags, gripOffset);
                }
                else
                {
                    hand.AttachObject(gameObject, startingGrabType, attachmentFlags, attachmentOffset);
                }

                hand.HideGrabHint();
            }
        }
        protected override void HandAttachedUpdate(Hand hand)
        {
            if (interactable.skeletonPoser != null)
            {
                interactable.skeletonPoser.SetBlendingBehaviourEnabled("PinchPose", hand.currentAttachedObjectInfo.Value.grabbedWithType == GrabTypes.Pinch);
            }

            base.HandAttachedUpdate(hand);
        }
    }
}