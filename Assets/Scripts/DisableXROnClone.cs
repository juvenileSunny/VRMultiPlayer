using UnityEngine;
using UnityEngine.XR.Management;
using System.Collections;

#if UNITY_EDITOR
using ParrelSync;
#endif

/// <summary>
/// Disables XR when running in a ParrelSync clone to prevent VR conflicts
/// Attach this to a GameObject in your startup scene
/// </summary>
public class DisableXROnClone : MonoBehaviour
{
    void Awake()
    {
#if UNITY_EDITOR
        // Check if this is a ParrelSync clone
        if (ClonesManager.IsClone())
        {
            Debug.Log("Running in ParrelSync clone - Disabling XR");
            StartCoroutine(DisableXR());
        }
        else
        {
            Debug.Log("Running in main editor - XR enabled");
        }
#endif
    }

    IEnumerator DisableXR()
    {
        // Stop XR if it's already initialized
        if (XRGeneralSettings.Instance != null && XRGeneralSettings.Instance.Manager != null)
        {
            Debug.Log("Stopping XR Manager...");
            XRGeneralSettings.Instance.Manager.StopSubsystems();
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();
            Debug.Log("XR Disabled successfully");
        }
        
        yield return null;
    }
}
