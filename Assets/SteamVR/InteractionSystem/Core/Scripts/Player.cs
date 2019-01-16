//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Player interface used to query HMD transforms and VR hands
//
//=============================================================================

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Valve.VR.InteractionSystem
{
	//-------------------------------------------------------------------------
	// Singleton representing the local VR player/user, with methods for getting
	// the player's hands, head, tracking origin, and guesses for various properties.
	//-------------------------------------------------------------------------
	public class Player : MonoBehaviour
	{
		[Tooltip( "Virtual transform corresponding to the meatspace tracking origin. Devices are tracked relative to this." )]
		public Transform trackingOriginTransform;

		[Tooltip( "List of possible transforms for the head/HMD, including the no-SteamVR fallback camera." )]
		public Transform[] hmdTransforms;

		[Tooltip( "List of possible Hands, including no-SteamVR fallback Hands." )]
		public Hand[] hands;

		[Tooltip( "Reference to the physics collider that follows the player's HMD position." )]
		public Collider headCollider;

		[Tooltip( "These objects are enabled when SteamVR is available" )]
		public GameObject rigSteamVR;

		[Tooltip( "These objects are enabled when SteamVR is not available, or when the user toggles out of VR" )]
		public GameObject rig2DFallback;

		[Tooltip( "The audio listener for this player" )]
		public Transform audioListener;

        [Tooltip("This action lets you know when the player has placed the headset on their head")]
        public SteamVR_Action_Boolean headsetOnHead = SteamVR_Input.GetBooleanAction("HeadsetOnHead");

		public bool allowToggleTo2D = true;


		//-------------------------------------------------
		// Singleton instance of the Player. Only one can exist at a time.
		//-------------------------------------------------
		private static Player _instance;
		public static Player instance
		{
			get
			{
				if ( _instance == null )
				{
					_instance = FindObjectOfType<Player>();
				}
				return _instance;
			}
		}


		//-------------------------------------------------
		// Get the number of active Hands.
		//-------------------------------------------------
		public int handCount
		{
			get
			{
				int count = 0;
				for ( int i = 0; i < hands.Length; i++ )
				{
					if ( hands[i].gameObject.activeInHierarchy )
					{
						count++;
					}
				}
				return count;
			}
		}


		//-------------------------------------------------
		// Get the i-th active Hand.
		//
		// i - Zero-based index of the active Hand to get
		//-------------------------------------------------
		public Hand GetHand( int i )
		{
			for ( int j = 0; j < hands.Length; j++ )
			{
				if ( !hands[j].gameObject.activeInHierarchy )
				{
					continue;
				}

				if ( i > 0 )
				{
					i--;
					continue;
				}

				return hands[j];
			}

			return null;
		}


		//-------------------------------------------------
		public Hand leftHand
		{
			get
			{
				for ( int j = 0; j < hands.Length; j++ )
				{
					if ( !hands[j].gameObject.activeInHierarchy )
					{
						continue;
					}

					if ( hands[j].handType != SteamVR_Input_Sources.LeftHand)
					{
						continue;
					}

					return hands[j];
				}

				return null;
			}
		}


		//-------------------------------------------------
		public Hand rightHand
		{
			get
			{
				for ( int j = 0; j < hands.Length; j++ )
				{
					if ( !hands[j].gameObject.activeInHierarchy )
					{
						continue;
					}

					if ( hands[j].handType != SteamVR_Input_Sources.RightHand)
					{
						continue;
					}

					return hands[j];
				}

				return null;
			}
		}

        //-------------------------------------------------
        // Get Player scale. Assumes it is scaled equally on all axes.
        //-------------------------------------------------

        public float scale
        {
            get
            {
                return transform.lossyScale.x;
            }
        }


        //-------------------------------------------------
        // Get the HMD transform. This might return the fallback camera transform if SteamVR is unavailable or disabled.
        //-------------------------------------------------
        public Transform hmdTransform
		{
			get
			{
                if (hmdTransforms != null)
                {
                    for (int i = 0; i < hmdTransforms.Length; i++)
                    {
                        if (hmdTransforms[i].gameObject.activeInHierarchy)
                            return hmdTransforms[i];
                    }
                }
				return null;
			}
		}


		//-------------------------------------------------
		// Height of the eyes above the ground - useful for estimating player height.
		//-------------------------------------------------
		public float eyeHeight
		{
			get
			{
				Transform hmd = hmdTransform;
				if ( hmd )
				{
					Vector3 eyeOffset = Vector3.Project( hmd.position - trackingOriginTransform.position, trackingOriginTransform.up );
					return eyeOffset.magnitude / trackingOriginTransform.lossyScale.x;
				}
				return 0.0f;
			}
		}


		//-------------------------------------------------
		// Guess for the world-space position of the player's feet, directly beneath the HMD.
		//-------------------------------------------------
		public Vector3 feetPositionGuess
		{
			get
			{
				Transform hmd = hmdTransform;
				if ( hmd )
				{
					return trackingOriginTransform.position + Vector3.ProjectOnPlane( hmd.position - trackingOriginTransform.position, trackingOriginTransform.up );
				}
				return trackingOriginTransform.position;
			}
		}


		//-------------------------------------------------
		// Guess for the world-space direction of the player's hips/torso. This is effectively just the gaze direction projected onto the floor plane.
		//-------------------------------------------------
		public Vector3 bodyDirectionGuess
		{
			get
			{
				Transform hmd = hmdTransform;
				if ( hmd )
				{
					Vector3 direction = Vector3.ProjectOnPlane( hmd.forward, trackingOriginTransform.up );
					if ( Vector3.Dot( hmd.up, trackingOriginTransform.up ) < 0.0f )
					{
						// The HMD is upside-down. Either
						// -The player is bending over backwards
						// -The player is bent over looking through their legs
						direction = -direction;
					}
					return direction;
				}
				return trackingOriginTransform.forward;
			}
		}


		//-------------------------------------------------
		private void Awake()
		{
			if ( trackingOriginTransform == null )
			{
				trackingOriginTransform = this.transform;
			}
		}


		//-------------------------------------------------
		private IEnumerator Start()
		{
			_instance = this;

            while (SteamVR.initializedState == SteamVR.InitializedStates.None || SteamVR.initializedState == SteamVR.InitializedStates.Initializing)
                yield return null;

			if ( SteamVR.instance != null )
			{
				ActivateRig( rigSteamVR );
			}
			else
			{
#if !HIDE_DEBUG_UI
				ActivateRig( rig2DFallback );
#endif
			}
        }

        protected virtual void Update()
        {
            if (SteamVR.initializedState != SteamVR.InitializedStates.InitializeSuccess)
                return;

            if (headsetOnHead != null)
            {
                if (headsetOnHead.GetStateDown(SteamVR_Input_Sources.Head))
                {
                    Debug.Log("<b>SteamVR Interaction System</b> Headset placed on head");
                }
                else if (headsetOnHead.GetStateUp(SteamVR_Input_Sources.Head))
                {
                    Debug.Log("<b>SteamVR Interaction System</b> Headset removed");
                }
            }
        }

		//-------------------------------------------------
		void OnDrawGizmos()
		{
			if ( this != instance )
			{
				return;
			}

			//NOTE: These gizmo icons don't work in the plugin since the icons need to exist in a specific "Gizmos"
			//		folder in your Asset tree. These icons are included under Core/Icons. Moving them into a
			//		"Gizmos" folder should make them work again.

			Gizmos.color = Color.white;
			Gizmos.DrawIcon( feetPositionGuess, "vr_interaction_system_feet.png" );

			Gizmos.color = Color.cyan;
			Gizmos.DrawLine( feetPositionGuess, feetPositionGuess + trackingOriginTransform.up * eyeHeight );

			// Body direction arrow
			Gizmos.color = Color.blue;
			Vector3 bodyDirection = bodyDirectionGuess;
			Vector3 bodyDirectionTangent = Vector3.Cross( trackingOriginTransform.up, bodyDirection );
			Vector3 startForward = feetPositionGuess + trackingOriginTransform.up * eyeHeight * 0.75f;
			Vector3 endForward = startForward + bodyDirection * 0.33f;
			Gizmos.DrawLine( startForward, endForward );
			Gizmos.DrawLine( endForward, endForward - 0.033f * ( bodyDirection + bodyDirectionTangent ) );
			Gizmos.DrawLine( endForward, endForward - 0.033f * ( bodyDirection - bodyDirectionTangent ) );

			Gizmos.color = Color.red;
			int count = handCount;
			for ( int i = 0; i < count; i++ )
			{
				Hand hand = GetHand( i );

				if ( hand.handType == SteamVR_Input_Sources.LeftHand)
				{
					Gizmos.DrawIcon( hand.transform.position, "vr_interaction_system_left_hand.png" );
				}
				else if ( hand.handType == SteamVR_Input_Sources.RightHand)
				{
					Gizmos.DrawIcon( hand.transform.position, "vr_interaction_system_right_hand.png" );
				}
				else
				{
                    /*
					Hand.HandType guessHandType = hand.currentHandType;

					if ( guessHandType == Hand.HandType.Left )
					{
						Gizmos.DrawIcon( hand.transform.position, "vr_interaction_system_left_hand_question.png" );
					}
					else if ( guessHandType == Hand.HandType.Right )
					{
						Gizmos.DrawIcon( hand.transform.position, "vr_interaction_system_right_hand_question.png" );
					}
					else
					{
						Gizmos.DrawIcon( hand.transform.position, "vr_interaction_system_unknown_hand.png" );
					}
                    */
				}
			}
		}


		//-------------------------------------------------
		public void Draw2DDebug()
		{
			if ( !allowToggleTo2D )
				return;

			if ( !SteamVR.active )
				return;

			int width = 100;
			int height = 25;
			int left = Screen.width / 2 - width / 2;
			int top = Screen.height - height - 10;

			string text = ( rigSteamVR.activeSelf ) ? "2D Debug" : "VR";

			if ( GUI.Button( new Rect( left, top, width, height ), text ) )
			{
				if ( rigSteamVR.activeSelf )
				{
					ActivateRig( rig2DFallback );
				}
				else
				{
					ActivateRig( rigSteamVR );
				}
			}
		}


		//-------------------------------------------------
		private void ActivateRig( GameObject rig )
		{
			rigSteamVR.SetActive( rig == rigSteamVR );
			rig2DFallback.SetActive( rig == rig2DFallback );

			if ( audioListener )
			{
				audioListener.transform.parent = hmdTransform;
				audioListener.transform.localPosition = Vector3.zero;
				audioListener.transform.localRotation = Quaternion.identity;
			}
		}


		//-------------------------------------------------
		public void PlayerShotSelf()
		{
			//Do something appropriate here
		}
	}
}
