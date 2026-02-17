using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SlideShowManager : MonoBehaviour
{
    [Header("Config")]
    public SlideShowConfig slideShowConfig;

    [Header("UI")]
    public Image slideImage;
    public TMP_Text slideTitle;
    public TMP_Text slideTranscript;

    [Header("TTS")]
    public SlideTTSAgent slideTTSAgent;
    public bool speakOnShow = true;

    private int currentSlide = 0;

    public void ShowSlide(int index)
    {
        if (slideShowConfig == null ||
            slideShowConfig.slides == null ||
            index < 0 ||
            index >= slideShowConfig.slides.Length)
            return;

        currentSlide = index;

        var slide = slideShowConfig.slides[index];

        // Update UI
        slideTitle.text = slide.title ?? "";
        slideTranscript.text = slide.content ?? "";

        if (slide.slideImage != null)
        {
            slideImage.sprite = slide.slideImage;
            slideImage.enabled = true;
            slideImage.preserveAspect = true;
        }
        else
        {
            slideImage.enabled = false;
        }

        // Handle speech
        if (slideTTSAgent != null)
        {
            slideTTSAgent.StopSpeaking();

            if (speakOnShow && !string.IsNullOrWhiteSpace(slide.content))
            {
                slideTTSAgent.Speak(slide.content);
            }
        }
    }

    public void NextSlide()
    {
        ShowSlide(currentSlide + 1);
    }

    public void PreviousSlide()
    {
        ShowSlide(currentSlide - 1);
    }
}
