using UnityEngine;
using UnityEngine.InputSystem;

public class ControllerButtonAction : MonoBehaviour
{
    public InputActionProperty actionXButton; 
    public InputActionProperty actionYButton; // assign from inspector
    public GameObject objectXToActivate;       // optional
    public GameObject objectYToActivate;       // optional

    void Update()
    {
        if (actionXButton.action.WasPressedThisFrame())
        {
            if (objectXToActivate != null)
                objectXToActivate.SetActive(!objectXToActivate.activeSelf);
        }

        if (actionYButton.action.WasPressedThisFrame())
        {
            if (objectYToActivate != null)
                objectYToActivate.SetActive(!objectYToActivate.activeSelf);
        }
    }

    private void OnEnable()
    {
        actionXButton.action.Enable();
        actionYButton.action.Enable();
    }

    private void OnDisable()
    {
        actionXButton.action.Disable();
        actionYButton.action.Disable();
    }
}
