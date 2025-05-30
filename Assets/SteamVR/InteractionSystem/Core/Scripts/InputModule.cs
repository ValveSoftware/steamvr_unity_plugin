//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Makes the hand act as an input module for Unity's event system
//
//=============================================================================
#if UNITY_UGUI_UI || !UNITY_2019_2_OR_NEWER
using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Valve.VR.InteractionSystem
{
	//-------------------------------------------------------------------------
	public class InputModule : BaseInputModule
	{
		private GameObject submitObject;

		//-------------------------------------------------
		private static InputModule _instance;
		public static InputModule instance
		{
			get
			{
				if ( _instance == null )
                {
#if UNITY_2023_1_OR_NEWER
                    _instance = GameObject.FindFirstObjectByType<InputModule>();
#else
					_instance = GameObject.FindObjectOfType<InputModule>();
#endif
                }

                return _instance;
			}
		}

        protected override void OnEnable()
        {
            base.OnEnable();
#if UNITY_2019_1_OR_NEWER && ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
			StandaloneInputModule standaloneInputModule = this.GetComponent<StandaloneInputModule>();
			if (standaloneInputModule)
			{
				GameObject.Destroy(standaloneInputModule);
				if (this.GetComponent<InputSystemUIInputModule>() == null)
					this.gameObject.AddComponent<InputSystemUIInputModule>();
			}
#endif
		}


        //-------------------------------------------------
        public override bool ShouldActivateModule()
		{
			if ( !base.ShouldActivateModule() )
				return false;

			return submitObject != null;
		}


		//-------------------------------------------------
		public void HoverBegin( GameObject gameObject )
		{
			PointerEventData pointerEventData = new PointerEventData( eventSystem );
			ExecuteEvents.Execute( gameObject, pointerEventData, ExecuteEvents.pointerEnterHandler );
		}


		//-------------------------------------------------
		public void HoverEnd( GameObject gameObject )
		{
			PointerEventData pointerEventData = new PointerEventData( eventSystem );
			pointerEventData.selectedObject = null;
			ExecuteEvents.Execute( gameObject, pointerEventData, ExecuteEvents.pointerExitHandler );
		}


		//-------------------------------------------------
		public void Submit( GameObject gameObject )
		{
			submitObject = gameObject;
		}


		//-------------------------------------------------
		public override void Process()
		{
			if ( submitObject )
			{
				BaseEventData data = GetBaseEventData();
				data.selectedObject = submitObject;
				ExecuteEvents.Execute( submitObject, data, ExecuteEvents.submitHandler );

				submitObject = null;
			}
		}
	}
}
#else //if we haven't run the xr install script yet use this
using UnityEngine;
namespace Valve.VR.InteractionSystem { public class InputModule : MonoBehaviour {} }
#endif