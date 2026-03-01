using UnityEngine;
using UnityEngine.UI;

public class HostLectureUIBinder : MonoBehaviour
{
    public SlideNetworkController controller;

    [Header("Buttons")]
    public Button startBtn;
    public Button pauseBtn;
    public Button resumeBtn;
    public Button nextBtn;
    public Button prevBtn;
    public Button finishBtn;

    void Awake()
    {
        if (startBtn)  startBtn.onClick.AddListener(() => controller.HostStart());
        if (pauseBtn)  pauseBtn.onClick.AddListener(() => controller.HostPause());
        if (resumeBtn) resumeBtn.onClick.AddListener(() => controller.HostResume());
        if (nextBtn)   nextBtn.onClick.AddListener(() => controller.HostNext());
        if (prevBtn)   prevBtn.onClick.AddListener(() => controller.HostPrevious());
        if (finishBtn) finishBtn.onClick.AddListener(() => controller.HostFinish());
    }
}