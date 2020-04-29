#if ENABLE_VR && UNITY_INPUT_SYSTEM && !PACKAGE_DOCS_GENERATION
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.XR;
using UnityEngine.Scripting;

namespace Unity.XR.OpenVR
{
    [InputControlLayout(displayName = "[OpenVR] HMD")]
    [Preserve]
    public class OpenVRHMD : XRHMD
    {
        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control deviceVelocity { get; private set; }
        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control deviceAngularVelocity { get; private set; }
        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control leftEyeVelocity { get; private set; }
        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control leftEyeAngularVelocity { get; private set; }
        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control rightEyeVelocity { get; private set; }
        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control rightEyeAngularVelocity { get; private set; }
        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control centerEyeVelocity { get; private set; }
        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control centerEyeAngularVelocity { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            deviceVelocity = GetChildControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = GetChildControl<Vector3Control>("deviceAngularVelocity");

            leftEyeVelocity = GetChildControl<Vector3Control>("leftEyeVelocity");
            leftEyeAngularVelocity = GetChildControl<Vector3Control>("leftEyeAngularVelocity");
            rightEyeVelocity = GetChildControl<Vector3Control>("rightEyeVelocity");
            rightEyeAngularVelocity = GetChildControl<Vector3Control>("rightEyeAngularVelocity");
            centerEyeVelocity = GetChildControl<Vector3Control>("centerEyeVelocity");
            centerEyeAngularVelocity = GetChildControl<Vector3Control>("centerEyeAngularVelocity");
        }
    }

    /*
    [InputControlLayout(isGenericTypeOfDevice = true, displayName = "OpenVR Controller")]
    [Preserve]
    public class OpenVRController : XRControllerWithRumble
    {
        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control deviceVelocity { get; private set; }

        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control deviceAngularVelocity { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            deviceVelocity = GetChildControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = GetChildControl<Vector3Control>("deviceAngularVelocity");
        }
    }

    [InputControlLayout(isGenericTypeOfDevice = true, displayName = "OpenVR Tracked Device")]
    [Preserve]
    public class OpenVRTrackedDevice : TrackedDevice
    {
        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control deviceVelocity { get; private set; }

        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control deviceAngularVelocity { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            deviceVelocity = GetChildControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = GetChildControl<Vector3Control>("deviceAngularVelocity");
        }
    }
    */

    [InputControlLayout( displayName = "[OpenVR] Windows MR Controller", commonUsages = new[] { "LeftHand", "RightHand" })]
    [Preserve]
    public class OpenVRControllerWMR : XRControllerWithRumble
    {
        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control deviceVelocity { get; private set; }

        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control deviceAngularVelocity { get; private set; }

        [InputControl(aliases = new[] { "primary2DAxisClick" })]
        [Preserve]
        public ButtonControl touchpadClick { get; private set; }

        [InputControl(aliases = new[] { "primary2DAxisTouch" })]
        [Preserve]
        public ButtonControl touchpadTouch { get; private set; }

        [InputControl(aliases = new[] { "primary2DAxis" })]
        [Preserve]
        public Vector2Control touchpad { get; private set; }

        [InputControl(aliases = new[] { "secondary2DAxisClick" })]
        [Preserve]
        public ButtonControl joystickClick { get; private set; }

        [InputControl(aliases = new[] { "secondary2DAxis" })]
        [Preserve]
        public Vector2Control joystick { get; private set; }

        [InputControl]
        [Preserve]
        public AxisControl trigger { get; private set; }

        [InputControl]
        public ButtonControl triggerButton { get; private set; }

        [InputControl]
        [Preserve]
        public ButtonControl gripButton { get; private set; }

        [InputControl]
        [Preserve]
        public ButtonControl menuButton { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            deviceVelocity = GetChildControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = GetChildControl<Vector3Control>("deviceAngularVelocity");

            touchpadClick = GetChildControl<ButtonControl>("touchpadClick");
            touchpadTouch = GetChildControl<ButtonControl>("touchpadTouch");
            joystickClick = GetChildControl<ButtonControl>("joystickClick");

            gripButton = GetChildControl<ButtonControl>("gripButton");
            triggerButton = GetChildControl<ButtonControl>("triggerButton");
            menuButton = GetChildControl<ButtonControl>("menuButton");

            trigger = GetChildControl<AxisControl>("trigger");

            touchpad = GetChildControl<Vector2Control>("touchpad");
            joystick = GetChildControl<Vector2Control>("joystick");
        }
    }

    /// <summary>
    /// An HTC Vive Wand controller.
    /// </summary>
    [InputControlLayout(displayName = "[OpenVR] Vive Controller", commonUsages = new[] { "LeftHand", "RightHand" })]
    [Preserve]
    public class ViveWand : XRControllerWithRumble
    {
        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control deviceVelocity { get; private set; }

        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control deviceAngularVelocity { get; private set; }

        [InputControl(aliases = new[] { "primary2DAxisClick" })]
        [Preserve]
        public ButtonControl touchpadClick { get; private set; }

        [InputControl(aliases = new[] { "primary2DAxisTouch" })]
        [Preserve]
        public ButtonControl touchpadTouch { get; private set; }

        [InputControl(aliases = new[] { "primary2DAxis" })]
        [Preserve]
        public Vector2Control touchpad { get; private set; }

        [InputControl]
        [Preserve]
        public AxisControl trigger { get; private set; }

        [InputControl]
        public ButtonControl triggerButton { get; private set; }

        [InputControl]
        [Preserve]
        public ButtonControl gripButton { get; private set; }

        [InputControl]
        [Preserve]
        public ButtonControl menuButton { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            deviceVelocity = GetChildControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = GetChildControl<Vector3Control>("deviceAngularVelocity");

            touchpadClick = GetChildControl<ButtonControl>("touchpadClick");
            touchpadTouch = GetChildControl<ButtonControl>("touchpadTouch");

            gripButton = GetChildControl<ButtonControl>("gripButton");
            triggerButton = GetChildControl<ButtonControl>("triggerButton");
            menuButton = GetChildControl<ButtonControl>("menuButton");

            trigger = GetChildControl<AxisControl>("trigger");

            touchpad = GetChildControl<Vector2Control>("touchpad");
        }
    }

    /// <summary>
    /// An HTC Vive lighthouse.
    /// </summary>
    [InputControlLayout(displayName = "[OpenVR] Vive Lighthouse")]
    [Preserve]
    public class ViveLighthouse : TrackedDevice
    {
    }

    /// <summary>
    /// An HTC Vive lighthouse.
    /// </summary>
    [InputControlLayout(displayName = "[OpenVR] Valve Lighthouse")]
    [Preserve]
    public class ValveLighthouse : TrackedDevice
    {
    }

    /// <summary>
    /// An HTC Vive tracker.
    /// </summary>
    [InputControlLayout(displayName = "[OpenVR] Vive Tracker")]
    [Preserve]
    public class ViveTracker : XRControllerWithRumble
    {
        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control deviceVelocity { get; private set; }

        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control deviceAngularVelocity { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            deviceVelocity = GetChildControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = GetChildControl<Vector3Control>("deviceAngularVelocity");
        }
    }

    [InputControlLayout(displayName = "[OpenVR] Vive Tracker (handed)", commonUsages = new[] { "LeftHand", "RightHand" })]
    [Preserve]
    public class HandedViveTracker : XRControllerWithRumble
    {
        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control deviceVelocity { get; private set; }

        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control deviceAngularVelocity { get; private set; }

        [InputControl(aliases = new[] { "primary2DAxisClick" })]
        [Preserve]
        public ButtonControl touchpadClick { get; private set; }

        [InputControl(aliases = new[] { "primary2DAxisTouch" })]
        [Preserve]
        public ButtonControl touchpadTouch { get; private set; }

        [InputControl(aliases = new[] { "primary2DAxis" })]
        [Preserve]
        public Vector2Control touchpad { get; private set; }

        [InputControl]
        [Preserve]
        public AxisControl trigger { get; private set; }

        [InputControl]
        public ButtonControl triggerButton { get; private set; }

        [InputControl]
        [Preserve]
        public ButtonControl gripButton { get; private set; }

        [InputControl]
        [Preserve]
        public ButtonControl menuButton { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            deviceVelocity = GetChildControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = GetChildControl<Vector3Control>("deviceAngularVelocity");

            touchpadClick = GetChildControl<ButtonControl>("touchpadClick");
            touchpadTouch = GetChildControl<ButtonControl>("touchpadTouch");

            gripButton = GetChildControl<ButtonControl>("gripButton");
            triggerButton = GetChildControl<ButtonControl>("triggerButton");
            menuButton = GetChildControl<ButtonControl>("menuButton");

            trigger = GetChildControl<AxisControl>("trigger");

            touchpad = GetChildControl<Vector2Control>("touchpad");
        }
    }

    /// <summary>
    /// An Oculus Touch controller.
    /// </summary>
    [InputControlLayout(displayName = "[OpenVR] Oculus Touch", commonUsages = new[] { "LeftHand", "RightHand" })]
    [Preserve]
    public class OpenVROculusTouchController : XRControllerWithRumble
    {
        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control deviceVelocity { get; private set; }

        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control deviceAngularVelocity { get; private set; }

        [InputControl(aliases = new[] { "primary2DAxis" })]
        [Preserve]
        public Vector2Control joystick { get; private set; }

        [InputControl(aliases = new[] { "primary2DAxisClick" })]
        [Preserve]
        public ButtonControl joystickClick { get; private set; }

        [InputControl(aliases = new[] { "primary2DAxisTouch" })]
        [Preserve]
        public ButtonControl joystickTouch { get; private set; }

        [InputControl]
        [Preserve]
        public AxisControl trigger { get; private set; }

        [InputControl()]
        [Preserve]
        public ButtonControl triggerButton { get; private set; }

        [InputControl()]
        [Preserve]
        public ButtonControl triggerTouch { get; private set; }

        [InputControl]
        [Preserve]
        public AxisControl grip { get; private set; }

        [InputControl]
        [Preserve]
        public ButtonControl gripButton { get; private set; }


        [InputControl]
        [Preserve]
        public ButtonControl primaryButton { get; private set; }

        [InputControl]
        [Preserve]
        public ButtonControl primaryTouch { get; private set; }

        [InputControl]
        [Preserve]
        public ButtonControl secondaryButton { get; private set; }

        [InputControl]
        [Preserve]
        public ButtonControl secondaryTouch { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            deviceVelocity = GetChildControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = GetChildControl<Vector3Control>("deviceAngularVelocity");

            joystick = GetChildControl<Vector2Control>("joystick");
            joystickClick = GetChildControl<ButtonControl>("joystickClick");
            joystickTouch = GetChildControl<ButtonControl>("joystickTouch");

            trigger = GetChildControl<AxisControl>("trigger");
            triggerButton = GetChildControl<ButtonControl>("triggerButton");
            triggerTouch = GetChildControl<ButtonControl>("triggerTouch");

            grip = GetChildControl<AxisControl>("grip");
            gripButton = GetChildControl<ButtonControl>("gripButton");

            primaryButton = GetChildControl<ButtonControl>("primaryButton");
            primaryTouch = GetChildControl<ButtonControl>("primaryTouch");

            secondaryButton = GetChildControl<ButtonControl>("secondaryButton");
            secondaryTouch = GetChildControl<ButtonControl>("secondaryTouch");
        }
    }

    /// <summary>
    /// A Vive Cosmos Controller
    /// </summary>
    [InputControlLayout(displayName = "[OpenVR] Vive Cosmos Controller", commonUsages = new[] { "LeftHand", "RightHand" })]
    [Preserve]
    public class OpenVRViveCosmosController : XRControllerWithRumble
    {
        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control deviceVelocity { get; private set; }

        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control deviceAngularVelocity { get; private set; }

        [InputControl(aliases = new[] { "primary2DAxis" })]
        [Preserve]
        public Vector2Control joystick { get; private set; }

        [InputControl(aliases = new[] { "primary2DAxisClick" })]
        [Preserve]
        public ButtonControl joystickClick { get; private set; }

        [InputControl(aliases = new[] { "primary2DAxisTouch" })]
        [Preserve]
        public ButtonControl joystickTouch { get; private set; }

        [InputControl]
        [Preserve]
        public AxisControl trigger { get; private set; }

        [InputControl()]
        [Preserve]
        public ButtonControl triggerButton { get; private set; }

        [InputControl()]
        [Preserve]
        public ButtonControl triggerTouch { get; private set; }


        [InputControl]
        [Preserve]
        public ButtonControl gripButton { get; private set; }



        [InputControl]
        [Preserve]
        public ButtonControl primaryButton { get; private set; }

        [InputControl]
        [Preserve]
        public ButtonControl primaryTouch { get; private set; }

        [InputControl]
        [Preserve]
        public ButtonControl secondaryButton { get; private set; }

        [InputControl]
        [Preserve]
        public ButtonControl secondaryTouch { get; private set; }

        [InputControl]
        [Preserve]
        public ButtonControl bumperButton { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            deviceVelocity = GetChildControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = GetChildControl<Vector3Control>("deviceAngularVelocity");

            joystick = GetChildControl<Vector2Control>("joystick");
            joystickClick = GetChildControl<ButtonControl>("joystickClick");
            joystickTouch = GetChildControl<ButtonControl>("joystickTouch");

            trigger = GetChildControl<AxisControl>("trigger");
            triggerButton = GetChildControl<ButtonControl>("triggerButton");
            triggerTouch = GetChildControl<ButtonControl>("triggerTouch");

            gripButton = GetChildControl<ButtonControl>("gripButton");

            primaryButton = GetChildControl<ButtonControl>("primaryButton");
            primaryTouch = GetChildControl<ButtonControl>("primaryTouch");

            secondaryButton = GetChildControl<ButtonControl>("secondaryButton");
            secondaryTouch = GetChildControl<ButtonControl>("secondaryTouch");

            bumperButton = GetChildControl<ButtonControl>("bumperButton");
        }
    }

    [InputControlLayout(displayName = "[OpenVR] Index Controller", commonUsages = new[] { "LeftHand", "RightHand" })]
    [Preserve]
    public class OpenVRControllerIndex : XRControllerWithRumble
    {
        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control deviceVelocity { get; private set; }

        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control deviceAngularVelocity { get; private set; }

        [InputControl(aliases = new[] { "primary2DAxis" })]
        [Preserve]
        public Vector2Control touchpad { get; private set; }

        [InputControl(aliases = new[] { "primary2DAxisClick" })]
        [Preserve]
        public ButtonControl touchpadClick { get; private set; }

        [InputControl(aliases = new[] { "primary2DAxisTouch" })]
        [Preserve]
        public ButtonControl touchpadTouch { get; private set; }

        [InputControl(aliases = new[] { "secondary2DAxis" })]
        [Preserve]
        public Vector2Control joystick { get; private set; }

        [InputControl(aliases = new[] { "secondary2DAxisClick" })]
        [Preserve]
        public ButtonControl joystickClick { get; private set; }

        [InputControl(aliases = new[] { "secondary2DAxisTouch" })]
        [Preserve]
        public ButtonControl joystickTouch { get; private set; }

        [InputControl(aliases = new[] { "primaryButton" })]
        [Preserve]
        public ButtonControl aButton { get; private set; }

        [InputControl(aliases = new[] { "primaryTouch" })]
        [Preserve]
        public ButtonControl aTouch { get; private set; }

        [InputControl(aliases = new[] { "secondaryButton" })]
        [Preserve]
        public ButtonControl bButton { get; private set; }

        [InputControl(aliases = new[] { "secondaryTouch" })]
        [Preserve]
        public ButtonControl bTouch { get; private set; }

        [InputControl]
        [Preserve]
        public AxisControl trigger { get; private set; }

        [InputControl]
        public ButtonControl triggerButton { get; private set; }


        [InputControl]
        [Preserve]
        public ButtonControl gripButton { get; private set; }

        [InputControl]
        [Preserve]
        public ButtonControl gripGrab { get; private set; }

        [InputControl(aliases = new[] { "grip" })]
        [Preserve]
        public AxisControl gripForce { get; private set; }

        [InputControl]
        [Preserve]
        public AxisControl gripCapacitiveSense { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            deviceVelocity = GetChildControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = GetChildControl<Vector3Control>("deviceAngularVelocity");

            touchpad = GetChildControl<Vector2Control>("touchpad");
            touchpadClick = GetChildControl<ButtonControl>("touchpadClick");
            touchpadTouch = GetChildControl<ButtonControl>("touchpadTouch");

            joystick = GetChildControl<Vector2Control>("joystick");
            joystickClick = GetChildControl<ButtonControl>("joystickClick");
            joystickTouch = GetChildControl<ButtonControl>("joystickTouch");

            trigger = GetChildControl<AxisControl>("trigger");
            triggerButton = GetChildControl<ButtonControl>("triggerButton");

            gripForce = GetChildControl<AxisControl>("gripForce");
            gripCapacitiveSense = GetChildControl<AxisControl>("gripCapacitiveSense");
            gripButton = GetChildControl<ButtonControl>("gripButton");
            gripGrab = GetChildControl<ButtonControl>("gripGrab");

            aButton = GetChildControl<ButtonControl>("aButton");
            aTouch = GetChildControl<ButtonControl>("aTouch");

            bButton = GetChildControl<ButtonControl>("bButton");
            bTouch = GetChildControl<ButtonControl>("bTouch");
        }
    }


    /// <summary>
    /// A Logitech Stylus tracker.
    /// </summary>
    [Preserve]
    [InputControlLayout(displayName = "[OpenVR] Logitech Stylus")]
    public class LogitechStylus : XRControllerWithRumble
    {
        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control deviceVelocity { get; private set; }

        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control deviceAngularVelocity { get; private set; }

        [InputControl(aliases = new[] { "primary2DAxis" })]
        [Preserve]
        public Vector2Control touchstrip { get; private set; }

        [InputControl(aliases = new[] { "primary2DAxisClick" })]
        [Preserve]
        public ButtonControl touchstripClick { get; private set; }

        [InputControl(aliases = new[] { "primary2DAxisTouch" })]
        [Preserve]
        public ButtonControl touchstripTouch { get; private set; }


        [InputControl]
        [Preserve]
        public AxisControl primary { get; private set; }

        [InputControl]
        [Preserve]
        public ButtonControl primaryButton { get; private set; }

        [InputControl]
        [Preserve]
        public ButtonControl primaryTouch { get; private set; }

        [InputControl]
        [Preserve]
        public AxisControl tip { get; private set; }

        [InputControl]
        [Preserve]
        public ButtonControl tipButton { get; private set; }

        [InputControl]
        [Preserve]
        public ButtonControl tipTouch { get; private set; }


        [InputControl]
        [Preserve]
        public ButtonControl menuButton { get; private set; }

        [InputControl]
        [Preserve]
        public ButtonControl gripButton { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            deviceVelocity = GetChildControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = GetChildControl<Vector3Control>("deviceAngularVelocity");

            touchstrip = GetChildControl<Vector2Control>("touchstrip");
            touchstripClick = GetChildControl<ButtonControl>("touchstripClick");
            touchstripTouch = GetChildControl<ButtonControl>("touchstripTouch");

            primary = GetChildControl<AxisControl>("primary");
            primaryButton = GetChildControl<ButtonControl>("primaryButton");
            primaryTouch = GetChildControl<ButtonControl>("primaryTouch");

            tip = GetChildControl<AxisControl>("tip");
            tipButton = GetChildControl<ButtonControl>("tipButton");
            tipTouch = GetChildControl<ButtonControl>("tipTouch");

            menuButton = GetChildControl<ButtonControl>("menuButton");
            gripButton = GetChildControl<ButtonControl>("gripButton");
        }
    }
}
#endif
