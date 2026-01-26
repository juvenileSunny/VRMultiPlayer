using UnityEngine;
using UnityEngine.InputSystem; // Required for new Input System

public class ControllerUIInput : MonoBehaviour
{
    public GameObject panelA; // Assign in Inspector
    public GameObject panelB; // Assign in Inspector

    // Reference to your Input Action Asset
    public InputActionReference togglePanelAAction;
    public InputActionReference togglePanelBAction;

    private void OnEnable()
    {
        // Enable the input actions
        togglePanelAAction.action.Enable();
        togglePanelBAction.action.Enable();

        // Subscribe to button press events
        togglePanelAAction.action.performed += ctx => TogglePanel(panelA);
        togglePanelBAction.action.performed += ctx => TogglePanel(panelB);
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        togglePanelAAction.action.performed -= ctx => TogglePanel(panelA);
        togglePanelBAction.action.performed -= ctx => TogglePanel(panelB);
    }

    private void TogglePanel(GameObject panel)
    {
        panel.SetActive(!panel.activeSelf);
    }
}