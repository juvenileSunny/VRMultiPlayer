using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

[RequireComponent(typeof(LineRenderer))]
public class MindMapConnection : NetworkBehaviour
{
    public Transform pointA;
    public Transform pointB;
    public Button removeButton; // UI button to show/hide when connection is selected

    private LineRenderer lineRenderer;
    private BoxCollider boxCollider;
    private MindMapManager mapManager;
    
    // Network variables to sync which nodes this connection connects
    private NetworkVariable<ulong> m_NodeA_NetworkId = new NetworkVariable<ulong>();
    private NetworkVariable<ulong> m_NodeB_NetworkId = new NetworkVariable<ulong>();

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;

        // Find the MindMapManager in the scene
        mapManager = FindObjectOfType<MindMapManager>();
        
        // If networked, resolve node references from NetworkObjectIds
        if (IsSpawned)
        {
            ResolveNodeReferences();
        }

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
        // Try to resolve references if they're missing and we're networked
        if (IsSpawned && (pointA == null || pointB == null))
        {
            ResolveNodeReferences();
        }
        
        if (pointA != null && pointB != null)
        {
            lineRenderer.SetPosition(0, pointA.position);
            lineRenderer.SetPosition(1, pointB.position);
            UpdateCollider();
        }
    }
    
    // Set the node references and sync to network
    public void SetNodes(Transform nodeA, Transform nodeB)
    {
        pointA = nodeA;
        pointB = nodeB;
        
        // If networked, sync the NetworkObjectIds
        if (IsSpawned && IsServer)
        {
            NetworkObject netObjA = nodeA.GetComponent<NetworkObject>();
            NetworkObject netObjB = nodeB.GetComponent<NetworkObject>();
            
            if (netObjA != null && netObjB != null)
            {
                m_NodeA_NetworkId.Value = netObjA.NetworkObjectId;
                m_NodeB_NetworkId.Value = netObjB.NetworkObjectId;
            }
        }
    }
    
    // Resolve node references from NetworkObjectIds
    private void ResolveNodeReferences()
    {
        if (m_NodeA_NetworkId.Value != 0 && m_NodeB_NetworkId.Value != 0)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(m_NodeA_NetworkId.Value, out NetworkObject nodeA))
            {
                pointA = nodeA.transform;
            }
            
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(m_NodeB_NetworkId.Value, out NetworkObject nodeB))
            {
                pointB = nodeB.transform;
            }
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
