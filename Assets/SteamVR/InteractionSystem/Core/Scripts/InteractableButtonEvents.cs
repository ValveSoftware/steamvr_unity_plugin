//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Sends simple controller button events to UnityEvents
//
//=============================================================================

using UnityEngine;
using UnityEngine.Events;

namespace Valve.VR.InteractionSystem
{
	//-------------------------------------------------------------------------
	[RequireComponent( typeof( Interactable ) )]
	public class InteractableButtonEvents : MonoBehaviour
	{
		public UnityEvent onTriggerDown;
		public UnityEvent onTriggerUp;
		public UnityEvent onGripDown;
		public UnityEvent onGripUp;
		public UnityEvent onTouchpadDown;
		public UnityEvent onTouchpadUp;
		public UnityEvent onTouchpadTouch;
		public UnityEvent onTouchpadRelease;

		//-------------------------------------------------
		void Update()
		{
			for ( int i = 0; i < Player.instance.handCount; i++ )
			{
				Hand hand = Player.instance.GetHand( i );

				if ( hand.isActive )
				{
					//todo: delete this
				}
			}

		}
	}
}
