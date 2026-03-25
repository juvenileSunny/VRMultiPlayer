using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Management;

namespace ThrashPanda.VR.Simulator
{

    [XRConfigurationData("VR Simulator Settings", VRSimConstants.k_SettingsKey)]
    [System.Serializable]
    public class VRSimSettings : ScriptableObject
    {
        #if !UNITY_EDITOR
        /// <summary>Static instance that will hold the runtime asset instance we created in our build process.</summary>
        public static VRSimSettings s_RuntimeInstance = null;
        #endif

        [SerializeField, Tooltip("Include VR Simulator in Build? Needs script recompilation.")]
        bool m_includeInBuild = false;

        public bool includeInBuild
        {
            get { return m_includeInBuild; }
            set {m_includeInBuild = value;}
        }

#if UNITY_EDITOR
        public static void SetScriptingSymbol()
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var group = BuildPipeline.GetBuildTargetGroup(target);

            string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);

            if (symbols.Contains("VRSim_include_in_build"))
            {
                return;
            }

            else
            {
                symbols = symbols += ";VRSim_include_in_build";
                PlayerSettings.SetScriptingDefineSymbolsForGroup(group, symbols);
            }
        }

        public static void RemoveScriptingSymbol()
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var group = BuildPipeline.GetBuildTargetGroup(target);

            string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);

            if (symbols.Contains("VRSim_include_in_build"))
            {
                symbols = symbols.Replace("VRSim_include_in_build", "");
                PlayerSettings.SetScriptingDefineSymbolsForGroup(group, symbols);
            }
        }
#endif

        void Awake()
        {
            #if !UNITY_EDITOR
            s_RuntimeInstance = this;
            #endif
        }
    }
}
