using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SerialNumberSubmit : MonoBehaviour
{
    public TMP_InputField serialNumberInput;
    public Button submitButton;
    public Button skipTutorialButton;
    public string nextSceneName;
    public string lectureSceneName;

    private void Start()
    {
        submitButton.onClick.AddListener(SubmitSerialNumber);
        skipTutorialButton.onClick.AddListener(SkipTutorial);
    }

    private void SubmitSerialNumber()
    {
        string serialNumber = serialNumberInput.text;
        if (string.IsNullOrEmpty(serialNumber))
        {
            Debug.LogWarning("Serial Number cannot be empty.");
            return;
        }
        DataEcho.SessionCollector.SetInstanceSerialNumber(serialNumber);
        Debug.Log("Saved Serial Number: " + serialNumber);
        DataEcho.SessionCollector.Instance.loggingData = true;
        Debug.Log("Started Logging Data with Serial Number: " + serialNumber);
        // change the scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
    }

    private void SkipTutorial()
    {
        string serialNumber = serialNumberInput.text;
        if (string.IsNullOrEmpty(serialNumber))
        {
            Debug.LogWarning("Serial Number cannot be empty.");
            return;
        }
        DataEcho.SessionCollector.SetInstanceSerialNumber(serialNumber);
        Debug.Log("Saved Serial Number: " + serialNumber);
        DataEcho.SessionCollector.Instance.loggingData = true;
        Debug.Log("Started Logging Data with Serial Number and Tutorial Skipped: " + serialNumber);
        // change the scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(lectureSceneName);
    }
}