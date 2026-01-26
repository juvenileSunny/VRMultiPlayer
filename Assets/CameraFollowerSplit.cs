using UnityEngine;

public class CameraFollowerSplit : MonoBehaviour
{
    public Transform mainCamera;   // Assign VR camera (CenterEyeAnchor)
    public Transform body;         // Assign avatar body (not parented to camera)

    [Header("Offsets & Smoothing")]
    public float bodyYOffset = -0.1f; // adjust to match neck height
    public float bodyXOffset = -0.1f; // adjust to match neck height
    public float bodyZOffset = -0.1f; // adjust to match neck height
    public float smoothFollowSpeed = 5f;   // higher = faster follow

    private Quaternion initialRotationOffset;

    void Start()
    {
        if (mainCamera == null || body == null)
        {
            Debug.LogError("CameraFollowerSplit: Missing reference(s).");
            return;
        }

        // Store the initial yaw offset
        initialRotationOffset = Quaternion.Inverse(Quaternion.Euler(0f, mainCamera.eulerAngles.y, 0f)) * body.rotation;
    }

    void LateUpdate()
    {
        if (!mainCamera || !body) return;

        // Target body position matches camera (including Y) but with optional offset
        Vector3 targetPos = mainCamera.position + new Vector3(bodyXOffset, bodyYOffset, bodyZOffset);

        // Smoothly move the body toward that position
        body.position = Vector3.Lerp(body.position, targetPos, Time.deltaTime * smoothFollowSpeed);

        // Rotate body only around Y-axis to match camera
        float camY = mainCamera.eulerAngles.y;
        Quaternion targetRot = Quaternion.Euler(0f, camY, 0f) * initialRotationOffset;
        body.rotation = Quaternion.Lerp(body.rotation, targetRot, Time.deltaTime * smoothFollowSpeed);
    }
}
