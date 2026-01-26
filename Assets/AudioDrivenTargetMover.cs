using UnityEngine;
using System.Collections.Generic;

public class AudioDrivenTargetMover : MonoBehaviour
{
    [Header("Audio Input")]
    [Tooltip("Assign the AudioSource playing TTS or mic input")]
    public AudioSource audioSource;

    [System.Serializable]
    public class TargetSettings
    {
        public Transform target;

        [Header("Position Ranges")]
        public float maxX = 0.05f;
        public float maxY = 0.05f;
        public float maxZ = 0.05f;

        [Header("Rotation Ranges")]
        public float rotX = 5f;
        public float rotY = 5f;
        public float rotZ = 5f;

        [Header("Sensitivity Settings")]
        [Range(0.1f, 5f)] public float sensitivity = 1f;

        [Tooltip("Extra natural randomness on top of amplitude-based motion")]
        [Range(0f, 0.3f)] public float randomJitter = 0.05f;

        [Tooltip("How quickly the direction changes over time")]
        [Range(0.1f, 5f)] public float directionChangeSpeed = 1.5f;

        [HideInInspector] public Vector3 defaultPos;
        [HideInInspector] public Quaternion defaultRot;
    }

    [Header("Rig Targets List")]
    public List<TargetSettings> rigTargets = new List<TargetSettings>();

    [Header("Global Settings")]
    [Range(0.1f, 1f)] public float smoothSpeed = 1f;

    void Start()
    {
        if (!audioSource)
            Debug.LogError("[AudioDrivenTargetMover] AudioSource is not assigned.");

        foreach (var ts in rigTargets)
            SaveDefaults(ts);
    }

    void SaveDefaults(TargetSettings ts)
    {
        if (ts.target)
        {
            ts.defaultPos = ts.target.localPosition;
            ts.defaultRot = ts.target.localRotation;
        }
    }

    void Update()
    {
        if (!audioSource) return;

        float[] samples = new float[64];
        audioSource.GetOutputData(samples, 0);
        float amplitude = 0f;
        foreach (float s in samples) amplitude += Mathf.Abs(s);
        amplitude /= samples.Length;

        foreach (var ts in rigTargets)
            MoveTarget(ts, amplitude);
    }

    void MoveTarget(TargetSettings ts, float amplitude)
    {
        if (!ts.target) return;

        float audioValue = Mathf.Clamp01(amplitude * ts.sensitivity);

        float time = Time.time * ts.directionChangeSpeed;
        float dirX = (Mathf.PerlinNoise(time, 0f) - 0.5f) * 2f;
        float dirY = (Mathf.PerlinNoise(0f, time) - 0.5f) * 2f;
        float dirZ = (Mathf.PerlinNoise(time, time) - 0.5f) * 2f;

        Vector3 offset = new Vector3(
            dirX * ts.maxX * audioValue,
            dirY * ts.maxY * audioValue,
            dirZ * ts.maxZ * audioValue
        );

        offset += new Vector3(
            Random.Range(-ts.randomJitter, ts.randomJitter),
            Random.Range(-ts.randomJitter, ts.randomJitter),
            Random.Range(-ts.randomJitter, ts.randomJitter)
        );

        Vector3 targetPos = ts.defaultPos + offset;

        Quaternion targetRot = ts.defaultRot * Quaternion.Euler(
            dirX * ts.rotX * audioValue,
            dirY * ts.rotY * audioValue,
            dirZ * ts.rotZ * audioValue
        );

        ts.target.localPosition = Vector3.Lerp(ts.target.localPosition, targetPos, Time.deltaTime * smoothSpeed);
        ts.target.localRotation = Quaternion.Lerp(ts.target.localRotation, targetRot, Time.deltaTime * smoothSpeed);
    }
}
