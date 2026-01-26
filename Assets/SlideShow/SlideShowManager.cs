using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class SlideShowManager : MonoBehaviour
{
    [Header("Config")]
    public SlideShowConfig slideShowConfig;

    [Header("UI (World-Space Canvas)")]
    public Image slideImage;
    public TMP_Text slideTitle;
    public TMP_Text slideTranscript;

    [Header("TTS (Slide-only)")]
    public SlideTTSAgent slideTTSAgent;
    public float delayBeforeSpeaking = 0.5f;
    public float delayAfterSpeaking = 0.5f;

    [Header("Boundary Settings")]
    public List<GameObject> objectToDisableAtEnd;
    public List<GameObject> objectToEnableAtEnd;

    private int currentSlide = 0;

    void Start()
    {
        if(objectToEnableAtEnd != null)
        {
            foreach (GameObject obj in objectToEnableAtEnd)
            {
                if (obj != null)
                    obj.SetActive(false);
            }
        }


        if (slideShowConfig == null || slideShowConfig.slides == null || slideShowConfig.slides.Length == 0)
        {
            Debug.LogError("[SlideShow] SlideShowConfig is missing or empty.");
            return;
        }

        if (!slideImage || !slideTitle || !slideTranscript)
        {
            Debug.LogError("[SlideShow] Assign slideImage, slideTitle, slideTranscript in Inspector.");
            return;
        }

        StartCoroutine(PlaySlidesInOrder());
    }

    private IEnumerator PlaySlidesInOrder()
    {
        while (currentSlide < slideShowConfig.slides.Length)
        {
            ShowSlide(currentSlide);

            if (delayBeforeSpeaking > 0f)
                yield return new WaitForSeconds(delayBeforeSpeaking);

            var slide = slideShowConfig.slides[currentSlide];

            if (slideTTSAgent != null && !string.IsNullOrWhiteSpace(slide.content))
            {
                slideTTSAgent.StopSpeaking();
                slideTTSAgent.Speak(slide.content);

                // Wait for audio clip to finish
                if (slideTTSAgent.audioSource != null)
                {
                    yield return new WaitUntil(() => slideTTSAgent.audioSource.isPlaying);
                    yield return new WaitUntil(() => !slideTTSAgent.audioSource.isPlaying);
                }
                else
                {
                    Debug.LogWarning("[SlideShow] No audioSource found in SlideTTSAgent.");
                }
            }

            if (delayAfterSpeaking > 0f)
                yield return new WaitForSeconds(delayAfterSpeaking);

            currentSlide++;
        }

        Debug.Log("[SlideShow] All slides completed.");


        if (objectToDisableAtEnd != null)
        {
            foreach (GameObject obj in objectToDisableAtEnd)
            {
                if (obj != null)
                    obj.SetActive(false);
            }
        }

        if(objectToEnableAtEnd != null)
        {
            foreach (GameObject obj in objectToEnableAtEnd)
            {
                if (obj != null)
                    obj.SetActive(true);
            }
        }
    }

    public void ShowSlide(int index)
    {
        if (index < 0 || index >= slideShowConfig.slides.Length) return;

        currentSlide = index;
        var slide = slideShowConfig.slides[index];

        slideTitle.text = slide.title ?? "";
        slideTranscript.text = slide.content ?? "";

        if (slide.slideImage != null)
        {
            slideImage.sprite = slide.slideImage;
            slideImage.preserveAspect = true;
            slideImage.enabled = true;
        }
        else
        {
            slideImage.enabled = false;
        }

        Debug.Log($"[SlideShow] Showing slide {index + 1}/{slideShowConfig.slides.Length}: {slide.title}");
    }
}


// Manually Slide changes

// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;

// public class SlideShowManager : MonoBehaviour
// {
//     [Header("Config")]
//     public SlideShowConfig slideShowConfig;

//     [Header("UI (World-Space Canvas)")]
//     public Image slideImage;         // assign a UI Image
//     public TMP_Text slideTitle;      // assign TMP text
//     public TMP_Text slideTranscript; // assign TMP text

//     [Header("TTS (Slide-only)")]
//     public SlideTTSAgent slideTTSAgent;   // assign your SlideTTSAgent in Inspector
//     public bool speakOnShow = true;
//     public bool autoAdvanceAfterSpeech = false;
//     public float autoAdvanceDelay = 0.25f;

//     private int currentSlide = 0;
//     private bool waitingAutoAdvance = false;

//     void Start()
//     {
//         if (slideShowConfig == null || slideShowConfig.slides == null || slideShowConfig.slides.Length == 0)
//         {
//             Debug.LogError("[SlideShow] SlideShowConfig is missing or empty.");
//             return;
//         }
//         if (!slideImage || !slideTitle || !slideTranscript)
//         {
//             Debug.LogError("[SlideShow] Assign slideImage, slideTitle, slideTranscript in Inspector.");
//             return;
//         }

//         ShowSlide(0);
//     }

//     public void ShowSlide(int index)
//     {
//         if (slideShowConfig == null || slideShowConfig.slides == null) return;
//         if (index < 0 || index >= slideShowConfig.slides.Length) return;

//         currentSlide = index;
//         var slide = slideShowConfig.slides[index];

//         // Update UI
//         slideTitle.text = slide.title ?? "";
//         slideTranscript.text = slide.content ?? "";

//         if (slide.slideImage != null)
//         {
//             slideImage.sprite = slide.slideImage;
//             slideImage.preserveAspect = true;
//             slideImage.enabled = true;
//         }
//         else
//         {
//             slideImage.enabled = false;
//         }

//         Debug.Log($"[SlideShow] Showing slide {index + 1}/{slideShowConfig.slides.Length}: {slide.title}");

//         // Speak
//         if (speakOnShow && slideTTSAgent != null && !string.IsNullOrWhiteSpace(slide.content))
//         {
//             // stop previous narration if still playing
//             slideTTSAgent.StopSpeaking();
//             slideTTSAgent.Speak(slide.content);

//             if (autoAdvanceAfterSpeech && !waitingAutoAdvance)
//                 StartCoroutine(AutoAdvanceAfterSpeech());
//         }
//     }

//     public void NextSlide() => ShowSlide(currentSlide + 1);
//     public void PreviousSlide() => ShowSlide(currentSlide - 1);

//     private System.Collections.IEnumerator AutoAdvanceAfterSpeech()
//     {
//         waitingAutoAdvance = true;
//         // Wait while speaking
//         while (slideTTSAgent != null && slideTTSAgent.IsSpeaking) yield return null;
//         if (autoAdvanceDelay > 0f) yield return new WaitForSeconds(autoAdvanceDelay);
//         waitingAutoAdvance = false;
//         NextSlide();
//     }

//     // Optional: quick keyboard test in Editor
//     void Update()
//     {
//         if (Input.GetKeyDown(KeyCode.RightArrow)) NextSlide();
//         if (Input.GetKeyDown(KeyCode.LeftArrow))  PreviousSlide();
//     }
// }


// Automatically changes slides after speech
