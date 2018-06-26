//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: The hands used by the player in the vr interaction system
//
//=============================================================================

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Valve.VR.InteractionSystem
{
    //-------------------------------------------------------------------------
    // Links with an appropriate SteamVR controller and facilitates
    // interactions with objects in the virtual world.
    //-------------------------------------------------------------------------
    public class Hand : MonoBehaviour
    {
        // The flags used to determine how an object is attached to the hand.
        [Flags]
        public enum AttachmentFlags
        {
            SnapOnAttach = 1 << 0, // The object should snap to the position of the specified attachment point on the hand.
            DetachOthers = 1 << 1, // Other objects attached to this hand will be detached.
            DetachFromOtherHand = 1 << 2, // This object will be detached from the other hand.
            ParentToHand = 1 << 3, // The object will be parented to the hand.
            VelocityMovement = 1 << 4, // The object will attempt to move to match the position and rotation of the hand.
            TurnOnKinematic = 1 << 5, // The object will not respond to external physics.
            TurnOffGravity = 1 << 6, // The object will not respond to external physics.
            AllowSidegrade = 1 << 7, // The object is able to switch from a pinch grab to a grip grab. Decreases likelyhood of a good throw but also decreases likelyhood of accidental drop
        };

        public const AttachmentFlags defaultAttachmentFlags = AttachmentFlags.ParentToHand |
                                                              AttachmentFlags.DetachOthers |
                                                              AttachmentFlags.DetachFromOtherHand |
                                                                AttachmentFlags.TurnOnKinematic |
                                                              AttachmentFlags.SnapOnAttach;

        public Hand otherHand;
        public SteamVR_Input_Input_Sources handType;

        [DefaultInputAction("Pose")]
        public SteamVR_Input_Action_Pose poseAction;

        [DefaultInputAction("GrabPinch")]
        public SteamVR_Input_Action_Boolean grabPinchAction;

        [DefaultInputAction("GrabGrip")]
        public SteamVR_Input_Action_Boolean grabGripAction;

        [DefaultInputAction("Haptic")]
        public SteamVR_Input_Action_Vibration hapticAction;
        
        [DefaultInputAction("InteractUI")]
        public SteamVR_Input_Action_Boolean uiInteractAction;

        public Transform hoverSphereTransform;
        public float hoverSphereRadius = 0.05f;
        public LayerMask hoverLayerMask = -1;
        public float hoverUpdateInterval = 0.1f;

        public Camera noSteamVRFallbackCamera;
        public float noSteamVRFallbackMaxDistanceNoItem = 10.0f;
        public float noSteamVRFallbackMaxDistanceWithItem = 0.5f;
        private float noSteamVRFallbackInteractorDistance = -1.0f;

        public GameObject controllerPrefab;
        private GameObject controllerObject = null;

        public bool showDebugText = false;
        public bool spewDebugText = false;

        protected SteamVR_HistoryBuffer historyBuffer = new SteamVR_HistoryBuffer(30);

        public struct AttachedObject
        {
            public GameObject attachedObject;
            public Rigidbody attachedRigidbody;
            public bool attachedRigidbodyWasKinematic;
            public bool attachedRigidbodyUsedGravity;
            public GameObject originalParent;
            public bool isParentedToHand;
            public GrabTypes grabbedWithType;
            public AttachmentFlags attachmentFlags;
            public Vector3 initialPositionalOffset;
            public Quaternion initialRotationalOffset;

            public bool HasAttachFlag(AttachmentFlags flag)
            {
                return (attachmentFlags & flag) == flag;
            }
        }

        private List<AttachedObject> attachedObjects = new List<AttachedObject>();

        public ReadOnlyCollection<AttachedObject> AttachedObjects
        {
            get { return attachedObjects.AsReadOnly(); }
        }

        public bool hoverLocked { get; private set; }

        private Interactable _hoveringInteractable;

        private TextMesh debugText;
        private int prevOverlappingColliders = 0;

        private const int ColliderArraySize = 16;
        private Collider[] overlappingColliders;

        private Player playerInstance;

        private GameObject applicationLostFocusObject;

        private SteamVR_Events.Action inputFocusAction;

        protected Animator handAnimator;

        protected SpawnRenderModel spawnRenderModel;

        private string animatorParameterStateName = "AnimationState";
        protected int handAnimatorStateId = -1;

        protected SteamVR_Input_Skeleton skeleton;

        public bool isActive
        {
            get
            {
                return poseAction != null && poseAction.GetActive(handType);
            }
        }

        public bool isPoseValid
        {
            get
            {
                return poseAction != null && poseAction.GetPoseIsValid(handType);
            }
        }


        //-------------------------------------------------
        // The Interactable object this Hand is currently hovering over
        //-------------------------------------------------
        public Interactable hoveringInteractable
        {
            get { return _hoveringInteractable; }
            set
            {
                if (_hoveringInteractable != value)
                {
                    if (_hoveringInteractable != null)
                    {
                        if (spewDebugText)
                            HandDebugLog("HoverEnd " + _hoveringInteractable.gameObject);
                        _hoveringInteractable.SendMessage("OnHandHoverEnd", this, SendMessageOptions.DontRequireReceiver);

                        //Note: The _hoveringInteractable can change after sending the OnHandHoverEnd message so we need to check it again before broadcasting this message
                        if (_hoveringInteractable != null)
                        {
                            this.BroadcastMessage("OnParentHandHoverEnd", _hoveringInteractable, SendMessageOptions.DontRequireReceiver); // let objects attached to the hand know that a hover has ended
                        }
                    }

                    _hoveringInteractable = value;

                    if (_hoveringInteractable != null)
                    {
                        if (spewDebugText)
                            HandDebugLog("HoverBegin " + _hoveringInteractable.gameObject);
                        _hoveringInteractable.SendMessage("OnHandHoverBegin", this, SendMessageOptions.DontRequireReceiver);

                        //Note: The _hoveringInteractable can change after sending the OnHandHoverBegin message so we need to check it again before broadcasting this message
                        if (_hoveringInteractable != null)
                        {
                            this.BroadcastMessage("OnParentHandHoverBegin", _hoveringInteractable, SendMessageOptions.DontRequireReceiver); // let objects attached to the hand know that a hover has begun
                        }
                    }
                }
            }
        }


        //-------------------------------------------------
        // Active GameObject attached to this Hand
        //-------------------------------------------------
        public GameObject currentAttachedObject
        {
            get
            {
                CleanUpAttachedObjectStack();

                if (attachedObjects.Count > 0)
                {
                    return attachedObjects[attachedObjects.Count - 1].attachedObject;
                }

                return null;
            }
        }

        public AttachedObject? currentAttachedObjectInfo
        {
            get
            {
                CleanUpAttachedObjectStack();

                if (attachedObjects.Count > 0)
                {
                    return attachedObjects[attachedObjects.Count - 1];
                }

                return null;
            }
        }


        //-------------------------------------------------
        public Transform GetAttachmentTransform(string attachmentPoint = "")
        {
            Transform attachmentTransform = null;

            if (!string.IsNullOrEmpty(attachmentPoint))
            {
                attachmentTransform = transform.Find(attachmentPoint);
            }

            if (!attachmentTransform)
            {
                attachmentTransform = this.transform;
            }

            return attachmentTransform;
        }

        public void ShowController(bool defaultToHidden)
        {
            if (spawnRenderModel != null)
            {
                spawnRenderModel.DefaultControllerToHidden(defaultToHidden);
                spawnRenderModel.ShowController();
            }
        }

        public void ShowController()
        {
            if (spawnRenderModel != null)
                spawnRenderModel.ShowController();
        }

        public void HideController()
        {
            if (spawnRenderModel != null)
                spawnRenderModel.HideController();
        }

        public void Show()
        {
            if (controllerObject != null)
                controllerObject.SetActive(true);
        }

        public void Hide()
        {
            if (controllerObject != null)
                controllerObject.SetActive(false);
        }

        public void HideController(bool defaultToHidden)
        {
            if (spawnRenderModel != null)
            {
                spawnRenderModel.DefaultControllerToHidden(defaultToHidden);
                spawnRenderModel.HideController();
            }
        }

        public void SetSkeletonRangeOfMotion(EVRSkeletalMotionRange newRangeOfMotion)
        {
            if (skeleton != null)
            {
                skeleton.rangeOfMotion = newRangeOfMotion;
            }
        }

        public void SetAnimationState(int stateValue)
        {
            if (skeleton != null && skeleton.isBlending == false)
            {
                skeleton.BlendToAnimation();
            }

            if (CheckAnimatorInit())
            {
                handAnimator.SetInteger(handAnimatorStateId, stateValue);
            }
        }

        public void StopAnimation()
        {
            if (skeleton != null && skeleton.isBlending == false)
            {
                skeleton.BlendToSkeleton();
            }

            if (CheckAnimatorInit())
            {
                handAnimator.SetInteger(handAnimatorStateId, 0);
            }
        }

        private bool CheckAnimatorInit()
        {
            if (handAnimatorStateId == -1 && handAnimator != null)
            {
                if (handAnimator.gameObject.activeInHierarchy && handAnimator.isInitialized)
                {
                    var parameters = handAnimator.parameters;
                    for (int parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++)
                    {
                        if (string.Equals(parameters[parameterIndex].name, animatorParameterStateName, StringComparison.CurrentCultureIgnoreCase))
                            handAnimatorStateId = parameters[parameterIndex].nameHash;
                    }
                }
            }

            return handAnimatorStateId != -1 && handAnimator != null && handAnimator.isInitialized;
        }


        //-------------------------------------------------
        // Attach a GameObject to this GameObject
        //
        // objectToAttach - The GameObject to attach
        // flags - The flags to use for attaching the object
        // attachmentPoint - Name of the GameObject in the hierarchy of this Hand which should act as the attachment point for this GameObject
        //-------------------------------------------------
        public void AttachObject(GameObject objectToAttach, GrabTypes grabbedWithType, AttachmentFlags flags = defaultAttachmentFlags, string attachmentPoint = "")
        {
            AttachedObject attachedObject = new AttachedObject();
            attachedObject.attachmentFlags = flags;

            if (flags == 0)
            {
                flags = defaultAttachmentFlags;
            }

            //Make sure top object on stack is non-null
            CleanUpAttachedObjectStack();

            //Detach the object if it is already attached so that it can get re-attached at the top of the stack
            DetachObject(objectToAttach);

            //Detach from the other hand if requested
            if (attachedObject.HasAttachFlag(AttachmentFlags.DetachFromOtherHand))
            {
                otherHand.DetachObject(objectToAttach);
            }

            if (attachedObject.HasAttachFlag(AttachmentFlags.DetachOthers))
            {
                //Detach all the objects from the stack
                while (attachedObjects.Count > 0)
                {
                    DetachObject(attachedObjects[0].attachedObject);
                }
            }

            if (currentAttachedObject)
            {
                currentAttachedObject.SendMessage("OnHandFocusLost", this, SendMessageOptions.DontRequireReceiver);
            }

            attachedObject.attachedObject = objectToAttach;
            attachedObject.attachedRigidbody = objectToAttach.GetComponent<Rigidbody>();
            if (attachedObject.attachedRigidbody != null)
            {
                attachedObject.attachedRigidbodyWasKinematic = attachedObject.attachedRigidbody.isKinematic;
                attachedObject.attachedRigidbodyUsedGravity = attachedObject.attachedRigidbody.useGravity;
            }
            attachedObject.initialPositionalOffset = this.transform.InverseTransformPoint(objectToAttach.transform.position);
            attachedObject.initialRotationalOffset = Quaternion.Inverse(this.transform.rotation) * objectToAttach.transform.rotation;
            attachedObject.grabbedWithType = grabbedWithType;
            attachedObject.originalParent = objectToAttach.transform.parent != null ? objectToAttach.transform.parent.gameObject : null;
            if (attachedObject.HasAttachFlag(AttachmentFlags.ParentToHand))
            {
                //Parent the object to the hand
                objectToAttach.transform.parent = GetAttachmentTransform(attachmentPoint);
                attachedObject.isParentedToHand = true;
            }
            else
            {
                attachedObject.isParentedToHand = false;
            }
            attachedObjects.Add(attachedObject);

            if (attachedObject.HasAttachFlag(AttachmentFlags.SnapOnAttach))
            {
                objectToAttach.transform.localPosition = Vector3.zero;
                objectToAttach.transform.localRotation = Quaternion.identity;
            }

            if (attachedObject.HasAttachFlag(AttachmentFlags.TurnOnKinematic))
            {
                if (attachedObject.attachedRigidbody != null)
                {
                    attachedObject.attachedRigidbodyWasKinematic = attachedObject.attachedRigidbody.isKinematic;
                    attachedObject.attachedRigidbody.isKinematic = true;
                }
            }

            if (attachedObject.HasAttachFlag(AttachmentFlags.TurnOffGravity))
            {
                if (attachedObject.attachedRigidbody != null)
                {
                    attachedObject.attachedRigidbodyUsedGravity = attachedObject.attachedRigidbody.useGravity;
                    attachedObject.attachedRigidbody.useGravity = false;
                }
            }


            if (spewDebugText)
                HandDebugLog("AttachObject " + objectToAttach);
            objectToAttach.SendMessage("OnAttachedToHand", this, SendMessageOptions.DontRequireReceiver);

            UpdateHovering();
        }


        //-------------------------------------------------
        // Detach this GameObject from the attached object stack of this Hand
        //
        // objectToDetach - The GameObject to detach from this Hand
        //-------------------------------------------------
        public void DetachObject(GameObject objectToDetach, bool restoreOriginalParent = true)
        {
            int index = attachedObjects.FindIndex(l => l.attachedObject == objectToDetach);
            if (index != -1)
            {
                if (spewDebugText)
                    HandDebugLog("DetachObject " + objectToDetach);

                GameObject prevTopObject = currentAttachedObject;

                Transform parentTransform = null;
                if (attachedObjects[index].isParentedToHand)
                {
                    if (restoreOriginalParent && (attachedObjects[index].originalParent != null))
                    {
                        parentTransform = attachedObjects[index].originalParent.transform;
                    }
                    attachedObjects[index].attachedObject.transform.parent = parentTransform;
                }

                if (attachedObjects[index].HasAttachFlag(AttachmentFlags.TurnOnKinematic))
                {
                    if (attachedObjects[index].attachedRigidbody != null)
                        attachedObjects[index].attachedRigidbody.isKinematic = attachedObjects[index].attachedRigidbodyWasKinematic;
                }

                if (attachedObjects[index].HasAttachFlag(AttachmentFlags.TurnOffGravity))
                {
                    if (attachedObjects[index].attachedRigidbody != null)
                        attachedObjects[index].attachedRigidbody.useGravity = attachedObjects[index].attachedRigidbodyUsedGravity;
                }

                attachedObjects[index].attachedObject.SetActive(true);
                attachedObjects[index].attachedObject.SendMessage("OnDetachedFromHand", this, SendMessageOptions.DontRequireReceiver);
                attachedObjects.RemoveAt(index);

                GameObject newTopObject = currentAttachedObject;

                //Give focus to the top most object on the stack if it changed
                if (newTopObject != null && newTopObject != prevTopObject)
                {
                    newTopObject.SetActive(true);
                    newTopObject.SendMessage("OnHandFocusAcquired", this, SendMessageOptions.DontRequireReceiver);
                }
            }

            CleanUpAttachedObjectStack();
        }


        //-------------------------------------------------
        // Get the world velocity of the VR Hand.
        //-------------------------------------------------
        public Vector3 GetTrackedObjectVelocity()
        {
            if (poseAction != null && poseAction.GetActive(handType))
            {
                return transform.parent.TransformVector(poseAction.GetVelocity(handType));
            }

            return Vector3.zero;
        }


        public Vector3 GetOldControllerVelocity()
        {
            var controller = SteamVR_Controller.Input((int)poseAction.GetDeviceIndex(handType));
            return controller.velocity;
        }

        //-------------------------------------------------
        // Get the world angular velocity of the VR Hand.
        //-------------------------------------------------
        public Vector3 GetTrackedObjectAngularVelocity()
        {
            if (poseAction != null && poseAction.GetActive(handType))
            {
                return transform.parent.TransformVector(poseAction.GetAngularVelocity(handType));
            }

            return Vector3.zero;
        }

        public Vector3 GetOldControllerAngularVelocity()
        {
            var controller = SteamVR_Controller.Input((int)poseAction.GetDeviceIndex(handType));
            return controller.angularVelocity;
        }

        public void GetEstimatedPeakVelocities(out Vector3 velocity, out Vector3 angularVelocity)
        {
            int top = historyBuffer.GetTopVelocity(10, 1);

            historyBuffer.GetAverageVelocities(out velocity, out angularVelocity, 2, top);
        }


        //-------------------------------------------------
        private void CleanUpAttachedObjectStack()
        {
            attachedObjects.RemoveAll(l => l.attachedObject == null);
        }


        //-------------------------------------------------
        protected virtual void Awake()
        {
            inputFocusAction = SteamVR_Events.InputFocusAction(OnInputFocus);

            if (hoverSphereTransform == null)
            {
                hoverSphereTransform = this.transform;
            }

            applicationLostFocusObject = new GameObject("_application_lost_focus");
            applicationLostFocusObject.transform.parent = transform;
            applicationLostFocusObject.SetActive(false);
        }


        //-------------------------------------------------
        protected virtual IEnumerator Start()
        {
            // save off player instance
            playerInstance = Player.instance;
            if (!playerInstance)
            {
                Debug.LogError("No player instance found in Hand Start()");
            }

            // allocate array for colliders
            overlappingColliders = new Collider[ColliderArraySize];

            // We are a "no SteamVR fallback hand" if we have this camera set
            // we'll use the right mouse to look around and left mouse to interact
            // - don't need to find the device
            if (noSteamVRFallbackCamera)
            {
                yield break;
            }

            //Debug.Log( "Hand - initializing connection routine" );

            while (true)
            {
                if (poseAction != null && poseAction.GetActive(handType) == true && poseAction.GetPoseIsValid(handType))
                {
                    InitController();
                    break;
                }

                yield return null;
            }
        }


        //-------------------------------------------------
        protected virtual void UpdateHovering()
        {
            if ((noSteamVRFallbackCamera == null) && (isActive == false))
            {
                return;
            }

            if (hoverLocked)
                return;

            if (applicationLostFocusObject.activeSelf)
                return;

            float closestDistance = float.MaxValue;
            Interactable closestInteractable = null;

            // Pick the closest hovering
            float flHoverRadiusScale = playerInstance.transform.lossyScale.x;
            float flScaledSphereRadius = hoverSphereRadius * flHoverRadiusScale;

            // if we're close to the floor, increase the radius to make things easier to pick up
            float handDiff = Mathf.Abs(transform.position.y - playerInstance.trackingOriginTransform.position.y);
            float boxMult = Util.RemapNumberClamped(handDiff, 0.0f, 0.5f * flHoverRadiusScale, 5.0f, 1.0f) * flHoverRadiusScale;

            // null out old vals
            for (int i = 0; i < overlappingColliders.Length; ++i)
            {
                overlappingColliders[i] = null;
            }

            Physics.OverlapBoxNonAlloc(
                hoverSphereTransform.position - new Vector3(0, flScaledSphereRadius * boxMult - flScaledSphereRadius, 0),
                new Vector3(flScaledSphereRadius, flScaledSphereRadius * boxMult * 2.0f, flScaledSphereRadius),
                overlappingColliders,
                Quaternion.identity,
                hoverLayerMask.value
            );

            // DebugVar
            int iActualColliderCount = 0;

            foreach (Collider collider in overlappingColliders)
            {
                if (collider == null)
                    continue;

                Interactable contacting = collider.GetComponentInParent<Interactable>();

                // Yeah, it's null, skip
                if (contacting == null)
                    continue;

                // Ignore this collider for hovering
                IgnoreHovering ignore = collider.GetComponent<IgnoreHovering>();
                if (ignore != null)
                {
                    if (ignore.onlyIgnoreHand == null || ignore.onlyIgnoreHand == this)
                    {
                        continue;
                    }
                }

                // Can't hover over the object if it's attached
                if (attachedObjects.FindIndex(l => l.attachedObject == contacting.gameObject) != -1)
                    continue;

                // Occupied by another hand, so we can't touch it
                if (otherHand && otherHand.hoveringInteractable == contacting)
                    continue;

                // Best candidate so far...
                float distance = Vector3.Distance(contacting.transform.position, hoverSphereTransform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestInteractable = contacting;
                }
                iActualColliderCount++;
            }

            // Hover on this one
            hoveringInteractable = closestInteractable;

            if (iActualColliderCount > 0 && iActualColliderCount != prevOverlappingColliders)
            {
                prevOverlappingColliders = iActualColliderCount;

                if (spewDebugText)
                    HandDebugLog("Found " + iActualColliderCount + " overlapping colliders.");
            }
        }


        //-------------------------------------------------
        protected virtual void UpdateNoSteamVRFallback()
        {
            if (noSteamVRFallbackCamera)
            {
                Ray ray = noSteamVRFallbackCamera.ScreenPointToRay(Input.mousePosition);

                if (attachedObjects.Count > 0)
                {
                    // Holding down the mouse:
                    // move around a fixed distance from the camera
                    transform.position = ray.origin + noSteamVRFallbackInteractorDistance * ray.direction;
                }
                else
                {
                    // Not holding down the mouse:
                    // cast out a ray to see what we should mouse over

                    // Don't want to hit the hand and anything underneath it
                    // So move it back behind the camera when we do the raycast
                    Vector3 oldPosition = transform.position;
                    transform.position = noSteamVRFallbackCamera.transform.forward * (-1000.0f);

                    RaycastHit raycastHit;
                    if (Physics.Raycast(ray, out raycastHit, noSteamVRFallbackMaxDistanceNoItem))
                    {
                        transform.position = raycastHit.point;

                        // Remember this distance in case we click and drag the mouse
                        noSteamVRFallbackInteractorDistance = Mathf.Min(noSteamVRFallbackMaxDistanceNoItem, raycastHit.distance);
                    }
                    else if (noSteamVRFallbackInteractorDistance > 0.0f)
                    {
                        // Move it around at the distance we last had a hit
                        transform.position = ray.origin + Mathf.Min(noSteamVRFallbackMaxDistanceNoItem, noSteamVRFallbackInteractorDistance) * ray.direction;
                    }
                    else
                    {
                        // Didn't hit, just leave it where it was
                        transform.position = oldPosition;
                    }
                }
            }
        }


        //-------------------------------------------------
        private void UpdateDebugText()
        {
            if (showDebugText)
            {
                if (debugText == null)
                {
                    debugText = new GameObject("_debug_text").AddComponent<TextMesh>();
                    debugText.fontSize = 120;
                    debugText.characterSize = 0.001f;
                    debugText.transform.parent = transform;

                    debugText.transform.localRotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
                }

                if (handType == SteamVR_Input_Input_Sources.RightHand)
                {
                    debugText.transform.localPosition = new Vector3(-0.05f, 0.0f, 0.0f);
                    debugText.alignment = TextAlignment.Right;
                    debugText.anchor = TextAnchor.UpperRight;
                }
                else
                {
                    debugText.transform.localPosition = new Vector3(0.05f, 0.0f, 0.0f);
                    debugText.alignment = TextAlignment.Left;
                    debugText.anchor = TextAnchor.UpperLeft;
                }

                debugText.text = string.Format(
                    "Hovering: {0}\n" +
                    "Hover Lock: {1}\n" +
                    "Attached: {2}\n" +
                    "Total Attached: {3}\n" +
                    "Type: {4}\n",
                    (hoveringInteractable ? hoveringInteractable.gameObject.name : "null"),
                    hoverLocked,
                    (currentAttachedObject ? currentAttachedObject.name : "null"),
                    attachedObjects.Count,
                    handType.ToString());
            }
            else
            {
                if (debugText != null)
                {
                    Destroy(debugText.gameObject);
                }
            }
        }


        //-------------------------------------------------
        protected virtual void OnEnable()
        {
            inputFocusAction.enabled = true;

            // Stagger updates between hands
            float hoverUpdateBegin = ((otherHand != null) && (otherHand.GetInstanceID() < GetInstanceID())) ? (0.5f * hoverUpdateInterval) : (0.0f);
            InvokeRepeating("UpdateHovering", hoverUpdateBegin, hoverUpdateInterval);
            InvokeRepeating("UpdateDebugText", hoverUpdateBegin, hoverUpdateInterval);

            SteamVR_Input.OnPosesUpdated += SteamVR_Input_OnPosesUpdated;
        }

        protected virtual void SteamVR_Input_OnPosesUpdated(bool obj)
        {
            poseAction.UpdateTransform(handType, this.transform);
        }


        //-------------------------------------------------
        protected virtual void OnDisable()
        {
            inputFocusAction.enabled = false;

            CancelInvoke();

            SteamVR_Input.OnPosesUpdated -= SteamVR_Input_OnPosesUpdated;
        }


        //-------------------------------------------------
        protected virtual void Update()
        {
            UpdateNoSteamVRFallback();

            GameObject attachedObject = currentAttachedObject;
            if (attachedObject != null)
            {
                attachedObject.SendMessage("HandAttachedUpdate", this, SendMessageOptions.DontRequireReceiver);
            }

            if (hoveringInteractable)
            {
                hoveringInteractable.SendMessage("HandHoverUpdate", this, SendMessageOptions.DontRequireReceiver);
            }
        }


        protected virtual void FixedUpdate()
        {
            historyBuffer.Update(transform);

            if (currentAttachedObject != null)
            {
                AttachedObject attachedInfo = currentAttachedObjectInfo.Value;
                if (attachedInfo.attachedObject != null)
                {
                    if (attachedInfo.HasAttachFlag(AttachmentFlags.VelocityMovement))
                    {
                        UpdateAttachedVelocity(attachedInfo);
                    }
                }
            }
        }


        //-------------------------------------------------
        protected virtual void LateUpdate()
        {
            //Re-attach the controller if nothing else is attached to the hand
            if (controllerObject != null && attachedObjects.Count == 0)
            {
                AttachObject(controllerObject, GrabTypes.Scripted);
            }
        }

        protected const float MaxVelocityChange = 10f;
        protected const float VelocityMagic = 6000f;
        protected const float AngularVelocityMagic = 50f;
        protected const float MaxAngularVelocityChange = 20f;

        protected void UpdateAttachedVelocity(AttachedObject attachedObjectInfo)
        {
            Vector3 targetItemPosition = this.transform.TransformPoint(attachedObjectInfo.initialPositionalOffset);
            Vector3 positionDelta = (targetItemPosition - attachedObjectInfo.attachedObject.transform.position);
            Vector3 velocityTarget = (positionDelta * VelocityMagic * Time.deltaTime);

            if (float.IsNaN(velocityTarget.x) == false && float.IsInfinity(velocityTarget.x) == false)
            {
                attachedObjectInfo.attachedRigidbody.velocity = Vector3.MoveTowards(attachedObjectInfo.attachedRigidbody.velocity, velocityTarget, MaxVelocityChange);
                //attachedObjectInfo.attachedRigidbody.position = targetItemPosition;
            }


            Quaternion targetItemRotation = this.transform.rotation * attachedObjectInfo.initialRotationalOffset;
            Quaternion rotationDelta = targetItemRotation * Quaternion.Inverse(attachedObjectInfo.attachedObject.transform.rotation);

            float angle;
            Vector3 axis;
            rotationDelta.ToAngleAxis(out angle, out axis);

            if (angle > 180)
                angle -= 360;

            if (angle != 0 && float.IsNaN(axis.x) == false && float.IsInfinity(axis.x) == false)
            {
                Vector3 angularTarget = angle * axis * AngularVelocityMagic * Time.deltaTime;

                attachedObjectInfo.attachedRigidbody.angularVelocity = Vector3.MoveTowards(attachedObjectInfo.attachedRigidbody.angularVelocity, angularTarget, MaxAngularVelocityChange);
                //attachedObjectInfo.attachedRigidbody.rotation = targetItemRotation;
            }
        }


        //-------------------------------------------------
        protected virtual void OnInputFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                DetachObject(applicationLostFocusObject, true);
                applicationLostFocusObject.SetActive(false);
                UpdateHovering();
                BroadcastMessage("OnParentHandInputFocusAcquired", SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                applicationLostFocusObject.SetActive(true);
                AttachObject(applicationLostFocusObject, GrabTypes.Scripted, AttachmentFlags.ParentToHand);
                BroadcastMessage("OnParentHandInputFocusLost", SendMessageOptions.DontRequireReceiver);
            }
        }

        //-------------------------------------------------
        protected virtual void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.5f, 1.0f, 0.5f, 0.9f);
            Transform sphereTransform = hoverSphereTransform ? hoverSphereTransform : this.transform;
            Gizmos.DrawWireSphere(sphereTransform.position, hoverSphereRadius);
        }


        //-------------------------------------------------
        private void HandDebugLog(string msg)
        {
            if (spewDebugText)
            {
                Debug.Log("Hand (" + this.name + "): " + msg);
            }
        }


        //-------------------------------------------------
        // Continue to hover over this object indefinitely, whether or not the Hand moves out of its interaction trigger volume.
        //
        // interactable - The Interactable to hover over indefinitely.
        //-------------------------------------------------
        public void HoverLock(Interactable interactable)
        {
            if (spewDebugText)
                HandDebugLog("HoverLock " + interactable);
            hoverLocked = true;
            hoveringInteractable = interactable;
        }


        //-------------------------------------------------
        // Stop hovering over this object indefinitely.
        //
        // interactable - The hover-locked Interactable to stop hovering over indefinitely.
        //-------------------------------------------------
        public void HoverUnlock(Interactable interactable)
        {
            if (spewDebugText)
                HandDebugLog("HoverUnlock " + interactable);
            if (hoveringInteractable == interactable)
            {
                hoverLocked = false;
            }
        }

        public void TriggerHapticPulse(ushort microSecondsDuration)
        {
            float seconds = (float)microSecondsDuration / 1000000f;
            hapticAction.Execute(0, seconds, 1f / seconds, 1, handType);
        }

        public void TriggerHapticPulse(float duration, float frequency, float amplitude)
        {
            hapticAction.Execute(0, duration, frequency, amplitude, handType);
        }

        public void ShowGrabHint()
        {
            ControllerButtonHints.HideButtonHint(this, grabGripAction); //todo: assess
        }

        public void HideGrabHint()
        {
            ControllerButtonHints.HideButtonHint(this, grabGripAction); //todo: assess
        }

        public void ShowGrabHint(string text)
        {
            ControllerButtonHints.ShowTextHint(this, grabGripAction, text);
        }

        public GrabTypes GetGrabStarting(GrabTypes explicitType = GrabTypes.None)
        {
            if (explicitType != GrabTypes.None)
            {
                if (explicitType == GrabTypes.Pinch && grabPinchAction.GetStateDown(handType))
                    return GrabTypes.Pinch;
                if (explicitType == GrabTypes.Grip && grabGripAction.GetStateDown(handType))
                    return GrabTypes.Grip;
            }
            else
            {
                if (grabPinchAction.GetStateDown(handType))
                    return GrabTypes.Pinch;
                if (grabGripAction.GetStateDown(handType))
                    return GrabTypes.Grip;
            }

            return GrabTypes.None;
        }

        public GrabTypes GetGrabEnding(GrabTypes explicitType = GrabTypes.None)
        {
            if (explicitType != GrabTypes.None)
            {
                if (explicitType == GrabTypes.Pinch && grabPinchAction.GetStateUp(handType))
                    return GrabTypes.Pinch;
                if (explicitType == GrabTypes.Grip && grabGripAction.GetStateUp(handType))
                    return GrabTypes.Grip;
            }
            else
            {
                if (grabPinchAction.GetStateUp(handType))
                    return GrabTypes.Pinch;
                if (grabGripAction.GetStateUp(handType))
                    return GrabTypes.Grip;
            }

            return GrabTypes.None;
        }

        public bool IsGrabEnding(GameObject attachedObject)
        {
            for (int attachedObjectIndex = 0; attachedObjectIndex < attachedObjects.Count; attachedObjectIndex++)
            {
                if (attachedObjects[attachedObjectIndex].attachedObject == attachedObject)
                {
                    return IsGrabbingWithType(attachedObjects[attachedObjectIndex].grabbedWithType) == false;
                }
            }

            return false;
        }

        public bool IsGrabbingWithType(GrabTypes type)
        {
            switch (type)
            {
                case GrabTypes.Pinch:
                    return grabPinchAction.GetState(handType);

                case GrabTypes.Grip:
                    return grabGripAction.GetState(handType);

                default:
                    return false;
            }
        }

        public GrabTypes GetBestGrabbingType()
        {
            return GetBestGrabbingType(GrabTypes.None);
        }

        public GrabTypes GetBestGrabbingType(GrabTypes preferred, bool forcePreference = false)
        {
            if (preferred == GrabTypes.Pinch)
            {
                if (grabPinchAction.GetState(handType))
                    return GrabTypes.Pinch;
                else if (forcePreference)
                    return GrabTypes.None;
            }
            if (preferred == GrabTypes.Grip)
            {
                if (grabGripAction.GetState(handType))
                    return GrabTypes.Grip;
                else if (forcePreference)
                    return GrabTypes.None;
            }

            if (grabPinchAction.GetState(handType))
                return GrabTypes.Pinch;
            if (grabGripAction.GetState(handType))
                return GrabTypes.Grip;

            return GrabTypes.None;
        }


        //-------------------------------------------------
        private void InitController()
        {
            if (spewDebugText)
                HandDebugLog("Hand " + name + " connected with type " + handType.ToString());

            controllerObject = GameObject.Instantiate(controllerPrefab);
            controllerObject.SetActive(true);
            controllerObject.name = controllerPrefab.name + "_" + this.name;
            controllerObject.layer = gameObject.layer;
            controllerObject.tag = gameObject.tag;
            AttachObject(controllerObject, GrabTypes.Scripted);
            TriggerHapticPulse(800);

            // If the player's scale has been changed the object to attach will be the wrong size.
            // To fix this we change the object's scale back to its original, pre-attach scale.
            controllerObject.transform.localScale = controllerPrefab.transform.localScale;

            int deviceIndex = (int)poseAction.GetDeviceIndex();

            spawnRenderModel = controllerObject.GetComponentInChildren<SpawnRenderModel>();

            handAnimator = controllerObject.GetComponentInChildren<Animator>();
            CheckAnimatorInit();

            skeleton = controllerObject.GetComponentInChildren<SteamVR_Input_Skeleton>();

            this.BroadcastMessage("OnHandInitialized", deviceIndex, SendMessageOptions.DontRequireReceiver); // let child objects know we've initialized
        }
    }



#if UNITY_EDITOR
    //-------------------------------------------------------------------------
    [UnityEditor.CustomEditor( typeof( Hand ) )]
	public class HandEditor : UnityEditor.Editor
	{
		//-------------------------------------------------
		// Custom Inspector GUI allows us to click from within the UI
		//-------------------------------------------------
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
            
			Hand hand = (Hand)target;

			if ( hand.otherHand )
			{
				if ( hand.otherHand.otherHand != hand )
				{
					UnityEditor.EditorGUILayout.HelpBox( "The otherHand of this Hand's otherHand is not this Hand.", UnityEditor.MessageType.Warning );
				}

				if ( hand.handType == SteamVR_Input_Input_Sources.LeftHand && hand.otherHand.handType != SteamVR_Input_Input_Sources.RightHand)
				{
					UnityEditor.EditorGUILayout.HelpBox( "This is a left Hand but otherHand is not a right Hand.", UnityEditor.MessageType.Warning );
				}

				if ( hand.handType == SteamVR_Input_Input_Sources.RightHand && hand.otherHand.handType != SteamVR_Input_Input_Sources.LeftHand)
				{
					UnityEditor.EditorGUILayout.HelpBox( "This is a right Hand but otherHand is not a left Hand.", UnityEditor.MessageType.Warning );
				}

				if ( hand.handType == SteamVR_Input_Input_Sources.Any && hand.otherHand.handType != SteamVR_Input_Input_Sources.Any)
				{
					UnityEditor.EditorGUILayout.HelpBox( "This is an any-handed Hand but otherHand is not an any-handed Hand.", UnityEditor.MessageType.Warning );
				}
			}
		}
	}
#endif
}
