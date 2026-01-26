// using UnityEngine;
// using UnityEngine.UI;
// using Whisper.Utils;

// namespace Whisper.Samples
// {
//     /// <summary>
//     /// Stream transcription from microphone input.
//     /// </summary>
//     public class StreamingSampleMic : MonoBehaviour
//     {
//         public WhisperManager whisper;
//         public MicrophoneRecord microphoneRecord;
    
//         [Header("UI")] 
//         public Button button;
//         public Text buttonText;
//         public Text text;
//         public ScrollRect scroll;
//         private WhisperStream _stream;

//         private async void Start()
//         {
//             _stream = await whisper.CreateStream(microphoneRecord);
//             _stream.OnResultUpdated += OnResult;
//             _stream.OnSegmentUpdated += OnSegmentUpdated;
//             _stream.OnSegmentFinished += OnSegmentFinished;
//             _stream.OnStreamFinished += OnFinished;

//             microphoneRecord.OnRecordStop += OnRecordStop;
//             button.onClick.AddListener(OnButtonPressed);
//         }

//         private void OnButtonPressed()
//         {
//             if (!microphoneRecord.IsRecording)
//             {
//                 _stream.StartStream();
//                 microphoneRecord.StartRecord();
//             }
//             else
//                 microphoneRecord.StopRecord();
        
//             buttonText.text = microphoneRecord.IsRecording ? "Stop" : "Record";
//         }
    
//         private void OnRecordStop(AudioChunk recordedAudio)
//         {
//             buttonText.text = "Record";
//         }
    
//         private void OnResult(string result)
//         {
//             text.text = result;
//             UiUtils.ScrollDown(scroll);
//         }
        
//         private void OnSegmentUpdated(WhisperResult segment)
//         {
//             print($"Segment updated: {segment.Result}");
//         }
        
//         private void OnSegmentFinished(WhisperResult segment)
//         {
//             print($"Segment finished: {segment.Result}");
//         }
        
//         private void OnFinished(string finalResult)
//         {
//             print("Stream finished!");
//         }
//     }
// }


using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Needed for pointer events
using Whisper.Utils;
using TMPro; 

namespace Whisper.Samples
{
    public class StreamingSampleMic : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public WhisperManager whisper;
        public MicrophoneRecord microphoneRecord;
        public LocalChatWithTTS chatAgent;

        [Header("Audio")]
        public AudioSource audioSource;  // Link this in the Inspector

        private bool isSpeaking = false;


        [Header("UI")]
        public Button button;
        public Text buttonText;
        public TMP_Text text;
        public ScrollRect scroll;

        private WhisperStream _stream;
        private string finalTranscript = "";

        private async void Start()
        {
            _stream = await whisper.CreateStream(microphoneRecord);
            _stream.OnResultUpdated += OnResult;
            _stream.OnSegmentFinished += OnSegmentFinished;
            _stream.OnStreamFinished += OnFinished;

            microphoneRecord.OnRecordStop += OnRecordStop;

            // Dynamically add EventTrigger if not using a separate custom UI object
            if (button != null)
            {
                var trigger = button.gameObject.AddComponent<EventTrigger>();
                trigger.triggers = new System.Collections.Generic.List<EventTrigger.Entry>();

                var downEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
                downEntry.callback.AddListener((data) => OnPointerDown(null));
                trigger.triggers.Add(downEntry);

                var upEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
                upEntry.callback.AddListener((data) => OnPointerUp(null));
                trigger.triggers.Add(upEntry);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (chatAgent != null && chatAgent.IsSpeaking)
            {
                chatAgent.StopSpeaking();
                Debug.Log("🛑 TTS playback stopped.");
            }

            finalTranscript = "";
            _stream.StartStream();
            microphoneRecord.StartRecord();
            buttonText.text = "Recording...";
        }


        public void OnPointerUp(PointerEventData eventData)
        {
            microphoneRecord.StopRecord();
            buttonText.text = "Thinking...";
        }

        private void OnRecordStop(AudioChunk recordedAudio)
        {
            buttonText.text = "Processing...";
        }

        private void OnResult(string result)
        {
            finalTranscript = result;
            text.text = result;
            UiUtils.ScrollDown(scroll);
        }

        private void OnSegmentFinished(WhisperResult segment)
        {
            print($"Segment finished: {segment.Result}");
        }

        // private void OnFinished(string finalResult)
        // {
        //     print("Stream finished.");
        //     buttonText.text = "Hold to Talk";

        //     // Trigger your local LLM or TTS agent here
        //     Debug.Log($"[🤖 Agent Reply] You said: {finalTranscript}");
        // }

        private void OnFinished(string finalResult)
        {
            print("Stream finished.");
            buttonText.text = "Hold to Talk";

            if (!string.IsNullOrWhiteSpace(finalTranscript) && chatAgent != null)
            {
                Debug.Log($"[🎙️ Transcribed] {finalTranscript}");
                chatAgent.TriggerLLMResponseFromText(finalTranscript);
            }
        }

    }
}
