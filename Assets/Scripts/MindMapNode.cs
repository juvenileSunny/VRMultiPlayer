using System.Collections;
using System.Collections.Generic;
using PanettoneGames.GenEvents;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using Unity.Collections;

public class MindMapNode : NetworkBehaviour
{
    [Header("Connection System")]
    public LayerMask targetLayer;
    public DualGameObjectEvent mindMapEvent;

    [Header("Interaction UI")]
    public GameObject interactionPanel; // Panel containing all interaction buttons
    public Button deleteButton;
    public Button addTextButton;
    public Button changeColorButton;

    [Header("Node Components")]
    public TextMeshProUGUI nodeText;
    public GameObject textInputField; // GameObject to toggle for text input
    public TMP_InputField inputFieldComponent; // The actual input field component
    public Renderer nodeRenderer;
    public GameObject highlightObject; // Optional highlight object (outline, glow, etc.)
    public Color[] availableColors = { Color.red, Color.blue, Color.green, Color.yellow, Color.cyan, Color.magenta };

    private MindMapManager mapManager;
    private int currentColorIndex = 0;
    private bool isSelected = false;
    private bool textInputActive = false;
    private Color originalColor;
    private Material originalMaterial;
    
    // Position tracking for automatic updates
    private Vector3 lastPosition;
    private bool trackPosition = true;

    // Prevents trigger collider from firing immediately at spawn (e.g. when two nodes appear at the same position)
    private bool connectionReady = false;

    // Per-collider cooldown so OnTriggerStay doesn't spam RPCs (one attempt per second per pair is enough)
    private Dictionary<Collider, float> connectionAttemptTime = new Dictionary<Collider, float>();
    private const float CONNECTION_RPC_COOLDOWN = 1f;

    // Reference to the grab interactable so we can poll isSelected in Update
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;

    // Static lookup: root NetworkObjectId → MindMapNode
    // Used by MindMapConnection to resolve node transforms without GetComponentInChildren,
    // which fails when XR grab reparents the node under the XR controller attachment point.
    public static readonly Dictionary<ulong, MindMapNode> Registry = new Dictionary<ulong, MindMapNode>();

    // NetworkVariables persist current state for late joiners — unlike ClientRpcs which are fire-and-forget.
    // Any client that joins after text/color was set will automatically receive the current value.
    private NetworkVariable<FixedString512Bytes> m_NodeText = new NetworkVariable<FixedString512Bytes>(
        new FixedString512Bytes(""),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    private NetworkVariable<Color> m_NodeColor = new NetworkVariable<Color>(
        Color.white,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    // Syncs whether the text label is visible on the node for all users
    private NetworkVariable<bool> m_TextVisible = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    void Start()
    {
        // Find the MindMapManager in the scene
        mapManager = FindObjectOfType<MindMapManager>();

        // Setup interaction panel - hide initially
        if (interactionPanel != null)
        {
            interactionPanel.SetActive(false);
        }

        // Setup text input field - hide initially
        if (textInputField != null)
        {
            textInputField.SetActive(false);
            
            // Get the input field component if not assigned
            if (inputFieldComponent == null)
            {
                inputFieldComponent = textInputField.GetComponent<TMP_InputField>();
            }
            
            // Setup input field listeners
            if (inputFieldComponent != null)
            {
                inputFieldComponent.onValueChanged.AddListener(OnTextInputChanged); // Real-time updates
            }
        }
        
        // Setup real-time text display listener
        if (nodeText != null)
        {
            // We'll monitor this in Update() since TMP doesn't have a direct change event
        }

        // Initialize position tracking
        lastPosition = transform.position;
        
        // Initialize node in data structure
        InitializeNodeInDataStructure();

        // Allow a short grace period before the trigger is active so spawn-position overlaps don't create false connections
        StartCoroutine(EnableConnectionAfterDelay(0.5f));

        // Store original color and material
        if (nodeRenderer != null)
        {
            originalMaterial = nodeRenderer.material;
            originalColor = originalMaterial.color;
        }

        // Setup highlight object - hide initially
        if (highlightObject != null)
        {
            highlightObject.SetActive(false);
        }

        // Setup button listeners
        deleteButton?.onClick.AddListener(OnDeleteButtonClicked);
        addTextButton?.onClick.AddListener(OnAddTextButtonClicked);
        changeColorButton?.onClick.AddListener(OnChangeColorButtonClicked);


        // setup activate node selection on grab from parent's grab interactable
        var interactable = GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        grabInteractable = interactable;
        interactable?.activated.AddListener((interactor) => ToggleNodeSelection());

        // When this client grabs the node, request ownership so ClientNetworkTransform lets us move it
        interactable?.selectEntered.AddListener((args) => OnGrabbed());

        // Apply synced NetworkVariable state now that all references are initialized.
        // OnNetworkSpawn fires before Start(), so any Apply calls there would hit null references.
        // Re-applying here is the reliable late-joiner path.
        if (IsSpawned)
        {
            string currentText = m_NodeText.Value.ToString();
            if (!string.IsNullOrEmpty(currentText))
                ApplyTextChange(currentText);
            ApplyColorChange(m_NodeColor.Value);
            ApplyTextVisibility(m_TextVisible.Value);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        NetworkObject root = GetComponentInParent<NetworkObject>();
        if (root != null)
            Registry[root.NetworkObjectId] = this;

        // Subscribe to NetworkVariable changes so all clients (including late joiners) stay in sync
        m_NodeText.OnValueChanged += OnNodeTextChanged;
        m_NodeColor.OnValueChanged += OnNodeColorChanged;
        m_TextVisible.OnValueChanged += OnTextVisibleChanged;

        // Apply current values immediately — this is what late joiners receive on join
        string currentText = m_NodeText.Value.ToString();
        if (!string.IsNullOrEmpty(currentText))
            ApplyTextChange(currentText);
        ApplyColorChange(m_NodeColor.Value);
        ApplyTextVisibility(m_TextVisible.Value);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        NetworkObject root = GetComponentInParent<NetworkObject>();
        if (root != null)
            Registry.Remove(root.NetworkObjectId);

        m_NodeText.OnValueChanged -= OnNodeTextChanged;
        m_NodeColor.OnValueChanged -= OnNodeColorChanged;
        m_TextVisible.OnValueChanged -= OnTextVisibleChanged;
    }

    void Update()
    {
        // Track position changes
        if (trackPosition && Vector3.Distance(transform.position, lastPosition) > 0.01f)
        {
            OnPositionChanged();
            lastPosition = transform.position;
        }

    }

    private IEnumerator EnableConnectionAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        connectionReady = true;
    }

    // Request ownership when this client grabs the node
    private void OnGrabbed()
    {
        if (IsSpawned && !IsOwner)
        {
            RequestOwnershipServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestOwnershipServerRpc(ulong requestingClientId)
    {
        NetworkObject rootNetObj = GetComponentInParent<NetworkObject>();
        if (rootNetObj != null)
        {
            rootNetObj.ChangeOwnership(requestingClientId);
            Debug.Log($"Ownership of {gameObject.name} transferred to client {requestingClientId}");
        }
    }

    // Handle real-time text input changes
    private void OnTextInputChanged(string newText)
    {
        // Update locally first
        if (nodeText != null && nodeText.text != newText)
        {
            nodeText.text = newText;
        }
        
        // Update the data structure
        if (mapManager != null)
        {
            mapManager.UpdateNodeText(gameObject, newText);
        }
        
        // Sync to network if networked
        if (IsSpawned)
        {
            UpdateTextServerRpc(newText);
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void UpdateTextServerRpc(string newText)
    {
        // Setting the NetworkVariable replicates to all connected clients AND persists for late joiners.
        // OnNodeTextChanged callback fires on all clients when they receive the new value.
        m_NodeText.Value = new FixedString512Bytes(newText);
    }

    private void OnNodeTextChanged(FixedString512Bytes oldValue, FixedString512Bytes newValue)
    {
        ApplyTextChange(newValue.ToString());
    }

    private void ApplyTextChange(string newText)
    {
        if (nodeText != null && nodeText.text != newText)
            nodeText.text = newText;
        if (inputFieldComponent != null && inputFieldComponent.text != newText)
            inputFieldComponent.text = newText;
        if (mapManager != null)
            mapManager.UpdateNodeText(gameObject, newText);
    }
    
    // Handle position changes
    private void OnPositionChanged()
    {
        if (mapManager != null)
            mapManager.UpdateNodePosition(gameObject, transform.position);
    }

    // Initialize this node in the data structure
    private void InitializeNodeInDataStructure()
    {
        if (mapManager != null)
        {
            // Register this node with the data structure
            // This will create a node entry with current text and color
            string currentText = GetNodeText();
            Color currentColor = GetNodeColor();
            
            // Update methods will automatically add the node if it doesn't exist
            mapManager.UpdateNodeText(gameObject, currentText);
            mapManager.UpdateNodeColor(gameObject, currentColor);
            mapManager.UpdateNodePosition(gameObject, transform.position);
            
            Debug.Log($"Initialized node {gameObject.name} in data structure");
        }
    }

    // Sends an event to the mind map manager that a connection has been created when it touches another MindNode.
    void OnTriggerEnter(Collider other) => TryCreateConnection(other);

    // Also check OnTriggerStay: XRGrabInteractable uses kinematic movement during grab,
    // which can prevent OnTriggerEnter from firing while the node is being held.
    // Connection creation is idempotent (AreConnected check prevents duplicates).
    void OnTriggerStay(Collider other) => TryCreateConnection(other);

    // Clean up cooldown entries when colliders separate to avoid stale dictionary growth
    void OnTriggerExit(Collider other) => connectionAttemptTime.Remove(other);

    private void TryCreateConnection(Collider other)
    {
        // Ignore triggers during the spawn grace period
        if (!connectionReady) return;

        if ((targetLayer.value & (1 << other.gameObject.layer)) == 0) return;

        // Rate-limit per collider so OnTriggerStay doesn't spam RPCs
        float now = Time.time;
        if (connectionAttemptTime.TryGetValue(other, out float lastAttempt) && now - lastAttempt < CONNECTION_RPC_COOLDOWN)
            return;
        connectionAttemptTime[other] = now;

        bool isNetworked = Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsListening;
        if (isNetworked)
        {
            // Must be spawned to send RPCs
            if (!IsSpawned) return;

            // Route through server so both host and clients can create connections
            NetworkObject myNetObj = GetComponentInParent<Unity.Netcode.NetworkObject>();
            NetworkObject otherNetObj = other.GetComponentInParent<Unity.Netcode.NetworkObject>();
            if (myNetObj != null && otherNetObj != null)
                RequestConnectionServerRpc(myNetObj.NetworkObjectId, otherNetObj.NetworkObjectId);
            else
                Debug.LogWarning($"[MindMapNode] TryCreateConnection: Could not find NetworkObject on one or both nodes (myNetObj={myNetObj}, otherNetObj={otherNetObj})");
        }
        else
        {
            // Non-networked scene (tutorial) - use existing event directly
            mindMapEvent.Raise(this.gameObject, other.gameObject);
        }
    }
    
    [Unity.Netcode.ServerRpc(RequireOwnership = false)]
    private void RequestConnectionServerRpc(ulong nodeAId, ulong nodeBId)
    {
        // Server looks up the GameObjects and raises the connection event
        var spawnedObjects = Unity.Netcode.NetworkManager.Singleton.SpawnManager.SpawnedObjects;

        if (!spawnedObjects.TryGetValue(nodeAId, out Unity.Netcode.NetworkObject nodeA))
        {
            Debug.LogWarning($"[MindMapNode] Could not find spawned object with ID {nodeAId}");
            return;
        }
        if (!spawnedObjects.TryGetValue(nodeBId, out Unity.Netcode.NetworkObject nodeB))
        {
            Debug.LogWarning($"[MindMapNode] Could not find spawned object with ID {nodeBId}");
            return;
        }

        MindMapNode mindNodeA = nodeA.GetComponentInChildren<MindMapNode>();
        MindMapNode mindNodeB = nodeB.GetComponentInChildren<MindMapNode>();

        if (mindNodeA == null) { Debug.LogWarning($"[MindMapNode] No MindMapNode found in children of {nodeA.name}"); return; }
        if (mindNodeB == null) { Debug.LogWarning($"[MindMapNode] No MindMapNode found in children of {nodeB.name}"); return; }

        // Re-find mapManager if null (can happen if Start() ran before MindMapManager was initialized)
        MindMapManager manager = mapManager != null ? mapManager : FindObjectOfType<MindMapManager>();
        if (manager == null) { Debug.LogWarning($"[MindMapNode] mapManager is null and could not be found in scene!"); return; }
        mapManager = manager; // cache it for future calls

        // Directly call the manager instead of the event to avoid double-firing
        manager.OnEventRaised(mindNodeA.gameObject, mindNodeB.gameObject);
    }


    // Toggle node selection state
    public void ToggleNodeSelection()
    {
        isSelected = !isSelected;
        Debug.Log($"Node {gameObject.name} {(isSelected ? "selected" : "deselected")}");

        if (isSelected)
        {
            // Show interaction panel
            if (interactionPanel != null)
            {
                interactionPanel.SetActive(true);
            }

            // Show highlight
            ShowHighlight();
        }
        else
        {
            // Hide interaction panel
            if (interactionPanel != null)
            {
                interactionPanel.SetActive(false);
            }

            // Hide highlight
            HideHighlight();
        }
    }

    // Show highlight effect
    private void ShowHighlight()
    {
        // Use dedicated highlight object
        if (highlightObject != null)
        {
            highlightObject.SetActive(true);
        }
        // Subtle brightness increase (fallback)
        else if (nodeRenderer != null && originalMaterial != null)
        {
            Color brightColor = originalColor * 1.3f; // Slightly brighter
            brightColor.a = originalColor.a; // Preserve alpha
            nodeRenderer.material.color = brightColor;
        }
    }

    // Hide highlight effect
    private void HideHighlight()
    {
        //  dedicated highlight object
        if (highlightObject != null)
        {
            highlightObject.SetActive(false);
        }
        // Restore original color
        else if (nodeRenderer != null)
        {
            RestoreOriginalColor();
        }
    }

    // Delete button functionality
    private void OnDeleteButtonClicked()
    {
        if (interactionPanel != null)
            interactionPanel.SetActive(false);

        bool isNetworked = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (isNetworked && IsSpawned)
        {
            NetworkObject rootNetObj = GetComponentInParent<NetworkObject>();
            if (rootNetObj != null)
                DeleteNodeServerRpc(rootNetObj.NetworkObjectId);
        }
        else
        {
            // Non-networked (tutorial) — clean up locally
            if (mapManager != null)
                mapManager.RemoveAllConnectionsToNode(gameObject);
            NetworkObject rootNetObj = GetComponentInParent<NetworkObject>();
            Destroy(rootNetObj != null ? rootNetObj.gameObject : gameObject);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DeleteNodeServerRpc(ulong nodeNetworkId)
    {
        var spawnedObjects = NetworkManager.Singleton.SpawnManager.SpawnedObjects;
        if (!spawnedObjects.TryGetValue(nodeNetworkId, out NetworkObject rootNetObj)) return;

        // Use the MindMapNode child gameObject — that's what MindMapManager tracks
        MindMapNode mindNode = rootNetObj.GetComponentInChildren<MindMapNode>(true);
        GameObject nodeGO = mindNode != null ? mindNode.gameObject : rootNetObj.gameObject;

        MindMapManager manager = mapManager != null ? mapManager : FindObjectOfType<MindMapManager>();
        if (manager != null)
            manager.RemoveAllConnectionsToNode(nodeGO);

        rootNetObj.Despawn(true);
    }

    // Text button functionality - toggles text input field
    private void OnAddTextButtonClicked()
    {
        if (textInputField != null)
        {
            textInputActive = !textInputActive;

            // The input field is local-only (only the editing user needs it)
            textInputField.SetActive(textInputActive);
            if (textInputActive && inputFieldComponent != null)
            {
                inputFieldComponent.text = GetNodeText();
                inputFieldComponent.ActivateInputField();
            }

            // Sync visibility to all clients via NetworkVariable
            if (IsSpawned)
                SetTextVisibleServerRpc(textInputActive);
        }
        else
        {
            Debug.LogWarning("No textInputField assigned to this node!");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetTextVisibleServerRpc(bool visible)
    {
        m_TextVisible.Value = visible;
    }

    private void OnTextVisibleChanged(bool oldValue, bool newValue)
    {
        ApplyTextVisibility(newValue);
    }

    private void ApplyTextVisibility(bool visible)
    {
        if (textInputField != null)
            textInputField.SetActive(visible);
    }

    // Change color button functionality
    private void OnChangeColorButtonClicked()
    {
        Debug.Log($"Change Color button clicked for node {gameObject.name}");

        if (nodeRenderer != null && availableColors.Length > 0)
        {
            currentColorIndex = (currentColorIndex + 1) % availableColors.Length;
            Color newColor = availableColors[currentColorIndex];
            
            // Update locally
            ApplyColorChange(newColor);
            
            // Sync to network if networked
            if (IsSpawned)
            {
                UpdateColorServerRpc(newColor);
            }
        }
    }
    
    private void ApplyColorChange(Color newColor)
    {
        // Update the visual representation
        if (nodeRenderer != null)
        {
            nodeRenderer.material.color = newColor;
        }

        // Update the data structure via MindMapManager
        if (mapManager != null)
        {
            mapManager.UpdateNodeColor(gameObject, newColor);
        }

        // Update the stored original color to the new color
        UpdateOriginalColor();

        // Re-apply highlight if currently selected
        if (isSelected)
        {
            ShowHighlight();
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void UpdateColorServerRpc(Color newColor)
    {
        // Setting the NetworkVariable replicates to all connected clients AND persists for late joiners.
        m_NodeColor.Value = newColor;
    }

    private void OnNodeColorChanged(Color oldValue, Color newValue)
    {
        ApplyColorChange(newValue);
    }

    // Helper method to restore original color
    private void RestoreOriginalColor()
    {
        if (nodeRenderer != null)
        {
            nodeRenderer.material.color = originalColor;
        }
    }

    // Update the stored original color when color is changed
    private void UpdateOriginalColor()
    {
        if (nodeRenderer != null)
        {
            originalColor = nodeRenderer.material.color;
        }
    }



    // Public method to get node text
    public string GetNodeText()
    {
        return nodeText != null ? nodeText.text : "";
    }
    
    // Public method to get node color
    public Color GetNodeColor()
    {
        return nodeRenderer != null ? nodeRenderer.material.color : Color.white;
    }
    
    // Public method to set node text (can be called from external scripts)
    public void SetNodeText(string newText)
    {
        if (nodeText != null)
        {
            nodeText.text = newText;
        }
        
        if (inputFieldComponent != null)
        {
            inputFieldComponent.text = newText;
        }
        
        // Update data structure
        if (mapManager != null)
        {
            mapManager.UpdateNodeText(gameObject, newText);
        }
    }
    
    // Public method to enable/disable position tracking
    public void SetPositionTracking(bool enabled)
    {
        trackPosition = enabled;
        if (enabled)
        {
            lastPosition = transform.position;
        }
    }
    
    // Force update position in data structure
    public void ForceUpdatePosition()
    {
        OnPositionChanged();
        lastPosition = transform.position;
    }
    
    // Public method to set node color (can be called from external scripts)
    public void SetNodeColor(Color newColor)
    {
        if (nodeRenderer != null)
        {
            nodeRenderer.material.color = newColor;
            UpdateOriginalColor();
        }
        
        // Update data structure
        if (mapManager != null)
        {
            mapManager.UpdateNodeColor(gameObject, newColor);
        }
        
        // Re-apply highlight if currently selected
        if (isSelected)
        {
            ShowHighlight();
        }
    }
    
    // Sync node properties from data structure (useful for loading saved data)
    public void SyncFromDataStructure()
    {
        if (mapManager != null)
        {
            string savedText = mapManager.GetNodeText(gameObject);
            Color savedColor = mapManager.GetNodeColor(gameObject);
            
            // Update visual components without triggering data structure updates
            if (nodeText != null && !string.IsNullOrEmpty(savedText))
            {
                nodeText.text = savedText;
            }
            
            if (inputFieldComponent != null && !string.IsNullOrEmpty(savedText))
            {
                inputFieldComponent.text = savedText;
            }
            
            if (nodeRenderer != null)
            {
                nodeRenderer.material.color = savedColor;
                UpdateOriginalColor();
            }
        }
    }
    
    // DEBUG METHOD - Print this node's data structure info
    [ContextMenu("Print Node Data")]
    public void PrintNodeData()
    {
        if (mapManager != null)
        {
            Debug.Log($"=== PRINTING DATA FOR: {gameObject.name} ===");
            mapManager.PrintNodeInfo(gameObject);
        }
        else
        {
            Debug.LogWarning($"No MindMapManager found for {gameObject.name}");
        }
    }
    
    // DEBUG METHOD - Print all connected nodes
    [ContextMenu("Print Connected Nodes")]
    public void PrintConnectedNodes()
    {
        if (mapManager != null)
        {
            var connected = mapManager.GetConnectedGameObjects(gameObject);
            Debug.Log($"{gameObject.name} is connected to {connected.Count} nodes:");
            foreach (var connectedNode in connected)
            {
                Debug.Log($"  - {connectedNode.name}");
            }
        }
        else
        {
            Debug.LogWarning($"No MindMapManager found for {gameObject.name}");
        }
    }
}
