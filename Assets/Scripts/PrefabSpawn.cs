// Attach this script to any clickable GameObject
using UnityEngine;
using PanettoneGames.GenEvents;
using Unity.Netcode;

public class PrefabSpawn : NetworkBehaviour
{
    public IntEvent tutorialEvents;
    public TutorialManager tutorialManager;
    public GameObject prefabToSpawn; // Assign in Inspector
    public Transform spawnLocation;  // Optional: set where the prefab should spawn
    public string lectureSceneName; // Scene to load if skipping tutorial

    // This function is responsible for spawning the mind map nodes
    public void SpawnPrefab()
    {
        // trigger tutorial event for spawning mind node
        if(tutorialEvents != null && TutorialManager.currentEvent == TutorialManager.TutorialEventIDs.PressSpawnMindNodeButton)
        {
            tutorialEvents.Raise((int)TutorialManager.TutorialEventIDs.PressSpawnMindNodeButton);
        }

        if (prefabToSpawn != null)
        {
            Vector3 spawnPos = GetSpawnPosition();

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                // Route through server so any client can spawn nodes for everyone
                SpawnNodeServerRpc(spawnPos);
            }
            else
            {
                // Non-networked scene (tutorial) - spawn locally
                Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
                Debug.Log($"Spawned node locally (no network)");
            }
        }
    }

    public void SkipTutorialScene()
    {
        if (lectureSceneName != null && lectureSceneName != "")
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(lectureSceneName);
        }
        else
        {
            Debug.LogWarning("Lecture scene name not set in PrefabSpawn component!");
        }
    }

    // Returns spawn position: uses spawnLocation if assigned, otherwise this object's position + small offset
    private Vector3 GetSpawnPosition()
    {
        if (spawnLocation != null)
            return spawnLocation.position;

        return transform.position + Vector3.up * 0.1f;
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void SpawnNodeServerRpc(Vector3 spawnPos)
    {
        // Spawn with SERVER ownership. Position is authoritative from the server and
        // syncs correctly to all clients via ClientNetworkTransform.
        // Ownership is transferred to the client only when they physically grab the node
        // (handled by MindMapNode.OnGrabbed → RequestOwnershipServerRpc).
        GameObject spawnedObject = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        NetworkObject networkObject = spawnedObject.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            Debug.LogWarning($"Spawned object {spawnedObject.name} has no NetworkObject component!");
            return;
        }

        networkObject.Spawn();
        Debug.Log($"Server spawned node at {spawnPos} (owner: server until grabbed)");
    }
}
