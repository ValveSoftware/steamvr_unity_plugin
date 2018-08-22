using UnityEngine;
using UnityEditor;
using System.Collections;
using UnityEditor.Callbacks;
using System.IO;

namespace Valve.VR
{
    public class SteamVR_Input_PostProcessBuild
    {
        [PostProcessBuildAttribute(1)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            SteamVR_Input.InitializeFile();

            FileInfo fileInfo = new FileInfo(pathToBuiltProject);
            string buildPath = fileInfo.Directory.FullName;

            string[] files = SteamVR_Input.actionFile.GetFilesToCopy();

            bool overwrite = EditorPrefs.GetBool(SteamVR_Input_Generator.steamVRInputOverwriteBuildKey);

            foreach (string file in files)
            {
                FileInfo bindingInfo = new FileInfo(file);
                string newFilePath = Path.Combine(buildPath, bindingInfo.Name);

                bool exists = false;
                if (File.Exists(newFilePath))
                    exists = true;

                if (exists)
                {
                    if (overwrite)
                    {
                        FileInfo existingFile = new FileInfo(newFilePath);
                        existingFile.IsReadOnly = false;
                        existingFile.Delete();

                        File.Copy(file, newFilePath);

                        UpdateAppKey(newFilePath, fileInfo.Name);

                        Debug.Log("[SteamVR] Copied (overwrote) SteamVR Input file at build path: " + newFilePath);
                    }
                    else
                    {
                        Debug.Log("[SteamVR] Skipped writing existing file at build path: " + newFilePath);
                    }
                }
                else
                {
                    File.Copy(file, newFilePath);
                    UpdateAppKey(newFilePath, fileInfo.Name);

                    Debug.Log("[SteamVR] Copied SteamVR Input file to build folder: " + newFilePath);
                }

            }
        }

        private static void UpdateAppKey(string newFilePath, string executableName)
        {
            if (File.Exists(newFilePath))
            {
                string jsonText = System.IO.File.ReadAllText(newFilePath);

                string findString = "\"app_key\" : \"";
                int stringStart = jsonText.IndexOf(findString);

                if (stringStart == -1)
                {
                    findString = findString.Replace(" ", "");
                    stringStart = jsonText.IndexOf(findString);

                    if (stringStart == -1)
                        return; //no app key
                }

                stringStart += findString.Length;
                int stringEnd = jsonText.IndexOf("\"", stringStart);

                int stringLength = stringEnd - stringStart;

                string appKey = jsonText.Substring(stringStart, stringLength);

                if (string.Equals(appKey, SteamVR_Settings.instance.appKey, System.StringComparison.CurrentCultureIgnoreCase) == false)
                {
                    jsonText = jsonText.Replace(appKey, SteamVR_Settings.instance.appKey);

                    File.WriteAllText(newFilePath, jsonText);
                }
            }
        }
    }
}