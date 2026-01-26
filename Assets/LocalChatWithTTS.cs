using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public class PABlendShapeTarget
{
    public SkinnedMeshRenderer skinnedMesh;
    public string blendShapeName;
    [Range(0f, 1f)] public float maxWeight = 1f;  // Increased for visibility
    [HideInInspector] public int blendShapeIndex = -1;
}

[System.Serializable]
public class AudioDrivenTarget
{
    public Transform target;
    public Vector3 axis = Vector3.up;
    public float amplitudeMultiplier = 1f;
    public float speed = 1f;
    [HideInInspector] public Vector3 initialPos;
}

[System.Serializable]
public class ChatMessage
{
    public string role;
    public string content;
}

[System.Serializable]
public class LocalChatRequest
{
    public string model;
    public List<ChatMessage> messages;
    public float temperature = 0.6f;
    public float top_p = 0.95f;
    public int max_tokens = 1024;
    public bool stream = false;
}

[System.Serializable]
public class ChatResponse
{
    public List<Choice> choices;
}

[System.Serializable]
public class Choice
{
    public Message content;
}

[System.Serializable]
public class Message
{
    public string role;
    public string content;
}

public class LocalChatWithTTS : MonoBehaviour
{
    [Header("LLM Settings")]
    public string ollamaUrl = "http://localhost:11500/v1/chat/completions";
    // public string ollamaUrl = "http://eac-st-23.ad.ualr.edu:11434/v1/chat/completions";
    public string ttsUrl = "http://localhost:5005/tts";
    [Header("UI")]
    public TMP_Text inputField;
    public TMP_Text responseText;
    public Button sendButton;
    public GameObject loadingIndicator;

    [Header("Avatar Blendshapes")]
    public List<PABlendShapeTarget> blendShapeTargets = new List<PABlendShapeTarget>();

    [Header("Audio")]
    public AudioSource audioSource;

    [Header("Avatar Animator")]
    public Animator avatarAnimator;

    [Header("Floating Motion")]
    public List<AudioDrivenTarget> floatingTargets = new List<AudioDrivenTarget>();
    public bool onlyFloatWhileSpeaking = true;

    private float[] _sampleData = new float[512];
    // private string ollamaUrl = "http://localhost:11500/v1/chat/completions"; 

    private string model = "llama3";
    private bool isSpeakingNow = false;
    public bool IsSpeaking => isSpeakingNow;

    private List<ChatMessage> messageHistory = new List<ChatMessage>
    {
        new ChatMessage
        {
            role = "system",
            content = @"You are a biology teacher who specializes in virology. Use academic terminology and respond briefly in 1-2 sentences. Avoid long paragraphs. Keep explanations clear and concise. Do not use any markdown formatting, special characters like asterisks (*), backslashes (\), or emojis. Only include plain text in your replies."
        }
    };

    private void Start()
    {
        sendButton.onClick.AddListener(() =>
        {
            string prompt = inputField.text.Trim();
            if (!string.IsNullOrWhiteSpace(prompt))
                StartCoroutine(SendChatAndSpeak(prompt));
        });

        foreach (var target in blendShapeTargets)
        {
            if (target.skinnedMesh != null && !string.IsNullOrEmpty(target.blendShapeName))
            {
                target.blendShapeIndex = target.skinnedMesh.sharedMesh.GetBlendShapeIndex(target.blendShapeName);
                if (target.blendShapeIndex == -1)
                    Debug.LogWarning($"BlendShape '{target.blendShapeName}' not found on {target.skinnedMesh.name}");
            }
        }

        foreach (var ft in floatingTargets)
        {
            if (ft.target != null)
                ft.initialPos = ft.target.localPosition;
        }
    }

    private void Update()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            float loudness = GetCurrentAmplitude();
            ApplyBlendShapes(loudness);

            if (!onlyFloatWhileSpeaking || isSpeakingNow)
                AnimateFloatingTargets(loudness);

            Debug.Log($"🎤 isPlaying: {audioSource.isPlaying}, amplitude: {loudness:F3}");
        }
    }

    private float GetCurrentAmplitude()
    {
        audioSource.GetOutputData(_sampleData, 0);
        float sum = 0f;
        for (int i = 0; i < _sampleData.Length; i++)
            sum += _sampleData[i] * _sampleData[i];

        return Mathf.Clamp01(Mathf.Sqrt(sum / _sampleData.Length) * 10f);
    }

    private void ApplyBlendShapes(float amplitude)
    {
        foreach (var target in blendShapeTargets)
        {
            if (target.skinnedMesh == null || target.blendShapeIndex == -1) continue;
            float weight = amplitude * target.maxWeight * 100f;
            target.skinnedMesh.SetBlendShapeWeight(target.blendShapeIndex, weight);
        }
    }

    private void AnimateFloatingTargets(float amplitude)
    {
        foreach (var ft in floatingTargets)
        {
            if (ft.target == null) continue;
            float offset = Mathf.Sin(Time.time * ft.speed) * amplitude * ft.amplitudeMultiplier;
            ft.target.localPosition = ft.initialPos + ft.axis * offset;
        }
    }

    IEnumerator SendChatAndSpeak(string prompt)
    {
        if (loadingIndicator) loadingIndicator.SetActive(true);

        messageHistory.Add(new ChatMessage { role = "user", content = prompt });

        LocalChatRequest requestData = new LocalChatRequest
        {
            model = model,
            messages = messageHistory
        };

        string json = JsonUtility.ToJson(requestData).Replace("\"messages\":[]", "\"messages\":" + BuildMessagesJson());

        using (UnityWebRequest request = new UnityWebRequest(ollamaUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                string reply = ExtractContent(response);
                responseText.text = reply;
                messageHistory.Add(new ChatMessage { role = "assistant", content = reply });
                StartCoroutine(SendToTTS(reply));
            }
            else
            {
                Debug.LogError("LLM Error: " + request.error);
                responseText.text = "LLM Error: " + request.error;
            }
        }

        if (loadingIndicator) loadingIndicator.SetActive(false);
    }

    IEnumerator SendToTTS(string text)
    {
        string json = "{\"text\":\"" + EscapeJson(text) + "\"}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(ttsUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                byte[] audioData = request.downloadHandler.data;
                WAV wav = new WAV(audioData);
                AudioClip clip = AudioClip.Create("TTS", wav.SampleCount, 1, wav.Frequency, false);
                clip.SetData(wav.LeftChannel, 0);
                audioSource.clip = clip;

                isSpeakingNow = true;
                if (avatarAnimator != null) avatarAnimator.SetTrigger("StartTalking");

                Debug.Log("✅ TTS Audio Ready. Playing...");
                audioSource.Play();
                StartCoroutine(WaitForTTSPlaybackToEnd(clip.length));
            }
            else
            {
                Debug.LogError("TTS Error: " + request.error);
            }
        }
    }

    IEnumerator WaitForTTSPlaybackToEnd(float duration)
    {
        yield return new WaitForSeconds(duration);
        isSpeakingNow = false;
        if (avatarAnimator != null) avatarAnimator.SetTrigger("StopTalking");
    }

    private string EscapeJson(string input)
    {
        return input.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "").Replace("\t", "\\t");
    }

    private string ExtractContent(string json)
    {
        int startIndex = json.IndexOf("\"content\":\"");
        if (startIndex == -1) return "[No content]";
        startIndex += "\"content\":\"".Length;
        int endIndex = json.IndexOf("\"", startIndex);
        string content = json.Substring(startIndex, endIndex - startIndex);
        return content.Replace("\\n", "\n").Replace("\\\"", "\"");
    }

    public void TriggerLLMResponseFromText(string prompt)
    {
        if (!string.IsNullOrWhiteSpace(prompt))
        {
            if (inputField != null) inputField.text = prompt;
            StartCoroutine(SendChatAndSpeak(prompt));
            Debug.Log($"📨 Triggered via voice: {prompt}");
        }
    }

    public void StopSpeaking()
    {
        if (audioSource.isPlaying)
            audioSource.Stop();
    }

    private string BuildMessagesJson()
    {
        StringBuilder sb = new StringBuilder("[");
        for (int i = 0; i < messageHistory.Count; i++)
        {
            sb.Append("{\"role\":\"" + messageHistory[i].role + "\",\"content\":\"" + EscapeJson(messageHistory[i].content) + "\"}");
            if (i < messageHistory.Count - 1) sb.Append(",");
        }
        sb.Append("]");
        return sb.ToString();
    }

    public void SpeakOnly(string text)
    {
        StartCoroutine(SendToTTS(text));
    }
}
