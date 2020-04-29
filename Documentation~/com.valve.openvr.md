# About

The purpose of this package is to provide OpenVR XR SDK Support. This package provides the necessary sdk libraries for users to build Applications that work with the OpenVR runtime. The OpenVR XR Plugin gives you access to all major VR devices through one interface. Explicit support for: HTC Vive, HTC Vive Cosmos, HTC Vive Tracker, Oculus Touch, Windows Mixed Reality, Logitech VR Ink, and Valve Index. Other SteamVR compatible devices are supported though may have inaccurate or incomplete features.

## Subsystems

### Display 

The display subsystem provides rendering support for the XR Plugin. It currently supports DirectX 11 and Vulcan.

### Input 

* **SteamVR Legacy Input**
Alone, this plugin runs using SteamVR's Legacy Input mode. That means we provide access to a number of "actions" that have the names of sensors on the controllers but the user is free to remap those at runtime. Through this interface we are not able to provide access to features like creating your own actions, action sets, or Skeletal Input (hands).

* **Custom Legacy Bindings** A default set of legacy input bindings are included with this plugin. If you would like to further customize these bindings you can do so by modifying these bindings to better suit your application by editing the json files directly at Assets/StreamingAssets/SteamVR. You'll be unable to add bindings to actions not already referenced, but you can modify the existing bindings.

* **SteamVR Input**
To use the full power of SteamVR we recommend also downloading our SteamVR for Unity plugin. It is in beta [on our github releases page.](https://github.com/ValveSoftware/steamvr_unity_plugin/releases/tag/2.6.0b1) This plugin can run alongside the OpenVR XR plugin. However, you will not be able to query Unity's input functions while using SteamVR Input. The two systems are currently incompatible and you must chose to use one or the other.



## XR Management

To use our XR Management support open the XR Management settings page (Project Settings -> XR Plugin Management) and click the checkbox next to OpenVR Loader. Or for older versions of the XR Management UI: click the '+' icon under Plugin Providers, then select Open VR Loader.

* **Settings** 
 * **Application Type** - This gives you the option between creating a Scene app (most games / applications) and an Overlay app. Overlay apps can run alongside Scene apps providing tools to the user. For more information on Overlay Apps see [this documentation page here](https://github.com/ValveSoftware/openvr/wiki/IVROverlay_Overview)
 * **Stereo Rendering Mode** - Currently supported modes are Multi Pass (render each eye independently) and Single Pass Instanced (render both eyes at once). For more information on the types of rendering modes see [Unity's documentation page here](https://docs.unity3d.com/Manual/SinglePassStereoRendering.html)
 * **Mirror View Mode** - This is a setting you can change at runtime that controls what view is shown in the desktop window. Please note that selecting these options does not trigger a separate full render of the scene. Options are None, Left or Right eye, or OpenVR's view. OpenVR's view will show you the same thing you can see out of SteamVR's preview VR View. This is usually a blend between the left and right eye as well as any SteamVR overlays.
