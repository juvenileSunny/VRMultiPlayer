using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using PanettoneGames.GenEvents;

public class TutorialVRTriggers : MonoBehaviour
{
    [Header("Tutorial Events")]
    public IntEvent tutorialEvents;
    
    [Header("VR Movement Detection")]
    public float joystickThreshold = 0.3f;
    
    [Header("VR Look Around Detection")]
    public float headRotationThreshold = 5f; // Degrees of rotation needed
    
    [Header("VR Controller Pointing Detection")]
    public GameObject[] targetObjects; // List of objects that can be pointed at (must have XR Interactable components)
    
    private Quaternion lastHeadRotation;
    
    void Start()
    {
        Debug.Log("VR Tutorial Triggers initialized - monitoring joystick input, head rotation, and controller pointing");
        StartCoroutine(InitializeHeadTrackingCoroutine());
    }

    void Update()
    {
        // Check what the current tutorial step expects and only monitor that input
        switch (TutorialManager.currentEvent)
        {
            case TutorialManager.TutorialEventIDs.DirectionInputEvent:
                CheckForVRJoystickMovement();
                break;
                
            case TutorialManager.TutorialEventIDs.MouseInputEvent:
                CheckForVRHeadRotation();
                break;
                
            case TutorialManager.TutorialEventIDs.GazeObjectEvent:
                CheckForVRControllerPointing();
                break;
                
            // Add more cases here for other VR tutorial steps as needed
            case TutorialManager.TutorialEventIDs.GrabObjectEvent:
                CheckForVRObjectGrab();
                break;
                
            case TutorialManager.TutorialEventIDs.ObjectOnTableEvent:
                // These would be handled by other VR detection methods
                break;
                
            case TutorialManager.TutorialEventIDs.Finished:
                // Tutorial complete, no monitoring needed
                break;
        }
    }
    
    IEnumerator InitializeHeadTrackingCoroutine()
    {
        // Wait for VR to fully initialize
        yield return new WaitForSeconds(1f);
        
        int retryAttempts = 0;
        const int maxRetries = 10;
        
        while (retryAttempts < maxRetries)
        {
            InputDevice headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (headDevice.isValid && headDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion headRot))
            {
                lastHeadRotation = headRot;
                Debug.Log("VR Head tracking initialized successfully!");
                yield break; // Success, exit coroutine
            }
            
            retryAttempts++;
            Debug.Log($"VR Head tracking attempt {retryAttempts}/{maxRetries} - waiting...");
            yield return new WaitForSeconds(0.5f);
        }
        
        Debug.LogWarning("VR Head tracking failed to initialize after maximum retries. Will try on-demand.");
    }
    
    void InitializeHeadTracking()
    {
        InputDevice headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        if (headDevice.isValid && headDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion headRot))
        {
            lastHeadRotation = headRot;
            Debug.Log("VR Head tracking initialized");
            return;
        }
        Debug.LogWarning("VR Head tracking not available");
    }
    
    void CheckForVRJoystickMovement()
    {
        // Check right controller joystick
        InputDevice rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (rightController.isValid && rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 rightStick))
        {
            if (rightStick.magnitude > joystickThreshold)
            {
                TriggerDirectionEvent("Right Controller Joystick", rightStick);
                return;
            }
        }
        
        // Check left controller joystick
        InputDevice leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (leftController.isValid && leftController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 leftStick))
        {
            if (leftStick.magnitude > joystickThreshold)
            {
                TriggerDirectionEvent("Left Controller Joystick", leftStick);
                return;
            }
        }
    }
    
    void TriggerDirectionEvent(string inputSource, Vector2 inputVector)
    {
        Debug.Log($"VR Joystick movement detected! Source: {inputSource}, Input: {inputVector}");
        
        // Only trigger if we're currently expecting this event
        if (TutorialManager.currentEvent == TutorialManager.TutorialEventIDs.DirectionInputEvent)
        {
            if (tutorialEvents != null)
            {
                tutorialEvents.Raise((int)TutorialManager.TutorialEventIDs.DirectionInputEvent);
                Debug.Log("DirectionInputEvent raised for VR joystick movement");
            }
        }
    }
    
    void CheckForVRHeadRotation()
    {
        InputDevice headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        if (headDevice.isValid && headDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion currentHeadRotation))
        {
            // Initialize lastHeadRotation if this is the first valid reading
            if (lastHeadRotation == Quaternion.identity)
            {
                lastHeadRotation = currentHeadRotation;
                Debug.Log("Head tracking initialized in CheckForVRHeadRotation");
                return;
            }
            
            // Calculate the angle difference between current and last head rotation
            float angleDifference = Quaternion.Angle(lastHeadRotation, currentHeadRotation);
            
            if (angleDifference > headRotationThreshold)
            {
                TriggerLookAroundEvent(angleDifference);
            }
            
            lastHeadRotation = currentHeadRotation;
        }
        else
        {
            Debug.LogWarning("VR Head device not available or invalid");
        }
    }
    
    void TriggerLookAroundEvent(float rotationAmount)
    {
        Debug.Log($"VR Head rotation detected! Rotation: {rotationAmount:F1} degrees");
        
        // Only trigger if we're currently expecting this event
        if (TutorialManager.currentEvent == TutorialManager.TutorialEventIDs.MouseInputEvent)
        {
            if (tutorialEvents != null)
            {
                tutorialEvents.Raise((int)TutorialManager.TutorialEventIDs.MouseInputEvent);
                Debug.Log("MouseInputEvent raised for VR head rotation (look around)");
            }
        }
    }
    
    void CheckForVRControllerPointing()
    {
        // Check if we have target objects configured
        if (targetObjects == null || targetObjects.Length == 0)
        {
            Debug.LogWarning("No target objects configured for VR controller pointing detection");
            return;
        }
        
        // Check each target object to see if any interactor is hovering over it
        foreach (GameObject targetObj in targetObjects)
        {
            if (targetObj == null) continue;
            
            // Look for XR Grab Interactable component
            var grabInteractable = targetObj.GetComponent<XRGrabInteractable>();
            if (grabInteractable != null && grabInteractable.interactorsHovering.Count > 0)
            {
                TriggerControllerPointingEvent("VR Controller", targetObj);
                return;
            }
            
            // Also check for basic XR Base Interactable
            var baseInteractable = targetObj.GetComponent<XRBaseInteractable>();
            if (baseInteractable != null && baseInteractable.interactorsHovering.Count > 0)
            {
                TriggerControllerPointingEvent("VR Controller", targetObj);
                return;
            }
        }
    }
    
    void CheckForVRObjectGrab()
    {
        // Check if we have target objects configured
        if (targetObjects == null || targetObjects.Length == 0)
        {
            Debug.LogWarning("No target objects configured for VR object grab detection");
            return;
        }
        
        // Check each target object to see if any interactor is selecting (grabbing) it
        foreach (GameObject targetObj in targetObjects)
        {
            if (targetObj == null) continue;
            
            // Look for XR Grab Interactable component
            var grabInteractable = targetObj.GetComponent<XRGrabInteractable>();
            if (grabInteractable != null && grabInteractable.interactorsSelecting.Count > 0)
            {
                TriggerVRGrabEvent("VR Controller", targetObj);
                return;
            }
            
            // Also check for basic XR Base Interactable
            var baseInteractable = targetObj.GetComponent<XRBaseInteractable>();
            if (baseInteractable != null && baseInteractable.interactorsSelecting.Count > 0)
            {
                TriggerVRGrabEvent("VR Controller", targetObj);
                return;
            }
        }
    }
    
    void TriggerVRGrabEvent(string controllerName, GameObject grabbedObject)
    {
        Debug.Log($"VR Object grab detected! {controllerName} grabbed: {grabbedObject.name}");
        
        // Only trigger if we're currently expecting this event
        if (TutorialManager.currentEvent == TutorialManager.TutorialEventIDs.GrabObjectEvent)
        {
            if (tutorialEvents != null)
            {
                tutorialEvents.Raise((int)TutorialManager.TutorialEventIDs.GrabObjectEvent);
                Debug.Log("GrabObjectEvent raised for VR object grab");
            }
        }
    }
    
    void TriggerControllerPointingEvent(string controllerName, GameObject pointedObject)
    {
        Debug.Log($"VR Controller pointing detected! {controllerName} pointing at: {pointedObject.name}");
        
        // Only trigger if we're currently expecting this event
        if (TutorialManager.currentEvent == TutorialManager.TutorialEventIDs.GazeObjectEvent)
        {
            if (tutorialEvents != null)
            {
                tutorialEvents.Raise((int)TutorialManager.TutorialEventIDs.GazeObjectEvent);
                Debug.Log("GazeObjectEvent raised for VR controller pointing");
            }
        }
    }
    
    // Public methods for testing/debugging (no longer needed for normal flow)
    public void ResetAllEvents()
    {
        Debug.Log("VR Tutorial Triggers reset - will follow TutorialManager.currentEvent");
    }
}
