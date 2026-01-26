// Attach this script to any clickable GameObject
using UnityEngine;
using PanettoneGames.GenEvents;

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
            Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        }
    }
}
