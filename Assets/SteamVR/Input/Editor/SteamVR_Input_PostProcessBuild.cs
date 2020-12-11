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

                }
                else
                    return; //no app key

                stringStart += findString.Length;
                int stringEnd = jsonText.IndexOf(",", stringStart + findString.Length);

                int stringLength = stringEnd - stringStart + 1;

                string removed = jsonText.Remove(stringStart, stringLength);

                FileInfo file = new FileInfo(newFilePath);
                file.IsReadOnly = false;

                File.WriteAllText(newFilePath, removed);
            }
        }
    }
}