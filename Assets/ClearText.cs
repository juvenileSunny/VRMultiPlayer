using TMPro;
using UnityEngine;

public class ClearText : MonoBehaviour
{
    public TMP_InputField textMeshPro;

    public void ClearTextMesh()
    {
        textMeshPro.text = string.Empty;
    }
}