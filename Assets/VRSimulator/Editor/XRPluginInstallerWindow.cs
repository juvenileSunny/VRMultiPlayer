using UnityEngine;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine.XR;
using System.IO;

[InitializeOnLoad]
public class XRPluginInstallerWindow : EditorWindow
{
    [MenuItem("VRSimulator/Installer")]
    public static void ShowWindow()
    {
        EditorWindow window = EditorWindow.GetWindow(typeof(XRPluginInstallerWindow),true,"XR Plugin Package Installer",true);
        window.maxSize = new Vector2(400, 200);
        window.minSize = window.maxSize;
    }

    void Awake()
    {
    }

    void OnGUI()
    {

        EditorGUILayout.Space();

        GUILayout.TextArea("Click the button below to install the required XR Plugin Package via the package manager. " +
            "Please remember that new XR Plugins need to be activated through XR Management in Project Settings.", EditorStyles.wordWrappedLabel);

        EditorGUILayout.Space();


        EditorGUI.BeginDisabledGroup(false);

        using (new EditorGUI.DisabledScope(XRPluginInstaller.CheckForPackage()))
        {
            if (GUILayout.Button("Install VR Simulator Package", EditorStyles.miniButton))
            {
                XRPluginInstaller.InstallPackage();
            }
        }

        EditorGUILayout.Space();

        GUILayout.TextArea("Use this button to import optional sample scenes into your project.", EditorStyles.wordWrappedLabel);

        EditorGUILayout.Space();

        if (GUILayout.Button("Import sample scenes", EditorStyles.miniButton))
        {
            XRPluginInstaller.InstallSamples();
        }

    }
}
