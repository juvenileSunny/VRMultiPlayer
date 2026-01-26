using UnityEngine;

public class SessionAlert : MonoBehaviour
{
    public GameObject alertCanvas;  // Assign your full Canvas in the Inspector
    public float displayDuration = 3f;

    private bool isAlertActive = false;

    void Start()
    {
        if (alertCanvas != null)
            alertCanvas.SetActive(false);  // Hide at startup
        else
            Debug.LogError("Alert Canvas is not assigned.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isAlertActive)
        {
            Debug.Log("Player triggered alert zone");
            alertCanvas.SetActive(true);
            isAlertActive = true;
            Invoke(nameof(HideCanvas), displayDuration);
        }
    }

    void HideCanvas()
    {
        alertCanvas.SetActive(false);
        isAlertActive = false;
    }
}
