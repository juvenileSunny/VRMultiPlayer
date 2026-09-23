#if (UNITY_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM)
#define USE_INPUT_SYSTEM
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

#if USE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#elif ENABLE_LEGACY_INPUT_MANAGER
using static LegacyKeyAssign;
#endif

#if USE_INPUT_SYSTEM && UNITY_EDITOR
[CustomEditor(typeof(VRSimulatorInputHandler))]
public class VRSimulatorInputHandlerEditor : Editor
{
    override public void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("Edit Input Asset", EditorStyles.miniButton))
        {
            SerializedProperty property = serializedObject.FindProperty("inputActions");
            int id = property.objectReferenceInstanceIDValue;
            AssetDatabase.OpenAsset(id);
        }
    }
}
#endif

/* Class: VRSimulatorInputHandler
 * Component for handling user inputs to control the required <VRSimulator> component.
 */
[RequireComponent(typeof(VRSimulator))]
public class VRSimulatorInputHandler : MonoBehaviour
{
#if UNITY_EDITOR || VRSim_include_in_build
    //Variable: invertYAxis
    //Decides if the y-axis to control the simulator hmd should be inverted.
    [SerializeField]
    bool invertYAxis = false;

    bool controllersActive = false;

    bool switchAxis = false;

    bool toggleRotation = false;

    bool switchController = false;

    bool controllerMoving = false;

    bool looking = false;
    bool moving = false;
    bool stickActive = false;

    //Variable: controllerMovementSpeed
    //Speed for controller movement.
    [SerializeField]
    private float controllerMovementSpeed = 0.02f;

    //Variable: controllerRotationSpeed
    //Speed for controller rotation.
    [SerializeField]
    private float controllerRotationSpeed = 10f;

    //Variable: movementSpeed
    //Speed for HMD movement.
    [SerializeField]
    private float movementSpeed = 1f;

    //Variable: lookSpeed
    //Speed for HMD rotation.
    [SerializeField]
    private float lookSpeed = 0.1f;

    //Variable: lockCursor
    //Bool for deciding if the mouse can be locked/hidden while controlling the simulator.
    [SerializeField]
    private bool lockCursor = true;
    private bool m_cursorIsLocked = false;

    //Variable: inputActions
    //InputActionAsset that holds the control bindings for all simulator actions. Only visible if using Unity Input System.
#if USE_INPUT_SYSTEM
    [SerializeField]
    InputActionAsset inputActions = null;

#elif ENABLE_LEGACY_INPUT_MANAGER
    private float legacyMouseCompensation = 20f;

    //Variable: assignedKeys
    //<InputAssign> holding control bindings for all simulator actions. Only visible if using Legacy Input Manager.
    [SerializeField]
    InputAssign assignedKeys = new InputAssign(
        new MovementInput(
            KeyCode.W,
            KeyCode.S,
            KeyCode.A,
            KeyCode.D
            ),
        new InteractionInput(
            KeyCode.Mouse1,
            KeyCode.Mouse0,
            KeyCode.E,
            KeyCode.F,
            KeyCode.UpArrow,
            KeyCode.DownArrow,
            KeyCode.LeftArrow,
            KeyCode.RightArrow
            ),
        new ControllerMovementInput(
            KeyCode.G,
            KeyCode.H,
            KeyCode.R,
            KeyCode.LeftControl,
            KeyCode.Q
            )
        );
#endif
    VRSimulator simulator;
    private bool showOverlay = true;
    private Text overlayText = null;

    Vector2 moveDirection = Vector2.zero;
    Vector2 lookDelta = Vector2.zero;
    Vector3 controllerDelta = Vector3.zero;

    private void Start()
    {
#if USE_INPUT_SYSTEM
        InitInputSystem();
#elif ENABLE_LEGACY_INPUT_MANAGER
#endif

        simulator = this.GetComponent<VRSimulator>();
        overlayText = this.GetComponentInChildren<Text>();
        ShowKeyOverlay(showOverlay);

    }

    private void Update()
    {
        MoveUpdate();
        LookUpdate();
        UpdateControllerHandling();
        ControllerUpdate();
        UpdateInteraction();

#if USE_INPUT_SYSTEM
        if (Keyboard.current.f1Key.wasReleasedThisFrame)
        {
            showOverlay = !showOverlay;
            ShowKeyOverlay(showOverlay);
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.F1))
        {
            showOverlay = !showOverlay;
            ShowKeyOverlay(showOverlay);
        }
#endif
        UpdateCursorLock();
    }

    //Function: InitInputSystem
    //Initializes the required input actions found in <inputActions> and sets event callbacks.
#if USE_INPUT_SYSTEM
    void InitInputSystem()
    {
        inputActions.Enable();
        inputActions.FindActionMap("Rig").FindAction("Look").started += context => { looking = true; };
        inputActions.FindActionMap("Rig").FindAction("Look").canceled += context => { looking = false; };
        inputActions.FindActionMap("Rig").FindAction("Move").started += context => { moving = true; };
        inputActions.FindActionMap("Rig").FindAction("Move").canceled += context => { moving = false; };
        inputActions.FindActionMap("Controller").FindAction("Move").started += context => { controllerMoving = true; };
        inputActions.FindActionMap("Controller").FindAction("Move").canceled += context => { controllerMoving = false; };
        inputActions.FindActionMap("Controller").FindAction("ToggleRotation").performed += context => { toggleRotation = !toggleRotation; };
        inputActions.FindActionMap("Controller").FindAction("Reset").performed += context => { simulator.ResetControllers(); };

        inputActions.FindActionMap("Controller").FindAction("SwitchController").performed += context => {
            switchController = !switchController;
            simulator.switchHand = switchController;
        };

        inputActions.FindActionMap("Controller").FindAction("ToggleController").performed += context => {
            controllersActive = !controllersActive;
            simulator.handsActive = controllersActive;
        };

        inputActions.FindActionMap("Controller").FindAction("SwitchAxis").started += context => {
            switchAxis = true;
            simulator.switchHandAxis = switchAxis;
        };

        inputActions.FindActionMap("Controller").FindAction("SwitchAxis").canceled += context => {
            switchAxis = false;
            simulator.switchHandAxis = switchAxis;
        };

        inputActions.FindActionMap("XRInput").FindAction("Trigger").started += context => { VRSimulatorInputInterface.SetTriggerState(switchController, true); };
        inputActions.FindActionMap("XRInput").FindAction("Trigger").canceled += context => { VRSimulatorInputInterface.SetTriggerState(switchController, false); };

        inputActions.FindActionMap("XRInput").FindAction("Grip").started += context => { VRSimulatorInputInterface.SetGripState(switchController, true); };
        inputActions.FindActionMap("XRInput").FindAction("Grip").canceled += context => { VRSimulatorInputInterface.SetGripState(switchController, false); };

        inputActions.FindActionMap("XRInput").FindAction("PrimaryButton").started += context => { VRSimulatorInputInterface.SetPrimaryState(switchController, true); };
        inputActions.FindActionMap("XRInput").FindAction("PrimaryButton").canceled += context => { VRSimulatorInputInterface.SetPrimaryState(switchController, false); };

        inputActions.FindActionMap("XRInput").FindAction("SecondaryButton").started += context => { VRSimulatorInputInterface.SetSecondaryState(switchController, true); };
        inputActions.FindActionMap("XRInput").FindAction("SecondaryButton").canceled += context => { VRSimulatorInputInterface.SetSecondaryState(switchController, false); };

        inputActions.FindActionMap("XRInput").FindAction("Stick").started += context => { stickActive = true; };
        inputActions.FindActionMap("XRInput").FindAction("Stick").canceled += context => {
            stickActive = false;
            VRSimulatorInputInterface.SetPrimary2D(switchController, Vector2.zero);
            };

    }
#endif

    //Function: LookUpdate
    //Update function for handling hmd rotation input.
    void LookUpdate()
    {
        if (!controllersActive)
        {

#if USE_INPUT_SYSTEM
            if (looking)
            {
                lookDelta = inputActions.FindActionMap("Rig").FindAction("Look").ReadValue<Vector2>();

            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            lookDelta.x = Input.GetAxis("Mouse X");
            lookDelta.y = Input.GetAxis("Mouse Y");
            lookDelta *= legacyMouseCompensation;
#endif
            lookDelta.y *= invertYAxis ? 1 : -1;
            lookDelta *= lookSpeed;
            if (lookDelta.x != 0 || lookDelta.y != 0)
                simulator.RotateHeadset(new Vector3(lookDelta.y, lookDelta.x, 0));
        }
    }

    //Function: MoveUpdate
    //Update function for handling hmd movement input.
    void MoveUpdate()
    {
#if USE_INPUT_SYSTEM
        #region
        if (moving)
        {
            moveDirection = inputActions.FindActionMap("Rig").FindAction("Move").ReadValue<Vector2>() * Time.deltaTime * movementSpeed;
            simulator.Move(moveDirection);
        }
        #endregion
#elif ENABLE_LEGACY_INPUT_MANAGER
        #region
        moveDirection = Vector2.zero;

        if (Input.GetKey(assignedKeys.movementKeys.forward))
        {
            moveDirection.y += 1;
        }

        if (Input.GetKey(assignedKeys.movementKeys.back))
        {
            moveDirection.y -= 1;
        }

        if (Input.GetKey(assignedKeys.movementKeys.left))
        {
            moveDirection.x -= 1;
        }

        if (Input.GetKey(assignedKeys.movementKeys.right))
        {
            moveDirection.x += 1;
        }
        simulator.Move(moveDirection * Time.deltaTime * movementSpeed);
        #endregion
#endif
    }

    //Function: ControllerUpdate
    //Update function for handling controller movement and rotation input. Rotates the active controller if <toggleRotation> is true or moves it otherwise.
    void ControllerUpdate()
    {
        if (controllersActive)
        {
#if USE_INPUT_SYSTEM
            if (controllerMoving)
            {
                controllerDelta = inputActions.FindActionMap("Controller").FindAction("Move").ReadValue<Vector2>();
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.mousePresent)
            {
                controllerMoving = true;
                controllerDelta.x = Input.GetAxis("Mouse X");
                controllerDelta.y = Input.GetAxis("Mouse Y");

                controllerDelta *= legacyMouseCompensation;
            }

#endif
            if (controllerMoving)
            {

                if (toggleRotation)
                {
                    controllerDelta.z = switchAxis ? controllerDelta.x : 0;
                    controllerDelta.x = switchAxis ? 0 : controllerDelta.x;

                    controllerDelta *= controllerRotationSpeed * Time.deltaTime;

                    simulator.RotateController(switchController, new Vector3(controllerDelta.y, -controllerDelta.x, controllerDelta.z));
                }
                else
                {
                    controllerDelta.z = switchAxis ? controllerDelta.y : 0;
                    controllerDelta.y = switchAxis ? 0 : controllerDelta.y;

                    controllerDelta *= controllerMovementSpeed * Time.deltaTime;

                    simulator.MoveController(switchController, new Vector3(controllerDelta.x, controllerDelta.y, controllerDelta.z));
                }
            }

        }
    }

    //Function: UpdateControllerHandling
    //Update function for handling controller input states (Reset/Toggle Controllers/Rotation/Switch Hands). Only needed for Legacy Input.
    void UpdateControllerHandling()
    {
#if USE_INPUT_SYSTEM
        //handled directly with events

#elif ENABLE_LEGACY_INPUT_MANAGER
        #region
        if (Input.GetKeyDown(assignedKeys.controllerMovementKeys.resetControllers))
        {
            simulator.ResetControllers();
        }

        if (Input.GetKeyUp(assignedKeys.controllerMovementKeys.toggleHands))
        {
            controllersActive = !controllersActive;
            simulator.handsActive = controllersActive;
        }

        if (Input.GetKeyUp(assignedKeys.controllerMovementKeys.toggleHandsRotation))
        {
            toggleRotation = !toggleRotation;
        }

        if (Input.GetKeyUp(assignedKeys.controllerMovementKeys.switchHands))
        {
            switchController = !switchController;
            simulator.switchHand = switchController;
        }

        switchAxis = Input.GetKey(assignedKeys.controllerMovementKeys.switchHandAxis) ? true : false;
        simulator.switchHandAxis = switchAxis;
        #endregion
#endif
    }

    //Function: UpdateInteraction
    //Update function for handling controller interaction input (buttons etc.).
    void UpdateInteraction()
    {
#if USE_INPUT_SYSTEM
        if (stickActive)
        {
            VRSimulatorInputInterface.SetPrimary2D(switchController, inputActions.FindActionMap("XRInput").FindAction("Stick").ReadValue<Vector2>());
        }
        //others handled directly with events
#elif ENABLE_LEGACY_INPUT_MANAGER
        #region
        if (Input.GetKeyDown(assignedKeys.interactionKeys.grip))
        {
            VRSimulatorInputInterface.SetGripState(switchController, true);
        }
        else if (Input.GetKeyUp(assignedKeys.interactionKeys.grip))
        {
            VRSimulatorInputInterface.SetGripState(switchController, false);
        }

        if (Input.GetKeyDown(assignedKeys.interactionKeys.trigger))
        {
            VRSimulatorInputInterface.SetTriggerState(switchController, true);
        }
        else if (Input.GetKeyUp(assignedKeys.interactionKeys.trigger))
        {
            VRSimulatorInputInterface.SetTriggerState(switchController, false);
        }

        if (Input.GetKeyDown(assignedKeys.interactionKeys.primaryButton))
        {
            VRSimulatorInputInterface.SetPrimaryState(switchController, true);
        }
        else if (Input.GetKeyUp(assignedKeys.interactionKeys.primaryButton))
        {
            VRSimulatorInputInterface.SetPrimaryState(switchController, false);
        }

        if (Input.GetKeyDown(assignedKeys.interactionKeys.secondaryButton))
        {
            VRSimulatorInputInterface.SetSecondaryState(switchController, true);
        }
        else if (Input.GetKeyUp(assignedKeys.interactionKeys.secondaryButton))
        {
            VRSimulatorInputInterface.SetSecondaryState(switchController, false);
        }

        Vector2 stickValue = Vector2.zero;
        if (Input.GetKey(assignedKeys.interactionKeys.stickUp))
        {
            stickValue.y += 1;
        }

        if (Input.GetKey(assignedKeys.interactionKeys.stickDown))
        {
            stickValue.y -= 1;
        }

        if (Input.GetKey(assignedKeys.interactionKeys.stickRight))
        {
            stickValue.x += 1;
        }

        if (Input.GetKey(assignedKeys.interactionKeys.stickLeft))
        {
            stickValue.x -= 1;
        }

        VRSimulatorInputInterface.SetPrimary2D(switchController, stickValue.normalized);

        #endregion
#endif
    }

    //Function: ShowKeyOverlay
    //Function for showing or hiding overlay ui with simulator controls.
    void ShowKeyOverlay(bool show)
    {
#if USE_INPUT_SYSTEM
        overlayText.text = "";
        if (inputActions.FindActionMap("Rig").FindAction("Move").bindings[0].isComposite)
        {
            overlayText.text += "Forward: " + inputActions.FindActionMap("Rig").FindAction("Move").bindings[1].ToDisplayString() + "\n" +
                "Back: " + inputActions.FindActionMap("Rig").FindAction("Move").bindings[2].ToDisplayString() + "\n" +
                "Left: " + inputActions.FindActionMap("Rig").FindAction("Move").bindings[3].ToDisplayString() + "\n" +
                "Right: " + inputActions.FindActionMap("Rig").FindAction("Move").bindings[4].ToDisplayString();
        } else
        {
            overlayText.text += inputActions.FindActionMap("Rig").FindAction("Move").bindings[0].ToDisplayString();
        }        
        overlayText.text += "\n" + "\n";
        overlayText.text +=
            "Grip : " + inputActions.FindActionMap("XRInput").FindAction("Grip").bindings[0].ToDisplayString() + "\n" +
            "Trigger: " + inputActions.FindActionMap("XRInput").FindAction("Trigger").bindings[0].ToDisplayString() + "\n" +
            "Primary: " + inputActions.FindActionMap("XRInput").FindAction("PrimaryButton").bindings[0].ToDisplayString() + "\n" +
            "Secondary: " + inputActions.FindActionMap("XRInput").FindAction("SecondaryButton").bindings[0].ToDisplayString() + "\n";
        overlayText.text += "Stick:" + "\n";

        if (inputActions.FindActionMap("XRInput").FindAction("Stick").bindings[0].isComposite)
        {
            overlayText.text += "\t" + inputActions.FindActionMap("XRInput").FindAction("Stick").bindings[1].ToDisplayString() + "\n" +
                "\t" + inputActions.FindActionMap("XRInput").FindAction("Stick").bindings[2].ToDisplayString() + "\n" +
                "\t" + inputActions.FindActionMap("XRInput").FindAction("Stick").bindings[3].ToDisplayString() + "\n" +
                "\t" + inputActions.FindActionMap("XRInput").FindAction("Stick").bindings[4].ToDisplayString();
        }
        else
        {
            overlayText.text += inputActions.FindActionMap("Rig").FindAction("Move").bindings[0].ToDisplayString();
        }
        overlayText.text += "\n" + "\n";
        overlayText.text += 
            "Toggle Controller: " + inputActions.FindActionMap("Controller").FindAction("ToggleController").bindings[0].ToDisplayString() + "\n" +
            "Switch Axis: " + inputActions.FindActionMap("Controller").FindAction("SwitchAxis").bindings[0].ToDisplayString() + "\n" +
            "Switch Hands: " + inputActions.FindActionMap("Controller").FindAction("SwitchController").bindings[0].ToDisplayString() + "\n" +
            "Toggle Rotation: " + inputActions.FindActionMap("Controller").FindAction("ToggleRotation").bindings[0].ToDisplayString() + "\n" +
            "Reset Controller: " + inputActions.FindActionMap("Controller").FindAction("Reset").bindings[0].ToDisplayString() + "\n" + 
            "\n" +
            "Toggle Overlay: " + KeyCode.F1;

#elif ENABLE_LEGACY_INPUT_MANAGER
        overlayText.text = "Forward: " + assignedKeys.movementKeys.forward + "\n" +
            "Back: " + assignedKeys.movementKeys.back + "\n" +
            "Left: " + assignedKeys.movementKeys.left + "\n" +
            "Right: " + assignedKeys.movementKeys.right + "\n" + "\n" +
            "Grip: " + assignedKeys.interactionKeys.grip + "\n" +
            "Trigger: " + assignedKeys.interactionKeys.trigger + "\n" +
            "Primary: " + assignedKeys.interactionKeys.primaryButton + "\n" +
            "Secondary: " + assignedKeys.interactionKeys.secondaryButton + "\n" +
            "Stick:" + "\n" +
            "\t" + assignedKeys.interactionKeys.stickUp + "\n" +
            "\t" + assignedKeys.interactionKeys.stickDown + "\n" +
            "\t" + assignedKeys.interactionKeys.stickLeft + "\n" +
            "\t" + assignedKeys.interactionKeys.stickRight + "\n" +
            "\n" +
            "Toggle Controller: " + assignedKeys.controllerMovementKeys.toggleHands + "\n" +
            "Toggle Move Axis: " + assignedKeys.controllerMovementKeys.switchHandAxis + "\n" +
            "Switch Hands: " + assignedKeys.controllerMovementKeys.switchHands + "\n" +
            "Toggle Rotation: " + assignedKeys.controllerMovementKeys.toggleHandsRotation + "\n" +
            "Reset Controller: " + assignedKeys.controllerMovementKeys.resetControllers + "\n" + "\n" +
            "Toggle Overlay: " + KeyCode.F1;
#endif
        overlayText.transform.parent.gameObject.SetActive(show);
    }

    //Function: SetCursorLock
    //Sets the cursor lock state if <lockCursor> is true.
    public void SetCursorLock(bool value)
    {
        lockCursor = value;
        if (!lockCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    //Function: UpdateCursorLock
    //Updates the cursor lock state if <lockCursor> is true.
    private void UpdateCursorLock()
    {
        if (lockCursor)
        {
#if USE_INPUT_SYSTEM
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                m_cursorIsLocked = false;
            }
            else if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                m_cursorIsLocked = true;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyUp(KeyCode.Escape))
            {
                m_cursorIsLocked = false;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                m_cursorIsLocked = true;
            }
#endif

            if (m_cursorIsLocked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else if (!m_cursorIsLocked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
#endif
}
