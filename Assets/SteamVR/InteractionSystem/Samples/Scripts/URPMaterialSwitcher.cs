using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR && UNITY_URP
using UnityEditor;
#endif

[ExecuteInEditMode]
public class URPMaterialSwitcher : MonoBehaviour
{
	public bool children = false;

#if UNITY_EDITOR && UNITY_URP

	private const string searchTemplate = "URP{0} t:material";
	void Start()
	{
		Renderer renderer;

		if (children)
			renderer = this.GetComponentInChildren<Renderer>();
		else
			renderer = this.GetComponent<Renderer>();

		if (renderer.sharedMaterial.name.StartsWith("URP") == false)
		{
			string[] mats = UnityEditor.AssetDatabase.FindAssets(string.Format(searchTemplate, renderer.sharedMaterial.name));
			if (mats.Length > 0)
			{
				string path = UnityEditor.AssetDatabase.GUIDToAssetPath(mats[0]);

				if (PrefabUtility.IsPartOfPrefabInstance(this))
				{
					string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(this);
					GameObject myPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
					URPMaterialSwitcher[] switchers = myPrefab.GetComponentsInChildren<URPMaterialSwitcher>(true);
					foreach (var switcher in switchers)
					{
						switcher.Execute();
					}
					EditorUtility.SetDirty(myPrefab);
				}
				else
				{
					this.Execute();
				}
			}
		}
	}

	public void Execute()
	{
		if (children)
		{
			Renderer[] renderers = this.GetComponentsInChildren<Renderer>();
			foreach (var renderer in renderers)
				SwitchRenderer(renderer);
		}
		else
		{
			SwitchRenderer(this.GetComponent<Renderer>());
		}
	}

	private void SwitchRenderer(Renderer renderer)
	{ 
		if (renderer != null && renderer.sharedMaterial.name.StartsWith("URP") == false)
		{
			string[] foundMaterials = UnityEditor.AssetDatabase.FindAssets(string.Format(searchTemplate, renderer.sharedMaterial.name));
			if (foundMaterials.Length > 0)
			{
				string urpMaterialPath = UnityEditor.AssetDatabase.GUIDToAssetPath(foundMaterials[0]);
				Material urpMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(urpMaterialPath);

				SerializedObject serializedRenderer = new SerializedObject(renderer);
				serializedRenderer.Update();

				SerializedProperty materialProp = serializedRenderer.FindProperty("m_Materials");
				materialProp.ClearArray();
				materialProp.InsertArrayElementAtIndex(0);
				materialProp.GetArrayElementAtIndex(0).objectReferenceValue = urpMaterial;

				serializedRenderer.ApplyModifiedProperties();
				if (PrefabUtility.IsPartOfPrefabInstance(renderer))
				{
					PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
					UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(this.gameObject.scene);
				}
			}
		}
	}
#endif
}
