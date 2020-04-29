using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Unity.XR.OpenVR
{
    public class OpenVRHelpers
    {
        public static bool IsUsingSteamVRInput()
        {
            return DoesTypeExist("SteamVR_Input");
        }

        public static bool DoesTypeExist(string className, bool fullname = false)
        {
            return GetType(className, fullname) != null;
        }

        public static Type GetType(string className, bool fullname = false)
        {
            Type foundType = null;
            if (fullname)
            {
                foundType = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                             from type in assembly.GetTypes()
                             where type.FullName == className
                             select type).FirstOrDefault();
            }
            else
            {
                foundType = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                             from type in assembly.GetTypes()
                             where type.Name == className
                             select type).FirstOrDefault();
            }

            return foundType;
        }

        public static string GetActionManifestPathFromPlugin()
        {
            Type steamvrInputType = GetType("SteamVR_Input");
            MethodInfo getPathMethod = steamvrInputType.GetMethod("GetActionsFilePath");
            object path = getPathMethod.Invoke(null, new object[] { false });

            return (string)path;
        }

        public static string GetActionManifestNameFromPlugin()
        {
            Type steamvrInputType = GetType("SteamVR_Input");
            MethodInfo getPathMethod = steamvrInputType.GetMethod("GetActionsFileName");
            object path = getPathMethod.Invoke(null, null);

            return (string)path;
        }

        public static string GetEditorAppKeyFromPlugin()
        {
            Type steamvrInputType = GetType("SteamVR_Input");
            MethodInfo getPathMethod = steamvrInputType.GetMethod("GetEditorAppKey");
            object path = getPathMethod.Invoke(null, null);

            return (string)path;
        }
    }
}