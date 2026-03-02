using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MinigameFinishObjectToggler : NetworkBehaviour
{
    [Header("When minigame finishes...")]
    public List<GameObject> activateOnFinish = new();
    public List<GameObject> deactivateOnFinish = new();

    [Header("Optional")]
    public bool alsoRunOnServerImmediately = true;

    // Call this from the host UI Finish button
    public void RequestFinishToggles()
    {
        if (IsServer)
        {
            ApplyFinishTogglesServer();
        }
        else
        {
            RequestFinishTogglesServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestFinishTogglesServerRpc()
    {
        ApplyFinishTogglesServer();
    }

    void ApplyFinishTogglesServer()
    {
        if (alsoRunOnServerImmediately)
            ApplyTogglesLocal();

        ApplyFinishTogglesClientRpc();
    }

    [ClientRpc]
    void ApplyFinishTogglesClientRpc()
    {
        // Runs on every client (including host client)
        ApplyTogglesLocal();
    }

    void ApplyTogglesLocal()
    {
        foreach (var obj in deactivateOnFinish)
            if (obj != null) obj.SetActive(false);

        foreach (var obj in activateOnFinish)
            if (obj != null) obj.SetActive(true);
    }
}