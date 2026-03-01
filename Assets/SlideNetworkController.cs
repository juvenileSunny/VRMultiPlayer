using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class SlideNetworkController : NetworkBehaviour
{
    public enum LectureState : byte { Stopped, Playing, Paused, Finished }

    [Header("References")]
    public SlideShowManager slideShow;

    [Tooltip("HostAuthority that stores the HostClientId (for QuickJoin host UI visibility + permission)")]
    public HostAuthority host;

    [Header("Auto-play safety")]
    [Tooltip("How long to wait for TTS audio to START after a slide is shown.")]
    public float waitForSpeechStartSeconds = 5f;

    [Tooltip("Minimum time to stay on each slide even if speech fails.")]
    public float minimumSlideDwellSeconds = 1f;

    private NetworkVariable<int> slideIndex =
        new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private NetworkVariable<LectureState> lectureState =
        new NetworkVariable<LectureState>(
            LectureState.Stopped,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private Coroutine serverAutoRoutine;

    public override void OnNetworkSpawn()
    {
        if (slideShow == null)
        {
            Debug.LogError("[SlideNetworkController] slideShow not assigned in Inspector.");
            return;
        }

        // Hook changes
        slideIndex.OnValueChanged += (_, n) => ApplySlide(n);
        lectureState.OnValueChanged += (_, n) => ApplyState(n);

        // Late joiner: apply current state immediately
        ApplySlide(slideIndex.Value);
        ApplyState(lectureState.Value);

        // If server already playing, resume autoplay
        if (IsServer && lectureState.Value == LectureState.Playing)
            StartServerAutoIfNeeded();

        // Debug info (helpful while you wire HostAuthority)
        if (NetworkManager.Singleton != null)
        {
            ulong localId = NetworkManager.Singleton.LocalClientId;
            ulong hostId = (host != null) ? host.HostClientId.Value : 999;
            Debug.Log($"[SlideNetworkController] Spawned. IsServer={IsServer} LocalId={localId} HostId={hostId} SlideCount={slideShow.SlideCount}");
        }
    }

    // ==========================
    // APPLY (runs on all peers)
    // ==========================
    void ApplySlide(int idx)
    {
        if (slideShow == null) return;

        bool speak = (lectureState.Value == LectureState.Playing);
        slideShow.ShowSlide(idx, speak);
    }

    void ApplyState(LectureState state)
    {
        if (slideShow == null) return;

        if (state == LectureState.Paused)
            slideShow.PauseSpeech();
        else if (state == LectureState.Playing)
            slideShow.ResumeSpeech();
        else // Stopped or Finished
            slideShow.StopSpeech();
    }

    // ==========================
    // AUTHORIZATION
    // ==========================
    bool IsAuthorized(ulong senderClientId)
    {
        // If HostAuthority not wired yet, fall back to server
        if (host == null)
            return senderClientId == NetworkManager.ServerClientId;

        return senderClientId == host.HostClientId.Value;
    }

    // ==========================
    // UI BUTTON CALLS (can be called from any peer)
    // ==========================
    public void HostStart()
    {
        if (IsServer) DoStart();
        else StartServerRpc();
    }

    public void HostPause()
    {
        if (IsServer) DoPause();
        else PauseServerRpc();
    }

    public void HostResume()
    {
        if (IsServer) DoResume();
        else ResumeServerRpc();
    }

    public void HostFinish()
    {
        if (IsServer) DoFinish();
        else FinishServerRpc();
    }

    public void HostNext()
    {
        if (IsServer) DoNext();
        else NextServerRpc();
    }

    public void HostPrevious()
    {
        if (IsServer) DoPrevious();
        else PreviousServerRpc();
    }

    // ==========================
    // SERVER RPCs (enforce host permission)
    // ==========================
    [ServerRpc(RequireOwnership = false)]
    void StartServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsAuthorized(rpcParams.Receive.SenderClientId)) return;
        DoStart();
    }

    [ServerRpc(RequireOwnership = false)]
    void PauseServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsAuthorized(rpcParams.Receive.SenderClientId)) return;
        DoPause();
    }

    [ServerRpc(RequireOwnership = false)]
    void ResumeServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsAuthorized(rpcParams.Receive.SenderClientId)) return;
        DoResume();
    }

    [ServerRpc(RequireOwnership = false)]
    void FinishServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsAuthorized(rpcParams.Receive.SenderClientId)) return;
        DoFinish();
    }

    [ServerRpc(RequireOwnership = false)]
    void NextServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsAuthorized(rpcParams.Receive.SenderClientId)) return;
        DoNext();
    }

    [ServerRpc(RequireOwnership = false)]
    void PreviousServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsAuthorized(rpcParams.Receive.SenderClientId)) return;
        DoPrevious();
    }

    // ==========================
    // SERVER LOGIC (ONLY server mutates NetworkVariables)
    // ==========================
    void DoStart()
    {
        if (slideShow == null || slideShow.SlideCount <= 0)
        {
            Debug.LogError("[SlideNetworkController] No slides to play. Check SlideShowConfig.");
            return;
        }

        lectureState.Value = LectureState.Playing;

        // Re-apply current slide so TTS triggers reliably
        ApplySlide(slideIndex.Value);

        StartServerAutoIfNeeded();
    }

    void DoPause()
    {
        lectureState.Value = LectureState.Paused;
        StopServerAuto();
    }

    void DoResume()
    {
        lectureState.Value = LectureState.Playing;

        // Re-trigger speech for current slide
        ApplySlide(slideIndex.Value);

        StartServerAutoIfNeeded();
    }

    void DoFinish()
    {
        lectureState.Value = LectureState.Finished;
        StopServerAuto();

        if (slideShow != null)
            slideShow.StopSpeech();
    }

    void DoNext()
    {
        if (slideShow == null || slideShow.SlideCount <= 0) return;

        StopServerAuto();

        int next = Mathf.Clamp(slideIndex.Value + 1, 0, slideShow.SlideCount - 1);
        slideIndex.Value = next;

        if (lectureState.Value == LectureState.Playing)
            StartServerAutoIfNeeded();
    }

    void DoPrevious()
    {
        if (slideShow == null || slideShow.SlideCount <= 0) return;

        StopServerAuto();

        int prev = Mathf.Clamp(slideIndex.Value - 1, 0, slideShow.SlideCount - 1);
        slideIndex.Value = prev;

        if (lectureState.Value == LectureState.Playing)
            StartServerAutoIfNeeded();
    }

    // ==========================
    // AUTOPLAY (SERVER ONLY)
    // ==========================
    void StartServerAutoIfNeeded()
    {
        if (!IsServer) return;
        if (serverAutoRoutine != null) return;

        // Only autoplay while playing
        if (lectureState.Value != LectureState.Playing) return;

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

            // 1) Wait for speech to START (prevents instant skipping if TTS fails)
            bool started = false;
            float t0 = Time.time;

            while (Time.time - t0 < waitForSpeechStartSeconds)
            {
                if (lectureState.Value != LectureState.Playing) break;

                if (slideShow != null && slideShow.IsSpeaking)
                {
                    started = true;
                    break;
                }
                yield return null;
            }

            // If speech didn't start, PAUSE (don’t fast-skip)
            if (!started)
            {
                Debug.LogWarning("[SlideNetworkController] Speech did NOT start on server. Autoplay paused to prevent fast skipping. Fix TTS/audio on host.");
                lectureState.Value = LectureState.Paused;
                serverAutoRoutine = null;
                yield break;
            }

            // 2) Wait until speech finishes
            while (lectureState.Value == LectureState.Playing && slideShow != null && slideShow.IsSpeaking)
                yield return null;

            // Minimum dwell safeguard
            float elapsed = Time.time - dwellStart;
            if (elapsed < minimumSlideDwellSeconds)
                yield return new WaitForSeconds(minimumSlideDwellSeconds - elapsed);

            if (lectureState.Value != LectureState.Playing)
                break;

            // 3) Advance or finish
            if (slideShow == null || slideShow.SlideCount <= 0)
            {
                lectureState.Value = LectureState.Finished;
                break;
            }

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