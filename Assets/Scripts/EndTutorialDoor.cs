using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class EndTutorialDoor : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float triggerDistance = 3f; // Distance from camera to trigger scene load

    [Header("Scene Settings")]
    public string sceneToLoad;

    private Outline outline;
    private Camera mainCamera;

    void Start()
    {
        outline = GetComponent<Outline>();
        FindActiveCamera();

        if (outline != null)
            outline.enabled = false;
    }

    void FindActiveCamera()
    {
        // Find any active camera in the scene
        Camera[] cameras = FindObjectsOfType<Camera>();
        foreach (Camera cam in cameras)
        {
            if (cam.gameObject.activeInHierarchy && cam.enabled)
            {
                mainCamera = cam;
                Debug.Log($"EndTutorialDoor: Found active camera - {mainCamera.name}");
                return;
            }
        }
        
        Debug.LogWarning("EndTutorialDoor: No active camera found in scene!");
    }

    void Update()
    {
        // Check if tutorial is finished
        if (TutorialManager.currentEvent == TutorialManager.TutorialEventIDs.Finished)
        {
            // Enable outline when tutorial is finished
            if (outline != null)
                outline.enabled = true;
            
            // Check distance to main camera
            if (IsPlayerClose())
            {
                Debug.Log("Player close to door - loading scene");
                LoadScene();
            }
        }
        else if (outline != null)
        {
            outline.enabled = false;
        }
    }

    bool IsPlayerClose()
    {
        // Auto-refresh camera reference if it's null
        if (mainCamera == null)
        {
            FindActiveCamera();
        }
        
        if (mainCamera == null) return false;
        
        float distance = Vector3.Distance(mainCamera.transform.position, transform.position);
        return distance <= triggerDistance;
    }

    void LoadScene()
    {
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogWarning("Scene name not set on " + gameObject.name);
        }
    }
}
