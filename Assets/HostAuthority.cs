using Unity.Netcode;
using UnityEngine;

public class HostAuthority : NetworkBehaviour
{
    public NetworkVariable<ulong> HostClientId =
        new NetworkVariable<ulong>(
            ulong.MaxValue,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public bool IsLocalHost
    {
        get
        {
            if (NetworkManager.Singleton == null) return false;
            return HostClientId.Value == NetworkManager.Singleton.LocalClientId;
        }
    }

    public override void OnNetworkSpawn()
    {
        // If server starts and host isn't assigned yet, auto-assign host to server's local client id
        if (IsServer && HostClientId.Value == ulong.MaxValue)
        {
            HostClientId.Value = NetworkManager.Singleton.LocalClientId;
            Debug.Log($"[HostAuthority] Host assigned to ClientId={HostClientId.Value}");
        }
    }

    // Optional: let someone claim host role (if you want a button to do it)
    public void TryClaimHost()
    {
        if (NetworkManager.Singleton == null) return;
        ClaimHostServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ClaimHostServerRpc(ulong requesterClientId)
    {
        // If you want "first claim wins", keep this condition:
        if (HostClientId.Value == ulong.MaxValue)
        {
            HostClientId.Value = requesterClientId;
            Debug.Log($"[HostAuthority] Host claimed by ClientId={requesterClientId}");
        }

        // If you want "server can reassign anytime", replace above with:
        // HostClientId.Value = requesterClientId;
    }

    // Optional: release host
    public void ReleaseHost()
    {
        if (NetworkManager.Singleton == null) return;
        ReleaseHostServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReleaseHostServerRpc(ulong requesterClientId)
    {
        if (HostClientId.Value == requesterClientId)
        {
            HostClientId.Value = ulong.MaxValue;
            Debug.Log("[HostAuthority] Host released.");
        }
    }
}
