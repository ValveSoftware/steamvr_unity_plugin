using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;
using Valve.VR.InteractionSystem;

namespace Valve.VR.InteractionSystem.Sample
{
    public class BuggyController : MonoBehaviour
    {
        public Transform modelJoystick;
        public float joystickRot = 20;

        public Transform modelTrigger;
        public float triggerRot = 20;

        public BuggyBuddy buggy;

        public Transform buttonBrake;
        public Transform buttonReset;

        //ui stuff

        public Canvas ui_Canvas;
        public Image ui_rpm;
        public Image ui_speed;
        public RectTransform ui_steer;

        public float ui_steerangle;

        public Vector2 ui_fillAngles;

        public Transform resetToPoint;

        [SteamVR_DefaultActionSet("buggy")]
        public SteamVR_ActionSet actionSet;

        [SteamVR_DefaultAction("Steering", "buggy")]
        public SteamVR_Action_Vector2 a_steering;

        [SteamVR_DefaultAction("Throttle", "buggy")]
        public SteamVR_Action_Single a_trigger;

        [SteamVR_DefaultAction("Brake", "buggy")]
        public SteamVR_Action_Boolean a_brake;

        [SteamVR_DefaultAction("Reset", "buggy")]
        public SteamVR_Action_Boolean a_reset;

        private float usteer;

        private Interactable interactable;

        private Quaternion trigSRot;

        private Quaternion joySRot;

        private Coroutine resettingRoutine;

        private Vector3 initialScale;

        private void Start()
        {
            joySRot = modelJoystick.localRotation;
            trigSRot = modelTrigger.localRotation;

            interactable = GetComponent<Interactable>();
            interactable.activateActionSetOnAttach = actionSet;

            StartCoroutine(DoBuzz());
            buggy.controllerReference = transform;
            initialScale = buggy.transform.localScale;
        }

        private void Update()
        {
            Vector2 steer = Vector2.zero;
            float throttle = 0;
            float brake = 0;

            bool reset = false;

            bool b_brake = false;
            bool b_reset = false;


            if (interactable.attachedToHand)
            {
                SteamVR_Input_Sources hand = interactable.attachedToHand.handType;

                steer = a_steering.GetAxis(hand);

                throttle = a_trigger.GetAxis(hand);
                b_brake = a_brake.GetState(hand);
                b_reset = a_reset.GetState(hand);
                brake = b_brake ? 1 : 0;
                reset = a_reset.GetStateDown(hand);
            }

            if (reset && resettingRoutine == null)
            {
                resettingRoutine = StartCoroutine(DoReset());
            }

            if (ui_Canvas != null)
            {
                ui_Canvas.gameObject.SetActive(interactable.attachedToHand);

                usteer = Mathf.Lerp(usteer, steer.x, Time.deltaTime * 9);
                ui_steer.localEulerAngles = Vector3.forward * usteer * -ui_steerangle;
                ui_rpm.fillAmount = Mathf.Lerp(ui_rpm.fillAmount, Mathf.Lerp(ui_fillAngles.x, ui_fillAngles.y, throttle), Time.deltaTime * 4);
                float speedLim = 40;
                ui_speed.fillAmount = Mathf.Lerp(ui_fillAngles.x, ui_fillAngles.y, 1 - (Mathf.Exp(-buggy.speed / speedLim)));

            }

            modelJoystick.localRotation = joySRot;
            /*if (input.AttachedHand != null && input.AttachedHand.IsLeft)
            {
                Joystick.Rotate(steer.y * -joystickRot, steer.x * -joystickRot, 0, Space.Self);
            }
            else if (input.AttachedHand != null && input.AttachedHand.IsRight)
            {
                Joystick.Rotate(steer.y * -joystickRot, steer.x * joystickRot, 0, Space.Self);
            }
            else*/
            //{
            modelJoystick.Rotate(steer.y * -joystickRot, steer.x * -joystickRot, 0, Space.Self);
            //}

            modelTrigger.localRotation = trigSRot;
            modelTrigger.Rotate(throttle * -triggerRot, 0, 0, Space.Self);
            buttonBrake.localScale = new Vector3(1, 1, b_brake ? 0.4f : 1.0f);
            buttonReset.localScale = new Vector3(1, 1, b_reset ? 0.4f : 1.0f);

            buggy.steer = steer;
            buggy.throttle = throttle;
            buggy.handBrake = brake;
            buggy.controllerReference = transform;
        }

        private IEnumerator DoReset()
        {
            float startTime = Time.time;
            float overTime = 1f;
            float endTime = startTime + overTime;

            buggy.transform.position = resetToPoint.transform.position;
            buggy.transform.rotation = resetToPoint.transform.rotation;
            buggy.transform.localScale = initialScale * 0.1f;

            while (Time.time < endTime)
            {
                buggy.transform.localScale = Vector3.Lerp(buggy.transform.localScale, initialScale, Time.deltaTime * 5f);
                yield return null;
            }

            buggy.transform.localScale = initialScale;

            resettingRoutine = null;
        }

        private float buzztimer;
        private IEnumerator DoBuzz()
        {
            while (true)
            {
                while (buzztimer < 1)
                {
                    buzztimer += Time.deltaTime * buggy.mvol * 70;
                    yield return null;
                }

                buzztimer = 0;
                if (interactable.attachedToHand)
                {
                    interactable.attachedToHand.TriggerHapticPulse((ushort)Mathf.RoundToInt(300 * Mathf.Lerp(1.0f, 0.6f, buggy.mvol)));
                }
            }
        }
    }
}