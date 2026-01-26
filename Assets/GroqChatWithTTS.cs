using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class GroqChatWithHFTTS : MonoBehaviour
{
    [Header("Groq API Settings")]
    [SerializeField] private string groqApiKey = "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";
    private string model = "deepseek-r1-distill-llama-70b";
    private List<Dictionary<string, string>> messageHistory = new List<Dictionary<string, string>>();

    [Header("Hugging Face TTS Settings")]
    [SerializeField] private string hfApiKey = "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";  // Use your HF token
    private string hfTtsEndpoint = "https://api-inference.huggingface.co/models/SWivid/F5-TTS";

    [Header("UI Elements")]
    public TMP_InputField promptInputField;
    public TMP_Text responseText;
    public Button sendButton;
    public GameObject loadingIndicator;
    public AudioSource audioSource;

    private void Start()
    {
        sendButton.onClick.AddListener(() =>
        {
            string prompt = promptInputField.text;
            if (!string.IsNullOrEmpty(prompt))
                StartCoroutine(SendPrompt(prompt));
        });
    }

    IEnumerator SendPrompt(string prompt)
    {
        // Add user message
        messageHistory.Add(new Dictionary<string, string> { { "role", "user" }, { "content", prompt } });

        // Build history
        StringBuilder messagesJson = new StringBuilder();
        messagesJson.Append("[");
        for (int i = 0; i < messageHistory.Count; i++)
        {
            var msg = messageHistory[i];
            messagesJson.Append($"{{\"role\":\"{msg["role"]}\",\"content\":\"{EscapeJson(msg["content"])}\"}}");
            if (i < messageHistory.Count - 1) messagesJson.Append(",");
        }
        messagesJson.Append("]");

        string json = $@"
        {{
            ""messages"": {messagesJson},
            ""model"": ""{model}"",
            ""temperature"": 0.6,
            ""max_completion_tokens"": 4096,
            ""top_p"": 0.95,
            ""stream"": false
        }}";

        if (loadingIndicator) loadingIndicator.SetActive(true);

        using (UnityWebRequest request = new UnityWebRequest("https://api.groq.com/openai/v1/chat/completions", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + groqApiKey);

            yield return request.SendWebRequest();
            if (loadingIndicator) loadingIndicator.SetActive(false);

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseJson = request.downloadHandler.text;
                string reply = ExtractContent(responseJson);

                messageHistory.Add(new Dictionary<string, string> { { "role", "assistant" }, { "content", reply } });

                Debug.Log("Assistant: " + reply);
                responseText.text = reply;

                StartCoroutine(SendToTTS(reply)); // <-- Speak via HF TTS
            }
            else
            {
                Debug.LogError("API Error: " + request.error);
                responseText.text = "Error: " + request.error;
            }
        }
    }

    IEnumerator SendToTTS(string text)
    {
        string ttsJson = $@"{{ ""inputs"": ""{EscapeJson(text)}"" }}";

        using (UnityWebRequest request = new UnityWebRequest(hfTtsEndpoint, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(ttsJson);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerAudioClip(hfTtsEndpoint, AudioType.WAV);
            request.SetRequestHeader("Authorization", "Bearer " + hfApiKey);
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                audioSource.clip = clip;
                audioSource.Play();
            }
            else
            {
                Debug.LogError("eTTS API Error: " + request.error);
            }
        }
    }

    private string EscapeJson(string input)
    {
        return input.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
    }

    private string ExtractContent(string json)
    {
        const string key = "\"content\":\"";
        int start = json.IndexOf(key);
        if (start == -1) return "[No content]";
        start += key.Length;
        int end = json.IndexOf("\"", start);
        if (end == -1) end = json.Length;
        string raw = json.Substring(start, end - start);
        return raw.Replace("\\n", "\n").Replace("\\u003c", "<").Replace("\\u003e", ">");
    }
}
