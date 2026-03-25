#if (UNITY_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM)
#define USE_INPUT_SYSTEM
#endif
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
#if USE_INPUT_SYSTEM
using ThrashPanda.VR.Simulator.Input;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.XR;
#endif
#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using System;
using UnityEditor.XR.Management;
#endif

namespace ThrashPanda.VR.Simulator
{
#if USE_INPUT_SYSTEM
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    static class InputLayoutLoader
    {
        static InputLayoutLoader()
        {
            RegisterInputLayouts();
        }

        public static void RegisterInputLayouts()
        {
            InputSystem.RegisterLayout<SimulatorXRController>(
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithProduct(@"(^(Simulator XR Controller))"));
           
        }
    }
#endif
    /*
#if UNITY_EDITOR
    [XRSupportedBuildTarget(BuildTargetGroup.Standalone, new BuildTarget[] { BuildTarget.StandaloneWindows, BuildTarget.StandaloneWindows64 })]
    [XRSupportedBuildTarget(BuildTargetGroup.Android)]
#endif
    */
    public class VRSimulatorXRLoader : XRLoaderHelper
    {
        private static List<XRInputSubsystemDescriptor> s_InputSubsystemDescriptors =
            new List<XRInputSubsystemDescriptor>();

        /// <summary>Return the currently active Input Subsystem intance, if any.</summary>
        public XRInputSubsystem inputSubsystem
        {
            get { return GetLoadedSubsystem<XRInputSubsystem>(); }
        }

        //No useful settings included at the moment ... can be left out for now.
        
        VRSimSettings GetSettings()
        {
            VRSimSettings settings = null;
            // When running in the Unity Editor, we have to load user's customization of configuration data directly from
            // EditorBuildSettings. At runtime, we need to grab it from the static instance field instead.
#if UNITY_EDITOR
            UnityEditor.EditorBuildSettings.TryGetConfigObject(VRSimConstants.k_SettingsKey, out settings);
#else
            settings = VRSimSettings.s_RuntimeInstance;
#endif
            return settings;
        }
        

        public override bool Initialize()
        {
            Debug.Log("Init");
#if USE_INPUT_SYSTEM
            InputLayoutLoader.RegisterInputLayouts();
#endif

            CreateSubsystem<XRInputSubsystemDescriptor, XRInputSubsystem>(s_InputSubsystemDescriptors, "simulator input");
            Debug.Log("Subsystem created.");
            return true;
        }

        public override bool Start()
        {

            StartSubsystem<XRInputSubsystem>();
            return true;

        }

        public override bool Stop()
        {

            StopSubsystem<XRInputSubsystem>();

            return true;
        }

        public override bool Deinitialize()
        {
            DestroySubsystem<XRInputSubsystem>();
            return true;
        }
    }
}
