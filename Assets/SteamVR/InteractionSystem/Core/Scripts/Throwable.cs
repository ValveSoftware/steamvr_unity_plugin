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
	//-------------------------------------------------------------------------
	[RequireComponent( typeof( Interactable ) )]
	[RequireComponent( typeof( Rigidbody ) )]
	[RequireComponent( typeof( VelocityEstimator ) )]
	public class Throwable : MonoBehaviour
	{
		[EnumFlags]
		[Tooltip( "The flags used to attach this object to the hand." )]
		public Hand.AttachmentFlags attachmentFlags = Hand.AttachmentFlags.ParentToHand | Hand.AttachmentFlags.DetachFromOtherHand | Hand.AttachmentFlags.TurnOnKinematic;

		[Tooltip( "Name of the attachment transform under in the hand's hierarchy which the object should should snap to." )]
		public string attachmentPoint;

		[Tooltip( "How fast must this object be moving to attach due to a trigger hold instead of a trigger press?" )]
		public float catchSpeedThreshold = 0.0f;

        public ReleaseStyle releaseVelocityStyle = ReleaseStyle.RawFromHand;

		[Tooltip( "When detaching the object, should it return to its original parent?" )]
		public bool restoreOriginalParent = false;

        [Tooltip("Set the hand animation on pickup. 0 for none")]
        public int handAnimationOnPickup = 0;

        public bool displayHandWhileHeld = false;

        public bool attachEaseIn = false;
		public AnimationCurve snapAttachEaseInCurve = AnimationCurve.EaseInOut( 0.0f, 0.0f, 1.0f, 1.0f );
		public float snapAttachEaseInTime = 0.15f;
		public string[] attachEaseInAttachmentNames;

		protected VelocityEstimator velocityEstimator;
        protected bool attached = false;
        protected float attachTime;
        protected Vector3 attachPosition;
        protected Quaternion attachRotation;
        protected Transform attachEaseInTransform;

		public UnityEvent onPickUp;
		public UnityEvent onDetachFromHand;

		public bool snapAttachEaseInCompleted = false;
        
        protected RigidbodyInterpolation hadInterpolation = RigidbodyInterpolation.None;

        protected new Rigidbody rigidbody;


        //-------------------------------------------------
        protected virtual void Awake()
		{
			velocityEstimator = GetComponent<VelocityEstimator>();

			if ( attachEaseIn )
			{
				attachmentFlags &= ~Hand.AttachmentFlags.SnapOnAttach;
			}

            rigidbody = GetComponent<Rigidbody>();
            rigidbody.maxAngularVelocity = 50.0f;
		}


        //-------------------------------------------------
        protected virtual void OnHandHoverBegin( Hand hand )
		{
			bool showHint = false;

			// "Catch" the throwable by holding down the interaction button instead of pressing it.
			// Only do this if the throwable is moving faster than the prescribed threshold speed,
			// and if it isn't attached to another hand
			if ( !attached )
			{
                GrabTypes bestGrabType = hand.GetBestGrabbingType();

                if ( bestGrabType != GrabTypes.None )
				{
					if (rigidbody.velocity.magnitude >= catchSpeedThreshold )
					{
						hand.AttachObject( gameObject, bestGrabType, attachmentFlags, attachmentPoint );
						showHint = false;
					}
				}
			}

			if ( showHint )
			{
                hand.ShowGrabHint();
			}
		}


        //-------------------------------------------------
        protected virtual void OnHandHoverEnd( Hand hand )
		{
            hand.HideGrabHint();
		}


        //-------------------------------------------------
        protected virtual void HandHoverUpdate( Hand hand )
        {
            GrabTypes startingGrabType = hand.GetGrabStarting();
            
            if (startingGrabType != GrabTypes.None)
            {
				hand.AttachObject( gameObject, startingGrabType, attachmentFlags, attachmentPoint );
                hand.HideGrabHint();
            }
		}

        //-------------------------------------------------
        protected virtual void OnAttachedToHand( Hand hand )
		{
            //Debug.Log("Pickup: " + hand.GetGrabStarting().ToString());

            if (displayHandWhileHeld)
                hand.Show();

            if (handAnimationOnPickup != 0)
                hand.SetAnimationState(handAnimationOnPickup);

            hadInterpolation = this.rigidbody.interpolation;

            attached = true;

			onPickUp.Invoke();

			hand.HoverLock( null );
            
            rigidbody.interpolation = RigidbodyInterpolation.None;
            
		    velocityEstimator.BeginEstimatingVelocity();

			attachTime = Time.time;
			attachPosition = transform.position;
			attachRotation = transform.rotation;

			if ( attachEaseIn )
			{
				attachEaseInTransform = hand.transform;
				if ( !Util.IsNullOrEmpty( attachEaseInAttachmentNames ) )
				{
					float smallestAngle = float.MaxValue;
					for ( int i = 0; i < attachEaseInAttachmentNames.Length; i++ )
					{
						Transform t = hand.GetAttachmentTransform( attachEaseInAttachmentNames[i] );
						float angle = Quaternion.Angle( t.rotation, attachRotation );
						if ( angle < smallestAngle )
						{
							attachEaseInTransform = t;
							smallestAngle = angle;
						}
					}
				}
			}

			snapAttachEaseInCompleted = false;
		}


        //-------------------------------------------------
        protected virtual void OnDetachedFromHand(Hand hand)
        {
            attached = false;

            if (handAnimationOnPickup != 0)
                hand.StopAnimation();

            onDetachFromHand.Invoke();

            hand.HoverUnlock(null);
            
            rigidbody.interpolation = hadInterpolation;

            if (releaseVelocityStyle == ReleaseStyle.OldControllerStyle)
            {
                rigidbody.velocity = hand.GetOldControllerVelocity();
                rigidbody.angularVelocity = hand.GetOldControllerAngularVelocity();
            }
            else if (releaseVelocityStyle == ReleaseStyle.EstimatedOnObject)
            {
                Vector3 position = Vector3.zero;
                Vector3 velocity = Vector3.zero;
                Vector3 angularVelocity = Vector3.zero;

                velocityEstimator.FinishEstimatingVelocity();
                velocity = velocityEstimator.GetVelocityEstimate();
                angularVelocity = velocityEstimator.GetAngularVelocityEstimate();
                position = velocityEstimator.transform.position;

                Vector3 r = transform.TransformPoint(rigidbody.centerOfMass) - position;
                rigidbody.velocity = velocity + Vector3.Cross(angularVelocity, r);
                rigidbody.angularVelocity = angularVelocity;

                // Make the object travel at the release velocity for the amount
                // of time it will take until the next fixed update, at which
                // point Unity physics will take over
                float timeUntilFixedUpdate = (Time.fixedDeltaTime + Time.fixedTime) - Time.time;
                transform.position += timeUntilFixedUpdate * velocity;
                float angle = Mathf.Rad2Deg * angularVelocity.magnitude;
                Vector3 axis = angularVelocity.normalized;
                transform.rotation *= Quaternion.AngleAxis(angle * timeUntilFixedUpdate, axis);
            }
            else if (releaseVelocityStyle == ReleaseStyle.RawFromHand)
            {
                rigidbody.velocity = hand.GetTrackedObjectVelocity();
                rigidbody.angularVelocity = hand.GetTrackedObjectAngularVelocity();
            }
            else if (releaseVelocityStyle == ReleaseStyle.EstimatedOnHand)
            {
                Vector3 velocity;
                Vector3 angularVelocity;

                hand.GetEstimatedPeakVelocities(out velocity, out angularVelocity);

                rigidbody.velocity = velocity;
                rigidbody.angularVelocity = angularVelocity;
            }
        }


        //-------------------------------------------------
        protected virtual void HandAttachedUpdate( Hand hand )
		{
			if ( hand.IsGrabEnding(this.gameObject) )
			{
				// Detach ourselves late in the frame.
				// This is so that any vehicles the player is attached to
				// have a chance to finish updating themselves.
				// If we detach now, our position could be behind what it
				// will be at the end of the frame, and the object may appear
				// to teleport behind the hand when the player releases it.
				StartCoroutine( LateDetach( hand ) );
			}

			if ( attachEaseIn )
			{
				float t = Util.RemapNumberClamped( Time.time, attachTime, attachTime + snapAttachEaseInTime, 0.0f, 1.0f );
				if ( t < 1.0f )
				{
					t = snapAttachEaseInCurve.Evaluate( t );
					transform.position = Vector3.Lerp( attachPosition, attachEaseInTransform.position, t );
					transform.rotation = Quaternion.Lerp( attachRotation, attachEaseInTransform.rotation, t );
				}
				else if ( !snapAttachEaseInCompleted )
				{
					gameObject.SendMessage( "OnThrowableAttachEaseInCompleted", hand, SendMessageOptions.DontRequireReceiver );
					snapAttachEaseInCompleted = true;
				}
			}
		}


        //-------------------------------------------------
        protected virtual IEnumerator LateDetach( Hand hand )
		{
			yield return new WaitForEndOfFrame();

			hand.DetachObject( gameObject, restoreOriginalParent );
		}


        //-------------------------------------------------
        protected virtual void OnHandFocusAcquired( Hand hand )
		{
			gameObject.SetActive( true );
			velocityEstimator.BeginEstimatingVelocity();
		}


        //-------------------------------------------------
        protected virtual void OnHandFocusLost( Hand hand )
		{
			gameObject.SetActive( false );
			velocityEstimator.FinishEstimatingVelocity();
		}
	}

    public enum ReleaseStyle
    {
        NoChange,
        OldControllerStyle,
        EstimatedOnObject,
        EstimatedOnHand,
        RawFromHand,
    }
}
