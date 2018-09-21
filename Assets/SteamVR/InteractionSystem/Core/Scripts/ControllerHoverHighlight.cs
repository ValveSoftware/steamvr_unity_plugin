//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Highlights the controller when hovering over interactables
//
//=============================================================================

using UnityEngine;
using System.Collections;

namespace Valve.VR.InteractionSystem
{
    //-------------------------------------------------------------------------
    public class ControllerHoverHighlight : MonoBehaviour
    {
        public Material highLightMaterial;
        public bool fireHapticsOnHightlight = true;

        protected Hand hand;

        protected RenderModel renderModel;

        protected SteamVR_Events.Action renderModelLoadedAction;

        protected void Awake()
        {
            hand = GetComponentInParent<Hand>();
        }

        protected void OnHandInitialized(int deviceIndex)
        {
            GameObject renderModelGameObject = GameObject.Instantiate(hand.renderModelPrefab);
            renderModelGameObject.transform.parent = this.transform;
            renderModelGameObject.transform.localPosition = Vector3.zero;
            renderModelGameObject.transform.localRotation = Quaternion.identity;
            renderModelGameObject.transform.localScale = hand.renderModelPrefab.transform.localScale;


            renderModel = renderModelGameObject.GetComponent<RenderModel>();

            renderModel.SetInputSource(hand.handType);
            renderModel.OnHandInitialized(deviceIndex);
            renderModel.SetMaterial(highLightMaterial);

            hand.SetHoverRenderModel(renderModel);
            renderModel.onControllerLoaded += RenderModel_onControllerLoaded;
            renderModel.Hide();
        }

        private void RenderModel_onControllerLoaded()
        {
            renderModel.Hide();
        }


        //-------------------------------------------------
        protected void OnParentHandHoverBegin(Interactable other)
        {
            if (!this.isActiveAndEnabled)
            {
                return;
            }

            if (other.transform.parent != transform.parent)
            {
                ShowHighlight();
            }
        }


        //-------------------------------------------------
        private void OnParentHandHoverEnd(Interactable other)
        {
            HideHighlight();
        }


        //-------------------------------------------------
        private void OnParentHandInputFocusAcquired()
        {
            if (!this.isActiveAndEnabled)
            {
                return;
            }

            if (hand.hoveringInteractable && hand.hoveringInteractable.transform.parent != transform.parent)
            {
                ShowHighlight();
            }
        }


        //-------------------------------------------------
        private void OnParentHandInputFocusLost()
        {
            HideHighlight();
        }


        //-------------------------------------------------
        public void ShowHighlight()
        {
            if (renderModel == null)
            {
                return;
            }

            if (fireHapticsOnHightlight)
            {
                hand.TriggerHapticPulse(500);
            }

            renderModel.Show();
        }


        //-------------------------------------------------
        public void HideHighlight()
        {
            if (renderModel == null)
            {
                return;
            }

            if (fireHapticsOnHightlight)
            {
                hand.TriggerHapticPulse(300);
            }

            renderModel.Hide();
        }
    }
}