//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Displays text and button hints on the controllers
//
//=============================================================================

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Text;

namespace Valve.VR.InteractionSystem
{
	//-------------------------------------------------------------------------
	public class ControllerButtonHints : MonoBehaviour
	{
		public Material controllerMaterial;
		public Color flashColor = new Color( 1.0f, 0.557f, 0.0f );
		public GameObject textHintPrefab;

        public SteamVR_Action_Vibration hapticFlash = SteamVR_Input.GetAction<SteamVR_Action_Vibration>("Haptic");

        public bool autoSetWithControllerRangeOfMotion = true;

        [Header( "Debug" )]
		public bool debugHints = false;

		private SteamVR_RenderModel renderModel;
		private Player player;

		private List<MeshRenderer> renderers = new List<MeshRenderer>();
		private List<MeshRenderer> flashingRenderers = new List<MeshRenderer>();
		private float startTime;
		private float tickCount;

		private enum OffsetType
		{
			Up,
			Right,
			Forward,
			Back
		}

		//Info for each of the buttons
		private class ActionHintInfo
		{
			public string componentName;
			public List<MeshRenderer> renderers;
			public Transform localTransform;

			//Text hint
			public GameObject textHintObject;
			public Transform textStartAnchor;
			public Transform textEndAnchor;
			public Vector3 textEndOffsetDir;
			public Transform canvasOffset;

			public Text text;
			public TextMesh textMesh;
			public Canvas textCanvas;
			public LineRenderer line;

			public float distanceFromCenter;
			public bool textHintActive = false;
		}

		private Dictionary<ISteamVR_Action_In_Source, ActionHintInfo> actionHintInfos;
		private Transform textHintParent;

		private int colorID;

		public bool initialized { get; private set; }
		private Vector3 centerPosition = Vector3.zero;

		SteamVR_Events.Action renderModelLoadedAction;

        protected SteamVR_Input_Sources inputSource;

        //-------------------------------------------------
        void Awake()
		{
			renderModelLoadedAction = SteamVR_Events.RenderModelLoadedAction( OnRenderModelLoaded );
			colorID = Shader.PropertyToID( "_Color" );
		}


		//-------------------------------------------------
		void Start()
		{
			player = Player.instance;
		}


		//-------------------------------------------------
		private void HintDebugLog( string msg )
		{
			if ( debugHints )
			{
				Debug.Log("<b>[SteamVR Interaction]</b> Hints: " + msg );
			}
		}


		//-------------------------------------------------
		void OnEnable()
		{
			renderModelLoadedAction.enabled = true;
		}


		//-------------------------------------------------
		void OnDisable()
		{
			renderModelLoadedAction.enabled = false;
			Clear();
		}


		//-------------------------------------------------
		private void OnParentHandInputFocusLost()
		{
			//Hide all the hints when the controller is no longer the primary attached object
			HideAllButtonHints();
			HideAllText();
		}


        public virtual void SetInputSource(SteamVR_Input_Sources newInputSource)
        {
            inputSource = newInputSource;
            if (renderModel != null)
                renderModel.SetInputSource(newInputSource);
        }

        //-------------------------------------------------
        // Gets called when the hand has been initialized and a render model has been set
        //-------------------------------------------------
        private void OnHandInitialized(int deviceIndex)
        {
			//Create a new render model for the controller hints
			renderModel = new GameObject( "SteamVR_RenderModel" ).AddComponent<SteamVR_RenderModel>();
			renderModel.transform.parent = transform;
			renderModel.transform.localPosition = Vector3.zero;
			renderModel.transform.localRotation = Quaternion.identity;
			renderModel.transform.localScale = Vector3.one;

            renderModel.SetInputSource(inputSource);
            renderModel.SetDeviceIndex(deviceIndex);

            if ( !initialized )
			{
				//The controller hint render model needs to be active to get accurate transforms for all the individual components
				renderModel.gameObject.SetActive( true );
			}
		}


        private Dictionary<string, Transform> componentTransformMap = new Dictionary<string, Transform>();

        //-------------------------------------------------
        void OnRenderModelLoaded(SteamVR_RenderModel renderModel, bool succeess)
        {
            //Only initialize when the render model for the controller hints has been loaded
            if (renderModel == this.renderModel)
            {
                //Debug.Log("<b>[SteamVR Interaction]</b> OnRenderModelLoaded: " + this.renderModel.renderModelName);
                if (initialized)
                {
                    Destroy(textHintParent.gameObject);
                    componentTransformMap.Clear();
                    flashingRenderers.Clear();
                }

                renderModel.SetMeshRendererState(false);

                StartCoroutine(DoInitialize(renderModel));
            }
        }
        private IEnumerator DoInitialize(SteamVR_RenderModel renderModel)
        {
            while (renderModel.initializedAttachPoints == false)
                yield return null;

            textHintParent = new GameObject("Text Hints").transform;
            textHintParent.SetParent(this.transform);
            textHintParent.localPosition = Vector3.zero;
            textHintParent.localRotation = Quaternion.identity;
            textHintParent.localScale = Vector3.one;

            //Get the button mask for each component of the render model

            var renderModels = OpenVR.RenderModels;
            if (renderModels != null)
            {
                string renderModelDebug = "";

                if (debugHints)
                    renderModelDebug = "Components for render model " + renderModel.index;

                for (int childIndex = 0; childIndex < renderModel.transform.childCount; childIndex++)
                {
                    Transform child = renderModel.transform.GetChild(childIndex);

                    if (componentTransformMap.ContainsKey(child.name))
                    {
                        if (debugHints)
                            renderModelDebug += "\n\t!    Child component already exists with name: " + child.name;
                    }
                    else
                        componentTransformMap.Add(child.name, child);

                    if (debugHints)
                        renderModelDebug += "\n\t" + child.name + ".";
                }

                //Uncomment to show the button mask for each component of the render model
                HintDebugLog(renderModelDebug);
            }

            actionHintInfos = new Dictionary<ISteamVR_Action_In_Source, ActionHintInfo>();

            for (int actionIndex = 0; actionIndex < SteamVR_Input.actionsNonPoseNonSkeletonIn.Length; actionIndex++)
            {
                ISteamVR_Action_In action = SteamVR_Input.actionsNonPoseNonSkeletonIn[actionIndex];

                if (action.GetActive(inputSource))
                    CreateAndAddButtonInfo(action, inputSource);
            }

            ComputeTextEndTransforms();

            initialized = true;

            //Set the controller hints render model to not active
            renderModel.SetMeshRendererState(true);
            renderModel.gameObject.SetActive(false);
        }


		//-------------------------------------------------
		private void CreateAndAddButtonInfo(ISteamVR_Action_In action, SteamVR_Input_Sources inputSource)
		{
			Transform buttonTransform = null;
			List<MeshRenderer> buttonRenderers = new List<MeshRenderer>();

            StringBuilder buttonDebug = new StringBuilder();
            buttonDebug.Append("Looking for action: ");

            buttonDebug.AppendLine(action.GetShortName());

            buttonDebug.Append("Action localized origin: ");
            buttonDebug.AppendLine(action.GetLocalizedOrigin(inputSource));

            string actionComponentName = action.GetRenderModelComponentName(inputSource);

            if (componentTransformMap.ContainsKey(actionComponentName))
            {
                buttonDebug.AppendLine(string.Format("Found component: {0} for {1}", actionComponentName, action.GetShortName()));
                Transform componentTransform = componentTransformMap[actionComponentName];

                buttonTransform = componentTransform;

                buttonDebug.AppendLine(string.Format("Found componentTransform: {0}. buttonTransform: {1}", componentTransform, buttonTransform));

                buttonRenderers.AddRange(componentTransform.GetComponentsInChildren<MeshRenderer>());
            }
            else
            {
                buttonDebug.AppendLine(string.Format("Can't find component transform for action: {0}. Component name: \"{1}\"", action.GetShortName(), actionComponentName));
            }

            buttonDebug.AppendLine(string.Format("Found {0} renderers for {1}", buttonRenderers.Count, action.GetShortName()));

			foreach ( MeshRenderer renderer in buttonRenderers )
			{
                buttonDebug.Append("\t");
                buttonDebug.AppendLine(renderer.name);
			}

			HintDebugLog( buttonDebug.ToString() );

			if ( buttonTransform == null )
			{
				HintDebugLog( "Couldn't find buttonTransform for " + action.GetShortName());
				return;
			}

			ActionHintInfo hintInfo = new ActionHintInfo();
			actionHintInfos.Add( action, hintInfo );

			hintInfo.componentName = buttonTransform.name;
			hintInfo.renderers = buttonRenderers;

			//Get the local transform for the button
            for (int childIndex = 0; childIndex < buttonTransform.childCount; childIndex++)
            {
                Transform child = buttonTransform.GetChild(childIndex);
                if (child.name == SteamVR_RenderModel.k_localTransformName)
                    hintInfo.localTransform = child;
            }

			OffsetType offsetType = OffsetType.Right;

            /*
            switch ( buttonID )
			{
				case EVRButtonId.k_EButton_SteamVR_Trigger:
					{
						offsetType = OffsetType.Right;
					}
					break;
				case EVRButtonId.k_EButton_ApplicationMenu:
					{
						offsetType = OffsetType.Right;
					}
					break;
				case EVRButtonId.k_EButton_System:
					{
						offsetType = OffsetType.Right;
					}
					break;
				case Valve.VR.EVRButtonId.k_EButton_Grip:
					{
						offsetType = OffsetType.Forward;
					}
					break;
				case Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad:
					{
						offsetType = OffsetType.Up;
					}
					break;
			}
            */

            //Offset for the text end transform
            switch ( offsetType )
			{
				case OffsetType.Forward:
					hintInfo.textEndOffsetDir = hintInfo.localTransform.forward;
					break;
				case OffsetType.Back:
					hintInfo.textEndOffsetDir = -hintInfo.localTransform.forward;
					break;
				case OffsetType.Right:
					hintInfo.textEndOffsetDir = hintInfo.localTransform.right;
					break;
				case OffsetType.Up:
					hintInfo.textEndOffsetDir = hintInfo.localTransform.up;
					break;
			}

			//Create the text hint object
			Vector3 hintStartPos = hintInfo.localTransform.position + ( hintInfo.localTransform.forward * 0.01f );
			hintInfo.textHintObject = GameObject.Instantiate( textHintPrefab, hintStartPos, Quaternion.identity ) as GameObject;
			hintInfo.textHintObject.name = "Hint_" + hintInfo.componentName + "_Start";
			hintInfo.textHintObject.transform.SetParent( textHintParent );
			hintInfo.textHintObject.layer = gameObject.layer;
			hintInfo.textHintObject.tag = gameObject.tag;

			//Get all the relevant child objects
			hintInfo.textStartAnchor = hintInfo.textHintObject.transform.Find( "Start" );
			hintInfo.textEndAnchor = hintInfo.textHintObject.transform.Find( "End" );
			hintInfo.canvasOffset = hintInfo.textHintObject.transform.Find( "CanvasOffset" );
			hintInfo.line = hintInfo.textHintObject.transform.Find( "Line" ).GetComponent<LineRenderer>();
			hintInfo.textCanvas = hintInfo.textHintObject.GetComponentInChildren<Canvas>();
			hintInfo.text = hintInfo.textCanvas.GetComponentInChildren<Text>();
			hintInfo.textMesh = hintInfo.textCanvas.GetComponentInChildren<TextMesh>();

			hintInfo.textHintObject.SetActive( false );

			hintInfo.textStartAnchor.position = hintStartPos;

			if ( hintInfo.text != null )
			{
				hintInfo.text.text = hintInfo.componentName;
			}

			if ( hintInfo.textMesh != null )
			{
				hintInfo.textMesh.text = hintInfo.componentName;
			}

			centerPosition += hintInfo.textStartAnchor.position;

			// Scale hint components to match player size
			hintInfo.textCanvas.transform.localScale = Vector3.Scale( hintInfo.textCanvas.transform.localScale, player.transform.localScale );
			hintInfo.textStartAnchor.transform.localScale = Vector3.Scale( hintInfo.textStartAnchor.transform.localScale, player.transform.localScale );
			hintInfo.textEndAnchor.transform.localScale = Vector3.Scale( hintInfo.textEndAnchor.transform.localScale, player.transform.localScale );
			hintInfo.line.transform.localScale = Vector3.Scale( hintInfo.line.transform.localScale, player.transform.localScale );
		}


		//-------------------------------------------------
		private void ComputeTextEndTransforms()
		{
			//This is done as a separate step after all the ButtonHintInfos have been initialized
			//to make the text hints fan out appropriately based on the button's position on the controller.

			centerPosition /= actionHintInfos.Count;
			float maxDistanceFromCenter = 0.0f;

			foreach ( var hintInfo in actionHintInfos )
			{
				hintInfo.Value.distanceFromCenter = Vector3.Distance( hintInfo.Value.textStartAnchor.position, centerPosition );

				if ( hintInfo.Value.distanceFromCenter > maxDistanceFromCenter )
				{
					maxDistanceFromCenter = hintInfo.Value.distanceFromCenter;
				}
			}

			foreach ( var hintInfo in actionHintInfos )
			{
				Vector3 centerToButton = hintInfo.Value.textStartAnchor.position - centerPosition;
				centerToButton.Normalize();

				centerToButton = Vector3.Project( centerToButton, renderModel.transform.forward );

				//Spread out the text end positions based on the distance from the center
				float t = hintInfo.Value.distanceFromCenter / maxDistanceFromCenter;
				float scale = hintInfo.Value.distanceFromCenter * Mathf.Pow( 2, 10 * ( t - 1.0f ) ) * 20.0f;

				//Flip the direction of the end pos based on which hand this is
				float endPosOffset = 0.1f;

				Vector3 hintEndPos = hintInfo.Value.textStartAnchor.position + ( hintInfo.Value.textEndOffsetDir * endPosOffset ) + ( centerToButton * scale * 0.1f );

                if (SteamVR_Utils.IsValid(hintEndPos))
                {
                    hintInfo.Value.textEndAnchor.position = hintEndPos;

                    hintInfo.Value.canvasOffset.position = hintEndPos;
                }
                else
                {
                    Debug.LogWarning("<b>[SteamVR Interaction]</b> Invalid end position for: " + hintInfo.Value.textStartAnchor.name, hintInfo.Value.textStartAnchor.gameObject);
                }
				hintInfo.Value.canvasOffset.localRotation = Quaternion.identity;
			}
		}


		//-------------------------------------------------
		private void ShowButtonHint( params ISteamVR_Action_In_Source[] actions )
		{
			renderModel.gameObject.SetActive( true );

			renderModel.GetComponentsInChildren<MeshRenderer>( renderers );
			for ( int i = 0; i < renderers.Count; i++ )
			{
				Texture mainTexture = renderers[i].material.mainTexture;
				renderers[i].sharedMaterial = controllerMaterial;
				renderers[i].material.mainTexture = mainTexture;

				// This is to poke unity into setting the correct render queue for the model
				renderers[i].material.renderQueue = controllerMaterial.shader.renderQueue;
			}

			for ( int i = 0; i < actions.Length; i++ )
			{
				if ( actionHintInfos.ContainsKey( actions[i] ) )
				{
					ActionHintInfo hintInfo = actionHintInfos[actions[i]];
					foreach ( MeshRenderer renderer in hintInfo.renderers )
					{
						if ( !flashingRenderers.Contains( renderer ) )
						{
							flashingRenderers.Add( renderer );
						}
					}
				}
			}

			startTime = Time.realtimeSinceStartup;
			tickCount = 0.0f;
		}


		//-------------------------------------------------
		private void HideAllButtonHints()
		{
			Clear();

            if (renderModel != null && renderModel.gameObject != null)
			    renderModel.gameObject.SetActive( false );
		}


		//-------------------------------------------------
		private void HideButtonHint( params ISteamVR_Action_In_Source[] actions )
		{
			Color baseColor = controllerMaterial.GetColor( colorID );
			for ( int i = 0; i < actions.Length; i++ )
			{
				if ( actionHintInfos.ContainsKey(actions[i] ) )
				{
					ActionHintInfo hintInfo = actionHintInfos[actions[i]];
					foreach ( MeshRenderer renderer in hintInfo.renderers )
					{
						renderer.material.color = baseColor;
						flashingRenderers.Remove( renderer );
					}
				}
			}

			if ( flashingRenderers.Count == 0 )
			{
				renderModel.gameObject.SetActive( false );
			}
		}




		//-------------------------------------------------
		private bool IsButtonHintActive(ISteamVR_Action_In_Source action )
		{
			if ( actionHintInfos.ContainsKey(action) )
			{
				ActionHintInfo hintInfo = actionHintInfos[action];
				foreach ( MeshRenderer buttonRenderer in hintInfo.renderers )
				{
					if ( flashingRenderers.Contains( buttonRenderer ) )
					{
						return true;
					}
				}
			}

			return false;
		}


		//-------------------------------------------------
		private IEnumerator TestButtonHints()
		{
			while ( true )
			{
                for (int actionIndex = 0; actionIndex < SteamVR_Input.actionsNonPoseNonSkeletonIn.Length; actionIndex++)
                {
                    ISteamVR_Action_In action = SteamVR_Input.actionsNonPoseNonSkeletonIn[actionIndex];
                    if (action.GetActive(inputSource))
                    {
                        ShowButtonHint(action);
                        yield return new WaitForSeconds(1.0f);
                    }
                    yield return null;
                }
			}
		}


		//-------------------------------------------------
		private IEnumerator TestTextHints()
		{
			while ( true )
            {
                for (int actionIndex = 0; actionIndex < SteamVR_Input.actionsNonPoseNonSkeletonIn.Length; actionIndex++)
                {
                    ISteamVR_Action_In action = SteamVR_Input.actionsNonPoseNonSkeletonIn[actionIndex];
                    if (action.GetActive(inputSource))
                    {
                        ShowText(action, action.GetShortName());
                        yield return new WaitForSeconds(3.0f);
                    }
                    yield return null;
                }

				HideAllText();
				yield return new WaitForSeconds( 3.0f );
			}
		}


		//-------------------------------------------------
		void Update()
		{
			if ( renderModel != null && renderModel.gameObject.activeInHierarchy && flashingRenderers.Count > 0 )
			{
				Color baseColor = controllerMaterial.GetColor( colorID );

				float flash = ( Time.realtimeSinceStartup - startTime ) * Mathf.PI * 2.0f;
				flash = Mathf.Cos( flash );
				flash = Util.RemapNumberClamped( flash, -1.0f, 1.0f, 0.0f, 1.0f );

				float ticks = ( Time.realtimeSinceStartup - startTime );
				if ( ticks - tickCount > 1.0f )
				{
					tickCount += 1.0f;
                    hapticFlash.Execute(0, 0.005f, 0.005f, 1, inputSource);
				}

				for ( int i = 0; i < flashingRenderers.Count; i++ )
				{
					Renderer r = flashingRenderers[i];
					r.material.SetColor( colorID, Color.Lerp( baseColor, flashColor, flash ) );
				}

				if ( initialized )
				{
					foreach ( var hintInfo in actionHintInfos )
					{
						if ( hintInfo.Value.textHintActive )
						{
							UpdateTextHint( hintInfo.Value );
						}
					}
				}
			}
		}


		//-------------------------------------------------
		private void UpdateTextHint( ActionHintInfo hintInfo )
		{
			Transform playerTransform = player.hmdTransform;
			Vector3 vDir = playerTransform.position - hintInfo.canvasOffset.position;

			Quaternion standardLookat = Quaternion.LookRotation( vDir, Vector3.up );
			Quaternion upsideDownLookat = Quaternion.LookRotation( vDir, playerTransform.up );

			float flInterp;
			if ( playerTransform.forward.y > 0.0f )
			{
				flInterp = Util.RemapNumberClamped( playerTransform.forward.y, 0.6f, 0.4f, 1.0f, 0.0f );
			}
			else
			{
				flInterp = Util.RemapNumberClamped( playerTransform.forward.y, -0.8f, -0.6f, 1.0f, 0.0f );
			}

			hintInfo.canvasOffset.rotation = Quaternion.Slerp( standardLookat, upsideDownLookat, flInterp );

			Transform lineTransform = hintInfo.line.transform;

			hintInfo.line.useWorldSpace = false;
			hintInfo.line.SetPosition( 0, lineTransform.InverseTransformPoint( hintInfo.textStartAnchor.position ) );
			hintInfo.line.SetPosition( 1, lineTransform.InverseTransformPoint( hintInfo.textEndAnchor.position ) );
		}


		//-------------------------------------------------
		private void Clear()
		{
			renderers.Clear();
			flashingRenderers.Clear();
		}


		//-------------------------------------------------
		private void ShowText(ISteamVR_Action_In_Source action, string text, bool highlightButton = true )
        {
            if ( actionHintInfos.ContainsKey(action) )
            {
                ActionHintInfo hintInfo = actionHintInfos[action];
				hintInfo.textHintObject.SetActive( true );
				hintInfo.textHintActive = true;

				if ( hintInfo.text != null )
				{
					hintInfo.text.text = text;
				}

				if ( hintInfo.textMesh != null )
				{
					hintInfo.textMesh.text = text;
				}

				UpdateTextHint( hintInfo );

				if ( highlightButton )
				{
					ShowButtonHint(action);
				}

				renderModel.gameObject.SetActive( true );
			}
		}


		//-------------------------------------------------
		private void HideText(ISteamVR_Action_In_Source action)
		{
			if ( actionHintInfos.ContainsKey(action) )
			{
				ActionHintInfo hintInfo = actionHintInfos[action];
				hintInfo.textHintObject.SetActive( false );
				hintInfo.textHintActive = false;

				HideButtonHint(action);
			}
		}


		//-------------------------------------------------
		private void HideAllText()
		{
            if (actionHintInfos != null)
            {

                foreach (var hintInfo in actionHintInfos)
                {
                    hintInfo.Value.textHintObject.SetActive(false);
                    hintInfo.Value.textHintActive = false;
                }

                HideAllButtonHints();
            }
		}


		//-------------------------------------------------
		private string GetActiveHintText(ISteamVR_Action_In_Source action )
		{
			if ( actionHintInfos.ContainsKey(action) )
			{
				ActionHintInfo hintInfo = actionHintInfos[action];
				if ( hintInfo.textHintActive )
				{
					return hintInfo.text.text;
				}
			}
			return string.Empty;
		}


		//-------------------------------------------------
		// These are the static functions which are used to show/hide the hints
		//-------------------------------------------------

		//-------------------------------------------------
		private static ControllerButtonHints GetControllerButtonHints( Hand hand )
		{
			if ( hand != null )
			{
				ControllerButtonHints hints = hand.GetComponentInChildren<ControllerButtonHints>();
				if ( hints != null && hints.initialized )
				{
					return hints;
				}
			}

			return null;
		}


		//-------------------------------------------------
		public static void ShowButtonHint( Hand hand, params ISteamVR_Action_In_Source[] actions )
		{
			ControllerButtonHints hints = GetControllerButtonHints( hand );
			if ( hints != null )
			{
				hints.ShowButtonHint( actions );
			}
		}


		//-------------------------------------------------
		public static void HideButtonHint( Hand hand, params ISteamVR_Action_In_Source[] actions )
		{
			ControllerButtonHints hints = GetControllerButtonHints( hand );
			if ( hints != null )
			{
				hints.HideButtonHint( actions );
			}
		}


		//-------------------------------------------------
		public static void HideAllButtonHints( Hand hand )
		{
			ControllerButtonHints hints = GetControllerButtonHints( hand );
			if ( hints != null )
			{
				hints.HideAllButtonHints();
			}
		}


		//-------------------------------------------------
		public static bool IsButtonHintActive( Hand hand, ISteamVR_Action_In_Source action )
		{
			ControllerButtonHints hints = GetControllerButtonHints( hand );
			if ( hints != null )
			{
				return hints.IsButtonHintActive(action);
			}

			return false;
		}


		//-------------------------------------------------
		public static void ShowTextHint( Hand hand, ISteamVR_Action_In_Source action, string text, bool highlightButton = true )
        {
            ControllerButtonHints hints = GetControllerButtonHints( hand );
			if ( hints != null )
			{
				hints.ShowText(action, text, highlightButton );

                if (hand != null)
                {
                    if (hints.autoSetWithControllerRangeOfMotion)
                        hand.SetTemporarySkeletonRangeOfMotion(SkeletalMotionRangeChange.WithController);
                }
            }
		}


		//-------------------------------------------------
		public static void HideTextHint( Hand hand, ISteamVR_Action_In_Source action)
		{
			ControllerButtonHints hints = GetControllerButtonHints( hand );
			if ( hints != null )
			{
				hints.HideText(action);

                if (hand != null)
                {
                    if (hints.autoSetWithControllerRangeOfMotion)
                        hand.ResetTemporarySkeletonRangeOfMotion();
                }
            }

		}


		//-------------------------------------------------
		public static void HideAllTextHints( Hand hand )
		{
			ControllerButtonHints hints = GetControllerButtonHints( hand );
			if ( hints != null )
			{
				hints.HideAllText();
			}
		}


		//-------------------------------------------------
		public static string GetActiveHintText( Hand hand, ISteamVR_Action_In_Source action)
		{
			ControllerButtonHints hints = GetControllerButtonHints( hand );
			if ( hints != null )
			{
				return hints.GetActiveHintText(action);
			}

			return string.Empty;
		}
	}
}
