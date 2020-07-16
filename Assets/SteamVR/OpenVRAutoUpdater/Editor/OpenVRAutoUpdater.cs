#if UNITY_EDITOR && (UNITY_2019_3_OR_NEWER || VALVE_UPDATE_FORCE)
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
    public class OpenVRAutoUpdater : ScriptableObject
    {
        private const string valveOpenVRPackageStringOld = "com.valve.openvr";
        private const string valveOpenVRPackageString = "com.valvesoftware.unity.openvr";

        public const string npmRegistryStringValue = "\"name\": \"Valve\", \"url\": \"https://registry.npmjs.org\", \"scopes\": [ \"com.valvesoftware.unity.openvr\" ]";
        public const string scopedRegisteryKey = "scopedRegistries";

        private static ListRequest listRequest;
        private static AddRequest addRequest;
        private static RemoveRequest removeRequest;
        private static SearchRequest searchRequest;

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

        static OpenVRAutoUpdater()
        {
#if UNITY_2020_1_OR_NEWER || VALVE_UPDATE_FORCE
            Start();
#endif
        }

        public static void Start()
        {
            EditorApplication.update -= Update;
            EditorApplication.update += Update;
        }

        /// <summary>
        /// State Machine
        /// Idle: Start from last known state. If none is known go to request a removal of the current openvr package
        /// WaitingForList: enumerate the packages to see if we have an existing package that needs to be removed. If so, request removal, if not, add scoped registry
        /// WaitingForRemove: if the remove request has been nulled or completed successfully, request a list of packages for confirmation
        /// WaitingForRemoveConfirmation: enumerate the packages and verify the removal succeeded. If it failed, try again. 
        ///                                 If it succeeded, add the scoped registry.
        /// WaitingForScopedRegistry: search for available packages until the openvr package is available. Then add the package
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
                        RequestList();
                        packageTime.Start();
                    }
                    break;

                case UpdateStates.WaitingForList:
                    if (listRequest == null)
                    {
                        //the list request got nulled for some reason. Request it again.
                        RequestList();
                    }
                    else if (listRequest != null && listRequest.IsCompleted)
                    {
                        if (listRequest.Error != null || listRequest.Status == UnityEditor.PackageManager.StatusCode.Failure)
                        {
                            DisplayErrorAndStop("Error while checking for an existing openvr package.", listRequest);
                        }
                        else
                        {
                            if (listRequest.Result.Any(package => package.name == valveOpenVRPackageString))
                            {
                                //if it's there then remove it in preparation for adding the scoped registry
                                RequestRemove(valveOpenVRPackageString);
                            }
                            else if (listRequest.Result.Any(package => package.name == valveOpenVRPackageStringOld))
                            {
                                //if it's there then remove it in preparation for adding the scoped registry
                                RequestRemove(valveOpenVRPackageStringOld);
                            }
                            else
                            {
                                AddScopedRegistry();
                            }
                        }
                    }
                    else
                    {
                        if (runningSeconds > estimatedTimeToInstall)
                        {
                            DisplayErrorAndStop("Error while confirming package removal.", listRequest);
                        }
                        else
                            DisplayProgressBar();
                    }
                    break;

                case UpdateStates.WaitingForRemove:
                    if (removeRequest == null)
                    {
                        //if our remove request was nulled out we should check if the package has already been removed.
                        RequestRemoveConfirmation();
                    }
                    else if (removeRequest != null && removeRequest.IsCompleted)
                    {
                        if (removeRequest.Error != null || removeRequest.Status == UnityEditor.PackageManager.StatusCode.Failure)
                        {
                            DisplayErrorAndStop("Error removing old version of OpenVR package.", removeRequest);
                        }
                        else
                        {
                            //verify that the package has been removed (then add)
                            RequestRemoveConfirmation();
                        }
                    }
                    else
                    {
                        if (packageTime.Elapsed.TotalSeconds > estimatedTimeToInstall)
                            DisplayErrorAndStop("Error removing old version of OpenVR package.", removeRequest);
                        else
                            DisplayProgressBar();
                    }
                    break;

                case UpdateStates.WaitingForRemoveConfirmation:
                    if (listRequest == null)
                    {
                        //the list request got nulled for some reason. Request it again.
                        RequestRemoveConfirmation();
                    }
                    else if (listRequest != null && listRequest.IsCompleted)
                    {
                        if (listRequest.Error != null || listRequest.Status == UnityEditor.PackageManager.StatusCode.Failure)
                        {
                            DisplayErrorAndStop("Error while confirming package removal.", listRequest);
                        }
                        else
                        {
                            if (listRequest.Result.Any(package => package.name == valveOpenVRPackageString))
                            {
                                //try remove again if it didn't work and we don't know why.
                                RequestRemove(valveOpenVRPackageString);
                            }
                            else if (listRequest.Result.Any(package => package.name == valveOpenVRPackageStringOld))
                            {
                                //try remove again if it didn't work and we don't know why.
                                RequestRemove(valveOpenVRPackageStringOld);
                            }
                            else
                            {
                                AddScopedRegistry();
                            }
                        }
                    }
                    else
                    {
                        if (runningSeconds > estimatedTimeToInstall)
                        {
                            DisplayErrorAndStop("Error while confirming package removal.", listRequest);
                        }
                        else
                            DisplayProgressBar();
                    }
                    break;

                case UpdateStates.WaitingForScopedRegistry:
                    if (searchRequest == null)
                    {
                        //the search request got nulled for some reason, request again
                        RequestScope();
                    }
                    else if (searchRequest != null && searchRequest.IsCompleted)
                    {
                        if (searchRequest.Error != null || searchRequest.Status == UnityEditor.PackageManager.StatusCode.Failure)
                        {
                            DisplayErrorAndStop("Error adding Valve scoped registry to project.", searchRequest);
                        }

                        RequestAdd();
                    }
                    else
                    {
                        if (packageTime.Elapsed.TotalSeconds > estimatedTimeToInstall)
                            DisplayErrorAndStop("Error while trying to add scoped registry.", addRequest);
                        else
                            DisplayProgressBar();
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
                                UnityEditor.EditorUtility.DisplayDialog("OpenVR", "OpenVR Unity XR successfully updated.", "Ok");
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

                    var script = MonoScript.FromScriptableObject(OpenVRAutoUpdater.CreateInstance<OpenVRAutoUpdater>());
                    var path = AssetDatabase.GetAssetPath(script);
                    FileInfo updaterScript = new FileInfo(path);
                    FileInfo updaterScriptMeta = new FileInfo(path + ".meta");
                    FileInfo simpleJSONScript = new FileInfo(Path.Combine(updaterScript.Directory.FullName, "OpenVRSimpleJSON.cs"));
                    FileInfo simpleJSONScriptMeta = new FileInfo(Path.Combine(updaterScript.Directory.FullName, "OpenVRSimpleJSON.cs.meta"));

                    updaterScript.Delete();
                    updaterScriptMeta.Delete();
                    simpleJSONScript.Delete();
                    simpleJSONScriptMeta.Delete();
                    if (updaterScript.Directory.GetFiles().Length == 0 && updaterScript.Directory.GetDirectories().Length == 0)
                    {
                        path = updaterScript.Directory.FullName + ".meta";
                        updaterScript.Directory.Delete();
                        File.Delete(path);
                    }

                    AssetDatabase.Refresh();
                    break;
            }
        }

        private const string packageManifestPath = "Packages/manifest.json";
        private const string scopedRegistryKey = "scopedRegistries";
        private const string scopedRegistryValue = "{ \"name\": \"Valve\",\n" +
                                                   "\"url\": \"https://registry.npmjs.org/\"," +
                                                   "\"scopes\": [" +
                                                   "\"com.valvesoftware\", \"com.valvesoftware.unity.openvr\"" +
                                                   "] }";
        private const string scopedRegistryNodeTemplate = "[ {0} ]"; 

        //load packages.json
        //check for existing scoped registries
        //check for our scoped registry
        //if no to either then add it
        //save file
        //reload
        private static void AddScopedRegistry()
        {
            if (File.Exists(packageManifestPath) == false)
            {
                Debug.LogError("[OpenVR Installer] Could not find package manifest at: " + packageManifestPath);
                return;
            }

            bool needsSave = false;
            string jsonText = File.ReadAllText(packageManifestPath);
            JSONNode manifest = JSON.Parse(jsonText);

            if (manifest.HasKey(scopedRegistryKey) == false)
            {
                manifest.Add(scopedRegistryKey, JSON.Parse(string.Format(scopedRegistryNodeTemplate, scopedRegistryValue)));
                needsSave = true;
            }
            else
            {
                bool alreadyExists = false;
                foreach (var scopedRegistry in manifest[scopedRegistryKey].AsArray)
                {
                    if (scopedRegistry.Value != null && scopedRegistry.Value.HasKey("name") && scopedRegistry.Value["name"] == "Valve")
                    {
                        alreadyExists = true;
                        break;
                    }
                }

                if (alreadyExists == false)
                {
                    manifest[scopedRegistryKey].Add(JSON.Parse(scopedRegistryValue));
                    needsSave = true;
                }
            }

            if (needsSave)
            {
                File.WriteAllText(packageManifestPath, manifest.ToString(2));
                Debug.Log("[OpenVR Installer] Wrote scoped registry file.");
            }

            RequestScope();
        }

        private static void RequestList()
        {
            updateState = UpdateStates.WaitingForList;
            listRequest = Client.List();
        }

        private static void RequestRemove(string packageName)
        {
            updateState = UpdateStates.WaitingForRemove;
            removeRequest = UnityEditor.PackageManager.Client.Remove(packageName);
        }

        private static void RequestAdd()
        {
            updateState = UpdateStates.WaitingForAdd;
            addRequest = UnityEditor.PackageManager.Client.Add(valveOpenVRPackageString);
        }

        private static void RequestRemoveConfirmation()
        {
            updateState = UpdateStates.WaitingForRemoveConfirmation;
            listRequest = Client.List();
        }

        private static void RequestAddConfirmation()
        {
            updateState = UpdateStates.WaitingForAddConfirmation;
            listRequest = Client.List();
        }

        private static string dialogText = "Installing OpenVR Unity XR package from github using Unity Package Manager...";

        private static void DisplayProgressBar()
        {
            bool cancel = UnityEditor.EditorUtility.DisplayCancelableProgressBar("SteamVR", dialogText, (float)packageTime.Elapsed.TotalSeconds / estimatedTimeToInstall);
            if (cancel)
                Stop();
        }

        private static void RequestScope()
        {
            searchRequest = Client.SearchAll(false);
            updateState = UpdateStates.WaitingForScopedRegistry;
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

        private enum UpdateStates
        {
            Idle,
            WaitingForList,
            WaitingForRemove,
            WaitingForRemoveConfirmation,
            WaitingForScopedRegistry,
            WaitingForAdd,
            WaitingForAddConfirmation,
            RemoveSelf,
        }
    }
}
#endif