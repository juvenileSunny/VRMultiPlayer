using UnityEngine;

// Keeps a world-space canvas roughly in front of the user's camera without feeling hard-attached.
// - Ignores camera pitch (up/down); only responds to yaw (left/right) by re-centering when exceeded.
// - Smoothly recenters to a position in front of the camera on the horizontal plane.
// Attach this to the toast canvas GameObject when in VR mode.
public class VRToastFollower : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // Typically the active camera transform

    [Header("Placement")]
    public float distance = 1.5f; // Forward distance from camera on XZ plane
    public float fixedHeight = 1.3f; // Fixed Y position for the canvas
    public float yawRecenterAngle = 20f; // Degrees of yaw offset before we start recentering
    public float recenterSpeed = 5f; // Higher = snappier

    private Vector3 initialUp = Vector3.up;

    void Reset()
    {
        recenterSpeed = 5f;
        yawRecenterAngle = 20f;
        distance = 1.5f;
        fixedHeight = 1.2f;
    }

    void Update()
    {
        if (target == null)
            return;

        // Compute horizontal (yaw-only) forward from camera
        Vector3 flatForward = Vector3.ProjectOnPlane(target.forward, Vector3.up);
        if (flatForward.sqrMagnitude < 1e-4f)
        {
            // Edge case: looking straight up/down; fallback to target's right cross up
            flatForward = Vector3.ProjectOnPlane(target.rotation * Vector3.forward, Vector3.up);
            if (flatForward.sqrMagnitude < 1e-4f)
                flatForward = target.rotation * Vector3.right; // arbitrary fallback
        }
        flatForward.Normalize();

        // Always maintain fixed distance from camera on horizontal plane
        Vector3 desiredPos = target.position + flatForward * distance;
        desiredPos.y = fixedHeight; // Lock Y height

        // Current horizontal direction from camera to canvas
        Vector3 currentOffset = transform.position - target.position;
        Vector3 currentFlatDir = Vector3.ProjectOnPlane(currentOffset, Vector3.up);
        if (currentFlatDir.sqrMagnitude < 1e-6f)
        {
            currentFlatDir = flatForward; // if overlapped, assume forward
        }
        currentFlatDir.Normalize();

        float yawDelta = Vector3.SignedAngle(currentFlatDir, flatForward, Vector3.up);

        // Smooth move and rotate to maintain distance and face camera
        float k = 1f - Mathf.Exp(-recenterSpeed * Time.deltaTime);
        
        // Always update position to maintain fixed distance from camera
        transform.position = Vector3.Lerp(transform.position, desiredPos, k);

        // Face the camera horizontally (billboard), but only recenter rotation when yaw exceeds threshold
        // Use positive flatForward (not negative) for 90 degree rotation instead of -90
        Quaternion desiredRot = Quaternion.LookRotation(flatForward, initialUp);
        
        if (Mathf.Abs(yawDelta) > yawRecenterAngle)
        {
            // Recenter both position and rotation when yaw threshold exceeded
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, k);
        }
        else
        {
            // Still maintain rotation smoothly even within deadzone
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, k * 0.5f);
        }
    }
}
