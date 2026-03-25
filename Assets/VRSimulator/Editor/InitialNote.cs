using UnityEngine;
using UnityEngine.XR;

using UnityEditor;

public class InitialNote : EditorWindow
{
    [SerializeField]
    public bool firstTime = false;
    static EditorWindow window;

    public static void ShowWindow()
    {
        window = EditorWindow.GetWindow(typeof(InitialNote), true, "VR Simulator Installation Note", true);
        window.maxSize = new Vector2(400, 250);
        window.minSize = window.maxSize;
        var position = window.position;
        position.center = new Rect(0f, 0f, Screen.currentResolution.width/2, Screen.currentResolution.height/2).center;
        window.position = position;
    }

    void OnGUI()
    {
        EditorGUILayout.Space();

        GUILayout.Label("Thank you for getting the VR Simulator from the Unity Asset Store.", EditorStyles.wordWrappedLabel);

        EditorGUILayout.Space();

        GUILayout.Label("In order for the VR Simulator to work, it is required to install a custom XR Plugin. " +
            "Click the button below to install the required XR Plugin Package now. " +
            "Please remember that new XR Plugins need to be activated through XR Management in Project Settings. ", EditorStyles.wordWrappedLabel);

        EditorGUILayout.Space();

        EditorGUILayout.Space();
        
        EditorStyles.miniButton.stretchWidth = false;

        if (GUILayout.Button("Install VR Simulator Package", EditorStyles.miniButton))
        {
            window.Close();
            XRPluginInstaller.InstallPackage();
        }

        EditorGUILayout.Space();
        GUILayout.Label("---", EditorStyles.boldLabel);

        GUILayout.Label("Note that this can also be done manually from the package manager UI or " +
            "from the VR Simulator installer window (VRSimulator -> Installer) later on. \n" +
            "Additional sample scenes can be imported from there as well.", EditorStyles.wordWrappedLabel);
    }

}
