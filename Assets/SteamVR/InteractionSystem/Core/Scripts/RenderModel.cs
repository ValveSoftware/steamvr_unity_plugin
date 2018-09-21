//======= Copyright (c) Valve Corporation, All rights reserved. ===============

using UnityEngine;
using System.Collections;
using System;

namespace Valve.VR.InteractionSystem
{
    public class RenderModel : MonoBehaviour
    {
        public GameObject handPrefab;
        protected GameObject handInstance;
        protected Renderer[] handRenderers;
        public bool displayHandByDefault = true;
        protected SteamVR_Behaviour_Skeleton handSkeleton;
        protected Animator handAnimator;

        protected string animatorParameterStateName = "AnimationState";
        protected int handAnimatorStateId = -1;

        [Space]

        public GameObject controllerPrefab;
        protected GameObject controllerInstance;
        protected Renderer[] controllerRenderers;
        protected SteamVR_RenderModel controllerRenderModel;
        public bool displayControllerByDefault = true;
        protected Material delayedSetMaterial;

        public event Action onControllerLoaded;

        protected SteamVR_Events.Action renderModelLoadedAction;

        protected SteamVR_Input_Sources inputSource;

        protected void Awake()
        {
            renderModelLoadedAction = SteamVR_Events.RenderModelLoadedAction(OnRenderModelLoaded);

            InitializeHand();

            InitializeController();
        }

        protected void InitializeHand()
        {
            if (handPrefab != null)
            {
                handInstance = GameObject.Instantiate(handPrefab);
                handInstance.transform.parent = this.transform;
                handInstance.transform.localPosition = Vector3.zero;
                handInstance.transform.localRotation = Quaternion.identity;
                handInstance.transform.localScale = handPrefab.transform.localScale;
                handSkeleton = handInstance.GetComponent<SteamVR_Behaviour_Skeleton>();
                handSkeleton.updatePose = false;

                handRenderers = handInstance.GetComponentsInChildren<Renderer>();
                if (displayHandByDefault == false)
                    SetHandVisibility(false);

                handAnimator = handInstance.GetComponentInChildren<Animator>();
            }
        }

        protected void InitializeController()
        {
            if (controllerPrefab != null)
            {
                controllerInstance = GameObject.Instantiate(controllerPrefab);
                controllerInstance.transform.parent = this.transform;
                controllerInstance.transform.localPosition = Vector3.zero;
                controllerInstance.transform.localRotation = Quaternion.identity;
                controllerInstance.transform.localScale = controllerPrefab.transform.localScale;
                controllerRenderModel = controllerInstance.GetComponent<SteamVR_RenderModel>();
            }
        }

        protected virtual void Update()
        {
            if (handSkeleton != null && handSkeleton.isActive == false)
            {
                handSkeleton.skeletonAction.RemoveOnActiveChangeListener(OnSkeletonActiveChange, handSkeleton.inputSource);
                handSkeleton.skeletonAction.AddOnActiveChangeListener(OnSkeletonActiveChange, handSkeleton.inputSource); //watch for if it gets activated later
                DestroyHand();
            }
        }

        protected virtual void DestroyHand()
        {
            if (handInstance != null)
            {
                Destroy(handInstance);
                handRenderers = null;
                handInstance = null;
                handSkeleton = null;
                handAnimator = null;
            }
        }

        protected virtual void OnSkeletonActiveChange(SteamVR_Action_In action, bool newState)
        {
            if (newState)
            {
                InitializeHand();
            }
        }

        protected void OnEnable()
        {
            renderModelLoadedAction.enabled = true;
        }

        protected void OnDisable()
        {
            renderModelLoadedAction.enabled = false;
        }

        public virtual void SetInputSource(SteamVR_Input_Sources newInputSource)
        {
            inputSource = newInputSource;
            if (controllerRenderModel != null)
                controllerRenderModel.SetInputSource(inputSource);
        }

        public virtual void OnHandInitialized(int deviceIndex)
        {
            controllerRenderModel.SetInputSource(inputSource);
            controllerRenderModel.SetDeviceIndex(deviceIndex);
        }

        public void MatchHandToTransform(Transform match)
        {
            if (handInstance != null)
            {
                handInstance.transform.position = match.transform.position;
                handInstance.transform.rotation = match.transform.rotation;
            }
        }

        public void SetHandPosition(Vector3 newPosition)
        {
            if (handInstance != null)
            {
                handInstance.transform.position = newPosition;
            }
        }

        public void SetHandRotation(Quaternion newRotation)
        {
            if (handInstance != null)
            {
                handInstance.transform.rotation = newRotation;
            }
        }

        public Vector3 GetHandPosition()
        {
            if (handInstance != null)
            {
                return handInstance.transform.position;
            }

            return Vector3.zero;
        }

        public Quaternion GetHandRotation()
        {
            if (handInstance != null)
            {
                return handInstance.transform.rotation;
            }

            return Quaternion.identity;
        }

        private void OnRenderModelLoaded(SteamVR_RenderModel loadedRenderModel, bool success)
        {
            if (controllerRenderModel == loadedRenderModel)
            {
                controllerRenderers = controllerInstance.GetComponentsInChildren<Renderer>();
                if (displayControllerByDefault == false)
                    SetControllerVisibility(false);

                if (delayedSetMaterial != null)
                    SetControllerMaterial(delayedSetMaterial);

                if (onControllerLoaded != null)
                    onControllerLoaded.Invoke();
            }
        }

        public void SetVisibility(bool state, bool overrideDefault = false)
        {
            if (state == false || displayControllerByDefault || overrideDefault)
                SetControllerVisibility(state);

            if (state == false || displayHandByDefault || overrideDefault)
                SetHandVisibility(state);
        }

        public void Show(bool overrideDefault = false)
        {
            SetVisibility(true, overrideDefault);
        }

        public void Hide()
        {
            SetVisibility(false);
        }

        public virtual void SetMaterial(Material material)
        {
            SetControllerMaterial(material);
            SetHandMaterial(material);
        }

        public void SetControllerMaterial(Material material)
        {
            if (controllerRenderers == null)
            {
                delayedSetMaterial = material;
                return;
            }

            for (int rendererIndex = 0; rendererIndex < controllerRenderers.Length; rendererIndex++)
            {
                controllerRenderers[rendererIndex].material = material;
            }
        }

        public void SetHandMaterial(Material material)
        {
            for (int rendererIndex = 0; rendererIndex < handRenderers.Length; rendererIndex++)
            {
                handRenderers[rendererIndex].material = material;
            }
        }

        public void SetControllerVisibility(bool state, bool permanent = false)
        {
            if (permanent)
                displayControllerByDefault = state;

            if (controllerRenderers == null)
                return;

            for (int rendererIndex = 0; rendererIndex < controllerRenderers.Length; rendererIndex++)
            {
                controllerRenderers[rendererIndex].enabled = state;
            }
        }

        public void SetHandVisibility(bool state, bool permanent = false)
        {
            if (permanent)
                displayHandByDefault = state;

            if (handRenderers == null)
                return;

            for (int rendererIndex = 0; rendererIndex < handRenderers.Length; rendererIndex++)
            {
                handRenderers[rendererIndex].enabled = state;
            }
        }

        public bool IsHandVisibile()
        {
            if (handRenderers == null)
                return false;

            for (int rendererIndex = 0; rendererIndex < handRenderers.Length; rendererIndex++)
            {
                if (handRenderers[rendererIndex].enabled)
                    return true;
            }

            return false;
        }

        public bool IsControllerVisibile()
        {
            if (controllerRenderers == null)
                return false;

            for (int rendererIndex = 0; rendererIndex < controllerRenderers.Length; rendererIndex++)
            {
                if (controllerRenderers[rendererIndex].enabled)
                    return true;
            }

            return false;
        }

        public Transform GetBone(int boneIndex)
        {
            if (handSkeleton != null)
            {
                return handSkeleton.GetBone(boneIndex);
            }

            return null;
        }

        public Vector3 GetBonePosition(int boneIndex, bool local = false)
        {
            if (handSkeleton != null)
            {
                return handSkeleton.GetBonePosition(boneIndex, local);
            }

            return Vector3.zero;
        }

        public Vector3 GetControllerPosition(string componentName = null)
        {
            if (controllerRenderModel != null)
                return controllerRenderModel.GetComponentTransform(componentName).position;

            return Vector3.zero;
        }

        public Quaternion GetBoneRotation(int boneIndex, bool local = false)
        {
            if (handSkeleton != null)
            {
                return handSkeleton.GetBoneRotation(boneIndex, local);
            }

            return Quaternion.identity;
        }


        public void SetSkeletonRangeOfMotion(EVRSkeletalMotionRange newRangeOfMotion, float blendOverSeconds = 0.1f)
        {
            if (handSkeleton != null)
            {
                handSkeleton.SetRangeOfMotion(newRangeOfMotion, blendOverSeconds);
            }
        }

        public EVRSkeletalMotionRange GetSkeletonRangeOfMotion
        {
            get
            {
                return handSkeleton.rangeOfMotion;
            }
        }

        public void SetTemporarySkeletonRangeOfMotion(SkeletalMotionRangeChange temporaryRangeOfMotionChange, float blendOverSeconds = 0.1f)
        {
            if (handSkeleton != null)
            {
                handSkeleton.SetTemporaryRangeOfMotion((EVRSkeletalMotionRange)temporaryRangeOfMotionChange, blendOverSeconds);
            }
        }

        public void ResetTemporarySkeletonRangeOfMotion(float blendOverSeconds = 0.1f)
        {
            if (handSkeleton != null)
            {
                handSkeleton.ResetTemporaryRangeOfMotion(blendOverSeconds);
            }
        }

        public void SetAnimationState(int stateValue)
        {
            if (handSkeleton != null)
            {
                if (handSkeleton.isBlending == false)
                    handSkeleton.BlendToAnimation();

                if (CheckAnimatorInit())
                    handAnimator.SetInteger(handAnimatorStateId, stateValue);
            }
        }

        public void StopAnimation()
        {
            if (handSkeleton != null)
            {
                if (handSkeleton.isBlending == false)
                    handSkeleton.BlendToSkeleton();

                if (CheckAnimatorInit())
                    handAnimator.SetInteger(handAnimatorStateId, 0);
            }
        }

        private bool CheckAnimatorInit()
        {
            if (handAnimatorStateId == -1 && handAnimator != null)
            {
                if (handAnimator.gameObject.activeInHierarchy && handAnimator.isInitialized)
                {
                    AnimatorControllerParameter[] parameters = handAnimator.parameters;
                    for (int parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++)
                    {
                        if (string.Equals(parameters[parameterIndex].name, animatorParameterStateName, StringComparison.CurrentCultureIgnoreCase))
                            handAnimatorStateId = parameters[parameterIndex].nameHash;
                    }
                }
            }

            return handAnimatorStateId != -1 && handAnimator != null && handAnimator.isInitialized;
        }

        
    }
}