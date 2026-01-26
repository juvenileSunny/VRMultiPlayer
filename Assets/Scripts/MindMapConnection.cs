using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(LineRenderer))]
public class MindMapConnection : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;
    public Button removeButton; // UI button to show/hide when connection is selected

    private LineRenderer lineRenderer;
    private BoxCollider boxCollider;
    private MindMapManager mapManager;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;

        // Find the MindMapManager in the scene
        mapManager = FindObjectOfType<MindMapManager>();

        // Add or get BoxCollider for raycast interaction
        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null)
            boxCollider = gameObject.AddComponent<BoxCollider>();
        UpdateCollider();

        // Hide the remove button initially
        if (removeButton != null)
        {
            removeButton.gameObject.SetActive(false);
            // Add listener to the button
            removeButton.onClick.AddListener(OnRemoveButtonClicked);
        }

        if (GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>() == null)
        {
            gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            // enable dynamic attach
            GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>().useDynamicAttach = true;
            // disable track pos, rot and scale
            GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>().trackPosition = false;
            GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>().trackRotation = false;
            GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>().trackScale = false;
            // add listener for select event
            GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>().selectEntered.AddListener((interactor) => OnSelectConnection());
        }
    }

    // Maintains the rendered lines position so that the connection will move with the nodes as you move it around.
    void Update()
    {
        if (pointA != null && pointB != null)
        {
            lineRenderer.SetPosition(0, pointA.position);
            lineRenderer.SetPosition(1, pointB.position);
            UpdateCollider();
        }
    }

    // Update the BoxCollider to match the line
    void UpdateCollider()
    {
        if (boxCollider == null || pointA == null || pointB == null) return;
        Vector3 midPoint = (pointA.position + pointB.position) / 2f;
        boxCollider.transform.position = midPoint;
        Vector3 dir = pointB.position - pointA.position;
        boxCollider.size = new Vector3(0.05f, 0.05f, dir.magnitude);
        boxCollider.transform.rotation = Quaternion.LookRotation(dir.normalized);
    }

    public void OnSelectConnection()
    {
        Debug.Log("Connection selected between " + pointA.name + " and " + pointB.name);

        // Toggle the visibility of the remove button
        if (removeButton != null)
        {
            removeButton.gameObject.SetActive(!removeButton.gameObject.activeSelf);
        }
    }

    // Called when the remove button is clicked
    private void OnRemoveButtonClicked()
    {
        Debug.Log("Remove button clicked for connection");

        // Hide the button
        if (removeButton != null)
        {
            removeButton.gameObject.SetActive(false);
        }

        // Call the remove function in MindMapManager
        if (mapManager != null && pointA != null && pointB != null)
        {
            mapManager.RemoveConnection(pointA.gameObject, pointB.gameObject);
        }
        else
        {
            Debug.LogError("Cannot remove connection: Missing MindMapManager or point references");
        }
    }
}
