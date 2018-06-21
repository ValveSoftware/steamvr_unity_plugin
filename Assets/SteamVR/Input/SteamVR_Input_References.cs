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

                if (_instance == null)
                {
                    _instance = ScriptableObject.CreateInstance<SteamVR_Input_References>();
#if UNITY_EDITOR
                    string folderPath = SteamVR.GetResourcesFolderPath(true);
                    string assetPath = System.IO.Path.Combine(folderPath, "SteamVR_Input_References.asset");

                    UnityEditor.AssetDatabase.CreateAsset(_instance, assetPath);
                    UnityEditor.AssetDatabase.SaveAssets();
#endif
                }
            }

            return _instance;
        }
    }

    public string[] actionSetNames;
    public SteamVR_Input_ActionSet[] actionSetObjects;

    public string[] actionNames;
    public SteamVR_Input_Action[] actionObjects;
}
