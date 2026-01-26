using UnityEngine;

public class ConditionalCollider : MonoBehaviour
{
    [SerializeField] private float activeDuration = 5f; // Time in seconds before the object vanishes

    void Start()
    {
        // Start the countdown on session start
        Invoke(nameof(DisableObject), activeDuration);
    }

    public void DisableObject()
    {
        gameObject.SetActive(false);  // Disables the entire GameObject
    }

    public void EnableObject()
    {
        gameObject.SetActive(true);   // Re-enables it if needed
    }
}
