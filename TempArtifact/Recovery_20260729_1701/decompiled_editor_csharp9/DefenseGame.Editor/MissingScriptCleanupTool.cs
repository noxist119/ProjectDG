using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DefenseGame.Editor
{
	public static class MissingScriptCleanupTool
	{
		[MenuItem("DefenseGame/Tools/Remove Missing Scripts In Defense Prefabs")]
		public static void RemoveMissingScriptsInDefensePrefabs()
		{
			string[] searchFolders = ResolveExistingFolders("Assets/Prefabs", "Assets/Art/FX");
			if (searchFolders.Length == 0)
			{
				Debug.LogWarning("[DefenseGame] Missing script cleanup skipped: no prefab folders found.");
				return;
			}
			int scanned = 0;
			int changed = 0;
			int removed = 0;
			int sceneRemoved = 0;
			string[] guids = AssetDatabase.FindAssets("t:Prefab", searchFolders);
			for (int i = 0; i < guids.Length; i++)
			{
				string path = AssetDatabase.GUIDToAssetPath(guids[i]);
				if (string.IsNullOrWhiteSpace(path))
				{
					continue;
				}
				scanned++;
				GameObject root = PrefabUtility.LoadPrefabContents(path);
				try
				{
					int removedFromPrefab = RemoveMissingScriptsRecursive(root);
					if (removedFromPrefab > 0)
					{
						removed += removedFromPrefab;
						changed++;
						PrefabUtility.SaveAsPrefabAsset(root, path);
					}
				}
				finally
				{
					PrefabUtility.UnloadPrefabContents(root);
				}
			}
			sceneRemoved += RemoveMissingScriptsInScene("Assets/Scenes/DG.unity");
			AssetDatabase.SaveAssets();
			Debug.Log("[DefenseGame] Missing script cleanup complete. prefabScanned=" + scanned + ", prefabChanged=" + changed + ", prefabRemoved=" + removed + ", sceneRemoved=" + sceneRemoved);
		}

		public static void RemoveMissingScriptsInDefensePrefabsAndExit()
		{
			try
			{
				RemoveMissingScriptsInDefensePrefabs();
				EditorApplication.Exit(0);
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				EditorApplication.Exit(1);
			}
		}

		private static string[] ResolveExistingFolders(params string[] folders)
		{
			List<string> existing = new List<string>();
			foreach (string folder in folders)
			{
				if (!string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder))
				{
					existing.Add(folder);
				}
			}
			return existing.ToArray();
		}

		private static int RemoveMissingScriptsRecursive(GameObject root)
		{
			if (root == null)
			{
				return 0;
			}
			int removed = 0;
			Transform[] transforms = root.GetComponentsInChildren<Transform>(includeInactive: true);
			for (int i = 0; i < transforms.Length; i++)
			{
				if (transforms[i] != null)
				{
					removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transforms[i].gameObject);
				}
			}
			return removed;
		}

		private static int RemoveMissingScriptsInScene(string scenePath)
		{
			if (string.IsNullOrWhiteSpace(scenePath) || !File.Exists(scenePath))
			{
				return 0;
			}
			Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
			if (!scene.IsValid())
			{
				return 0;
			}
			int removed = 0;
			GameObject[] roots = scene.GetRootGameObjects();
			for (int i = 0; i < roots.Length; i++)
			{
				removed += RemoveMissingScriptsRecursive(roots[i]);
			}
			if (removed > 0)
			{
				EditorSceneManager.MarkSceneDirty(scene);
				EditorSceneManager.SaveScene(scene);
			}
			return removed;
		}
	}
}
