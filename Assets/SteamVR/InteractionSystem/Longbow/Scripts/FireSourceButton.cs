using UnityEditor;
using UnityEngine;
using Valve.VR.InteractionSystem;

[CustomEditor(typeof(FireSource))]
public class FireSourceButton : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        FireSource fire = (FireSource)target;
        if (GUILayout.Button("Set on fire"))
        {
            fire.FireExposure();
        }
    }
}
