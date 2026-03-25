using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEditor.XR.Management.Metadata;

namespace ThrashPanda.VR.Simulator
{
    class VRSimPackage : IXRPackage
    {
        class VRSimLoaderMetadata : IXRLoaderMetadata 
        {
            public string loaderName { get; set; }
            public string loaderType { get; set; }
            public List<BuildTargetGroup> supportedBuildTargets { get; set; }
        }

        class VRSimPackageMetadata : IXRPackageMetadata
        {
            public string packageName { get; set; }
            public string packageId { get; set; }
            public string settingsType { get; set; }
            public List<IXRLoaderMetadata> loaderMetadata { get; set; } 
        }

        private static IXRPackageMetadata s_Metadata = new VRSimPackageMetadata() {
                packageName = "VR Simulator Package",
                packageId = "com.thrashpanda.vr.simulator",
                settingsType = typeof(VRSimSettings).FullName,

                loaderMetadata = new List<IXRLoaderMetadata>() {
                    new VRSimLoaderMetadata() {
                        loaderName = "VR Simulator",
                        loaderType = typeof(VRSimulatorXRLoader).FullName,
                        supportedBuildTargets = new List<BuildTargetGroup>() {
                            BuildTargetGroup.Standalone
                        }
                    }
                }
        };

        public IXRPackageMetadata metadata => s_Metadata;

        public bool PopulateNewSettingsInstance(ScriptableObject obj)
        {
            return true;
        }

    }
}
