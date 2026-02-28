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

    // Cached IDs for retry in Update() regardless of whether NetworkVariables have synced yet
    private ulong m_PendingNodeAId = 0;
    private ulong m_PendingNodeBId = 0;

    void Awake()
    {
        mapManager = FindObjectOfType<MindMapManager>();
    }

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;

        // Fallback in case Awake ran before MindMapManager was initialized
        if (mapManager == null)
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

    // Called after network spawn - reliable place to subscribe to NetworkVariable changes
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Subscribe to NetworkVariable changes so we resolve as soon as values arrive
        m_NodeA_NetworkId.OnValueChanged += OnNodeIdsChanged;
        m_NodeB_NetworkId.OnValueChanged += OnNodeIdsChanged;
        
        // Try resolving immediately in case values are already set (host side)
        ResolveNodeReferences();
    }
    
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        m_NodeA_NetworkId.OnValueChanged -= OnNodeIdsChanged;
        m_NodeB_NetworkId.OnValueChanged -= OnNodeIdsChanged;

        // Remove connection from local data on all clients when the object is despawned.
        // Server-side data is already cleaned up by RemoveAllConnectionsToNode/RemoveConnection.
        if (!IsServer && pointA != null && pointB != null)
            mapManager?.RemoveConnectionData(pointA.gameObject, pointB.gameObject);
    }
    
    // Called whenever either NetworkVariable changes - resolves the node transforms
    private void OnNodeIdsChanged(ulong oldValue, ulong newValue)
    {
        ResolveNodeReferences();
    }

    // Maintains the rendered lines position so that the connection will move with the nodes as you move it around.
    void Update()
    {
        // Retry resolving transforms every frame until both are found
        // Uses cached pending IDs (from ClientRpc) OR NetworkVariable values (for late joiners)
        if (IsSpawned && (pointA == null || pointB == null))
        {
            ulong idA = m_PendingNodeAId != 0 ? m_PendingNodeAId : m_NodeA_NetworkId.Value;
            ulong idB = m_PendingNodeBId != 0 ? m_PendingNodeBId : m_NodeB_NetworkId.Value;
            if (idA != 0 && idB != 0)
                ApplyNodeReferences(idA, idB);
        }
        
        if (pointA != null && pointB != null && lineRenderer != null)
        {
            lineRenderer.SetPosition(0, pointA.position);
            lineRenderer.SetPosition(1, pointB.position);
            UpdateCollider();
        }
    }
    
    // Set the node references and sync to network
    public void SetNodes(Transform nodeA, Transform nodeB)
    {
        // Always anchor to the MindMapNode child transform (includeInactive:true to survive spawn init)
        NetworkObject netObjA = nodeA.GetComponentInParent<NetworkObject>();
        NetworkObject netObjB = nodeB.GetComponentInParent<NetworkObject>();

        if (netObjA != null)
        {
            MindMapNode mindNodeA = netObjA.GetComponentInChildren<MindMapNode>(true);
            pointA = mindNodeA != null ? mindNodeA.transform : netObjA.transform;
        }
        else pointA = nodeA;

        if (netObjB != null)
        {
            MindMapNode mindNodeB = netObjB.GetComponentInChildren<MindMapNode>(true);
            pointB = mindNodeB != null ? mindNodeB.transform : netObjB.transform;
        }
        else pointB = nodeB;

        // If networked server, sync the NetworkObjectIds to all clients
        if (IsSpawned && IsServer)
        {
            if (netObjA != null && netObjB != null)
            {
                m_NodeA_NetworkId.Value = netObjA.NetworkObjectId;
                m_NodeB_NetworkId.Value = netObjB.NetworkObjectId;
                SetupConnectionClientRpc(netObjA.NetworkObjectId, netObjB.NetworkObjectId);
            }
            else
            {
                Debug.LogWarning("SetNodes: Could not find NetworkObject on one or both nodes!");
            }
        }
    }
    
    [ClientRpc]
    private void SetupConnectionClientRpc(ulong nodeAId, ulong nodeBId)
    {
        // Skip on server - already set in SetNodes
        if (IsServer) return;
        ApplyNodeReferences(nodeAId, nodeBId);
    }
    
    // Resolve node references from NetworkObjectIds (used by late joiners via NetworkVariable)
    private void ResolveNodeReferences()
    {
        if (m_NodeA_NetworkId.Value != 0 && m_NodeB_NetworkId.Value != 0)
        {
            ApplyNodeReferences(m_NodeA_NetworkId.Value, m_NodeB_NetworkId.Value);
        }
    }
    
    // Apply node transforms from NetworkObjectIds — always resolves to MindMapNode child transform.
    // Does NOT fall back to Node_W root: if MindMapNode not found yet, leaves point null
    // so the Update() retry loop keeps trying each frame until it resolves.
    private void ApplyNodeReferences(ulong nodeAId, ulong nodeBId)
    {
        // Cache IDs so Update() retry always has them regardless of NetworkVariable sync state
        m_PendingNodeAId = nodeAId;
        m_PendingNodeBId = nodeBId;

        if (NetworkManager.Singleton == null) return; // not in a networked session

        // Use the static registry on MindMapNode — reliable even when XR grab temporarily
        // reparents the node under the XR controller's attach transform, which would make
        // GetComponentInChildren return null while grabbed.
        if (MindMapNode.Registry.TryGetValue(nodeAId, out MindMapNode mindNodeA))
            pointA = mindNodeA.transform;
        // else leave null — Update() retries next frame

        if (MindMapNode.Registry.TryGetValue(nodeBId, out MindMapNode mindNodeB))
            pointB = mindNodeB.transform;
        // else leave null — Update() retries next frame

        // Once both points resolved, sync to local MindMapData so client data reflects connections.
        // No-op on server (OnEventRaised already handles it) and safe to call multiple times.
        if (!IsServer && pointA != null && pointB != null)
        {
            if (mapManager == null) mapManager = FindObjectOfType<MindMapManager>();
            mapManager?.RegisterConnectionData(pointA.gameObject, pointB.gameObject);
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
        if (removeButton != null)
            removeButton.gameObject.SetActive(false);

        bool isNetworked = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (isNetworked && IsSpawned)
        {
            DeleteConnectionServerRpc();
        }
        else
        {
            if (mapManager != null && pointA != null && pointB != null)
                mapManager.RemoveConnection(pointA.gameObject, pointB.gameObject);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DeleteConnectionServerRpc()
    {
        MindMapManager manager = mapManager != null ? mapManager : FindObjectOfType<MindMapManager>();
        if (manager != null && pointA != null && pointB != null)
            manager.RemoveConnection(pointA.gameObject, pointB.gameObject);
        else if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn(true); // fallback if manager lookup fails
    }
}
