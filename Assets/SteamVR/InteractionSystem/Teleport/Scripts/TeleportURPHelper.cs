using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Valve.VR.InteractionSystem
{
    [ExecuteInEditMode]
    public class TeleportURPHelper : MonoBehaviour
    {
#if UNITY_URP && UNITY_EDITOR
        void Start()
        {
            if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(this) == false)
                return;

            string teleportAssetPath = UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(this);
            GameObject teleportPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(teleportAssetPath);

            Teleport teleport = teleportPrefab.GetComponent<Teleport>();
            UnityEditor.SerializedObject serializedTeleport = new UnityEditor.SerializedObject(teleport);
            serializedTeleport.Update();
            bool changed = false;

            changed |= FindURPVersion(serializedTeleport, "areaHighlightedMaterial");

            changed |= FindURPVersion(serializedTeleport, "areaLockedMaterial");

            changed |= FindURPVersion(serializedTeleport, "areaVisibleMaterial");

            changed |= FindURPVersion(serializedTeleport, "pointHighlightedMaterial");

            changed |= FindURPVersion(serializedTeleport, "pointLockedMaterial");

            changed |= FindURPVersion(serializedTeleport, "pointVisibleMaterial");

            if (changed)
            {
                serializedTeleport.ApplyModifiedProperties();
                UnityEditor.EditorUtility.SetDirty(teleport);
            }

            TeleportArc arc = teleportPrefab.GetComponent<TeleportArc>();
            UnityEditor.SerializedObject serializedArc = new UnityEditor.SerializedObject(arc);
            serializedArc.Update();

            changed = FindURPVersion(serializedArc, "material");

            if (changed)
            {
                serializedArc.ApplyModifiedProperties();
                UnityEditor.EditorUtility.SetDirty(arc);
            }

        }

        private bool FindURPVersion(UnityEditor.SerializedObject teleportObject, string propertyName)
        {
            UnityEditor.SerializedProperty materialProp = teleportObject.FindProperty(propertyName);
            Material original = materialProp.objectReferenceValue as Material;
            if (original != null && !original.name.StartsWith("URP"))
            {
                string[] mats = UnityEditor.AssetDatabase.FindAssets(string.Format("URP{0} t:material", original.name));
                if (mats.Length > 0)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(mats[0]);
                    materialProp.objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
                    return true;
                }

            }

            return false;
        }
#endif
    }
}