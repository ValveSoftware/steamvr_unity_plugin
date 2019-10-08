// Copyright (c) Valve Corporation, All rights reserved. ======================================================================================================



using UnityEngine;
using System.Collections;

namespace Valve.VR.InteractionSystem
{
    //-----------------------------------------------------------------------------
    public class SnapTurn : MonoBehaviour
    {
        public float snapAngle = 90.0f;

        public bool showTurnAnimation = true;

        public AudioSource snapTurnSource;
        public AudioClip rotateSound;

        public GameObject rotateRightFX;
        public GameObject rotateLeftFX;

        public SteamVR_Action_Boolean snapLeftAction = SteamVR_Input.GetBooleanAction("SnapTurnLeft");
        public SteamVR_Action_Boolean snapRightAction = SteamVR_Input.GetBooleanAction("SnapTurnRight");

        public bool fadeScreen = true;
        public float fadeTime = 0.1f;
        public Color screenFadeColor = Color.black;

        public float distanceFromFace = 1.3f;
        public Vector3 additionalOffset = new Vector3(0, -0.3f, 0);

        public static float teleportLastActiveTime;

        private bool canRotate = true;

        public float canTurnEverySeconds = 0.4f;


        private void Start()
        {
            AllOff();
        }

        private void AllOff()
        {
            if (rotateLeftFX != null)
                rotateLeftFX.SetActive(false);

            if (rotateRightFX != null)
                rotateRightFX.SetActive(false);
        }


        private void Update()
        {
            Player player = Player.instance;

            if (canRotate && snapLeftAction != null && snapRightAction != null && snapLeftAction.activeBinding && snapRightAction.activeBinding)
            {
                //only allow snap turning after a quarter second after the last teleport
                if (Time.time < (teleportLastActiveTime + canTurnEverySeconds))
                    return;

                // only allow snap turning when not holding something

                bool rightHandValid = player.rightHand.currentAttachedObject == null ||
                    (player.rightHand.currentAttachedObject != null
                    && player.rightHand.currentAttachedTeleportManager != null
                    && player.rightHand.currentAttachedTeleportManager.teleportAllowed);

                bool leftHandValid = player.leftHand.currentAttachedObject == null ||
                    (player.leftHand.currentAttachedObject != null
                    && player.leftHand.currentAttachedTeleportManager != null
                    && player.leftHand.currentAttachedTeleportManager.teleportAllowed);


                bool leftHandTurnLeft = snapLeftAction.GetStateDown(SteamVR_Input_Sources.LeftHand) && leftHandValid;
                bool rightHandTurnLeft = snapLeftAction.GetStateDown(SteamVR_Input_Sources.RightHand) && rightHandValid;

                bool leftHandTurnRight = snapRightAction.GetStateDown(SteamVR_Input_Sources.LeftHand) && leftHandValid;
                bool rightHandTurnRight = snapRightAction.GetStateDown(SteamVR_Input_Sources.RightHand) && rightHandValid;

                if (leftHandTurnLeft || rightHandTurnLeft)
                {
                    RotatePlayer(-snapAngle);
                }
                else if (leftHandTurnRight || rightHandTurnRight)
                {
                    RotatePlayer(snapAngle);
                }
            }
        }


        private Coroutine rotateCoroutine;
        public void RotatePlayer(float angle)
        {
            if (rotateCoroutine != null)
            {
                StopCoroutine(rotateCoroutine);
                AllOff();
            }

            rotateCoroutine = StartCoroutine(DoRotatePlayer(angle));
        }

        //-----------------------------------------------------
        private IEnumerator DoRotatePlayer(float angle)
        {
            Player player = Player.instance;

            canRotate = false;

            snapTurnSource.panStereo = angle / 90;
            snapTurnSource.PlayOneShot(rotateSound);

            if (fadeScreen)
            {
                SteamVR_Fade.Start(Color.clear, 0);

                Color tColor = screenFadeColor;
                tColor = tColor.linear * 0.6f;
                SteamVR_Fade.Start(tColor, fadeTime);
            }

            yield return new WaitForSeconds(fadeTime);

            Vector3 playerFeetOffset = player.trackingOriginTransform.position - player.feetPositionGuess;
            player.trackingOriginTransform.position -= playerFeetOffset;
            player.transform.Rotate(Vector3.up, angle);
            playerFeetOffset = Quaternion.Euler(0.0f, angle, 0.0f) * playerFeetOffset;
            player.trackingOriginTransform.position += playerFeetOffset;

            GameObject fx = angle > 0 ? rotateRightFX : rotateLeftFX;

            if (showTurnAnimation)
                ShowRotateFX(fx);

            if (fadeScreen)
            {
                SteamVR_Fade.Start(Color.clear, fadeTime);
            }

            float startTime = Time.time;
            float endTime = startTime + canTurnEverySeconds;

            while (Time.time <= endTime)
            {
                yield return null;
                UpdateOrientation(fx);
            };

            fx.SetActive(false);
            canRotate = true;
        }

        void ShowRotateFX(GameObject fx)
        {
            if (fx == null)
                return;

            fx.SetActive(false);

            UpdateOrientation(fx);

            fx.SetActive(true);

            UpdateOrientation(fx);
        }

        private void UpdateOrientation(GameObject fx)
        {
            Player player = Player.instance;

            //position fx in front of face
            this.transform.position = player.hmdTransform.position + (player.hmdTransform.forward * distanceFromFace);
            this.transform.rotation = Quaternion.LookRotation(player.hmdTransform.position - this.transform.position, Vector3.up);
            this.transform.Translate(additionalOffset, Space.Self);
            this.transform.rotation = Quaternion.LookRotation(player.hmdTransform.position - this.transform.position, Vector3.up);
        }
    }
}