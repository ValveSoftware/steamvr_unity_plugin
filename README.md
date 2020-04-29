# OpenVR XR SDK Package

The purpose of this package is to provide OpenVR XR SDK Support. This package provides the necessary sdk libraries for users to build Applications that work with the OpenVR runtime. The OpenVR XR Plugin gives you access to all major VR devices through one interface. Explicit support for: HTC Vive, HTC Vive Cosmos, HTC Vive Tracker, Oculus Touch, Windows Mixed Reality, Logitech VR Ink, and Valve Index. Other SteamVR compatible devices are supported though may have inaccurate or incomplete features.

# Documentation

There is some brief documentation included with this plugin at /Documentation~/com.valve.openvr.md

# Input choice

First, you have a choice to make between using SteamVR Input, and Unity XR Input. The two systems are currently mutually exclusive.

If you would like to use all the functionality SteamVR Input has to offer (user rebindable inputs, skeletal input, etc) you will need to also install the SteamVR Unity Plugin. This can be found at (https://github.com/ValveSoftware/steamvr_unity_plugin/releases/tag/2.6.0b1).

If you would prefer to use Unity’s XR Input so your application can use multiple SDKs and therefore be deployed to multiple stores you can use this plugin standalone.


## Known Issues:
* Display Provider
  * OpenVR Mirror View Mode (default) can cause black screens in the game view. Please send us bug reports if this happens.
  * OpenVR Mirror View Mode requires use of Linear Color Space (Project Settings > Player > Other Settings > (Rendering) Color Space)
  * In certain use cases, changing RenderScale and ViewPortScale in runtime causes some performance spikes
  * Vulkan currently only supported in Multi pass stereo rendering mode 
* Input Provider
  * You cannot access skeletal finger information. This api is incompatible with SteamVR Legacy Input.


## Bug reports:
* For bug reports please create an issue on our github (https://github.com/ValveSoftware/steamvr_unity_plugin/issues) and include the following information
  * Detailed steps of what you were doing at the time
  * Your editor or build log (editor log location: %LOCALAPPDATA%\Unity\Editor\Editor.log)
  * A SteamVR System report DIRECTLY AFTER encountering the issue. (SteamVR interface -> Menu -> Create System Report -> Save to file)


## QuickStart

### Unity XR Input (SteamVR Legacy Input):
* Go to the package manager window (Window Menu -> Package Manager)
* Hit the + button in the upper left hand corner
* Select “Add package from git URL”
* Paste in: https://github.com/ValveSoftware/steamvr_unity_plugin.git#UnityXRPlugin
* Open the XR Management UI (Edit Menu -> Project Settings -> XR Plugin Management)
* Click the checkbox next to OpenVR Loader - or in older versions - Under Plugin Providers hit the + icon and add “Open VR Loader”

#### Testing:

* Add a couple cubes to the scene (scale to 0.1)
* Add TrackedPoseDriver to both cubes and the Main Camera
 *	Main Camera: Under Tracked Pose Driver:
    * For Device select: “Generic XR Device”
    * For Pose Source select: “Center Eye - HMD Reference”
  * Cube 1:
    *	For Device select: “Generic XR Controller”
    *	For Pose Source select “Left Controller”
  * Cube 2:
    *	For Device select: “Generic XR Controller”
    *	For Pose Source select “Right Controller” 
* Hit play and you should see a tracked camera and two tracked cubes


### SteamVR Legacy Input with Unity Input System:
* Follow the above instructions
* Go to the package manager window (Window Menu -> Package Manager)
* Look for the Input System package
* Click Install
* Open Input System debug window (Window -> Analysis -> Input System Debugger)
* Verify devices load as expected and are getting reasonable values. All controllers should have the correct buttons and touch states (including index)


### SteamVR Input System:
* Install SteamVR Unity Plugin v2.6.0b1 (https://github.com/ValveSoftware/steamvr_unity_plugin/releases/tag/2.6.0b1)
* It should install the OpenVR XR API package automatically for 2020.1+ for 2019.3 you’ll need to add it with the instructions above.
* Open the SteamVR Input window (Window -> SteamVR Input)
* Accept the default json
* Click Save and Generate
* Open the Interactions_Example scene (Assets/SteamVR/InteractionSystem/Samples/Interaction_Example.unity)
* Hit play, verify that you can see your hands and teleport around


