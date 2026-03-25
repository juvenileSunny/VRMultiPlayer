using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class XRSimulatorPresentationController : MonoBehaviour
{
    [Header("Mode Override")]
    public bool forcePCMode = false;
    public bool forceVRMode = false;

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
        bool isVR;

        if (forcePCMode)
            isVR = false;
        else if (forceVRMode)
            isVR = true;
        else
            isVR = XRSettings.isDeviceActive;

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

        // Interactor roots
        if (leftInteractorRoot != null)
            leftInteractorRoot.SetActive(true);

        if (rightInteractorRoot != null)
            rightInteractorRoot.SetActive(true);

        // Controller visuals
        if (leftControllerVisual != null)
            leftControllerVisual.SetActive(true);

        if (rightControllerVisual != null)
            rightControllerVisual.SetActive(true);

        // Direct interactors
        if (leftDirectInteractor != null)
            leftDirectInteractor.enabled = true;

        if (rightDirectInteractor != null)
            rightDirectInteractor.enabled = true;

        // Poke interactors
        if (leftPokeInteractor != null)
            leftPokeInteractor.enabled = true;

        if (rightPokeInteractor != null)
            rightPokeInteractor.enabled = true;

        // Ray interactors
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

        // Line visuals
        if (leftRayLineVisual != null)
            leftRayLineVisual.enabled = true;

        if (rightRayLineVisual != null)
            rightRayLineVisual.enabled = true;

        // Line renderers
        if (leftLineRenderer != null)
            leftLineRenderer.enabled = true;

        if (rightLineRenderer != null)
            rightLineRenderer.enabled = true;

        // Reticles
        if (leftReticleVisual != null)
            leftReticleVisual.SetActive(true);

        if (rightReticleVisual != null)
            rightReticleVisual.SetActive(true);
    }

    private void ApplyPCMode()
    {
        Debug.Log("XRSimulatorPresentationController: PC / simulator mode");

        // Left interactor root completely off if desired
        if (disableLeftInteractorRootInPCMode && leftInteractorRoot != null)
            leftInteractorRoot.SetActive(false);
        else
        {
            // If not disabling whole root, manually disable left-side interactors/visuals
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

        // Right root should usually stay active because it is the mouse-like pointer hand
        if (rightInteractorRoot != null)
            rightInteractorRoot.SetActive(keepRightInteractorRootActiveInPCMode);

        // Controller visuals
        if (hideControllerVisualsInPCMode)
        {
            if (leftControllerVisual != null)
                leftControllerVisual.SetActive(false);

            if (rightControllerVisual != null)
                rightControllerVisual.SetActive(false);
        }

        // Direct interactors
        if (disableDirectInteractorsInPCMode)
        {
            if (leftDirectInteractor != null)
                leftDirectInteractor.enabled = false;

            if (rightDirectInteractor != null)
                rightDirectInteractor.enabled = false;
        }

        // Poke interactors
        if (disablePokeInteractorsInPCMode)
        {
            if (leftPokeInteractor != null)
                leftPokeInteractor.enabled = false;

            if (rightPokeInteractor != null)
                rightPokeInteractor.enabled = false;
        }

        // Left ray fully off
        if (leftRayInteractor != null)
            leftRayInteractor.enabled = false;

        if (leftRayLineVisual != null)
            leftRayLineVisual.enabled = false;

        if (leftLineRenderer != null)
            leftLineRenderer.enabled = false;

        if (leftReticleVisual != null)
            leftReticleVisual.SetActive(false);

        // Right ray is the only active pointer in PC mode
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