using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;

[System.Serializable]
public class BlendShapeTarget
{
    public SkinnedMeshRenderer skinnedMesh;
    public string blendShapeName;
    [Range(0f, 100f)] public float maxWeight = 10f; // weight in blendshape units (0..100)
    [HideInInspector] public int blendShapeIndex = -1;
}
[System.Serializable]
public class TTSConfig
{
    public string ttsUrl = "http://arsc-r-2wm6yb4.ddns.uark.edu:5005/tts";
    // public string ttsUrl = "http://10.0.0.146:5005/tts";
}
public class SlideTTSAgent : MonoBehaviour
{
    [Header("TTS HTTP Endpoint (PC IP, not 127.0.0.1 on Quest, loaded from config.json if exists)")]
    // public string ttsUrl = "http://10.0.0.146:5005/tts";
    public string ttsUrl = "http://arsc-r-2wm6yb4.ddns.uark.edu:5005/tts";

    [Header("Audio")]
    public AudioSource audioSource;

    [Header("Avatar Blendshapes")]
    public List<BlendShapeTarget> blendShapeTargets = new List<BlendShapeTarget>();

    [Header("Advanced")]
    public bool interruptOnNewSpeak = true;
    private bool _pauseRequested = false;
    public int requestTimeoutSeconds = 30;

    private Coroutine _speakRoutine;
    private float[] _sampleData = new float[1024];

    public bool IsSpeaking => audioSource != null && audioSource.isPlaying;
    public bool IsPaused => audioSource != null && audioSource.clip != null && !audioSource.isPlaying && audioSource.time > 0f;
    public bool EndOfSpeech => _speakRoutine == null && (audioSource == null || !audioSource.isPlaying);

    void Start()
    {
        LoadConfig();
        // Cache blendshape indices
        foreach (var target in blendShapeTargets)
        {
            if (target.skinnedMesh != null &&
                target.skinnedMesh.sharedMesh != null &&
                !string.IsNullOrEmpty(target.blendShapeName))
            {
                target.blendShapeIndex = target.skinnedMesh.sharedMesh.GetBlendShapeIndex(target.blendShapeName);
                if (target.blendShapeIndex < 0)
                    Debug.LogWarning($"[SlideTTSAgent] BlendShape '{target.blendShapeName}' not found on {target.skinnedMesh.name}");
            }
        }
    }
    void LoadConfig()
    {
        string path;

    #if UNITY_ANDROID && !UNITY_EDITOR
        path = Path.Combine(Application.persistentDataPath, "config.json");
    #else
        path = Path.Combine(Application.dataPath, "..", "config.json");
    #endif

        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                TTSConfig config = JsonUtility.FromJson<TTSConfig>(json);

                if (config != null && !string.IsNullOrWhiteSpace(config.ttsUrl))
                {
                    ttsUrl = config.ttsUrl;
                    Debug.Log("[SlideTTSAgent] Loaded TTS URL from config: " + ttsUrl);
                }
                else
                {
                    Debug.LogWarning("[SlideTTSAgent] config.json found but ttsUrl was empty. Using Inspector/default value: " + ttsUrl);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[SlideTTSAgent] Failed to read config.json: " + ex.Message);
            }
        }
        else
        {
            Debug.LogWarning("[SlideTTSAgent] config.json not found. Creating one with default URL: " + ttsUrl);

            try
            {
                TTSConfig defaultConfig = new TTSConfig();
                defaultConfig.ttsUrl = ttsUrl;

                string json = JsonUtility.ToJson(defaultConfig, true);
                File.WriteAllText(path, json);

                Debug.Log("[SlideTTSAgent] Created config at: " + path);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[SlideTTSAgent] Failed to create config.json: " + ex.Message);
            }
        }
    }
    void Update()
    {
        if (audioSource != null && audioSource.isPlaying && blendShapeTargets.Count > 0)
        {
            float amp = GetCurrentAmplitude(); // 0..1-ish
            ApplyBlendShapes(amp);
        }
        else
        {
            // relax mouth when not speaking
            ApplyBlendShapes(0f);
        }
    }

    float GetCurrentAmplitude()
    {
        if (audioSource == null) return 0f;
        audioSource.GetOutputData(_sampleData, 0);
        float sum = 0f;
        for (int i = 0; i < _sampleData.Length; i++)
            sum += _sampleData[i] * _sampleData[i];

        float rms = Mathf.Sqrt(sum / _sampleData.Length);
        // scale a bit; tune if needed
        return Mathf.Clamp01(rms * 10f);
    }

    void ApplyBlendShapes(float amplitude01)
    {
        foreach (var target in blendShapeTargets)
        {
            if (target.skinnedMesh == null || target.blendShapeIndex < 0) continue;
            float w = amplitude01 * target.maxWeight;
            target.skinnedMesh.SetBlendShapeWeight(target.blendShapeIndex, w);
        }
    }

    public void Speak(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        if (interruptOnNewSpeak)
            StopSpeaking();

        _speakRoutine = StartCoroutine(SendToTTS(text));
    }

    public void StopSpeaking()
    {
        if (_speakRoutine != null)
        {
            StopCoroutine(_speakRoutine);
            _speakRoutine = null;
        }

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = null;
        }
    }

    // public void PauseSpeaking()
    // {
    //     if (audioSource != null && audioSource.isPlaying)
    //         audioSource.Pause();
    // }

    // public void ResumeSpeaking()
    // {
    //     if (audioSource != null && audioSource.clip != null && !audioSource.isPlaying)
    //         audioSource.UnPause();
    // }
    public void PauseSpeaking()
    {
        _pauseRequested = true;

        if (audioSource != null && audioSource.isPlaying)
            audioSource.Pause();
    }

    public void ResumeSpeaking()
    {
        _pauseRequested = false;

        if (audioSource != null && audioSource.clip != null && !audioSource.isPlaying)
            audioSource.UnPause();
    }

    IEnumerator SendToTTS(string text)
    {
        if (audioSource == null)
        {
            Debug.LogError("[SlideTTSAgent] AudioSource is not assigned.");
            _speakRoutine = null;
            yield break;
        }

        string json = "{\"text\":\"" + EscapeJson(text) + "\"}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(ttsUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = requestTimeoutSeconds;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                // This helps debug your Flask 400:
                string body = request.downloadHandler != null ? request.downloadHandler.text : "(no body)";
                Debug.LogError($"[SlideTTSAgent] TTS Error: {request.responseCode} {request.error}\nBody: {body}\nURL: {ttsUrl}");
                _speakRoutine = null;
                yield break;
            }

            byte[] audioData = request.downloadHandler.data;
            if (audioData == null || audioData.Length == 0)
            {
                Debug.LogError("[SlideTTSAgent] TTS returned empty audio.");
                _speakRoutine = null;
                yield break;
            }

            WAV wav = new WAV(audioData);
            if (wav.SampleCount <= 0 || wav.Frequency <= 0 || wav.LeftChannel == null)
            {
                Debug.LogError("[SlideTTSAgent] Invalid WAV data.");
                _speakRoutine = null;
                yield break;
            }

            AudioClip clip = AudioClip.Create("SlideTTS", wav.SampleCount, 1, wav.Frequency, false);
            clip.SetData(wav.LeftChannel, 0);

            audioSource.clip = clip;
            audioSource.Play();
            if (_pauseRequested)
            {
                // pause immediately if host pressed pause during download
                audioSource.Pause();
            }

            // Wait until playback finishes or gets stopped
            while (audioSource != null && audioSource.isPlaying)
                yield return null;
        }

        _speakRoutine = null;
    }

    string EscapeJson(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        return input.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "")
                    .Replace("\t", "\\t");
    }
}