**Important**
This asset is made to function with the Unity XR Interaction Toolkit Package.
As this package is still in preview, the VR Simulator is likely to break with this package in future updates. If that is the case, please contact me and I'll try to fix any issues and update the VR Simulator as soon as possible.

**Installation**
Install the required package via the top menu under "VRSimulator -> Installer -> Install VR Simulator Package" or manually via the included tarball (.tgz) file using the Unity package manager. 
All required assets scripts and prefabs are included in this package. To use the custom XR Plugin it needs to be activated under XR Management in Project Settings.

Additionally demo scenes can be imported from the Unity package manager UI or via the menu "VRSimulator -> Installer -> Import sample scenes".

**Usage**
- drop an XR Rig from the XR Interaction Toolkit into your scene ("right Click in scene graph -> XR -> XR Rig")
- drop the Simulator Prefab ("right Click in scene graph -> XR -> VRSimulator") into Your Scene
	- the first XRRig found in the scene will be assigned to the simulator field "XR Rig To Follow"

- the assigned keyboard keys for interactions can be changed on the simulator script attached to the simulator prefab
