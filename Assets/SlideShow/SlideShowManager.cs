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

    public int CurrentSlideIndex { get; private set; } = 0;

    public int SlideCount =>
        (slideShowConfig != null && slideShowConfig.slides != null)
            ? slideShowConfig.slides.Length
            : 0;

    public void ShowSlide(int index, bool speak)
    {
        if (SlideCount <= 0) return;

        index = Mathf.Clamp(index, 0, SlideCount - 1);
        CurrentSlideIndex = index;

        var slide = slideShowConfig.slides[index];

        if (slideTitle) slideTitle.text = slide.title ?? "";
        if (slideTranscript) slideTranscript.text = slide.content ?? "";

        if (slideImage)
        {
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
        }

        if (slideTTSAgent != null)
        {
            slideTTSAgent.StopSpeaking();

            if (speak && !string.IsNullOrWhiteSpace(slide.content))
                slideTTSAgent.Speak(slide.content);
        }
    }

    public string GetSlideText(int index)
    {
        if (SlideCount <= 0) return "";
        index = Mathf.Clamp(index, 0, SlideCount - 1);
        return slideShowConfig.slides[index].content ?? "";
    }

    public bool IsSpeaking => slideTTSAgent != null && slideTTSAgent.IsSpeaking;

    public void PauseSpeech()
    {
        if (slideTTSAgent != null) slideTTSAgent.PauseSpeaking();
    }

    public void ResumeSpeech()
    {
        if (slideTTSAgent != null) slideTTSAgent.ResumeSpeaking();
    }

    public void StopSpeech()
    {
        if (slideTTSAgent != null) slideTTSAgent.StopSpeaking();
    }
}