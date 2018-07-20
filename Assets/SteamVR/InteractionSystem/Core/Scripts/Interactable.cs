//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: This object will get hover events and can be attached to the hands
//
//=============================================================================

using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace Valve.VR.InteractionSystem
{
	//-------------------------------------------------------------------------
	public class Interactable : MonoBehaviour
    {
        [Tooltip("Hide the hand on attachment and Show on detach")]
        public bool hideHandOnAttach = true;

        [Tooltip("The integer in the animator to trigger on pickup. 0 for none")]
        public int handAnimationOnPickup = 0;

        [Tooltip("The range of motion to set on the skeleton. None for no change.")]
        public SkeletalMotionRangeChange setRangeOfMotionOnPickup = SkeletalMotionRangeChange.None;

        public delegate void OnAttachedToHandDelegate( Hand hand );
		public delegate void OnDetachedFromHandDelegate( Hand hand );

		[HideInInspector]
		public event OnAttachedToHandDelegate onAttachedToHand;
		[HideInInspector]
		public event OnDetachedFromHandDelegate onDetachedFromHand;


        [Tooltip("Specify whether you want to snap to the hand's object attachment point, or just the raw hand")]
        public bool useHandObjectAttachmentPoint = true;

        [Tooltip("If you want the hand to stick to an object while attached, set the transform to stick to here")]
        public Transform handFollowTransform;
        public bool handFollowTransformPosition = true;
        public bool handFollowTransformRotation = true;

        [System.NonSerialized]
        public Hand attachedToHand;

        public bool isDestroying { get; protected set; }

        private void OnAttachedToHand( Hand hand )
        {
            if ( onAttachedToHand != null )
			{
				onAttachedToHand.Invoke( hand );
			}

            attachedToHand = hand;
        }

		private void OnDetachedFromHand( Hand hand )
		{
			if ( onDetachedFromHand != null )
			{
				onDetachedFromHand.Invoke( hand );
			}

            attachedToHand = null;
		}

        protected virtual void OnDestroy()
        {
            isDestroying = true;

            if (attachedToHand != null)
            {
                attachedToHand.DetachObject(this.gameObject, false);
            }
        }
    }
}
