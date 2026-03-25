using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;




public class StickCheck : MonoBehaviour
{
    private Image indicator = null;

    Vector2 value;

    [SerializeField]
    float maxVal = 10f;

    List<UnityEngine.XR.InputDevice> devices;

    [SerializeField]
    public InputDeviceCharacteristics controllerCharacteristics;

    // Start is called before the first frame update
    void Start()
    {
        indicator = this.GetComponent<Image>();

        devices = new List<UnityEngine.XR.InputDevice>();

        InputDevices.GetDevicesWithCharacteristics(controllerCharacteristics, devices);

    }

    void StateUpdate()
    {
        devices[0].TryGetFeatureValue(CommonUsages.primary2DAxis, out value);
        indicator.rectTransform.anchoredPosition = value*maxVal;

    }
    // Update is called once per frame
    void Update()
    {
        StateUpdate();

    }
}