using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScrollTextToTMPInputField : MonoBehaviour
{
    [Header("Source Text (UI.Text from Scroll View Content)")]
    public Text scrollViewText;  // Regular Unity UI Text

    [Header("Target TMP_InputField")]
    public TMP_InputField inputField;  // TextMeshPro Input Field

    public void CopyTextToInputField()
    {
        if (scrollViewText != null && inputField != null)
        {
            inputField.text = scrollViewText.text;
        }
        else
        {
            Debug.LogWarning("Missing reference(s): Make sure both scrollViewText and inputField are assigned.");
        }
    }
}
