#if (UNITY_EDITOR && UNITY_2019_1_OR_NEWER)

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.XR.OpenVR.SimpleJSON;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.Networking;

namespace Unity.XR.OpenVR
{
    [InitializeOnLoad]
    public class OpenVRPackageInstaller : ScriptableObject
    {
        private const string valveOpenVRPackageString = "com.valvesoftware.unity.openvr";


        private static ListRequest listRequest;
        private static AddRequest addRequest;

        private static System.Diagnostics.Stopwatch packageTime = new System.Diagnostics.Stopwatch();
        private const float estimatedTimeToInstall = 90; // in seconds

        private const string updaterKeyTemplate = "com.valvesoftware.unity.openvr.updateState.{0}";
        private static string updaterKey
        {
            get { return string.Format(updaterKeyTemplate, Application.productName); }
        }

        private static UpdateStates updateState
        {
            get { return _updateState; }
            set
            {
#if VALVE_DEBUG
                Debug.Log("[DEBUG] Update State: " + value.ToString());
#endif
                _updateState = value;
                EditorPrefs.SetInt(updaterKey, (int)value);
            }
        }
        private static UpdateStates _updateState = UpdateStates.Idle;

        private static double runningSeconds
        {
            get
            {
                if (packageTime.IsRunning == false)
                    packageTime.Start();
                return packageTime.Elapsed.TotalSeconds;
            }
        }

        private static bool forced = false;

        public static void Start(bool force = false)
        {
            EditorApplication.update -= Update;
            EditorApplication.update += Update;
            
            if (force)
            {
                RemoveScopedRegistry();
            }
        }

        static OpenVRPackageInstaller()
        {
            #if OPENVR_XR_API //if we're updating, go ahead and just start
            Start();
            #endif
        }

        /// <summary>
        /// State Machine
        /// Idle: Start from last known state. If none is known, ask user if they want to install, if yes goto remove scoped registry step
        /// WaitingOnExistingCheck:
        /// RemoveScopedRegistry: Remove the scoped registry entry if it exists
        /// WaitingForAdd: if the add request has been nulled or completed successfully, request a list of packages for confirmation
        /// WaitingForAddConfirmation: enumerate the packages and verify the add succeeded. If it failed, try again.
        ///                                 If it succeeded request removal of this script
        /// RemoveSelf: delete the key that we've been using to maintain state. Delete this script and the containing folder if it's empty.
        /// </summary>
        private static void Update()
        {
            switch (updateState)
            {
                case UpdateStates.Idle:
                    if (EditorPrefs.HasKey(updaterKey))
                    {
                        _updateState = (UpdateStates)EditorPrefs.GetInt(updaterKey);
                        packageTime.Start();
                    }
                    else
                    {
                        RequestExisting();
                    }
                    break;

                case UpdateStates.WaitingOnExistingCheck:
                    if (listRequest == null)
                    {
                        //the list request got nulled for some reason. Request it again.
                        RequestExisting();
                    }
                    else if (listRequest != null && listRequest.IsCompleted)
                    {
                        if (listRequest.Error != null || listRequest.Status == UnityEditor.PackageManager.StatusCode.Failure)
                        {
                            DisplayErrorAndStop("Error while checking for an existing OpenVR package.", listRequest);
                        }
                        else
                        {
                            if (listRequest.Result.Any(package => package.name == valveOpenVRPackageString))
                            {
                                var existingPackage = listRequest.Result.FirstOrDefault(package => package.name == valveOpenVRPackageString);

                                string latestTarball = GetLatestTarballVersion();

                                if (latestTarball != null && latestTarball.CompareTo(existingPackage.version) == 1) 
                                {
                                    //we have a tarball higher than the currently installed version
                                    string upgradeString = string.Format("This SteamVR Unity Plugin has a newer version of the Unity XR OpenVR package than you have installed. Would you like to upgrade?\n\nCurrent: {0}\nUpgrade: {1} (recommended)", existingPackage.version, latestTarball);
                                    bool upgrade = UnityEditor.EditorUtility.DisplayDialog("OpenVR XR Updater", upgradeString, "Upgrade", "Cancel");
                                    if (upgrade)
                                        RemoveScopedRegistry();
                                    else
                                    {
                                        bool delete = UnityEditor.EditorUtility.DisplayDialog("OpenVR XR Updater", "Would you like to remove this updater script so we don't ask again?", "Remove updater", "Keep");
                                        if (delete)
                                        {
                                            Stop();
                                            return;
                                        }
                                        else
                                        {
                                            GentleStop();
                                            return;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                #if UNITY_2020_1_OR_NEWER
                                RemoveScopedRegistry(); //just install if we're on 2020 and they don't have the package
                                return;
                                #else
                                //they don't have the package yet. Ask if they want to install (only for 2019)
                                bool blankInstall = UnityEditor.EditorUtility.DisplayDialog("OpenVR XR Installer", "The SteamVR Unity Plugin can be used with the legacy Unity VR API (Unity 5.4 - 2019) or with the Unity XR API (2019+). Would you like to install OpenVR for Unity XR?", "Install", "Cancel");
                                if (blankInstall)
                                    RemoveScopedRegistry();
                                else
                                {
                                    bool delete = UnityEditor.EditorUtility.DisplayDialog("OpenVR XR Installer", "Would you like to remove this installer script so we don't ask again?", "Remove installer", "Keep");
                                    if (delete)
                                    {
                                        Stop();
                                        return;
                                    }
                                    else
                                    {
                                        GentleStop();
                                        return;
                                    }
                                }
                                #endif
                            }
                        }
                    }
                    break;

                case UpdateStates.WaitingForAdd:
                    if (addRequest == null)
                    {
                        //the add request got nulled for some reason. Request an add confirmation
                        RequestAddConfirmation();
                    }
                    else if (addRequest != null && addRequest.IsCompleted)
                    {
                        if (addRequest.Error != null || addRequest.Status == UnityEditor.PackageManager.StatusCode.Failure)
                        {
                            DisplayErrorAndStop("Error adding new version of OpenVR package.", addRequest);
                        }
                        else
                        {
                            //verify that the package has been added (then stop)
                            RequestAddConfirmation();
                        }
                    }
                    else
                    {
                        if (packageTime.Elapsed.TotalSeconds > estimatedTimeToInstall)
                            DisplayErrorAndStop("Error while trying to add package.", addRequest);
                        else
                            DisplayProgressBar();
                    }
                    break;

                case UpdateStates.WaitingForAddConfirmation:
                    if (listRequest == null)
                    {
                        //the list request got nulled for some reason. Request it again.
                        RequestAddConfirmation();
                    }
                    else if (listRequest != null && listRequest.IsCompleted)
                    {
                        if (listRequest.Error != null || listRequest.Status == UnityEditor.PackageManager.StatusCode.Failure)
                        {
                            DisplayErrorAndStop("Error while confirming the OpenVR package has been added.", listRequest);
                        }
                        else
                        {
                            if (listRequest.Result.Any(package => package.name == valveOpenVRPackageString))
                            {
                                updateState = UpdateStates.RemoveSelf;
                                UnityEditor.EditorUtility.DisplayDialog("OpenVR Unity XR Installer", "OpenVR Unity XR successfully installed.\n\nA restart of the Unity Editor may be necessary.", "Ok");
                            }
                            else
                            {
                                //try to add again if it's not there and we don't know why
                                RequestAdd();
                            }
                        }
                    }
                    else
                    {
                        if (runningSeconds > estimatedTimeToInstall)
                        {
                            DisplayErrorAndStop("Error while confirming the OpenVR package has been added.", listRequest);
                        }
                        else
                            DisplayProgressBar();
                    }
                    break;

                case UpdateStates.RemoveSelf:
                    EditorPrefs.DeleteKey(updaterKey);
                    EditorUtility.ClearProgressBar();
                    EditorApplication.update -= Update;

#if VALVE_SKIP_DELETE
                    Debug.Log("[DEBUG] skipping script deletion. Complete.");
                    return;
#endif

                    var script = MonoScript.FromScriptableObject(OpenVRPackageInstaller.CreateInstance<OpenVRPackageInstaller>());
                    var path = AssetDatabase.GetAssetPath(script);
                    FileInfo updaterScript = new FileInfo(path); updaterScript.IsReadOnly = false;
                    FileInfo updaterScriptMeta = new FileInfo(path + ".meta");
                    FileInfo simpleJSONScript = new FileInfo(Path.Combine(updaterScript.Directory.FullName, "OpenVRSimpleJSON.cs"));
                    FileInfo simpleJSONScriptMeta = new FileInfo(Path.Combine(updaterScript.Directory.FullName, "OpenVRSimpleJSON.cs.meta"));

                    updaterScript.IsReadOnly = false;
                    updaterScriptMeta.IsReadOnly = false;
                    simpleJSONScript.IsReadOnly = false;
                    simpleJSONScriptMeta.IsReadOnly = false;

                    updaterScriptMeta.Delete();
                    if (updaterScriptMeta.Exists)
                    {
                        DisplayErrorAndStop("Error while removing package installer script. Please delete manually.", listRequest);
                        return;
                    }

                    simpleJSONScript.Delete();
                    simpleJSONScriptMeta.Delete();
                    updaterScript.Delete();

                    AssetDatabase.Refresh();
                    break;
            }
        }

        private static string GetLatestTarballVersion()
        {
            FileInfo[] files;
            FileInfo latest = GetAvailableTarballs(out files);

            if (latest == null)
                return null;

            return GetTarballVersion(latest);
        }

        private static FileInfo GetAvailableTarballs(out FileInfo[] packages)
        {
            var installerScript = MonoScript.FromScriptableObject(OpenVRPackageInstaller.CreateInstance<OpenVRPackageInstaller>());
            var scriptPath = AssetDatabase.GetAssetPath(installerScript);
            FileInfo thisScript = new FileInfo(scriptPath);

            packages = thisScript.Directory.GetFiles("*.tgz");

            if (packages.Length > 0)
            {
                if (packages.Length > 1)
                {
                    var descending = packages.OrderByDescending(file => file.Name);
                    var latest = descending.First();
                    packages = descending.Where(file => file != latest).ToArray();
                    return latest;
                }

                var onlyPackage = packages[0];
                packages = new FileInfo[0];
                return onlyPackage;
            }
            else
                return null;
        }

        private static string GetTarballVersion(FileInfo file)
        {
            int startIndex = file.Name.IndexOf('-') + 1;
            int endIndex = file.Name.IndexOf(".tgz");
            int len = endIndex - startIndex;
            return file.Name.Substring(startIndex, len);
        }

        private const string packageManifestPath = "Packages/manifest.json";
        private const string scopedRegistryKey = "scopedRegistries";
        private const string npmRegistryName = "Valve";

        //load packages.json
        //check for existing scoped registries
        //check for our scoped registry
        //if no to either then add it
        //save file
        //reload
        private static void RemoveScopedRegistry()
        {
            updateState = UpdateStates.RemoveOldRegistry;
            packageTime.Start();

            if (File.Exists(packageManifestPath) == false)
            {
                Debug.LogWarning("[OpenVR Installer] Could not find package manifest at: " + packageManifestPath);

                RequestAdd();
                return;
            }

            string jsonText = File.ReadAllText(packageManifestPath);
            JSONNode manifest = JSON.Parse(jsonText);

            if (manifest.HasKey(scopedRegistryKey) == true)
            {
                if (manifest[scopedRegistryKey].HasKey(npmRegistryName))
                {
                    manifest[scopedRegistryKey].Remove(npmRegistryName);

                    File.WriteAllText(packageManifestPath, manifest.ToString(2));
                    Debug.Log("[OpenVR Installer] Removed Valve entry from scoped registry.");
                }
            }

            RequestAdd();
        }

        private static void RequestAdd()
        {
            updateState = UpdateStates.WaitingForAdd;

            FileInfo[] oldFiles;
            FileInfo latest = GetAvailableTarballs(out oldFiles);

            if (latest != null)
            {
                if (oldFiles.Length > 0)
                {
                    var oldFilesNames = oldFiles.Select(file => file.Name);
                    string oldFilesString = string.Join("\n", oldFilesNames);
                    bool delete = UnityEditor.EditorUtility.DisplayDialog("OpenVR XR Installer", "Would you like to delete the old OpenVR packages?\n\n" + oldFilesString, "Delete old files", "Keep");
                    if (delete)
                    {
                        foreach (FileInfo file in oldFiles)
                        {
                            FileInfo meta = new FileInfo(file.FullName + ".meta");
                            if (meta.Exists)
                            {
                                meta.IsReadOnly = false;
                                meta.Delete();
                            }

                            if (file.Exists)
                            {
                                file.IsReadOnly = false;
                                file.Delete();
                            }
                        }
                    }
                }

                string packagePath = latest.FullName;
                if (packagePath != null)
                {
                    string packageAbsolute = packagePath.Replace("\\", "/");
                    string packageRelative = packageAbsolute.Substring(packageAbsolute.IndexOf("/Assets/"));
                    string packageURI = System.Uri.EscapeUriString(packageRelative);
                    addRequest = UnityEditor.PackageManager.Client.Add("file:.." + packageURI);
                }
                else
                {
                    updateState = UpdateStates.RemoveSelf;
                }
            }
        }

        private static void RequestAddConfirmation()
        {
            updateState = UpdateStates.WaitingForAddConfirmation;
            listRequest = Client.List(true, true);
        }

        private static void RequestExisting()
        {
            updateState = UpdateStates.WaitingOnExistingCheck;
            listRequest = Client.List(true, true);
        }

        private static string dialogText = "Installing OpenVR Unity XR package from local storage using Unity Package Manager...";

        private static void DisplayProgressBar()
        {
            bool cancel = UnityEditor.EditorUtility.DisplayCancelableProgressBar("SteamVR", dialogText, (float)packageTime.Elapsed.TotalSeconds / estimatedTimeToInstall);
            if (cancel)
                Stop();
        }

        private static void DisplayErrorAndStop(string stepInfo, Request request)
        {
            string error = "";
            if (request != null)
                error = request.Error.message;

            string errorMessage = string.Format("{0}:\n\t{1}\n\nPlease manually reinstall the package through the package manager.", stepInfo, error);

            UnityEngine.Debug.LogError(errorMessage);

            Stop();

            UnityEditor.EditorUtility.DisplayDialog("OpenVR Error", errorMessage, "Ok");
        }

        private static void Stop()
        {
            updateState = UpdateStates.RemoveSelf;
        }

        private static void GentleStop()
        {
            EditorApplication.update -= Update;
        }

        private enum UpdateStates
        {
            Idle,
            WaitingOnExistingCheck,
            RemoveOldRegistry,
            WaitingForAdd,
            WaitingForAddConfirmation,
            RemoveSelf,
        }
    }
}
#endif