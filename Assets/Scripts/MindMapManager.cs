using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PanettoneGames.GenEvents;
using SplineMesh;
using UnityEngine;

// Data classes for mind map structure
[System.Serializable]
public class MindMapNodeData
{
    public string id;
    public string text;
    public Color color;
    public Vector3 position;
    public List<string> connections; // Changed from HashSet to List for Unity serialization

    public MindMapNodeData(string nodeId, string nodeText = "", Color? nodeColor = null, Vector3? pos = null)
    {
        id = nodeId;
        text = nodeText;
        color = nodeColor ?? Color.white;
        position = pos ?? Vector3.zero;
        connections = new List<string>();
    }

    // Helper method to check if connection exists (since we're using List instead of HashSet)
    public bool HasConnection(string nodeId)
    {
        return connections.Contains(nodeId);
    }

    // Helper method to add connection (avoiding duplicates)
    public bool AddConnection(string nodeId)
    {
        if (!connections.Contains(nodeId))
        {
            connections.Add(nodeId);
            return true;
        }
        return false;
    }

    // Helper method to remove connection
    public bool RemoveConnection(string nodeId)
    {
        return connections.Remove(nodeId);
    }
}

[System.Serializable]
public class MindMapData
{
    // Use SerializeField for Unity inspector support and serialization
    [SerializeField] private List<MindMapNodeData> nodesList = new List<MindMapNodeData>();

    // Runtime dictionaries for fast lookup (rebuilt from lists)
    private Dictionary<string, MindMapNodeData> nodes;
    private Dictionary<string, GameObject> nodeGameObjects;

    public MindMapData()
    {
        if (nodesList == null) nodesList = new List<MindMapNodeData>();
        RebuildDictionaries();
    }

    // Rebuild dictionaries from serialized lists (call this after loading)
    public void RebuildDictionaries()
    {
        nodes = new Dictionary<string, MindMapNodeData>();
        nodeGameObjects = new Dictionary<string, GameObject>();

        if (nodesList != null)
        {
            foreach (var nodeData in nodesList)
            {
                if (!string.IsNullOrEmpty(nodeData.id))
                {
                    nodes[nodeData.id] = nodeData;
                }
            }
        }
    }

    public string AddNode(GameObject gameObject, string text = "", Color? color = null)
    {
        // Clean the text to remove invisible characters (call static method from MindMapManager)
        string cleanedText = MindMapManager.CleanText(text);
        
        string id = System.Guid.NewGuid().ToString();
        var nodeData = new MindMapNodeData(id, cleanedText, color, gameObject.transform.position);

        nodes[id] = nodeData;
        nodeGameObjects[id] = gameObject;

        // Also add to serializable list
        nodesList.Add(nodeData);

        Debug.Log($"AddNode: Created new node {gameObject.name} (ID: {id}) with text '{cleanedText}' (original: '{text}')");

        return id;
    }

    public void RemoveNode(string nodeId)
    {
        if (!nodes.ContainsKey(nodeId)) return;

        var nodeData = nodes[nodeId];
        foreach (string connectedId in nodeData.connections.ToList())
        {
            RemoveConnection(nodeId, connectedId);
        }

        nodes.Remove(nodeId);
        nodeGameObjects.Remove(nodeId);

        // Also remove from serializable list
        nodesList.RemoveAll(n => n.id == nodeId);
    }

    public bool AddConnection(string nodeId1, string nodeId2)
    {
        if (!nodes.ContainsKey(nodeId1) || !nodes.ContainsKey(nodeId2) || nodeId1 == nodeId2)
            return false;

        if (nodes[nodeId1].connections.Contains(nodeId2))
            return false; // Already connected

        nodes[nodeId1].AddConnection(nodeId2);
        nodes[nodeId2].AddConnection(nodeId1);
        return true;
    }

    public bool RemoveConnection(string nodeId1, string nodeId2)
    {
        if (!nodes.ContainsKey(nodeId1) || !nodes.ContainsKey(nodeId2))
            return false;

        nodes[nodeId1].RemoveConnection(nodeId2);
        nodes[nodeId2].RemoveConnection(nodeId1);
        return true;
    }

    public MindMapNodeData GetNode(string nodeId) => nodes.GetValueOrDefault(nodeId);
    public GameObject GetGameObject(string nodeId) => nodeGameObjects.GetValueOrDefault(nodeId);
    public string GetNodeId(GameObject gameObject) => nodeGameObjects.FirstOrDefault(x => x.Value == gameObject).Key;

    public List<string> GetConnectedNodes(string nodeId)
    {
        return nodes.ContainsKey(nodeId) ? nodes[nodeId].connections : new List<string>();
    }

    public bool AreConnected(string nodeId1, string nodeId2)
    {
        return nodes.ContainsKey(nodeId1) && nodes[nodeId1].HasConnection(nodeId2);
    }

    public void UpdateNodeText(string nodeId, string newText)
    {
        if (nodes.ContainsKey(nodeId))
            nodes[nodeId].text = newText;
    }

    public void UpdateNodeColor(string nodeId, Color newColor)
    {
        if (nodes.ContainsKey(nodeId))
            nodes[nodeId].color = newColor;
    }

    public void UpdateNodePosition(string nodeId, Vector3 newPosition)
    {
        if (nodes.ContainsKey(nodeId))
            nodes[nodeId].position = newPosition;
    }

    public Dictionary<string, MindMapNodeData> GetAllNodes() => new Dictionary<string, MindMapNodeData>(nodes);
}

public class MindMapManager : MonoBehaviour, IDualGameEventListener<GameObject, GameObject>
{

    public IntEvent tutorialEvents;
    public TutorialManager tutorialManager;
    public DualGameObjectEvent mindMapEvent;
    public GameObject connectionPrefab;

    // New data structure - SerializeField for inspector support
    [SerializeField] private MindMapData mindMapData;

    // Visual connections using string-based keys
    private Dictionary<(string, string), GameObject> visualConnections;

    // Helper method to clean text by removing invisible Unicode characters
    public static string CleanText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return "";
            
        // Remove common invisible Unicode characters
        string cleaned = text
            .Replace("\u200B", "") // Zero Width Space
            .Replace("\u200C", "") // Zero Width Non-Joiner
            .Replace("\u200D", "") // Zero Width Joiner
            .Replace("\uFEFF", "") // Byte Order Mark
            .Replace("\u00A0", " ") // Non-breaking space -> regular space
            .Trim()
            .ToLowerInvariant();
            
        return cleaned;
    }

    // Utility method to detect if text contains invisible characters
    public static bool HasInvisibleCharacters(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        
        return text.Contains("\u200B") || // Zero Width Space
               text.Contains("\u200C") || // Zero Width Non-Joiner
               text.Contains("\u200D") || // Zero Width Joiner
               text.Contains("\uFEFF") || // Byte Order Mark
               text.Contains("\u00A0");   // Non-breaking space
    }

    // Utility method to show invisible characters for debugging
    public static string ShowInvisibleCharacters(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        
        return text
            .Replace("\u200B", "[ZWSP]")     // Zero Width Space
            .Replace("\u200C", "[ZWNJ]")     // Zero Width Non-Joiner
            .Replace("\u200D", "[ZWJ]")      // Zero Width Joiner
            .Replace("\uFEFF", "[BOM]")      // Byte Order Mark
            .Replace("\u00A0", "[NBSP]");    // Non-breaking space
    }

    void Awake()
    {
        // Initialize new system
        if (mindMapData == null)
            mindMapData = new MindMapData();

        mindMapData.RebuildDictionaries(); // Ensure dictionaries are built from serialized data
        visualConnections = new Dictionary<(string, string), GameObject>();
    }

    // late update, 
    void LateUpdate()
    {
        // if in tutorial scene, and current tutorial event is for MindNodeCompleted complete, raise tutorial event 
        if (tutorialEvents != null && TutorialManager.currentEvent == TutorialManager.TutorialEventIDs.MindNodeCompleted)
        {
            Debug.Log("Checking Mind Map Completion for Tutorial...");
            // has 3 nodes, with texts Air, Water, Ice, connected to each other in a triangle?
            if (mindMapData.GetAllNodes().Count >= 3)
            {
                // Get all nodes and normalize their text for comparison (remove invisible chars)
                var allNodes = mindMapData.GetAllNodes().Values;
                var nodeTexts = allNodes.Select(n => CleanText(n.text)).ToList();
                                
                // Check for required texts using normalized comparison
                bool hasAir = nodeTexts.Contains("air");
                bool hasWater = nodeTexts.Contains("water");
                bool hasIce = nodeTexts.Contains("ice");

                if (hasAir && hasWater && hasIce)
                {
                    // Check connections - use direct access to avoid creating new dictionary
                    bool airWater = false, waterIce = false, iceAir = false;
                    
                    // Find node IDs by text using consistent normalization
                    string airNodeId = null, waterNodeId = null, iceNodeId = null;
                    foreach (var node in mindMapData.GetAllNodes().Values)
                    {
                        string normalizedText = CleanText(node.text);
                        if (normalizedText == "air")
                            airNodeId = node.id;
                        else if (normalizedText == "water")
                            waterNodeId = node.id;
                        else if (normalizedText == "ice")
                            iceNodeId = node.id;
                    }
                    
                    // Check specific connections using AreConnected method
                    if (airNodeId != null && waterNodeId != null)
                    {
                        airWater = mindMapData.AreConnected(airNodeId, waterNodeId);
                        if (airWater) Debug.Log("Found Air-Water connection");
                    }
                    if (waterNodeId != null && iceNodeId != null)
                    {
                        waterIce = mindMapData.AreConnected(waterNodeId, iceNodeId);
                        if (waterIce) Debug.Log("Found Water-Ice connection");
                    }
                    if (iceNodeId != null && airNodeId != null)
                    {
                        iceAir = mindMapData.AreConnected(iceNodeId, airNodeId);
                        if (iceAir) Debug.Log("Found Ice-Air connection");
                    }
                    
                    if (airWater && waterIce && iceAir)
                    {
                        // Additional condition: At least one node should have a non-default color
                        var airNode = mindMapData.GetNode(airNodeId);
                        var waterNode = mindMapData.GetNode(waterNodeId);
                        var iceNode = mindMapData.GetNode(iceNodeId);
                        
                        bool airColorChanged = airNode != null && airNode.color != Color.white;
                        bool waterColorChanged = waterNode != null && waterNode.color != Color.white;
                        bool iceColorChanged = iceNode != null && iceNode.color != Color.white;
                        
                        bool atLeastOneColorChanged = airColorChanged || waterColorChanged || iceColorChanged;
                        
                        if (atLeastOneColorChanged)
                        {
                            Debug.Log("Mind Map Completed!");
                            tutorialEvents.Raise((int)TutorialManager.TutorialEventIDs.MindNodeCompleted);
                        }
                        else
                        {
                            Debug.Log("Mind Map has correct nodes and connections, but needs at least one node color to be changed from default white.");
                        }
                    }
                }
            }
        }
    }

    void OnEnable()
    {
        mindMapEvent.RegisterListener(this);
    }
    void OnDisable()
    {
        mindMapEvent.UnregisterListener(this);
    }

    // This gets called when 2 mind nodes are touched together, it creates a line connection between the 2 nodes, it goes both ways
    public void OnEventRaised(GameObject item1, GameObject item2)
    {
        Debug.Log($"Connection event between {item1.name} and {item2.name}");

        // Handle with data structure
        string nodeId1 = mindMapData.GetNodeId(item1);
        string nodeId2 = mindMapData.GetNodeId(item2);

        // Add nodes if they don't exist
        if (string.IsNullOrEmpty(nodeId1))
            nodeId1 = mindMapData.AddNode(item1);
        if (string.IsNullOrEmpty(nodeId2))
            nodeId2 = mindMapData.AddNode(item2);

        // Check if already connected
        if (mindMapData.AreConnected(nodeId1, nodeId2))
        {
            Debug.Log("Connection already exists, skipping creation");
            return;
        }

        // Add logical connection
        if (mindMapData.AddConnection(nodeId1, nodeId2))
        {
            CreateVisualConnection(nodeId1, nodeId2, item1.transform, item2.transform);
            Debug.Log($"Connection created between {nodeId1} and {nodeId2}");
        }
    }

    private void CreateVisualConnection(string nodeId1, string nodeId2, Transform transform1, Transform transform2)
    {
        var connectionKey = GetConnectionKey(nodeId1, nodeId2);

        GameObject newLine = Instantiate<GameObject>(connectionPrefab);
        MindMapConnection line = newLine.GetComponent<MindMapConnection>();
        line.pointA = transform1;
        line.pointB = transform2;

        visualConnections[connectionKey] = newLine;
    }

    // NEW API METHODS for external scripts to use
    public void UpdateNodeText(GameObject nodeGameObject, string newText)
    {
        // Clean the text to remove invisible characters
        string cleanedText = CleanText(newText);
        
        string nodeId = mindMapData.GetNodeId(nodeGameObject);
        if (!string.IsNullOrEmpty(nodeId))
        {
            Debug.Log($"UpdateNodeText: Updating node {nodeGameObject.name} (ID: {nodeId}) text from '{mindMapData.GetNode(nodeId)?.text}' to '{cleanedText}' (original: '{newText}')");
            mindMapData.UpdateNodeText(nodeId, cleanedText);
        }
        else
        {
            Debug.LogWarning($"UpdateNodeText: Could not find node ID for GameObject {nodeGameObject.name}");
        }
    }

    public void UpdateNodeColor(GameObject nodeGameObject, Color newColor)
    {
        string nodeId = mindMapData.GetNodeId(nodeGameObject);
        if (!string.IsNullOrEmpty(nodeId))
            mindMapData.UpdateNodeColor(nodeId, newColor);
    }

    public void UpdateNodePosition(GameObject nodeGameObject, Vector3 newPosition)
    {
        string nodeId = mindMapData.GetNodeId(nodeGameObject);
        if (!string.IsNullOrEmpty(nodeId))
            mindMapData.UpdateNodePosition(nodeId, newPosition);
    }

    public string GetNodeText(GameObject nodeGameObject)
    {
        string nodeId = mindMapData.GetNodeId(nodeGameObject);
        var nodeData = mindMapData.GetNode(nodeId);
        return nodeData?.text ?? "";
    }

    public Color GetNodeColor(GameObject nodeGameObject)
    {
        string nodeId = mindMapData.GetNodeId(nodeGameObject);
        var nodeData = mindMapData.GetNode(nodeId);
        return nodeData?.color ?? Color.white;
    }

    public Vector3 GetNodePosition(GameObject nodeGameObject)
    {
        string nodeId = mindMapData.GetNodeId(nodeGameObject);
        var nodeData = mindMapData.GetNode(nodeId);
        return nodeData?.position ?? Vector3.zero;
    }

    public List<GameObject> GetConnectedGameObjects(GameObject nodeGameObject)
    {
        string nodeId = mindMapData.GetNodeId(nodeGameObject);
        if (string.IsNullOrEmpty(nodeId)) return new List<GameObject>();

        var connectedIds = mindMapData.GetConnectedNodes(nodeId);
        var connectedGameObjects = new List<GameObject>();

        foreach (string connectedId in connectedIds)
        {
            var gameObj = mindMapData.GetGameObject(connectedId);
            if (gameObj != null)
                connectedGameObjects.Add(gameObj);
        }

        return connectedGameObjects;
    }

    // Separate function to remove connections (can be called from UI or other methods)
    public void RemoveConnection(GameObject item1, GameObject item2)
    {
        // Remove from new system
        string nodeId1 = mindMapData.GetNodeId(item1);
        string nodeId2 = mindMapData.GetNodeId(item2);

        if (!string.IsNullOrEmpty(nodeId1) && !string.IsNullOrEmpty(nodeId2))
        {
            if (mindMapData.RemoveConnection(nodeId1, nodeId2))
            {
                var connectionKey = GetConnectionKey(nodeId1, nodeId2);
                if (visualConnections.ContainsKey(connectionKey))
                {
                    Destroy(visualConnections[connectionKey]);
                    visualConnections.Remove(connectionKey);
                }
            }
        }
    }

    // Remove all connections to a specific node (used when deleting nodes)
    public void RemoveAllConnectionsToNode(GameObject node)
    {
        Debug.Log($"Removing all connections to node {node.name}");

        string nodeId = mindMapData.GetNodeId(node);
        if (!string.IsNullOrEmpty(nodeId))
        {
            var connectedIds = mindMapData.GetConnectedNodes(nodeId);
            foreach (string connectedId in connectedIds)
            {
                var connectionKey = GetConnectionKey(nodeId, connectedId);
                if (visualConnections.ContainsKey(connectionKey))
                {
                    Destroy(visualConnections[connectionKey]);
                    visualConnections.Remove(connectionKey);
                }
            }
            mindMapData.RemoveNode(nodeId);
        }
    }

    // Helper method to create consistent connection keys
    private (string, string) GetConnectionKey(string a, string b)
    {
        return string.Compare(a, b) < 0 ? (a, b) : (b, a);
    }

    // DEBUG METHODS - for testing and verification
    [ContextMenu("Debug Text Issues")]
    public void DebugAllNodeTexts()
    {
        Debug.Log("=== DEBUGGING ALL NODE TEXTS ===");
        
        var allNodes = mindMapData.GetAllNodes();
        foreach (var kvp in allNodes)
        {
            var nodeData = kvp.Value;
            string rawText = nodeData.text ?? "";
            string cleanedText = CleanText(rawText);
            bool hasInvisible = HasInvisibleCharacters(rawText);
            
            Debug.Log($"Node ID: {nodeData.id}");
            Debug.Log($"  Raw text: '{ShowInvisibleCharacters(rawText)}'");
            Debug.Log($"  Cleaned text: '{cleanedText}'");
            Debug.Log($"  Has invisible chars: {hasInvisible}");
            Debug.Log($"  Length raw: {rawText.Length}, Length cleaned: {cleanedText.Length}");
            Debug.Log("  ---");
        }
        
        Debug.Log("=== END DEBUG ===");
    }
    public void PrintMindMapDataStructure()
    {
        Debug.Log("=== MIND MAP DATA STRUCTURE ===");

        var allNodes = mindMapData.GetAllNodes();
        Debug.Log($"Total Nodes: {allNodes.Count}");

        foreach (var kvp in allNodes)
        {
            var nodeData = kvp.Value;
            var gameObj = mindMapData.GetGameObject(nodeData.id);
            string objName = gameObj != null ? gameObj.name : "NULL";

            Debug.Log($"Node ID: {nodeData.id}");
            Debug.Log($"  - GameObject: {objName}");
            Debug.Log($"  - Text: '{nodeData.text}'");
            Debug.Log($"  - Color: {nodeData.color}");
            Debug.Log($"  - Position: {nodeData.position}");
            Debug.Log($"  - Connections ({nodeData.connections.Count}): [{string.Join(", ", nodeData.connections)}]");
            Debug.Log($"  ---");
        }

        Debug.Log($"Visual Connections: {visualConnections.Count}");
        foreach (var connection in visualConnections.Keys)
        {
            Debug.Log($"  - Visual Connection: {connection.Item1} <-> {connection.Item2}");
        }

        Debug.Log("=== END MIND MAP DATA ===");
    }

    [ContextMenu("Print Node Count")]
    public void PrintNodeCount()
    {
        var allNodes = mindMapData.GetAllNodes();
        Debug.Log($"Mind Map contains {allNodes.Count} nodes");
    }

    // Method to verify a specific node
    public void PrintNodeInfo(GameObject nodeGameObject)
    {
        string nodeId = mindMapData.GetNodeId(nodeGameObject);
        if (string.IsNullOrEmpty(nodeId))
        {
            Debug.Log($"Node {nodeGameObject.name} is NOT in the data structure");
            return;
        }

        var nodeData = mindMapData.GetNode(nodeId);
        if (nodeData != null)
        {
            Debug.Log($"=== NODE INFO: {nodeGameObject.name} ===");
            Debug.Log($"ID: {nodeData.id}");
            Debug.Log($"Text: '{nodeData.text}'");
            Debug.Log($"Color: {nodeData.color}");
            Debug.Log($"Stored Position: {nodeData.position}");
            Debug.Log($"Current Position: {nodeGameObject.transform.position}");
            Debug.Log($"Position Match: {Vector3.Distance(nodeData.position, nodeGameObject.transform.position) < 0.01f}");
            Debug.Log($"Connections: [{string.Join(", ", nodeData.connections)}]");

            // Print connected node names
            var connectedGameObjects = GetConnectedGameObjects(nodeGameObject);
            var connectedNames = connectedGameObjects.Select(go => go.name).ToArray();
            Debug.Log($"Connected to: [{string.Join(", ", connectedNames)}]");
        }
    }
}
