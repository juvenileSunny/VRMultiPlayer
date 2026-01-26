using UnityEngine;

public class FaceCamera : MonoBehaviour
{
      private Transform cameraTransform;

    void Start()
    {
        cameraTransform = Camera.main.transform;
    }

    void FixedUpdate()
    {
        // Correct rotation so canvas front (Z–) faces camera
        Vector3 directionToCamera = transform.position - cameraTransform.position;
        transform.rotation = Quaternion.LookRotation(directionToCamera);
    }
}
