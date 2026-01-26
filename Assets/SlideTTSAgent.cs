using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class BlendShapeTarget
{
    public SkinnedMeshRenderer skinnedMesh;
    public string blendShapeName;
    [Range(0f, 0.1f)] public float maxWeight = 0.001f;
    [HideInInspector] public int blendShapeIndex = -1;
}

public class SlideTTSAgent : MonoBehaviour
{
    [Header("TTS HTTP Endpoint")]
    public string ttsUrl = "http://127.0.0.1:5005/tts";

    [Header("Audio")]
    public AudioSource audioSource;

    [Header("Avatar Blendshapes")]
    [Tooltip("Drag & drop all SkinnedMeshRenderers here.")]
    public List<BlendShapeTarget> blendShapeTargets = new List<BlendShapeTarget>();

    [Header("Advanced")]
    public string authorizationHeader;
    public bool interruptOnNewSpeak = true;
    public int requestTimeoutSeconds = 30;

    private Coroutine _speakRoutine;
    private float[] _sampleData = new float[1024];

    public bool IsSpeaking => audioSource != null && audioSource.isPlaying;

    void Start()
    {
        // Pre-cache blendshape indices
        foreach (var target in blendShapeTargets)
        {
            if (target.skinnedMesh != null && !string.IsNullOrEmpty(target.blendShapeName))
            {
                target.blendShapeIndex = target.skinnedMesh.sharedMesh.GetBlendShapeIndex(target.blendShapeName);
                if (target.blendShapeIndex == -1)
                {
                    Debug.LogWarning($"[SlideTTSAgent] BlendShape '{target.blendShapeName}' not found on {target.skinnedMesh.name}");
                }
            }
        }
    }

    void Update()
    {
        if (audioSource != null && audioSource.isPlaying && blendShapeTargets.Count > 0)
        {
            float loudness = GetCurrentAmplitude();
            ApplyBlendShapes(loudness);
        }
    }

    float GetCurrentAmplitude()
    {
        audioSource.GetOutputData(_sampleData, 0);
        float sum = 0f;
        for (int i = 0; i < _sampleData.Length; i++)
        {
            sum += _sampleData[i] * _sampleData[i];
        }
        return Mathf.Clamp01(Mathf.Sqrt(sum / _sampleData.Length) * 10f);
    }

    void ApplyBlendShapes(float amplitude)
    {
        foreach (var target in blendShapeTargets)
        {
            if (target.skinnedMesh == null || target.blendShapeIndex == -1) continue;
            float weight = amplitude * target.maxWeight * 100f;
            target.skinnedMesh.SetBlendShapeWeight(target.blendShapeIndex, weight);
        }
    }

    public void Speak(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        if (interruptOnNewSpeak && _speakRoutine != null)
        {
            StopSpeaking();
        }

        _speakRoutine = StartCoroutine(SendToTTS(text));
    }

    public void StopSpeaking()
    {
        if (_speakRoutine != null)
        {
            StopCoroutine(_speakRoutine);
            _speakRoutine = null;
        }

        if (audioSource && audioSource.isPlaying)
            audioSource.Stop();
    }

    IEnumerator SendToTTS(string text)
    {
        if (audioSource == null)
        {
            Debug.LogError("[SlideTTSAgent] AudioSource is not assigned.");
            yield break;
        }

        string json = "{\"text\":\"" + EscapeJson(text) + "\"}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(ttsUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(authorizationHeader))
                request.SetRequestHeader("Authorization", authorizationHeader);

            request.timeout = requestTimeoutSeconds;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                byte[] audioData = request.downloadHandler.data;
                if (audioData == null || audioData.Length == 0)
                {
                    Debug.LogError("[SlideTTSAgent] TTS returned empty audio data.");
                    yield break;
                }

                WAV wav = new WAV(audioData);
                if (wav.SampleCount <= 0 || wav.Frequency <= 0 || wav.LeftChannel == null)
                {
                    Debug.LogError("[SlideTTSAgent] Invalid WAV data.");
                    yield break;
                }

                AudioClip clip = AudioClip.Create("SlideTTS", wav.SampleCount, 1, wav.Frequency, false);
                clip.SetData(wav.LeftChannel, 0);

                audioSource.clip = clip;
                audioSource.Play();

                yield return new WaitForSeconds(clip.length);
            }
            else
            {
                Debug.LogError($"[SlideTTSAgent] TTS Error: {request.error}");
            }
        }

        _speakRoutine = null;
    }

    string EscapeJson(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        return input.Replace("\\", "\\\\").Replace("\"", "\\\"")
                    .Replace("\n", "\\n").Replace("\r", "")
                    .Replace("\t", "\\t");
    }
}
