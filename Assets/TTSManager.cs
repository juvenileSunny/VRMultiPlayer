using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public class TTSRequest
{
    public string text;
}

public class TTSManager : MonoBehaviour
{
    public TextMeshProUGUI targetText; // Text you want to speak
    public AudioSource audioSource;    // AudioSource to play the TTS output
    public Button speakButton;         // Button to trigger speech

    private void Start()
    {
        speakButton.onClick.AddListener(OnSpeakButtonClicked);
    }

    private void OnSpeakButtonClicked()
    {
        string textToSpeak = targetText.text;
        if (!string.IsNullOrWhiteSpace(textToSpeak))
        {
            StartCoroutine(PlayTTS(textToSpeak));
        }
    }

    IEnumerator PlayTTS(string inputText)
    {
        TTSRequest requestData = new TTSRequest { text = inputText };
        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest("http://localhost:5005/tts", "POST"))
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
                audioSource.Play();
            }
            else
            {
                Debug.LogError("TTS Error: " + request.responseCode + " - " + request.error);
                Debug.LogError("Response Text: " + request.downloadHandler.text);
            }
        }
    }
}
