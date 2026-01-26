
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;

public class SpeechifyTTSPlayer : MonoBehaviour
{
    [Header("Speechify API Settings")]
    [SerializeField] private string speechifyApiKey = "amIx6_nbtUmK9C7mCxbj-kvDyU2XiJLJwVKCSp7yLHk="; // Replace with your actual key
    private const string speechifyUrl = "https://api.sws.speechify.com/v1/audio/speech";

    [Header("UI Elements")]
    public TMP_Text textToSpeak;
    public Button speakButton;
    public AudioSource audioSource;

    private void Start()
    {
        speakButton.onClick.AddListener(() =>
        {
            string text = textToSpeak.text;
            if (!string.IsNullOrWhiteSpace(text))
                StartCoroutine(SendSpeechifyTTSRequest(text));
        });
    }

    private IEnumerator SendSpeechifyTTSRequest(string text)
    {
        var requestBody = new SpeechifyRequest
        {
            input = text,
            voice_id = "cliff", // Make sure this ID is supported in your Speechify plan
            language = "en-US",
            options = new SpeechifyOptions { loudness_normalization = true }
        };

        string jsonPayload = JsonUtility.ToJson(requestBody);

        using (UnityWebRequest request = new UnityWebRequest(speechifyUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", "Bearer " + speechifyApiKey);
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                SpeechifyResponse response = JsonUtility.FromJson<SpeechifyResponse>(request.downloadHandler.text);
                if (!string.IsNullOrEmpty(response.audio_url))
                {
                    yield return StartCoroutine(PlayAudioFromUrl(response.audio_url));
                }
                else
                {
                    Debug.LogWarning("No audio URL returned in response.");
                }
            }
            else
            {
                Debug.LogError($"Speechify TTS Error [{request.responseCode}]: {request.downloadHandler.text}");
            }
        }
    }

    private IEnumerator PlayAudioFromUrl(string audioUrl)
    {
        using (UnityWebRequest audioRequest = UnityWebRequestMultimedia.GetAudioClip(audioUrl, AudioType.MPEG))
        {
            yield return audioRequest.SendWebRequest();

            if (audioRequest.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(audioRequest);
                audioSource.clip = clip;
                audioSource.Play();
            }
            else
            {
                Debug.LogError("Failed to download audio from URL: " + audioRequest.error);
            }
        }
    }

    [System.Serializable]
    private class SpeechifyRequest
    {
        public string input;
        public string voice_id;
        public string language;
        public SpeechifyOptions options;
    }

    [System.Serializable]
    private class SpeechifyOptions
    {
        public bool loudness_normalization;
    }

    [System.Serializable]
    private class SpeechifyResponse
    {
        public string audio_url;
    }
}
