#if UNITY_EDITOR || UNITY_STANDALONE

using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class XRSimulatorPresentationController : MonoBehaviour
{
    [Header("Mode Override")]
    public bool forcePCMode = false;
    public bool forceVRMode = false;

    [Header("Simulator Build Gate")]
    public GameObject xrDeviceSimulatorObject;
    public bool requireSimulatorObjectActiveForPCMode = true;

    [Header("Interactor Roots")]
    public GameObject leftInteractorRoot;
    public GameObject rightInteractorRoot;

    [Header("Controller Visuals")]
    public GameObject leftControllerVisual;
    public GameObject rightControllerVisual;

    [Header("Direct Interactors")]
    public XRDirectInteractor leftDirectInteractor;
    public XRDirectInteractor rightDirectInteractor;

    [Header("Poke Interactors")]
    public XRPokeInteractor leftPokeInteractor;
    public XRPokeInteractor rightPokeInteractor;

    [Header("Ray Interactors")]
    public XRRayInteractor leftRayInteractor;
    public XRRayInteractor rightRayInteractor;

    [Header("Ray Line Visuals")]
    public Behaviour leftRayLineVisual;
    public Behaviour rightRayLineVisual;

    [Header("Optional Line Renderers")]
    public LineRenderer leftLineRenderer;
    public LineRenderer rightLineRenderer;

    [Header("Optional Reticles")]
    public GameObject leftReticleVisual;
    public GameObject rightReticleVisual;

    [Header("Ray Settings")]
    public float vrRayDistance = 5f;
    public float pcRayDistance = 15f;

    [Header("PC Mode Options")]
    public bool hideControllerVisualsInPCMode = true;
    public bool disableDirectInteractorsInPCMode = true;
    public bool disablePokeInteractorsInPCMode = true;
    public bool disableLeftInteractorRootInPCMode = true;
    public bool keepRightInteractorRootActiveInPCMode = true;

    private void Start()
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

            // No HMD + simulator not included/active => do not enter PC simulator mode
            if (requireSimulatorObjectActiveForPCMode && !headsetActive && !simulatorAvailable)
            {
                Debug.Log("XRSimulatorPresentationController: No HMD and simulator not active/included. Applying VR-style safe state.");
                ApplyVRMode();
                return;
            }

            isVR = headsetActive;
        }

        ApplyMode(isVR);
    }

    public void ApplyMode(bool isVR)
    {
        if (isVR)
            ApplyVRMode();
        else
            ApplyPCMode();
    }

    private void ApplyVRMode()
    {
        Debug.Log("XRSimulatorPresentationController: VR mode");

        if (xrDeviceSimulatorObject != null)
            xrDeviceSimulatorObject.SetActive(false);

        if (leftInteractorRoot != null)
            leftInteractorRoot.SetActive(true);

        if (rightInteractorRoot != null)
            rightInteractorRoot.SetActive(true);

        if (leftControllerVisual != null)
            leftControllerVisual.SetActive(true);

        if (rightControllerVisual != null)
            rightControllerVisual.SetActive(true);

        if (leftDirectInteractor != null)
            leftDirectInteractor.enabled = true;

        if (rightDirectInteractor != null)
            rightDirectInteractor.enabled = true;

        if (leftPokeInteractor != null)
            leftPokeInteractor.enabled = true;

        if (rightPokeInteractor != null)
            rightPokeInteractor.enabled = true;

        if (leftRayInteractor != null)
        {
            leftRayInteractor.enabled = true;
            leftRayInteractor.maxRaycastDistance = vrRayDistance;
        }

        if (rightRayInteractor != null)
        {
            rightRayInteractor.enabled = true;
            rightRayInteractor.maxRaycastDistance = vrRayDistance;
        }

        if (leftRayLineVisual != null)
            leftRayLineVisual.enabled = true;

        if (rightRayLineVisual != null)
            rightRayLineVisual.enabled = true;

        if (leftLineRenderer != null)
            leftLineRenderer.enabled = true;

        if (rightLineRenderer != null)
            rightLineRenderer.enabled = true;

        if (leftReticleVisual != null)
            leftReticleVisual.SetActive(true);

        if (rightReticleVisual != null)
            rightReticleVisual.SetActive(true);
    }

    private void ApplyPCMode()
    {
        Debug.Log("XRSimulatorPresentationController: PC / simulator mode");

        if (xrDeviceSimulatorObject != null)
            xrDeviceSimulatorObject.SetActive(true);

        if (disableLeftInteractorRootInPCMode && leftInteractorRoot != null)
            leftInteractorRoot.SetActive(false);
        else
        {
            if (leftDirectInteractor != null)
                leftDirectInteractor.enabled = false;

            if (leftPokeInteractor != null)
                leftPokeInteractor.enabled = false;

            if (leftRayInteractor != null)
                leftRayInteractor.enabled = false;

            if (leftRayLineVisual != null)
                leftRayLineVisual.enabled = false;

            if (leftLineRenderer != null)
                leftLineRenderer.enabled = false;

            if (leftReticleVisual != null)
                leftReticleVisual.SetActive(false);
        }

        if (rightInteractorRoot != null)
            rightInteractorRoot.SetActive(keepRightInteractorRootActiveInPCMode);

        if (hideControllerVisualsInPCMode)
        {
            if (leftControllerVisual != null)
                leftControllerVisual.SetActive(false);

            if (rightControllerVisual != null)
                rightControllerVisual.SetActive(false);
        }

        if (disableDirectInteractorsInPCMode)
        {
            if (leftDirectInteractor != null)
                leftDirectInteractor.enabled = false;

            if (rightDirectInteractor != null)
                rightDirectInteractor.enabled = false;
        }

        if (disablePokeInteractorsInPCMode)
        {
            if (leftPokeInteractor != null)
                leftPokeInteractor.enabled = false;

            if (rightPokeInteractor != null)
                rightPokeInteractor.enabled = false;
        }

        if (leftRayInteractor != null)
            leftRayInteractor.enabled = false;

        if (leftRayLineVisual != null)
            leftRayLineVisual.enabled = false;

        if (leftLineRenderer != null)
            leftLineRenderer.enabled = false;

        if (leftReticleVisual != null)
            leftReticleVisual.SetActive(false);

        if (rightRayInteractor != null)
        {
            rightRayInteractor.enabled = true;
            rightRayInteractor.maxRaycastDistance = pcRayDistance;
        }

        if (rightRayLineVisual != null)
            rightRayLineVisual.enabled = true;

        if (rightLineRenderer != null)
            rightLineRenderer.enabled = true;

        if (rightReticleVisual != null)
            rightReticleVisual.SetActive(true);
    }
}

#endif