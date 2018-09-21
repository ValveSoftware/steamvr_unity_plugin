//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Demonstrates the use of the controller hint system
//
//=============================================================================

using UnityEngine;
using System.Collections;
using Valve.VR;

namespace Valve.VR.InteractionSystem.Sample
{
	//-------------------------------------------------------------------------
	public class ControllerHintsExample : MonoBehaviour
	{
		private Coroutine buttonHintCoroutine;
		private Coroutine textHintCoroutine;

		//-------------------------------------------------
		public void ShowButtonHints( Hand hand )
		{
			if ( buttonHintCoroutine != null )
			{
				StopCoroutine( buttonHintCoroutine );
			}
			buttonHintCoroutine = StartCoroutine( TestButtonHints( hand ) );
		}


		//-------------------------------------------------
		public void ShowTextHints( Hand hand )
		{
			if ( textHintCoroutine != null )
			{
				StopCoroutine( textHintCoroutine );
			}
			textHintCoroutine = StartCoroutine( TestTextHints( hand ) );
		}


		//-------------------------------------------------
		public void DisableHints()
		{
			if ( buttonHintCoroutine != null )
			{
				StopCoroutine( buttonHintCoroutine );
				buttonHintCoroutine = null;
			}

			if ( textHintCoroutine != null )
			{
				StopCoroutine( textHintCoroutine );
				textHintCoroutine = null;
			}

			foreach ( Hand hand in Player.instance.hands )
			{
				ControllerButtonHints.HideAllButtonHints( hand );
				ControllerButtonHints.HideAllTextHints( hand );
			}
		}


		//-------------------------------------------------
		// Cycles through all the button hints on the controller
		//-------------------------------------------------
		private IEnumerator TestButtonHints( Hand hand )
		{
			ControllerButtonHints.HideAllButtonHints( hand );

			while ( true )
            {
                for (int actionIndex = 0; actionIndex < SteamVR_Input.actionsIn.Length; actionIndex++)
                {
                    SteamVR_Action_In action = (SteamVR_Action_In)SteamVR_Input.actionsIn[actionIndex];
                    if (action.GetActive(hand.handType))
                    {
                        ControllerButtonHints.ShowButtonHint(hand, action);
                        yield return new WaitForSeconds(1.0f);
                        ControllerButtonHints.HideButtonHint(hand, action);
                        yield return new WaitForSeconds(0.5f);
                    }
                    yield return null;
                }

				ControllerButtonHints.HideAllButtonHints( hand );
				yield return new WaitForSeconds( 1.0f );
			}
		}


		//-------------------------------------------------
		// Cycles through all the text hints on the controller
		//-------------------------------------------------
		private IEnumerator TestTextHints( Hand hand )
		{
			ControllerButtonHints.HideAllTextHints( hand );

			while ( true )
            {
                for (int actionIndex = 0; actionIndex < SteamVR_Input.actionsIn.Length; actionIndex++)
                {
                    SteamVR_Action_In action = (SteamVR_Action_In)SteamVR_Input.actionsIn[actionIndex];
                    if (action.GetActive(hand.handType))
                    {
                        ControllerButtonHints.ShowTextHint(hand, action, action.GetShortName());
                        yield return new WaitForSeconds(3.0f);
                        ControllerButtonHints.HideTextHint(hand, action);
                        yield return new WaitForSeconds(0.5f);
                    }
                    yield return null;
                }

                ControllerButtonHints.HideAllTextHints(hand);
                yield return new WaitForSeconds(3.0f);
			}
		}
	}
}
