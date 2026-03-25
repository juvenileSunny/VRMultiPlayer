using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
#if USE_INTERACTION_TOOLKIT
using UnityEngine.XR.Interaction.Toolkit;
#elif USE_INTERACTION_TOOLKIT_2
using Unity.XR.CoreUtils;
#endif

[InitializeOnLoad]
public static class VRSimulatorPrefabEditorMenu
{
    static string packageName = "com.thrashpanda.vr.simulator";

    [MenuItem("GameObject/XR/VR Simulator"), MenuItem("VRSimulator/Create/VR Simulator")]
    public static void InstantiateVRSimulatorInScene()
    {
        Object prefab = AssetDatabase.LoadAssetAtPath("Packages/" + packageName + "/Runtime/Assets/Prefab/VRSimulator.prefab", typeof(GameObject));
        GameObject simulator = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
#if USE_INTERACTION_TOOLKIT
        Debug.Log("Unity Interaction Toolkit <2 detected, trying to find XRRig in Scene.");
        XRRig xRRig;
        if (xRRig = GameObject.FindObjectOfType<XRRig>())
            simulator.GetComponent<VRSimulator>().xRRigToFollow = xRRig.transform;
#elif USE_INTERACTION_TOOLKIT_2
        Debug.Log("Unity Interaction Toolkit >2 detected, trying to find XROrigin in Scene.");
        XROrigin xRRig;
        if (xRRig = GameObject.FindObjectOfType<XROrigin>())
            simulator.GetComponent<VRSimulator>().xRRigToFollow = xRRig.transform;
#endif
    }
}