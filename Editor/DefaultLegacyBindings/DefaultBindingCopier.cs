using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace Unity.XR.OpenVR.Editor
{
    public class DefaultBindingCopier : ScriptableObject
    {
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void CheckAndCopyDefaults()
        {
            if (OpenVRHelpers.IsUsingSteamVRInput())
                return; //don't copy if we're using steamvr input

            OpenVRSettings settings = OpenVRSettings.GetSettings();
            if (settings != null)
            {
                if (settings.HasCopiedDefaults)
                    return; //already copied
            }

            string folderPath = OpenVRSettings.GetStreamingSteamVRPath();
            string[] filesToCopy = GetFiles();

            foreach (string file in filesToCopy)
            {
                FileInfo fileInfo = new FileInfo(file);
                string newPath = Path.Combine(folderPath, fileInfo.Name);
                if (File.Exists(newPath) == false)
                    File.Copy(fileInfo.FullName, newPath);
            }

            if (settings != null)
            {
                settings.HasCopiedDefaults = true;
            }
        }


        private static string[] GetFiles()
        {
            var ms = MonoScript.FromScriptableObject(ScriptableObject.CreateInstance<DefaultBindingCopier>());
            var path = AssetDatabase.GetAssetPath(ms);
            path = Path.GetDirectoryName(path);

            return Directory.GetFiles(path, "*.json");
        }
    }
}