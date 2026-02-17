using Unity.Netcode;
using UnityEngine;

public class SlideNetworkController : NetworkBehaviour
{
    [Header("References")]
    public SlideShowManager slideShow;
    public SlideTTSAgent slideTTS;

    private NetworkVariable<int> currentSlide =
        new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public override void OnNetworkSpawn()
    {
        currentSlide.OnValueChanged += OnSlideChanged;

        // Apply current slide for late joiners
        ApplySlide(currentSlide.Value);
    }

    private void OnSlideChanged(int oldValue, int newValue)
    {
        ApplySlide(newValue);
    }

    private void ApplySlide(int index)
    {
        if (slideShow == null) return;
        if (slideShow.slideShowConfig == null) return;
        if (slideShow.slideShowConfig.slides == null) return;
        if (index < 0 || index >= slideShow.slideShowConfig.slides.Length) return;

        slideShow.ShowSlide(index);

        if (slideTTS != null)
        {
            string text = slideShow.slideShowConfig.slides[index].content;
            slideTTS.Speak(text);
        }
    }

    public void NextSlide()
    {
        if (!IsServer) return;

        if (currentSlide.Value < slideShow.slideShowConfig.slides.Length - 1)
            currentSlide.Value++;

        // Debug.Log("NextSlide pressed. IsServer = " + IsServer);

        // currentSlide.Value++;
    }

    public void PreviousSlide()
    {
        if (!IsServer) return;

        if (currentSlide.Value > 0)
            currentSlide.Value--;
    }
}
