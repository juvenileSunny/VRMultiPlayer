#if UNITY_EDITOR || UNITY_STANDALONE

using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

public class XRInputModeSwitcher : MonoBehaviour
{
    [Header("References")]
    public GameObject xrDeviceSimulatorObject;
    public InputActionManager inputActionManager;

    [Header("Action Assets")]
    public InputActionAsset vrInputActions;
    public InputActionAsset pcSimulatorActions;
    public InputActionAsset pcControllerActions;

    [Header("Optional components to disable in PC mode")]
    public Behaviour[] vrOnlyComponents;

    [Header("Optional components to disable in VR mode")]
    public Behaviour[] pcOnlyComponents;

    [Header("Debug Override")]
    public bool forcePCMode = false;
    public bool forceVRMode = false;

    [Header("Simulator Build Gate")]
    public bool requireSimulatorObjectActiveForPCMode = true;

    void Start()
    {
        bool simulatorAvailable =
            xrDeviceSimulatorObject != null &&
            xrDeviceSimulatorObject.activeInHierarchy;

        bool isVR;

        if (forcePCMode)
        {
            isVR = false;
        }
        else if (forceVRMode)
        {
            isVR = true;
        }
        else
        {
            bool headsetActive = XRSettings.isDeviceActive;

            // If simulator must be present/active and it is not,
            // do not switch into PC simulator mode.
            if (requireSimulatorObjectActiveForPCMode && !headsetActive && !simulatorAvailable)
            {
                Debug.Log("XRInputModeSwitcher: No HMD and simulator not active/included, staying in VR-style disabled simulator state.");
                EnableVRMode();
                return;
            }

            isVR = headsetActive;
        }

        if (isVR)
            EnableVRMode();
        else
            EnablePCMode();
    }

    void EnableVRMode()
    {
        Debug.Log("XRInputModeSwitcher: VR mode enabled");

        if (xrDeviceSimulatorObject != null)
            xrDeviceSimulatorObject.SetActive(false);

        if (vrInputActions != null) vrInputActions.Enable();
        if (pcSimulatorActions != null) pcSimulatorActions.Disable();
        if (pcControllerActions != null) pcControllerActions.Disable();

        if (vrOnlyComponents != null)
        {
            foreach (var c in vrOnlyComponents)
            {
                if (c != null) c.enabled = true;
            }
        }

        if (pcOnlyComponents != null)
        {
            foreach (var c in pcOnlyComponents)
            {
                if (c != null) c.enabled = false;
            }
        }
    }

    void EnablePCMode()
    {
        Debug.Log("XRInputModeSwitcher: PC mode enabled");

        if (xrDeviceSimulatorObject != null)
            xrDeviceSimulatorObject.SetActive(true);

        if (vrInputActions != null) vrInputActions.Disable();
        if (pcSimulatorActions != null) pcSimulatorActions.Enable();
        if (pcControllerActions != null) pcControllerActions.Enable();

        if (vrOnlyComponents != null)
        {
            foreach (var c in vrOnlyComponents)
            {
                if (c != null) c.enabled = false;
            }
        }

        if (pcOnlyComponents != null)
        {
            foreach (var c in pcOnlyComponents)
            {
                if (c != null) c.enabled = true;
            }
        }
    }
}

#endif