using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class SlideNetworkController : NetworkBehaviour
{
    public enum LectureState : byte { Stopped, Playing, Paused, Finished }

    [Header("References")]
    public SlideShowManager slideShow;

    [Header("Host Authority (optional)")]
    public HostAuthority host; // if you use HostAuthority-based UI visibility

    [Header("Auto-play safety (server only)")]
    [Tooltip("How long to wait for host TTS audio to START after a slide is shown.")]
    public float waitForSpeechStartSeconds = 5f;

    [Tooltip("Minimum time to stay on each slide even if speech ends instantly.")]
    public float minimumSlideDwellSeconds = 1f;

    private readonly NetworkVariable<int> slideIndex =
        new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<LectureState> lectureState =
        new NetworkVariable<LectureState>(LectureState.Stopped, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private Coroutine serverAutoRoutine;

    public override void OnNetworkSpawn()
    {
        if (slideShow == null)
        {
            Debug.LogError("[SlideNetworkController] slideShow not assigned in Inspector.");
            return;
        }

        // Subscribe first
        slideIndex.OnValueChanged += OnSlideIndexChanged;
        lectureState.OnValueChanged += OnLectureStateChanged;

        // Then apply current state for late joiners (and host)
        StartCoroutine(ApplyInitialStateNextFrame());

        if (IsServer && lectureState.Value == LectureState.Playing)
            StartServerAutoIfNeeded();
    }

    public override void OnNetworkDespawn()
    {
        slideIndex.OnValueChanged -= OnSlideIndexChanged;
        lectureState.OnValueChanged -= OnLectureStateChanged;

        StopServerAuto();
        base.OnNetworkDespawn();
    }

    IEnumerator ApplyInitialStateNextFrame()
    {
        // IMPORTANT for HMD builds: UI refs sometimes initialize a frame later
        yield return null;

        ApplySlide(slideIndex.Value);
        ApplyState(lectureState.Value);
    }

    void OnSlideIndexChanged(int oldValue, int newValue) => ApplySlide(newValue);
    void OnLectureStateChanged(LectureState oldValue, LectureState newValue) => ApplyState(newValue);

    void ApplySlide(int idx)
    {
        if (slideShow == null) return;
        if (slideShow.SlideCount <= 0) return;

        idx = Mathf.Clamp(idx, 0, slideShow.SlideCount - 1);

        bool speak = (lectureState.Value == LectureState.Playing);

        // Every device updates its own UI locally from the same index
        slideShow.ShowSlide(idx, speak);

        Debug.Log($"[SlideNetworkController] ApplySlide idx={idx} " +
                  $"LocalClientId={NetworkManager.Singleton.LocalClientId} IsServer={IsServer} IsHost={NetworkManager.Singleton.IsHost}");
    }

    void ApplyState(LectureState state)
    {
        if (slideShow == null) return;

        switch (state)
        {
            case LectureState.Playing:
                // If resumed mid-slide, resume audio
                slideShow.ResumeSpeech();
                break;

            case LectureState.Paused:
                slideShow.PauseSpeech();
                break;

            default: // Stopped or Finished
                slideShow.StopSpeech();
                break;
        }

        Debug.Log($"[SlideNetworkController] ApplyState {state} LocalClientId={NetworkManager.Singleton.LocalClientId}");
    }

    // =========================================================
    // PUBLIC BUTTON CALLS (work from host UI)
    // These call server even if local player isn't "IsServer"
    // =========================================================

    public void HostStart()
    {
        if (!CanLocalControl()) return;
        RequestStartServerRpc();
    }

    public void HostPause()
    {
        if (!CanLocalControl()) return;
        RequestPauseServerRpc();
    }

    public void HostResume()
    {
        if (!CanLocalControl()) return;
        RequestResumeServerRpc();
    }

    public void HostFinish()
    {
        if (!CanLocalControl()) return;
        RequestFinishServerRpc();
    }

    public void HostNext()
    {
        if (!CanLocalControl()) return;
        RequestNextServerRpc();
    }

    public void HostPrevious()
    {
        if (!CanLocalControl()) return;
        RequestPrevServerRpc();
    }

    bool CanLocalControl()
    {
        // If you use HostAuthority, prefer it.
        // Otherwise fallback to NGO host/server.
        if (host != null)
            return host.IsLocalHost;

        return NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
    }

    // ======================
    // SERVER RPCs
    // ======================

    [ServerRpc(RequireOwnership = false)]
    void RequestStartServerRpc()
    {
        lectureState.Value = LectureState.Playing;

        // re-trigger speaking on all devices by "touching" current slide index
        // easiest is to reassign same value (NGO won't fire OnValueChanged if same)
        // so we explicitly ApplySlide on server AND clients will already be at same index.
        // But for clients to re-speak, SlideShowManager already speaks on ShowSlide.
        // We can force a refresh by bumping and returning if >1 slides, otherwise just ApplySlide.
        ApplySlide(slideIndex.Value);

        StartServerAutoIfNeeded();
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestPauseServerRpc()
    {
        lectureState.Value = LectureState.Paused;
        StopServerAuto();
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestResumeServerRpc()
    {
        lectureState.Value = LectureState.Playing;

        // re-trigger speech on current slide (important if audio got stopped)
        ApplySlide(slideIndex.Value);

        StartServerAutoIfNeeded();
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestFinishServerRpc()
    {
        lectureState.Value = LectureState.Finished;
        StopServerAuto();
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestNextServerRpc()
    {
        StopServerAuto();

        if (slideShow == null || slideShow.SlideCount <= 0) return;

        int next = Mathf.Clamp(slideIndex.Value + 1, 0, slideShow.SlideCount - 1);
        slideIndex.Value = next;

        if (lectureState.Value == LectureState.Playing)
            StartServerAutoIfNeeded();
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestPrevServerRpc()
    {
        StopServerAuto();

        if (slideShow == null || slideShow.SlideCount <= 0) return;

        int prev = Mathf.Clamp(slideIndex.Value - 1, 0, slideShow.SlideCount - 1);
        slideIndex.Value = prev;

        if (lectureState.Value == LectureState.Playing)
            StartServerAutoIfNeeded();
    }

    // ==========================
    // Auto-play (SERVER ONLY)
    // ==========================

    void StartServerAutoIfNeeded()
    {
        if (!IsServer) return;
        if (serverAutoRoutine != null) return;
        serverAutoRoutine = StartCoroutine(ServerAutoPlay());
    }

    void StopServerAuto()
    {
        if (serverAutoRoutine != null)
        {
            StopCoroutine(serverAutoRoutine);
            serverAutoRoutine = null;
        }
    }

    IEnumerator ServerAutoPlay()
    {
        while (IsServer && lectureState.Value == LectureState.Playing)
        {
            float dwellStart = Time.time;

            // Wait for host speech to START, otherwise pause autoplay to prevent fast skipping.
            bool started = false;
            float t0 = Time.time;
            while (Time.time - t0 < waitForSpeechStartSeconds)
            {
                if (lectureState.Value != LectureState.Playing) break;

                // IMPORTANT: this checks host's audio only (server side)
                if (slideShow != null && slideShow.IsSpeaking)
                {
                    started = true;
                    break;
                }
                yield return null;
            }

            if (!started)
            {
                Debug.LogWarning("[SlideNetworkController] Host speech did NOT start. Autoplay paused to prevent skipping. Fix host TTS/audio.");
                lectureState.Value = LectureState.Paused;
                serverAutoRoutine = null;
                yield break;
            }

            // Wait until host finishes speaking
            while (lectureState.Value == LectureState.Playing && slideShow != null && slideShow.IsSpeaking)
                yield return null;

            // Minimum dwell
            float elapsed = Time.time - dwellStart;
            if (elapsed < minimumSlideDwellSeconds)
                yield return new WaitForSeconds(minimumSlideDwellSeconds - elapsed);

            if (lectureState.Value != LectureState.Playing)
                break;

            // Advance or finish
            if (slideShow == null || slideShow.SlideCount <= 0) break;

            if (slideIndex.Value >= slideShow.SlideCount - 1)
            {
                lectureState.Value = LectureState.Finished;
                break;
            }

            slideIndex.Value += 1;
            yield return null;
        }

        serverAutoRoutine = null;
    }
}