//======= Copyright (c) Valve Corporation, All rights reserved. ===============

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System.Linq;
using Valve.Newtonsoft.Json;
using System.IO;

namespace Valve.VR
{
    [System.Serializable]
    public class SteamVR_Input_ActionFile
    {
        public List<SteamVR_Input_ActionFile_Action> actions;
        public List<SteamVR_Input_ActionFile_ActionSet> action_sets;
        public List<SteamVR_Input_ActionFile_DefaultBinding> default_bindings;
        public List<Dictionary<string, string>> localization;

        [JsonIgnore]
        public List<SteamVR_Input_ActionFile_LocalizationItem> localizationHelperList = new List<SteamVR_Input_ActionFile_LocalizationItem>();

        public void InitializeHelperLists()
        {
            foreach (var actionset in action_sets)
            {
                actionset.actionsInList = new List<SteamVR_Input_ActionFile_Action>(actions.Where(action => action.path.StartsWith(actionset.name) && SteamVR_Input_ActionFile_ActionTypes.listIn.Contains(action.type)));
                actionset.actionsOutList = new List<SteamVR_Input_ActionFile_Action>(actions.Where(action => action.path.StartsWith(actionset.name) && SteamVR_Input_ActionFile_ActionTypes.listOut.Contains(action.type)));
            }

            foreach (var item in localization)
            {
                localizationHelperList.Add(new SteamVR_Input_ActionFile_LocalizationItem(item));
            }
        }

        public void SaveHelperLists()
        {
            actions.Clear();

            foreach (var actionset in action_sets)
            {
                actions.AddRange(actionset.actionsInList);
                actions.AddRange(actionset.actionsOutList);
            }

            localization.Clear();
            foreach (var item in localizationHelperList)
            {
                Dictionary<string, string> localizationItem = new Dictionary<string, string>();
                localizationItem.Add(SteamVR_Input_ActionFile_LocalizationItem.languageTagKeyName, item.language);

                foreach (var itemItem in item.items)
                {
                    localizationItem.Add(itemItem.Key, itemItem.Value);
                }

                localization.Add(localizationItem);
            }
        }

        public static string GetShortName(string name)
        {
            string fullName = name;
            int lastSlash = fullName.LastIndexOf('/');
            if (lastSlash != -1)
            {
                if (lastSlash == fullName.Length - 1)
                {
                    fullName = fullName.Remove(lastSlash);
                    lastSlash = fullName.LastIndexOf('/');
                    if (lastSlash == -1)
                    {
                        return GetCodeFriendlyName(fullName);
                    }
                }
                return GetCodeFriendlyName(fullName.Substring(lastSlash + 1));
            }

            return GetCodeFriendlyName(fullName);
        }

        public static string GetCodeFriendlyName(string name)
        {
            name = name.Replace('/', '_').Replace(' ', '_');

            if (char.IsLetter(name[0]) == false)
                name = "_" + name;

            for (int charIndex = 0; charIndex < name.Length; charIndex++)
            {
                if (char.IsLetterOrDigit(name[charIndex]) == false && name[charIndex] != '_')
                {
                    name = name.Remove(charIndex, 1);
                    name = name.Insert(charIndex, "_");
                }
            }

            return name;
        }

        public string[] GetFilesToCopy(bool throwErrors = false)
        {
            List<string> files = new List<string>();

            FileInfo actionFileInfo = new FileInfo(SteamVR_Input.actionsFilePath);
            string path = actionFileInfo.Directory.FullName;

            files.Add(SteamVR_Input.actionsFilePath);

            foreach (var binding in default_bindings)
            {
                string bindingPath = Path.Combine(path, binding.binding_url);

                if (File.Exists(bindingPath))
                    files.Add(bindingPath);
                else
                {
                    if (throwErrors)
                    {
                        Debug.LogError("[SteamVR] Could not bind binding file specified by the actions.json manifest: " + bindingPath);
                    }
                }
            }

            return files.ToArray();
        }
    }
    public enum SteamVR_Input_ActionFile_DefailtBinding_ControllerTypes
    {
        vive,
        vive_pro,
        vive_controller,
        generic,
        holographic_controller,
        oculus_touch,
        gamepad,
        knuckles,
    }

    [System.Serializable]
    public class SteamVR_Input_ActionFile_DefaultBinding
    {
        public string controller_type;
        public string binding_url;
    }

    [System.Serializable]
    public class SteamVR_Input_ActionFile_ActionSet
    {
        [JsonIgnore]
        private const string actionSetInstancePrefix = "instance_";

        public string name;
        public string usage;

        [JsonIgnore]
        public string codeFriendlyName
        {
            get
            {
                return SteamVR_Input_ActionFile.GetCodeFriendlyName(name);
            }
        }

        [JsonIgnore]
        public string shortName
        {
            get
            {
                return SteamVR_Input_ActionFile.GetShortName(name);
            }
        }

        public void SetNewShortName(string newShortName)
        {
            name = "/actions/" + newShortName;
        }

        public string CreateNewName()
        {
            return "/actions/NewSet";
        }

        [JsonIgnore]
        public List<SteamVR_Input_ActionFile_Action> actionsInList = new List<SteamVR_Input_ActionFile_Action>();

        [JsonIgnore]
        public List<SteamVR_Input_ActionFile_Action> actionsOutList = new List<SteamVR_Input_ActionFile_Action>();
    }

    public enum SteamVR_Input_ActionFile_Action_Requirements
    {
        optional,
        suggested,
        mandatory,
    }

    [System.Serializable]
    public class SteamVR_Input_ActionFile_Action
    {
        [JsonIgnore]
        private static string[] _requirementValues;
        [JsonIgnore]
        public static string[] requirementValues
        {
            get
            {
                if (_requirementValues == null)
                    _requirementValues = System.Enum.GetNames(typeof(SteamVR_Input_ActionFile_Action_Requirements));

                return _requirementValues;
            }
        }

        public string name;
        public string type;
        public string scope;
        public string skeleton;
        public string requirement;

        [JsonIgnore]
        public SteamVR_Input_ActionFile_Action_Requirements requirementEnum
        {
            get
            {
                for (int index = 0; index < requirementValues.Length; index++)
                {
                    if (string.Equals(requirementValues[index], requirement, System.StringComparison.CurrentCultureIgnoreCase))
                    {
                        return (SteamVR_Input_ActionFile_Action_Requirements)index;
                    }
                }

                return SteamVR_Input_ActionFile_Action_Requirements.suggested;
            }
            set
            {
                requirement = value.ToString();
            }
        }

        [JsonIgnore]
        public string codeFriendlyName
        {
            get
            {
                return SteamVR_Input_ActionFile.GetCodeFriendlyName(name);
            }
        }

        [JsonIgnore]
        public string shortName
        {
            get
            {
                return SteamVR_Input_ActionFile.GetShortName(name);
            }
        }

        [JsonIgnore]
        public string path
        {
            get
            {
                int lastIndex = name.LastIndexOf('/');
                if (lastIndex != -1 && lastIndex + 1 < name.Length)
                {
                    return name.Substring(0, lastIndex + 1);
                }

                return name;
            }
        }

        public string CreateNewName(string actionSet, string direction)
        {
            return string.Format("{0}/{1}/NewAction", actionSet, direction);
        }

        [JsonIgnore]
        public SteamVR_ActionDirections direction
        {
            get
            {
                if (type.ToLower() == SteamVR_Input_ActionFile_ActionTypes.vibration)
                    return SteamVR_ActionDirections.Out;

                return SteamVR_ActionDirections.In;
            }
        }

        public override string ToString()
        {
            return shortName;
        }
    }

    public class SteamVR_Input_ActionFile_LocalizationItem
    {
        public const string languageTagKeyName = "language_tag";

        public string language;
        public Dictionary<string, string> items = new Dictionary<string, string>();

        public SteamVR_Input_ActionFile_LocalizationItem(string newLanguage)
        {
            language = newLanguage;
        }

        public SteamVR_Input_ActionFile_LocalizationItem(Dictionary<string, string> dictionary)
        {
            if (dictionary == null)
                return;

            if (dictionary.ContainsKey(languageTagKeyName))
                language = (string)dictionary[languageTagKeyName];
            else
                Debug.Log("[SteamVR] Input: Error in actions file, no language_tag in localization array item.");

            foreach (KeyValuePair<string, string> item in dictionary)
            {
                if (item.Key != languageTagKeyName)
                    items.Add(item.Key, (string)item.Value);
            }
        }
    }

    public class SteamVR_Input_ManifestFile
    {
        public string source;
        public List<SteamVR_Input_ManifestFile_Application> applications;
    }

    public class SteamVR_Input_ManifestFile_Application
    {
        public string app_key;
        public string launch_type;
        public string url;
        public string binary_path_windows;
        public string binary_path_linux;
        public string binary_path_osx;
        public string action_manifest_path;
        //public List<SteamVR_Input_ManifestFile_Application_Binding> bindings = new List<SteamVR_Input_ManifestFile_Application_Binding>();
        public string image_path;
        public Dictionary<string, SteamVR_Input_ManifestFile_ApplicationString> strings = new Dictionary<string, SteamVR_Input_ManifestFile_ApplicationString>();
    }

    public class SteamVR_Input_ManifestFile_ApplicationString
    {
        public string name;
    }

    public class SteamVR_Input_ManifestFile_Application_Binding
    {
        public string controller_type;
        public string binding_url;
    }

    public class SteamVR_Input_ManifestFile_Application_Binding_ControllerTypes
    {
        public static string oculus_touch = "oculus_touch";
        public static string vive_controller = "vive_controller";
        public static string knuckles = "knuckles";
    }

    static public class SteamVR_Input_ActionFile_ActionTypes
    {
        public static string boolean = "boolean";
        public static string vector1 = "vector1";
        public static string vector2 = "vector2";
        public static string vector3 = "vector3";
        public static string vibration = "vibration";
        public static string pose = "pose";
        public static string skeleton = "skeleton";

        public static string skeletonLeftPath = "\\skeleton\\hand\\left";
        public static string skeletonRightPath = "\\skeleton\\hand\\right";

        public static string[] listAll = new string[] { boolean, vector1, vector2, vector3, vibration, pose, skeleton };
        public static string[] listIn = new string[] { boolean, vector1, vector2, vector3, pose, skeleton };
        public static string[] listOut = new string[] { vibration };
        public static string[] listSkeletons = new string[] { skeletonLeftPath, skeletonRightPath };
    }

    static public class SteamVR_Input_ActionFile_ActionSet_Usages
    {
        public static string leftright = "leftright";
        public static string single = "single";
        public static string hidden = "hidden";

        public static string leftrightDescription = "per hand";
        public static string singleDescription = "mirrored";
        public static string hiddenDescription = "hidden";

        public static string[] listValues = new string[] { leftright, single, hidden };
        public static string[] listDescriptions = new string[] { leftrightDescription, singleDescription, hiddenDescription };
    }
}