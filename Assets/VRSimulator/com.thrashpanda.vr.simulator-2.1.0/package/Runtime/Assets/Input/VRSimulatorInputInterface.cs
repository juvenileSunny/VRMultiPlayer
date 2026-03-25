using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

//Class: VRSimulatorInputInterface
//Interface for calling native plugin functions to control the simulated XR Devices.
internal static class VRSimulatorInputInterface
{
#if UNITY_EDITOR || VRSim_include_in_build
    /* Function: SetPrimaryState
     * Sets the primary button state of the left or right controller of the XR Plugin.
     * 
     * Parameters:
     * left - decides if setting left (true) or right (false) controller
     * pressed - state the button is set to
     */
    [DllImport("VRSimulatorXRPlugin")]
    internal static extern int SetPrimaryState(bool left, bool pressed);

    /* Function: SetSecondaryState
     * Sets the secondary button state of the left or right controller of the XR Plugin.
     * 
     * Parameters:
     * left - decides if setting left (true) or right (false) controller
     * pressed - state the button is set to
     */
    [DllImport("VRSimulatorXRPlugin")]
    internal static extern int SetSecondaryState(bool left, bool pressed);

    /* Function: SetTriggerState
     * Sets the trigger button state of the left or right controller of the XR Plugin.
     * 
     * Parameters:
     * left - decides if setting left (true) or right (false) controller
     * pressed - state the button is set to
     */
    [DllImport("VRSimulatorXRPlugin")]
    internal static extern int SetTriggerState(bool left, bool pressed);

    /* Function: SetTriggerValue
     * Sets the trigger of the left or right controller of the XR Plugin to value.
     * 
     * Parameters:
     * left - decides if setting left (true) or right (false) controller
     * value - value the trigger is set to
     */
    [DllImport("VRSimulatorXRPlugin")]
    internal static extern int SetTriggerValue(bool left, float value);

    /* Function: SetTriggerState
     * Sets the grip button state of the left or right controller of the XR Plugin.
     * 
     * Parameters:
     * left - decides if setting left (true) or right (false) controller
     * pressed - state the button is set to
     */
    [DllImport("VRSimulatorXRPlugin")]
    internal static extern int SetGripState(bool left, bool pressed);

    /* Function: SetGripValue
     * Sets the grip of the left or right controller of the XR Plugin to value.
     * 
     * Parameters:
     * left - decides if setting left (true) or right(false) controller
     * value - value the trigger is set to
     */
    [DllImport("VRSimulatorXRPlugin")]
    internal static extern int SetGripValue(bool left, float value);

    /* Function: SetPrimary2D
     * Sets the primary 2D axis of the left or right controller of the XR Plugin to a vector2 value.
     * 
     * Parameters:
     * left - decides if setting left (true) or right(false) controller
     * value - value the axis is set to
     */
    [DllImport("VRSimulatorXRPlugin")]
    internal static extern int SetPrimary2D(bool left, Vector2 value);

    /* Function: SetHMDTransform
     * Sets position and rotation of the XR Plugin hmd.
     * 
     * Parameters:
     * pos - position the hmd is set to
     * rot - quaternion the hmd is set to
     */
    [DllImport("VRSimulatorXRPlugin")]
    internal static extern int SetHMDTransform(Vector3 pos, Quaternion rot);

    /* Function: SetControllerTransform
     * Sets position and rotation of the left or right controller of the XR Plugin.
     * 
     * Parameters:
     * left - decides if setting left (true) or right(false) controller
     * pos - position the controller is set to
     * rot - quaternion the controller is set to
     */
    [DllImport("VRSimulatorXRPlugin")]
    internal static extern int SetControllerTransform(bool left, Vector3 pos, Quaternion rot);
#endif
}
