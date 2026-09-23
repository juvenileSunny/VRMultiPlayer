#if (UNITY_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM)
using UnityEngine.Scripting;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;

namespace ThrashPanda.VR.Simulator.Input
{
    /// <summary>
    /// An Simulator XR controller.
    /// </summary>
    [Preserve]
    [InputControlLayout(displayName = "Simulator XR Controller", commonUsages = new[] { "LeftHand", "RightHand" })]
    public class SimulatorXRController : XRControllerWithRumble
    {
        [Preserve]
        [InputControl(aliases = new[] { "Primary2DAxis", "Joystick" })]
        public Vector2Control thumbstick { get; private set; }

        [Preserve]
        [InputControl(aliases = new[] { "trigger" })]
        public AxisControl trigger { get; private set; }
        [Preserve]
        [InputControl(aliases = new[] { "grip" })]
        public AxisControl grip { get; private set; }

        [Preserve]
        [InputControl(aliases = new[] { "A", "X", "Alternate" })]
        public ButtonControl primaryButton { get; private set; }
        [Preserve]
        [InputControl(aliases = new[] { "B", "Y", "Primary" })]
        public ButtonControl secondaryButton { get; private set; }
        [Preserve]
        [InputControl(aliases = new[] { "GripButton" })]
        public ButtonControl gripPressed { get; private set; }
        [Preserve]
        [InputControl]
        public ButtonControl start { get; private set; }
        [Preserve]
        [InputControl(aliases = new[] { "JoystickOrPadPressed", "thumbstickClick" })]
        public ButtonControl thumbstickClicked { get; private set; }
        [Preserve]
        [InputControl(aliases = new[] { "ATouched", "XTouched", "ATouch", "XTouch" })]
        public ButtonControl primaryTouched { get; private set; }
        [Preserve]
        [InputControl(aliases = new[] { "BTouched", "YTouched", "BTouch", "YTouch" })]
        public ButtonControl secondaryTouched { get; private set; }
        [Preserve]
        [InputControl(aliases = new[] { "indexTouch", "indexNearTouched", "TriggerTouched" })]
        public AxisControl triggerTouched { get; private set; }
        [Preserve]
        [InputControl(aliases = new[] { "indexButton", "indexTouched", "TriggerButton" })]
        public ButtonControl triggerPressed { get; private set; }
        [Preserve]
        [InputControl(aliases = new[] { "JoystickOrPadTouched", "thumbstickTouch" })]
        public ButtonControl thumbstickTouched { get; private set; }

        /*
        [Preserve]
        [InputControl(aliases = new[] { "controllerTrackingState" })]
        public new IntegerControl trackingState { get; private set; }
        [Preserve]
        [InputControl(aliases = new[] { "ControllerIsTracked" })]
        public new ButtonControl isTracked { get; private set; }
        [Preserve]
        [InputControl(aliases = new[] { "controllerPosition" })]
        public new Vector3Control devicePosition { get; private set; }
        [Preserve]
        [InputControl(aliases = new[] { "controllerRotation" })]
        public new QuaternionControl deviceRotation { get; private set; }
        [Preserve]
        [InputControl(aliases = new[] { "controllerVelocity" })]
        public Vector3Control deviceVelocity { get; private set; }
        [Preserve]
        [InputControl(aliases = new[] { "controllerAngularVelocity" })]
        public Vector3Control deviceAngularVelocity { get; private set; }
        [Preserve]
        [InputControl(aliases = new[] { "controllerAcceleration" })]
        public Vector3Control deviceAcceleration { get; private set; }
        [Preserve]
        [InputControl(aliases = new[] { "controllerAngularAcceleration" })]
        public Vector3Control deviceAngularAcceleration { get; private set; }
        */

        protected override void FinishSetup()
        {
            base.FinishSetup();

            thumbstick = GetChildControl<Vector2Control>("thumbstick");
            trigger = GetChildControl<AxisControl>("trigger");
            triggerTouched = GetChildControl<AxisControl>("triggerTouched");
            grip = GetChildControl<AxisControl>("grip");

            primaryButton = GetChildControl<ButtonControl>("primaryButton");
            secondaryButton = GetChildControl<ButtonControl>("secondaryButton");
            gripPressed = GetChildControl<ButtonControl>("gripPressed");
            start = GetChildControl<ButtonControl>("start");
            thumbstickClicked = GetChildControl<ButtonControl>("thumbstickClicked");
            primaryTouched = GetChildControl<ButtonControl>("primaryTouched");
            secondaryTouched = GetChildControl<ButtonControl>("secondaryTouched");
            thumbstickTouched = GetChildControl<ButtonControl>("thumbstickTouched");
            triggerPressed = GetChildControl<ButtonControl>("triggerPressed");

            /*
            trackingState = GetChildControl<IntegerControl>("trackingState");
            isTracked = GetChildControl<ButtonControl>("isTracked");
            devicePosition = GetChildControl<Vector3Control>("devicePosition");
            deviceRotation = GetChildControl<QuaternionControl>("deviceRotation");
            deviceVelocity = GetChildControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = GetChildControl<Vector3Control>("deviceAngularVelocity");
            deviceAcceleration = GetChildControl<Vector3Control>("deviceAcceleration");
            deviceAngularAcceleration = GetChildControl<Vector3Control>("deviceAngularAcceleration");
            */
        }
    }

}
#endif