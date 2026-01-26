using UnityEngine;

public class AndroidTTS : MonoBehaviour
{
    private AndroidJavaObject tts;
    private AndroidJavaObject activity;

    void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            tts = new AndroidJavaObject("android.speech.tts.TextToSpeech", activity, new TTSInitListener());
        }
#endif
    }

    public void Speak(string message)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (tts != null)
        {
            tts.Call<int>("speak", message, 0, null, null);
        }
        else
        {
            Debug.LogWarning("TTS not initialized.");
        }
#endif
    }

    private class TTSInitListener : AndroidJavaProxy
    {
        public TTSInitListener() : base("android.speech.tts.TextToSpeech$OnInitListener") { }

        public void onInit(int status)
        {
            Debug.Log("TTS Init Status: " + status);
        }
    }
}
