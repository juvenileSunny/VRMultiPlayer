using UnityEngine;
using Unity.Netcode;
using XRMultiplayer.MiniGames;

public class HostFinishMinigameButton : MonoBehaviour
{
    public MiniGameManager miniGameManager;

    // Call this from your Finish button too (or from another button)
    public void HostFinishSession()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

        if (miniGameManager != null)
            miniGameManager.StopGameServerRpc();
    }
}