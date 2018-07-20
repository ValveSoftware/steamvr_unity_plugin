//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: This object will get hover events and can be attached to the hands
//
//=============================================================================

using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

namespace Valve.VR.InteractionSystem
{
	//-------------------------------------------------------------------------
	public class InteractableDebug : MonoBehaviour
	{
        [System.NonSerialized]
        public Hand attachedToHand;

        public float simulateReleasesForXSecondsAroundRelease = 0;
        public float simulateReleasesEveryXSeconds = 0.005f;
        public bool setPositionsForSimulations = false;

        private Renderer[] selfRenderers;

        private Collider[] colliders;

        private Color lastColor;

        private Throwable throwable;
        private bool isThrowable { get { return throwable != null; } }

        private const bool onlyColorOnChange = true;

        public new Rigidbody rigidbody;

        private void Awake()
        {
            selfRenderers = this.GetComponentsInChildren<Renderer>();
            throwable = this.GetComponent<Throwable>();
            rigidbody = this.GetComponent<Rigidbody>();
            colliders = this.GetComponentsInChildren<Collider>();
        }

        private void OnAttachedToHand( Hand hand )
		{
            attachedToHand = hand;

            CreateMarker(Color.green);
        }


        protected virtual void HandAttachedUpdate(Hand hand)
        {
            Color grabbedColor;
            switch (hand.currentAttachedObjectInfo.Value.grabbedWithType)
            {
                case GrabTypes.Grip:
                    grabbedColor = Color.blue;
                    break;
                case GrabTypes.Pinch:
                    grabbedColor = Color.green;
                    break;
                case GrabTypes.Trigger:
                    grabbedColor = Color.yellow;
                    break;
                case GrabTypes.Scripted:
                    grabbedColor = Color.red;
                    break;
                case GrabTypes.None:
                default:
                    grabbedColor = Color.white;
                    break;
            }

            if ((onlyColorOnChange && grabbedColor != lastColor) || onlyColorOnChange == false)
                ColorSelf(grabbedColor);

            lastColor = grabbedColor;
        }

        
        private void OnDetachedFromHand( Hand hand )
		{
            if (isThrowable)
            {
                Vector3 velocity;
                Vector3 angularVelocity;

                throwable.GetReleaseVelocities(hand, out velocity, out angularVelocity);

                CreateMarker(Color.cyan, velocity.normalized);
            }

            CreateMarker(Color.red);
            attachedToHand = null;

            if (isSimulation == false && simulateReleasesForXSecondsAroundRelease != 0)
            {
                float startTime = -simulateReleasesForXSecondsAroundRelease;
                float endTime = simulateReleasesForXSecondsAroundRelease;

                List<InteractableDebug> list = new List<InteractableDebug>();
                list.Add(this);

                for (float offset = startTime; offset <= endTime; offset += simulateReleasesEveryXSeconds)
                {
                    float lerp = Mathf.InverseLerp(startTime, endTime, offset);
                    InteractableDebug copy = CreateSimulation(hand, offset, Color.Lerp(Color.red, Color.green, lerp));
                    list.Add(copy);
                }

                for (int index = 0; index < list.Count; index++)
                {
                    for (int otherIndex = 0; otherIndex < list.Count; otherIndex++)
                    {
                        list[index].IgnoreObject(list[otherIndex]);
                    }
                }
            }
		}

        public Collider[] GetColliders()
        {
            return colliders;
        }

        public void IgnoreObject(InteractableDebug otherInteractable)
        {
            Collider[] otherColliders = otherInteractable.GetColliders();

            for (int myIndex = 0; myIndex < colliders.Length; myIndex++)
            {
                for (int otherIndex = 0; otherIndex < otherColliders.Length; otherIndex++)
                {
                    Physics.IgnoreCollision(colliders[myIndex], otherColliders[otherIndex]);
                }
            }
        }

        private bool isSimulation = false;
        public void SetIsSimulation()
        {
            isSimulation = true;
        }

        private InteractableDebug CreateSimulation(Hand fromHand, float timeOffset, Color copyColor)
        {
            GameObject copy = GameObject.Instantiate(this.gameObject);
            InteractableDebug debugCopy = copy.GetComponent<InteractableDebug>();
            debugCopy.SetIsSimulation();
            debugCopy.ColorSelf(copyColor);
            copy.name = string.Format("{0} [offset: {1:0.000}]", copy.name, timeOffset);

            Vector3 velocity = fromHand.GetTrackedObjectVelocity(timeOffset);
            velocity *= throwable.scaleReleaseVelocity;

            debugCopy.rigidbody.velocity = velocity;

            return debugCopy;
        }

        private void CreateMarker(Color markerColor, float destroyAfter = 10)
        {
            CreateMarker(markerColor, attachedToHand.GetTrackedObjectVelocity().normalized, destroyAfter);
        }

        private void CreateMarker(Color markerColor, Vector3 forward, float destroyAfter = 10)
        {
            GameObject baseMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            DestroyImmediate(baseMarker.GetComponent<Collider>());
            baseMarker.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);

            GameObject line = GameObject.Instantiate(baseMarker);
            line.transform.localScale = new Vector3(0.01f, 0.01f, 0.25f);
            line.transform.parent = baseMarker.transform;
            line.transform.localPosition = new Vector3(0, 0, line.transform.localScale.z / 2f);

            baseMarker.transform.position = attachedToHand.transform.position;
            baseMarker.transform.forward = forward;

            ColorThing(markerColor, baseMarker.GetComponentsInChildren<Renderer>());

            if (destroyAfter > 0)
                Destroy(baseMarker, destroyAfter);
        }

        private void ColorSelf(Color newColor)
        {
            ColorThing(newColor, selfRenderers);
        }

        private void ColorThing(Color newColor, Renderer[] renderers)
        {
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                renderers[rendererIndex].material.color = newColor;
            }
        }
    }
}
