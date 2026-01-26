using System.Collections;
using System.Collections.Generic; 
using UnityEngine;

public class SpawnAfter5Minute : MonoBehaviour
{
    public GameObject objectToSpawn;  // Reference to object already placed in scene (disabled)
    [SerializeField] private float spawnDelay = 30f;

    void Start()
    {
        if (objectToSpawn != null)
        {
            objectToSpawn.SetActive(false);
            StartCoroutine(ActivateAfterDelay(spawnDelay));
        }
    }

    private IEnumerator ActivateAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        objectToSpawn.SetActive(true);  // Simply activate it
    }
}
