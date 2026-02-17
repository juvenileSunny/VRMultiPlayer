using Unity.Netcode;
using UnityEngine;

public class HostOnlyUI : MonoBehaviour
{
    void OnEnable()
    {
        if (NetworkManager.Singleton == null)
            return;

        NetworkManager.Singleton.OnServerStarted += CheckRole;
    }

    void CheckRole()
    {
        if (!NetworkManager.Singleton.IsServer)
            gameObject.SetActive(false);
    }
}
