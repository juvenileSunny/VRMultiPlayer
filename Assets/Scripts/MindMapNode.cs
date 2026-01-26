using System.Collections;
using System.Collections.Generic;
using PanettoneGames.GenEvents;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MindMapNode : MonoBehaviour
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
        interactable?.activated.AddListener((interactor) => ToggleNodeSelection());
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
    
    // Handle real-time text input changes
    private void OnTextInputChanged(string newText)
    {
        // Update the data structure in real-time as user types
        if (mapManager != null)
        {
            mapManager.UpdateNodeText(gameObject, newText);
        }
        
        // Also update the display text if it's different
        if (nodeText != null && nodeText.text != newText)
        {
            nodeText.text = newText;
        }
    }
    
    // Handle position changes
    private void OnPositionChanged()
    {
        if (mapManager != null)
        {
            mapManager.UpdateNodePosition(gameObject, transform.position);
            Debug.Log($"Position updated for {gameObject.name}: {transform.position}");
        }
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
            
            // The manager will handle adding this node when connections are made
            // But we can also update existing data if node already exists
            mapManager.UpdateNodeText(gameObject, currentText);
            mapManager.UpdateNodeColor(gameObject, currentColor);
        }
    }

    // Sends an event to the mind map manager that a connection has been created when it touches another MindNode.
    void OnTriggerEnter(Collider other)
    {
        if ((targetLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            mindMapEvent.Raise(this.gameObject, other.gameObject);
            Debug.Log("MindMapNode triggered/collision detected for connection");
        }
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
        Debug.Log($"Delete button clicked for node {gameObject.name}");

        // Hide interaction panel
        if (interactionPanel != null)
        {
            interactionPanel.SetActive(false);
        }

        // Remove all connections to this node via MindMapManager
        if (mapManager != null)
        {
            mapManager.RemoveAllConnectionsToNode(gameObject);
        }

        // Destroy the node
        Destroy(gameObject);
    }

    // Text button functionality - toggles text input field
    private void OnAddTextButtonClicked()
    {
        Debug.Log($"Text button clicked for node {gameObject.name}");

        if (textInputField != null)
        {
            textInputActive = !textInputActive;
            textInputField.SetActive(textInputActive);
            
            // When showing input field, populate it with current text
            if (textInputActive && inputFieldComponent != null)
            {
                inputFieldComponent.text = GetNodeText();
                inputFieldComponent.ActivateInputField(); // Focus the input field
            }

            Debug.Log($"Text input field {(textInputActive ? "shown" : "hidden")}");
        }
        else
        {
            Debug.LogWarning("No textInputField assigned to this node!");
        }
    }

    // Change color button functionality
    private void OnChangeColorButtonClicked()
    {
        Debug.Log($"Change Color button clicked for node {gameObject.name}");

        if (nodeRenderer != null && availableColors.Length > 0)
        {
            currentColorIndex = (currentColorIndex + 1) % availableColors.Length;
            Color newColor = availableColors[currentColorIndex];
            
            // Update the visual representation
            nodeRenderer.material.color = newColor;

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
