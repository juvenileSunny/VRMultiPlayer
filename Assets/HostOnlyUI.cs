using System.Collections.Generic;
using System.Collections;
using UnityEngine;
// using System.Diagnostics;
// using System.Diagnostics;

public class HostOnlyUI : MonoBehaviour
{
    public HostAuthority hostAuthority;

    IEnumerator Start()
    {
        // Wait for host authority to spawn / be ready
        while (hostAuthority == null || !hostAuthority.IsSpawned)
        {
            // Debug.Log($"[HostOnlyUI] Waiting for HostAuthority... (current={hostAuthority}, spawned={hostAuthority?.IsSpawned})");
            yield return null;
        }
            

        // Update immediately and then whenever host changes
        UpdateVisibility();
        hostAuthority.HostClientId.OnValueChanged += (_, __) => UpdateVisibility();
    }

    void UpdateVisibility()
    {
        bool show = hostAuthority != null && hostAuthority.IsLocalHost;
        gameObject.SetActive(show);
    }
}