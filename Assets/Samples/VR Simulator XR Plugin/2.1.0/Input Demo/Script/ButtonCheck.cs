#if (UNITY_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM)
#define USE_INPUT_SYSTEM
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

#if USE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
#endif

public class ButtonCheck : MonoBehaviour
{
    private Color activeColor = Color.green;
    private Color inactiveColor = Color.white;

    private Image indicator = null;

#if USE_INPUT_SYSTEM
    InputAction action;

    [InputControl(layout = "Button")]
#endif
    [SerializeField]
    internal string path = "";

    [HideInInspector]
    [SerializeField]
    internal KeyCode buttonID;

    // Start is called before the first frame update
    void Start()
    {
        indicator = this.GetComponent<Image>();

        
#if USE_INPUT_SYSTEM
        action = new InputAction("button", binding: path);
        action.Enable();

        action.started += ctx => ChangeState(true);
        action.canceled += ctx => ChangeState(false);
#elif ENABLE_LEGACY_INPUT_MANAGER
       
#endif
    }

    void ChangeState(bool active)
    {
        
        indicator.color = active ? activeColor : inactiveColor;

    }
    // Update is called once per frame
    void Update()
    {
#if USE_INPUT_SYSTEM
        //handled with events
#elif ENABLE_LEGACY_INPUT_MANAGER
        if(Input.GetKeyDown(buttonID))
            ChangeState(true);
        else if (Input.GetKeyUp(buttonID))
            ChangeState(false);
#endif

    }
}

[CustomEditor(typeof(ButtonCheck))]
public class ButtonCheckEditor : Editor
{
    List<string> exludedProps = new List<string>();

#if USE_INPUT_SYSTEM
    bool inputSystem = true;
#else
    bool inputSystem = false;
#endif



    override public void OnInspectorGUI()
    {
        var script = target as ButtonCheck;

        if (inputSystem)
            base.OnInspectorGUI();
        else
        {
            script.buttonID = (KeyCode)EditorGUILayout.EnumPopup("Button KeyCode", script.buttonID);
        }


    }
}