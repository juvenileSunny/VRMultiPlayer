using UnityEngine;
using Unity.Netcode;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public class ClientOnlyTeleportAnchor : NetworkBehaviour
{
    [Header("Optional extra visuals")]
    public GameObject[] extraVisuals;

    private TeleportationAnchor anchor;
    private Collider[] allColliders;
    private Renderer[] allRenderers;

    private void Awake()
    {
        anchor = GetComponent<TeleportationAnchor>();
        allColliders = GetComponentsInChildren<Collider>(true);
        allRenderers = GetComponentsInChildren<Renderer>(true);
    }

    public override void OnNetworkSpawn()
    {
        ApplyVisibilityAndAccess();
    }

    private void ApplyVisibilityAndAccess()
    {
        if (NetworkManager.Singleton == null)
            return;

        bool allow = NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost;

        if (anchor != null)
            anchor.enabled = allow;

        foreach (var col in allColliders)
            if (col != null) col.enabled = allow;

        foreach (var rend in allRenderers)
            if (rend != null) rend.enabled = allow;

        if (extraVisuals != null)
        {
            foreach (var obj in extraVisuals)
                if (obj != null) obj.SetActive(allow);
        }
    }
}