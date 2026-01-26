using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using PanettoneGames.GenEvents;
using System;
// using UnityEditor.EditorTools;

public class TutorialManager : MonoBehaviour, IGameEventListener<int>
{
    // canvas object for toast notifications
    public GameObject toastCanvas;

    // list of pc objects to enable in non vr 
    public GameObject[] pcObjects;

    // list of vr objects to enable in vr
    public GameObject[] vrObjects;

    //All the tutorialIDs are handled here, so that I am not having to set a bunch of IDs in different classes
    //Thats what I was doing, but thats pretty dumb.

    //However, these will have to be manually updated and basically these corespond with the pos of the message
    //That prompts you to do the acton
    public enum TutorialEventIDs
    {
        DirectionInputEvent = 1,
        MouseInputEvent = 2,
        GazeObjectEvent = 5,
        GrabObjectEvent = 7,
        ObjectOnTableEvent = 8,
        PressSpawnMindNodeButton = 10,
        MindNodeCompleted = 11,
        Finished = 12
    }
    public static TutorialEventIDs currentEvent;
    //Here we associate each eventID with a message to prompt you to do that action.
    [Header("Tutorial Messages")]
    [Header("PC Messages")]
    [Tooltip("Messages shown to PC users - must match tutorial event order")]
    public string[] pcTutorialMessages;
    
    [Header("VR Messages")]
    [Tooltip("Messages shown to VR users - must match tutorial event order")]
    public string[] vrTutorialMessages;
    
    [Header("Settings")]
    public int messageTime = 5;
    
    [Header("VR Toast Placement")]
    [Tooltip("Local Z offset from the VR camera for the toast canvas when in VR mode")] 
    public float vrToastZOffset = 1.5f;
    [Tooltip("Local uniform scale applied to the toast canvas when in World Space (VR)")]
    public float vrToastScale = 0.002f;
    
    private bool isVRMode = false;
    private string tutorialMessageWithEvent(TutorialEventIDs id) {
        if (isVRMode)
        {
            return id switch{
                TutorialEventIDs.DirectionInputEvent => "Lets get you moving! Use your controller's joystick to move around.",
                TutorialEventIDs.MouseInputEvent => "Great! Now look around by moving your head naturally.",
                TutorialEventIDs.GazeObjectEvent => "Point your controller at one of the *Black Cubes* to highlight it.",
                TutorialEventIDs.GrabObjectEvent => "Now you can grab the object by pressing the grab button on your controller!",
                _ => "Unknown VR tutorial step."
            };
        }
        else
        {
            return id switch{
                TutorialEventIDs.DirectionInputEvent => "Lets get you moving, try using WASD or the Arrow Keys!",
                TutorialEventIDs.MouseInputEvent => "Awesome! Now use the Mouse to look around the room.",
                TutorialEventIDs.GazeObjectEvent => "Now lets put those peepers to some good use! Get close to one of those *Black Cubes* and line it up with the black dot in the middle of your screen.",
                TutorialEventIDs.GrabObjectEvent => "While maintaining eye contact, press the Left Mouse Button to pick up that object.",
                _ => "Unknown tutorial step."
            };
        }
    }
    
    // Get the appropriate message array based on VR mode
    private string[] GetCurrentTutorialMessages()
    {
        return isVRMode ? vrTutorialMessages : pcTutorialMessages;
    }
    // Function that returns the next tutorial step, if more events are added this has to be changed.
    private TutorialEventIDs GetNextEvent(TutorialEventIDs currentStep)
    {
        switch (currentStep)
        {
            case TutorialEventIDs.DirectionInputEvent:
                return TutorialEventIDs.MouseInputEvent;
            case TutorialEventIDs.MouseInputEvent:
                return TutorialEventIDs.GazeObjectEvent;
            case TutorialEventIDs.GazeObjectEvent:
                return TutorialEventIDs.GrabObjectEvent;
            case TutorialEventIDs.GrabObjectEvent:
                return TutorialEventIDs.ObjectOnTableEvent;
            
            //Notice that at the last event it just loops on itself, this is intential
            case TutorialEventIDs.ObjectOnTableEvent:
                return TutorialEventIDs.PressSpawnMindNodeButton;

            case TutorialEventIDs.PressSpawnMindNodeButton:
                return TutorialEventIDs.MindNodeCompleted;

            case TutorialEventIDs.MindNodeCompleted:
                return TutorialEventIDs.MindNodeCompleted;

            //As I have it written the only way to set the current event to finished is to manually set it.
            case TutorialEventIDs.Finished:
                return TutorialEventIDs.Finished;

            default:
                throw new System.Exception("Unknown tutorial step.");
        }
    }
    //This is here because I want to have capacity for messages between these input prompts, this also has to be changed if 
    //New input events are added.
    private bool requirePlayerInput(int currentMessage) {
        if( currentMessage == (int)TutorialEventIDs.DirectionInputEvent ||
            currentMessage == (int)TutorialEventIDs.MouseInputEvent || 
            currentMessage == (int)TutorialEventIDs.GazeObjectEvent ||
            currentMessage == (int)TutorialEventIDs.GrabObjectEvent ||
            currentMessage == (int)TutorialEventIDs.ObjectOnTableEvent ||
            currentMessage == (int)TutorialEventIDs.PressSpawnMindNodeButton ||
            currentMessage == (int)TutorialEventIDs.MindNodeCompleted){
                return true;
        }
        return false;
    }
    [Header("These are related to completing the tutorial")]
    public IntEvent tutorialEvents;
    public int eventCount;
    
    private int currentMessage = 0;
    [Header("This is for the Notifications, so that this knows when a message is hidden and can respond")]
    public static int ToastHideID = 1000;

    //This is wha gets checked to see if the tutorial is complete.
    private bool[] eventCompletion;


    void Awake()
    {
        // Detect if we're in VR mode
        DetectVRMode();

        if (isVRMode)
        {
            // Enable VR-specific objects
            foreach (var obj in vrObjects)
            {
                if (obj != null)
                    obj.SetActive(true);
            }
            // Disable PC-specific objects
            foreach (var obj in pcObjects)
            {
                if (obj != null)
                    obj.SetActive(false);
            }
        }
        else
        {
            // Enable PC-specific objects
            foreach (var obj in pcObjects)
            {
                if (obj != null)
                    obj.SetActive(true);
            }
            // Disable VR-specific objects
            foreach (var obj in vrObjects)
            {
                if (obj != null)
                    obj.SetActive(false);
            }
        }
        
        // Configure ToastNotification for VR or PC
        ConfigureToastNotificationForPlatform();
        
        eventCompletion = new bool[eventCount];
        for(int i = 0; i < eventCount; i++) {
            eventCompletion[i] = false;
        }

        // Use appropriate message array
        string[] currentMessages = GetCurrentTutorialMessages();
        ToastNotification.Show(currentMessages[0], messageTime);
        eventCompletion[0] = true;

        currentEvent = TutorialEventIDs.DirectionInputEvent;
    }
    
    void ConfigureToastNotificationForPlatform()
    {
        // Prefer the explicitly assigned canvas, else try to find from ToastNotification
        Canvas toastCanvasComp = null;
        if (toastCanvas != null)
        {
            // get Canvas on the object or its children
            toastCanvasComp = toastCanvas.GetComponent<Canvas>();
            if (toastCanvasComp == null)
                toastCanvasComp = toastCanvas.GetComponentInChildren<Canvas>(true);
        }
        if (toastCanvasComp == null)
        {
            var tn = FindObjectOfType<ToastNotification>();
            if (tn != null)
                toastCanvasComp = tn.GetComponent<Canvas>();
        }
        
        if (toastCanvasComp != null)
        {
            if (isVRMode)
            {
                // Configure for VR - World Space canvas
                toastCanvasComp.renderMode = RenderMode.WorldSpace;

                // Find an active camera to guide placement
                var cam = GetActiveCamera();
                if (cam != null)
                {
                    toastCanvasComp.worldCamera = cam;

                    // Do NOT parent to the camera (to avoid attached feeling)
                    var t = toastCanvasComp.transform;
                    t.SetParent(null, worldPositionStays: true);

                    // Initial placement in front of camera on horizontal plane
                    Vector3 flatFwd = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
                    if (flatFwd.sqrMagnitude < 1e-4f) flatFwd = cam.transform.forward;
                    Vector3 startPos = cam.transform.position + flatFwd * vrToastZOffset;
                    startPos.y = 1.2f; // Fixed height at 1.3
                    t.position = startPos;
                    t.rotation = Quaternion.LookRotation(flatFwd, Vector3.up); // Use positive flatFwd for 90 degree rotation
                    t.localScale = Vector3.one * Mathf.Max(0.0001f, vrToastScale);

                    // Ensure a VRToastFollower exists and is configured
                    var follower = t.GetComponent<VRToastFollower>();
                    if (follower == null) follower = t.gameObject.AddComponent<VRToastFollower>();
                    follower.target = cam.transform;
                    follower.distance = vrToastZOffset;
                    follower.fixedHeight = 1.2f;
                    // Keep defaults for yawRecenterAngle and recenterSpeed; can be exposed if needed
                }
                else
                {
                    Debug.LogWarning("TutorialManager: No active camera found for VR toast placement.");
                }
                
                Debug.Log("ToastNotification configured for VR (World Space)");
            }
            else
            {
                // Configure for PC - Screen Space overlay
                // Detach from camera if previously parented
                var t = toastCanvasComp.transform;
                if (t.parent != null)
                    t.SetParent(null, worldPositionStays: true);

                toastCanvasComp.renderMode = RenderMode.ScreenSpaceOverlay;
                toastCanvasComp.worldCamera = null;
                toastCanvasComp.sortingOrder = 100; // High sorting order to appear on top
                
                Debug.Log("ToastNotification configured for PC (Screen Space)");
            }
        }
        else
        {
            Debug.LogWarning("ToastNotification Canvas not found!");
        }
    }

    private Camera GetActiveCamera()
    {
        // Prefer an enabled, active camera
        var cameras = Camera.allCameras;
        for (int i = 0; i < cameras.Length; i++)
        {
            var c = cameras[i];
            if (c != null && c.isActiveAndEnabled && c.gameObject.activeInHierarchy)
                return c;
        }
        // Fallbacks
        return Camera.main ?? FindObjectOfType<Camera>();
    }
    
    void DetectVRMode()
    {
        // Modern XR detection approach
        bool xrManagerActive = XRGeneralSettings.Instance != null && 
                              XRGeneralSettings.Instance.Manager != null && 
                              XRGeneralSettings.Instance.Manager.activeLoader != null;
        
        // Also check for VR input devices
        bool vrDevicePresent = false;
        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevices(devices);
        
        foreach (InputDevice device in devices)
        {
            if (device.characteristics.HasFlag(InputDeviceCharacteristics.HeadMounted) ||
                device.characteristics.HasFlag(InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller) ||
                device.characteristics.HasFlag(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller))
            {
                vrDevicePresent = true;
                break;
            }
        }
        
        isVRMode = xrManagerActive || vrDevicePresent;
        
        Debug.Log($"Tutorial Manager: VR Mode = {isVRMode}");
        Debug.Log($"XR Manager Active: {xrManagerActive}");
        Debug.Log($"VR Device Present: {vrDevicePresent}");
        Debug.Log($"Total Input Devices: {devices.Count}");
        
        // Fallback: Also check legacy XRSettings for older Unity versions
        if (!isVRMode)
        {
            isVRMode = XRSettings.enabled && XRSettings.isDeviceActive;
            Debug.Log($"Legacy XR Check - Enabled: {XRSettings.enabled}, Active: {XRSettings.isDeviceActive}");
        }
    }
    void OnEnable()
    {
        tutorialEvents.RegisterListener(this);
    }
    void OnDisable()
    {
        tutorialEvents.UnregisterListener(this);
    }


    bool isTutorialComplete() {
        for(int i = 0; i < eventCount; i++) {
            if(!eventCompletion[i]) {
                return false;
            }
        }
        return true;
    }

    //This the function that gets called when in other scripts you call tutorialEvents.raise(int)
    public void OnEventRaised(int item) {
        string[] currentMessages = GetCurrentTutorialMessages();
        
        // If the event is the toastHidID that means its a message that is being hidden
        if(item == ToastHideID) {
            // Move to the next message
            if(currentMessage < eventCount) {
                eventCompletion[currentMessage] = true;
                currentMessage++;
                // If the next message requires the player to something, we want it to stay up for a while, otherwise it disapears after messageTime seconds
                if(requirePlayerInput(currentMessage)) {
                    ToastNotification.Show(currentMessages[currentMessage], 1000);
                }else {
                    ToastNotification.Show(currentMessages[currentMessage], messageTime);
                }
                
            }
        // Otherwise we check if this id requires player input.
        }else if(requirePlayerInput(currentMessage)){
            // Move to next input event event
            currentEvent = GetNextEvent(currentEvent);
            tutorialEvents.Raise(ToastHideID);
            eventCompletion[item] = true;
        }

        // Once the tutorial is complete we display the final message and set the current even to finished so no more events
        // are sent.
        if(isTutorialComplete()) {
            currentEvent = TutorialEventIDs.Finished;
            ToastNotification.Show(currentMessages[eventCount], messageTime+10);
        }
        
    }
}



// GenericPropertyJSON:{"name":"vrTutorialMessages","type":-1,"arraySize":11,"arrayType":"string","children":[{"name":"Array","type":-1,"arraySize":11,"arrayType":"string","children":[{"name":"size","type":12,"val":11},{"name":"data","type":3,"val":"Welcome to the Tutorial!"},{"name":"data","type":3,"val":"Lets get you moving! Use your controller's joystick to move around."},{"name":"data","type":3,"val":"Great! Now look around by moving your head naturally."},{"name":"data","type":3,"val":"The rays comming out of your controllers can be used to point at objects to interact with them."},{"name":"data","type":3,"val":"The rays comming out of your controllers can be used to point at objects to interact with them."},{"name":"data","type":3,"val":"Point your controller at one of the *Black Cubes* to interact with it."},{"name":"data","type":3,"val":"Your controller vibrates if the object is interactable."},{"name":"data","type":3,"val":"Now you can grab the object by pressing the grab button on your controller!"},{"name":"data","type":3,"val":"Place 3 of those cubes on the coffee table in front of the sofa."},{"name":"data","type":3,"val":"Tutorial Complete!"},{"name":"data","type":3,"val":"Now you can move towards the door to proceed to the next scene."}]}]}
// GenericPropertyJSON:{"name":"pcTutorialMessages","type":-1,"arraySize":11,"arrayType":"string","children":[{"name":"Array","type":-1,"arraySize":11,"arrayType":"string","children":[{"name":"size","type":12,"val":11},{"name":"data","type":3,"val":"Welcome to the Tutorial!"},{"name":"data","type":3,"val":"Lets get you moving, try using WASD or the Arrow Keys to walk around."},{"name":"data","type":3,"val":"Awesome now use the mouse to look around the room!"},{"name":"data","type":3,"val":"That black dot in the middle of the screen is your virtual eye."},{"name":"data","type":3,"val":"When prompted to look at something that means to move that black dot over that object."},{"name":"data","type":3,"val":"Now lets put those peepers to some good use! Get close to one of those *Black Cubes* and line it up with the black dot in the middle of your screen."},{"name":"data","type":3,"val":"When you look at an object and a Red Outline appears, that means you can pick it up."},{"name":"data","type":3,"val":"Now left click with the mouse to pick up the cube."},{"name":"data","type":3,"val":"Place 3 of those cubes on the coffee table in front of the sofa."},{"name":"data","type":3,"val":"Tutorial Complete!"},{"name":"data","type":3,"val":"Now you can move towards the door to proceed to the next scene."}]}]}