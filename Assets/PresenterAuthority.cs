using Unity.Netcode;
using UnityEngine;

public class PresenterAuthority : NetworkBehaviour
{
    // Who is allowed to control the lecture
    public NetworkVariable<ulong> PresenterClientId =
        new NetworkVariable<ulong>(ulong.MaxValue,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    public bool IsLocalPresenter =>
        NetworkManager.Singleton != null &&
        PresenterClientId.Value == NetworkManager.Singleton.LocalClientId;

    public override void OnNetworkSpawn()
    {
        // Optional: if server wants to auto-assign presenter to itself when hosting
        if (IsServer && PresenterClientId.Value == ulong.MaxValue)
        {
            // leave unassigned by default, OR uncomment next line to auto-assign server:
            // PresenterClientId.Value = NetworkManager.Singleton.LocalClientId;
        }
    }

    public void TryClaimPresenter()
    {
        if (!IsSpawned || NetworkManager.Singleton == null) return;
        ClaimPresenterServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ClaimPresenterServerRpc(ulong requesterClientId)
    {
        // If nobody is presenter, first claimer wins
        if (PresenterClientId.Value == ulong.MaxValue)
        {
            PresenterClientId.Value = requesterClientId;
            Debug.Log($"[PresenterAuthority] Presenter assigned to ClientId={requesterClientId}");
        }
        else
        {
            Debug.Log($"[PresenterAuthority] Presenter already assigned to ClientId={PresenterClientId.Value}");
        }
    }

    public void ReleasePresenter()
    {
        if (!IsSpawned || NetworkManager.Singleton == null) return;
        ReleasePresenterServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReleasePresenterServerRpc(ulong requesterClientId)
    {
        if (PresenterClientId.Value == requesterClientId)
        {
            PresenterClientId.Value = ulong.MaxValue;
            Debug.Log("[PresenterAuthority] Presenter released.");
        }
    }
} 