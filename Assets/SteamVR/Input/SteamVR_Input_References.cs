using UnityEngine;
using System.Collections;

public class SteamVR_Input_References : ScriptableObject
{
    private static SteamVR_Input_References _instance;
    public static SteamVR_Input_References instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<SteamVR_Input_References>("SteamVR_Input_References");

#if UNITY_EDITOR
                if (_instance == null)
                {
                    _instance = ScriptableObject.CreateInstance<SteamVR_Input_References>();

                    string folderPath = SteamVR_Input.GetResourcesFolderPath(true);
                    string assetPath = System.IO.Path.Combine(folderPath, "SteamVR_Input_References.asset");

                    UnityEditor.AssetDatabase.CreateAsset(_instance, assetPath);
                    UnityEditor.AssetDatabase.SaveAssets();
                }
#endif
            }

            return _instance;
        }
    }

    public string[] actionSetNames;
    public SteamVR_Input_ActionSet[] actionSetObjects;

    public string[] actionNames;
    public SteamVR_Input_Action[] actionObjects;

    public static SteamVR_Input_Action GetAction(string name)
    {
        for (int nameIndex = 0; nameIndex < instance.actionNames.Length; nameIndex++)
        {
            if (instance.actionNames[nameIndex] == name)
                return instance.actionObjects[nameIndex];
        }

        return null;
    }

    public static SteamVR_Input_ActionSet GetActionSet(string set)
    {
        for (int setIndex = 0; setIndex < instance.actionSetNames.Length; setIndex++)
        {
            if (instance.actionSetNames[setIndex] == set)
                return instance.actionSetObjects[setIndex];
        }
        return null;
    }
}
