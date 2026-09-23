using UnityEditor;

using UnityEngine;

namespace ThrashPanda.VR.Simulator
{
    [CustomEditor(typeof(VRSimSettings))]
    public class VRSimSettingsEditor : Editor
    {
        static string k_includeInBuild = "m_includeInBuild";

        static GUIContent k_ShowBuildSettingsLabel = new GUIContent("Build Settings");
        static GUIContent k_includeBuildLabel = new GUIContent("Include in Build");

        bool m_ShowBuildSettings = true;

        SerializedProperty m_IncludeInBuildProperty;

        public override void OnInspectorGUI()
        {
            if (serializedObject == null || serializedObject.targetObject == null)
                return;

            if (m_IncludeInBuildProperty == null) m_IncludeInBuildProperty = serializedObject.FindProperty(k_includeInBuild);

            serializedObject.Update();
            m_ShowBuildSettings = EditorGUILayout.Foldout(m_ShowBuildSettings, k_ShowBuildSettingsLabel);
            if (m_ShowBuildSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_IncludeInBuildProperty, k_includeBuildLabel);
                if (EditorGUI.EndChangeCheck())
                {
                    if (m_IncludeInBuildProperty.boolValue)
                        VRSimSettings.SetScriptingSymbol();

                    else
                        VRSimSettings.RemoveScriptingSymbol();
                }
                EditorGUI.indentLevel--;
            }


            serializedObject.ApplyModifiedProperties();
        }
    }
}
