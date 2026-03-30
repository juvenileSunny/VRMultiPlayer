using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.UI;

/* Class: VRSimulator
 * The main class of the VR Simulator. Defines functions to control a <VRSimulatorHMD> and two <VRSimulatorController>.
 */
public class VRSimulator : MonoBehaviour
{
#if UNITY_EDITOR || UNITY_STANDALONE || VRSim_include_in_build
    //Variable: xRRigToFollow
    //A Transform the virtual rig will match position and rotation to. Needs to be assigned in order to match the simulated controllers with the XR controllers in the scene.
    [SerializeField]
    public Transform xRRigToFollow = null;

    //Variable: simulatorRig
    //The transform of the simulator rig. Should be identical to this components transform.
    private Transform simulatorRig = null;

    //Variable: simulatorHMD
    //The simulated hmd that can be controlled.
    [SerializeField]
    VRSimulatorHMD simulatorHMD = null;

    //Variable: hmd
    //The transform of the simulated hmd that can be controlled.
    private Transform hmd = null;

    //Variable: simulatorControllerL
    //The left simulated controller that can be controlled.
    [SerializeField]
    VRSimulatorController simulatorControllerL = null;

    //Variable: controllerL
    //The transform of the left simulated controller that can be controlled.
    private Transform controllerL = null;

    //Variable: simulatorControllerR
    //The right simulated controller that can be controlled.
    [SerializeField]
    VRSimulatorController simulatorControllerR = null;

    //Variable: controllerR
    //The transform of the right simulated controller that can be controlled.
    private Transform controllerR = null;

    //Variable: leftControllerRestPosition
    //The rest position of <controllerL> relative to <hmd>.
    [SerializeField]
    private Vector3 leftControllerRestPosition = new Vector3(-.2f, -.2f, .5f);
    //Variable: leftControllerRestRotation
    //The rest rotation of <controllerL> relative to <hmd>.
    private Quaternion leftControllerRestRotation;

    //Variable: rightControllerRestPosition
    //The rest position of <controllerR> relative to <hmd>.
    [SerializeField]
    private Vector3 rightControllerRestPosition = new Vector3(.2f, -.2f, .5f);
    //Variable: rightControllerRestRotation
    //The rest rotation of <controllerR> relative to <hmd>.
    private Quaternion rightControllerRestRotation;

    //Variable: showDirectionHelpers
    //Bool that decides wether the direction helpers will be displayed. This will default to false if no <xRRigToFollow> is assigned.
    [SerializeField]
    private bool showDirectionHelpers = true;

    //Variable: directionPrefab
    //GameObject that gets instantiated per controller and will be shown if <showDirectionHelpers> is true.
    [SerializeField]
    private GameObject directionPrefab = null;
    //Variable: directionsLeft
    //Reference to the instantiated <directionPrefab> for the left controller.
    private GameObject directionsLeft;
    //Variable: directionsRight
    //Reference to the instantiated <directionPrefab> for the right controller.
    private GameObject directionsRight;

    internal bool rotateHands = false;
    private GameObject rotationHelper;

    //Variable: trackingOriginMode
    //Sets the tracking origin mode for the simulator. Needs to match the TrackingOriginModeFlags of the XRRig. If set to Floor the <headsetHeight> will be considered.
    [SerializeField]
    TrackingOriginModeFlags trackingOriginMode = TrackingOriginModeFlags.Floor;

    //Variable: headsetHeight
    //Offset of the simulated hmd from the ground. Will only be considered if <trackingOriginMode> is set to Floor.
    [SerializeField]
    private float headsetHeight = 1.6f;

    internal bool handsActive = false;
    internal bool switchHand = false;
    internal bool switchHandAxis = false;

    // Start is called before the first frame update
    void Start()
    {
        Init();
    }

    //Function: Init
    //Initializes the simulator.
    private void Init()
    {
        InitVariables();

        InitTrackingMode();

        rotationHelper = new GameObject("RotationHelper");
        rotationHelper.transform.SetParent(hmd);
        rotationHelper.transform.localEulerAngles = Vector3.zero;

        controllerL.transform.localPosition = leftControllerRestPosition + hmd.localPosition;
        leftControllerRestRotation = controllerL.transform.localRotation;
        controllerR.transform.localPosition = rightControllerRestPosition + hmd.localPosition;
        rightControllerRestRotation = controllerR.transform.localRotation;

    }

    //Function: InitVariables
    //Assigns transforms to  <simulatorRig>, <hmd>, <controllerL>, <controllerR> and instatiates the direction helpers.
    private void InitVariables()
    {
        simulatorRig = this.transform;
        hmd = simulatorHMD.transform;
        controllerL = simulatorControllerL.transform;
        controllerR = simulatorControllerR.transform;

        directionsLeft = Instantiate(directionPrefab, controllerL);
        directionsLeft.transform.GetChild(2).gameObject.SetActive(false);
        directionsLeft.gameObject.SetActive(false);

        directionsRight = Instantiate(directionPrefab, controllerR);
        directionsRight.transform.GetChild(2).gameObject.SetActive(false);
        directionsRight.gameObject.SetActive(false);
    }

    //Function: InitTrackingMode
    //Sets the hmd position to <headsetHeight> if <trackingOriginMode> is set to Floor.
    private void InitTrackingMode()
    {
        if (trackingOriginMode == TrackingOriginModeFlags.Floor)
        {
            hmd.localPosition += new Vector3(0, headsetHeight, 0);
        } else
        {
            Debug.LogWarning("<b><i>Tracking Origin Mode</i></b> is not set to Floor, <b><i>Headset Height</i></b> will be ignored!");
        }
    }

    //Function: ResetControllers
    //Resets position and rotation of the controllers.
    internal void ResetControllers()
    {
        controllerL.transform.localPosition = leftControllerRestPosition + hmd.localPosition;
            
        controllerR.transform.localPosition = rightControllerRestPosition + hmd.localPosition;

        directionsLeft.transform.SetParent(controllerL.parent);
        controllerL.transform.localRotation = leftControllerRestRotation;
        controllerL.Rotate(simulatorRig.up, hmd.localEulerAngles.y, Space.World);
        directionsLeft.transform.SetParent(controllerL);

        directionsRight.transform.SetParent(controllerR.parent);
        controllerR.transform.localRotation = rightControllerRestRotation;
        controllerR.Rotate(simulatorRig.up, hmd.localEulerAngles.y, Space.World);
        directionsRight.transform.SetParent(controllerR);

        controllerL.localPosition = VectorHelper.RotatePointAroundPivot(controllerL.localPosition, hmd.localPosition, new Vector3(0, hmd.localEulerAngles.y, 0));
        controllerR.localPosition = VectorHelper.RotatePointAroundPivot(controllerR.localPosition, hmd.localPosition, new Vector3(0, hmd.localEulerAngles.y, 0));
        
    }

    /*
        Function: UpdateDirectionsHelper
        Shows or hides the direction helper objects <directionsRight> and <directionsLeft> depending on <handsActive>
        and <showDirectionHelpers>.
    */
    void UpdateDirectionsHelper()
    {
        if (handsActive && showDirectionHelpers)
        {
            directionsRight.gameObject.SetActive(!switchHand);
            directionsLeft.gameObject.SetActive(switchHand);

            directionsRight.transform.GetChild(1).gameObject.SetActive(!switchHandAxis);
            directionsRight.transform.GetChild(2).gameObject.SetActive(switchHandAxis);
            directionsLeft.transform.GetChild(1).gameObject.SetActive(!switchHandAxis);
            directionsLeft.transform.GetChild(2).gameObject.SetActive(switchHandAxis);
        }
        else
        {
            directionsRight.gameObject.SetActive(false);
            directionsLeft.gameObject.SetActive(false);
        }
    }


    /*
     * Function: RotateHeadset
     * Rotates <hmd> the specified degrees defined by the given euler rotation.
     *
     * Parameters: 
     * angle - Euler roation in degrees
    */
    internal void RotateHeadset(Vector3 angle)
    {
        float resultingAngle = hmd.localEulerAngles.x + angle.x;
        resultingAngle = (resultingAngle > 180) ? resultingAngle - 360 : resultingAngle;
        if (resultingAngle > -85 && resultingAngle < 85)
            hmd.Rotate(angle.x, 0,0);

        hmd.Rotate(simulatorRig.up, angle.y, Space.World);

        rotationHelper.transform.eulerAngles = new Vector3(3, hmd.eulerAngles.y, hmd.eulerAngles.z);

        controllerL.localPosition = VectorHelper.RotatePointAroundPivot(controllerL.localPosition, hmd.localPosition, new Vector3(0, angle.y, 0));
        controllerR.localPosition = VectorHelper.RotatePointAroundPivot(controllerR.localPosition, hmd.localPosition, new Vector3(0, angle.y, 0));
        controllerL.Rotate(simulatorRig.up, angle.y, Space.World);
        controllerR.Rotate(simulatorRig.up, angle.y, Space.World);
        
    }

    /*
     * Function: MoveController
     * Moves either the left or the right controller by the given translation.
     * 
     * Parameters:
     * left - Defines which controller to move.
     * translation - The desired translation to move the controller by.
     */
    internal void MoveController(bool left, Vector3 translation)
    {
        if(left)
            controllerL.Translate(translation, directionsLeft.transform);
        else
            controllerR.Translate(translation, directionsRight.transform);
    }

    /*
     * Function: RotateController
     * Rotates either the left or the right controller by the given euler angles.
     * 
     * Parameters:
     * left - Defines which controller to rotate.
     * translation - The desired rotation in euler angles to rotate the controller by.
     */
    internal void RotateController(bool left, Vector3 rotation)
    {
        if (left)
        {
            directionsLeft.transform.SetParent(controllerL.parent);

            controllerL.Rotate(rotationHelper.transform.up, rotation.y, Space.World);
            controllerL.Rotate(rotationHelper.transform.right, rotation.x, Space.World);
            controllerL.Rotate(rotationHelper.transform.forward, rotation.z, Space.World);

            directionsLeft.transform.SetParent(controllerL);
        }else
        {
            directionsRight.transform.SetParent(controllerR.parent);

            controllerR.Rotate(rotationHelper.transform.up, rotation.y, Space.World);
            controllerR.Rotate(rotationHelper.transform.right, rotation.x, Space.World);
            controllerR.Rotate(rotationHelper.transform.forward, rotation.z, Space.World);

            directionsRight.transform.SetParent(controllerR);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (xRRigToFollow)
        {
            this.transform.SetPositionAndRotation(xRRigToFollow.position,xRRigToFollow.rotation);
        } else
        {
            if (showDirectionHelpers)
            {
                showDirectionHelpers = false;
                Debug.LogError("Can't display direction helpers without <b><i>XR Rig To Follow</i></b> assigned in the <b><i>VR Simulator</i></b> script ...");
            }
        }

        UpdateDirectionsHelper();
            
       

        //UpdateMouseMovement();

    }


    /*
     * Function: Move
     * Moves <hmd> horizontally by the given translation relative to <simulatorRig>.
     * 
     * Parameters:
     * direction - The desired translation to move <hmd> by.
     */
    public void Move(Vector2 direction)
    {
        Vector3 translation = direction.y * new Vector3(hmd.forward.x, 0, hmd.forward.z).normalized + direction.x * new Vector3(hmd.right.x,0,hmd.right.z).normalized;
        translation = Quaternion.AngleAxis(-simulatorRig.localEulerAngles.y,simulatorRig.up) * translation;
        hmd.Translate(translation, simulatorRig);
        controllerL.Translate(translation, simulatorRig);
        controllerR.Translate(translation, simulatorRig);

    }
#endif
}
