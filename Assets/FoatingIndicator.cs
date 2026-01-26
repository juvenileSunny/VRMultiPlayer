using UnityEngine;

public class FloatingIndicator : MonoBehaviour
{
    public float floatAmplitude = 0.25f;   // How high it floats
    public float floatFrequency = 1f;      // How fast it floats

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        float offset = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = startPos + new Vector3(0, offset, 0);
    }
}

