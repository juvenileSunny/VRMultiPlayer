using Unity.Netcode;
using UnityEngine;
using XRMultiplayer.MiniGames;
using System.Collections.Generic;

public class LectureSessionNetworkController : NetworkBehaviour
{
    [Header("References")]
    public LectureSessionManager lectureSession;
    public SlideTTSAgent slideTTSAgent;

    [Header("Objects To Deactivate On Finish")]
    public List<GameObject> objectsToDisableOnFinish = new();

    [Header("Objects To Activate For Next MiniGame")]
    public List<GameObject> objectsToEnableOnFinish = new();

    /* =========================
     * CALLED BY HOST BUTTON
     * ========================= */
    public void RequestFinishSession()
    {
        if (!IsServer)
            RequestFinishSessionServerRpc();
        else
            FinishSession();
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestFinishSessionServerRpc()
    {
        FinishSession();
    }

    /* =========================
     * SERVER LOGIC
     * ========================= */
    void FinishSession()
    {
        // Stop TTS on server
        if (slideTTSAgent != null)
            slideTTSAgent.StopSpeaking();

        // Deactivate lecture objects
        foreach (var obj in objectsToDisableOnFinish)
            if (obj != null)
                obj.SetActive(false);

        // Activate next minigame objects
        foreach (var obj in objectsToEnableOnFinish)
            if (obj != null)
                obj.SetActive(true);

        // Finish lecture logic
        if (lectureSession != null)
            lectureSession.FinishGame();

        // Sync to all clients
        FinishSessionClientRpc();
    }

    /* =========================
     * CLIENT SIDE
     * ========================= */
    [ClientRpc]
    void FinishSessionClientRpc()
    {
        if (slideTTSAgent != null)
            slideTTSAgent.StopSpeaking();

        foreach (var obj in objectsToDisableOnFinish)
            if (obj != null)
                obj.SetActive(false);

        foreach (var obj in objectsToEnableOnFinish)
            if (obj != null)
                obj.SetActive(true);

        if (lectureSession != null)
            lectureSession.FinishGame();
    }
}
