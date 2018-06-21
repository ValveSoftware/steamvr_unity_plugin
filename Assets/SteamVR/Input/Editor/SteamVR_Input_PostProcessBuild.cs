using UnityEngine;
using UnityEditor;
using System.Collections;
using UnityEditor.Callbacks;
using System.IO;

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

                Debug.Log("[SteamVR] Copied SteamVR Input file to build folder: " + newFilePath);
            }
        }
    }
}
