using Unity.Netcode;
using UnityEngine;

public class LecturePlaybackController : NetworkBehaviour
{
    public SlideShowManager slideShow;
    public SlideTTSAgent slideTTS;

    public enum LectureState
    {
        Stopped,
        Playing,
        Paused,
        Finished
    }

    private NetworkVariable<LectureState> lectureState =
        new NetworkVariable<LectureState>(
            LectureState.Stopped,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public override void OnNetworkSpawn()
    {
        lectureState.OnValueChanged += OnLectureStateChanged;
        OnLectureStateChanged(LectureState.Stopped, lectureState.Value);
    }

    /* =========================
     * HOST BUTTONS
     * ========================= */

    public void Play()
    {
        if (!IsServer) return;
        lectureState.Value = LectureState.Playing;
    }

    public void Pause()
    {
        if (!IsServer) return;
        lectureState.Value = LectureState.Paused;
    }

    public void Finish()
    {
        if (!IsServer) return;
        lectureState.Value = LectureState.Finished;
    }

    /* =========================
     * STATE SYNC
     * ========================= */

    void OnLectureStateChanged(LectureState oldState, LectureState newState)
    {
        switch (newState)
        {
            case LectureState.Playing:
                ResumeLecture();
                break;

            case LectureState.Paused:
                PauseLecture();
                break;

            case LectureState.Finished:
                FinishLecture();
                break;
        }
    }

    void ResumeLecture()
    {
        if (slideTTS != null && slideTTS.audioSource != null)
        {
            slideTTS.audioSource.UnPause();
        }
    }

    void PauseLecture()
    {
        if (slideTTS != null && slideTTS.audioSource != null)
        {
            slideTTS.audioSource.Pause();
        }
    }

    void FinishLecture()
    {
        if (slideTTS != null)
            slideTTS.StopSpeaking();

        Debug.Log("Lecture finished by host.");
    }
}