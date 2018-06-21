using UnityEngine;
using System.Collections;

public class SteamVR_Input_ActivateSetOnLoad : MonoBehaviour
{
    public SteamVR_Input_ActionSet actionSet;

    public bool disableAllOtherActionSets = false;

    private void Start()
    {
        if (actionSet != null)
        {
            Debug.Log(string.Format("[SteamVR] Activating {0} action set.", actionSet.fullPath));
            actionSet.ActivatePrimary(disableAllOtherActionSets);
        }
    }
}
