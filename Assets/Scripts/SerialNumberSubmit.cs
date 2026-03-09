using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SerialNumberSubmit : MonoBehaviour
{
    public TMP_InputField serialNumberInput;
    public Button submitButton;
    public string nextSceneName;

    private void Start()
    {
        submitButton.onClick.AddListener(SubmitSerialNumber);
    }

    private void SubmitSerialNumber()
    {
        string serialNumber = serialNumberInput.text;
        DataEcho.SessionCollector.SetInstanceSerialNumber(serialNumber);
        Debug.Log("Saved Serial Number: " + serialNumber);
        DataEcho.SessionCollector.Instance.loggingData = true;
        Debug.Log("Started Logging Data with Serial Number: " + serialNumber);
        // change the scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
    }
}