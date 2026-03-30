using UnityEngine;

public class HideVisualsOnPC : MonoBehaviour
{
    public GameObject[] visualsToHide;

    void Start()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        SetVisuals(false);
#else
        SetVisuals(true);
#endif
    }

    void SetVisuals(bool show)
    {
        if (visualsToHide == null) return;

        foreach (var obj in visualsToHide)
        {
            if (obj != null)
                obj.SetActive(show);
        }
    }
}