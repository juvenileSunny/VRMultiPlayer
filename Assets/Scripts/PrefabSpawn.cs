// Attach this script to any clickable GameObject
using UnityEngine;
using PanettoneGames.GenEvents;
using Unity.Netcode;

public class PrefabSpawn : MonoBehaviour
{
    public IntEvent tutorialEvents;
    public TutorialManager tutorialManager;
    public GameObject prefabToSpawn; // Assign in Inspector
    public Transform spawnLocation;  // Optional: set where the prefab should spawn

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
            Vector3 spawnPos = spawnLocation != null ? spawnLocation.position : transform.position + Vector3.up;
            GameObject spawnedObject = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
            
            // Network spawn if in networked scene
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkObject networkObject = spawnedObject.GetComponent<NetworkObject>();
                if (networkObject != null)
                {
                    networkObject.Spawn();
                    Debug.Log($"Spawned node on network: {spawnedObject.name}");
                }
                else
                {
                    Debug.LogWarning($"Spawned object {spawnedObject.name} has no NetworkObject component!");
                }
            }
            else
            {
                Debug.Log($"Spawned node locally (no network): {spawnedObject.name}");
            }
        }
    }
}
