#if UNITY_EDITOR || UNITY_STANDALONE

using UnityEngine;
using UnityEngine.XR;

public class HideVisualsInSimulatorOrPC : MonoBehaviour
{
    [Header("Objects to hide in simulator / PC mode")]
    public GameObject[] visualsToHide;

    [Header("Optional simulator object")]
    public GameObject xrDeviceSimulatorObject;

    [Header("Debug")]
    public bool forcePCMode = false;
    public bool forceVRMode = false;

    void Start()
    {
        bool isVR;

        if (forcePCMode)
        {
            isVR = false;
        }
        else if (forceVRMode)
        {
            isVR = true;
        }
        else
        {
            bool simulatorActive =
                xrDeviceSimulatorObject != null &&
                xrDeviceSimulatorObject.activeInHierarchy;

            bool headsetActive = XRSettings.isDeviceActive;

            // Treat as VR only when an actual HMD is active
            isVR = headsetActive && !simulatorActive;
        }

        ApplyVisualMode(isVR);
    }

    void ApplyVisualMode(bool isVR)
    {
        bool hide = !isVR;

        if (visualsToHide != null)
        {
            foreach (var obj in visualsToHide)
            {
                if (obj != null)
                    obj.SetActive(!hide);
            }
        }

        Debug.Log("HideVisualsInSimulatorOrPC: " + (isVR ? "VR visuals ON" : "Simulator/PC visuals OFF"));
    }
}

#endif