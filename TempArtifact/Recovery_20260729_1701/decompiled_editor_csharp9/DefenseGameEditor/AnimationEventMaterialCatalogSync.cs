using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DefenseGame;
using UnityEditor;
using UnityEngine;

namespace DefenseGameEditor
{
	[InitializeOnLoad]
	public static class AnimationEventMaterialCatalogSync
	{
		private const string DefaultConfigPath = "Assets/Data/DefenseGamePresentationConfig.asset";

		private static bool syncing;

		static AnimationEventMaterialCatalogSync()
		{
			EditorApplication.delayCall = (EditorApplication.CallbackFunction)Delegate.Combine(EditorApplication.delayCall, (EditorApplication.CallbackFunction)delegate
			{
				SyncAll(logSummary: false);
			});
		}

		[MenuItem("DefenseGame/Validation/Sync Animation Event Materials")]
		public static void SyncFromMenu()
		{
			SyncAll(logSummary: true);
		}

		public static string[] SyncAll(bool logSummary)
		{
			if (syncing)
			{
				return Array.Empty<string>();
			}
			syncing = true;
			try
			{
				Dictionary<string, HashSet<string>> required = CollectOverrideMaterialNames();
				Dictionary<string, List<Material>> available = CollectMaterials();
				List<string> unresolved = new List<string>();
				GamePresentationConfig config = AssetDatabase.LoadAssetAtPath<GamePresentationConfig>("Assets/Data/DefenseGamePresentationConfig.asset");
				if ((UnityEngine.Object)(object)config == null)
				{
					unresolved.Add("missing_config:Assets/Data/DefenseGamePresentationConfig.asset");
					return unresolved.ToArray();
				}
				List<Material> catalog = (config.animationEventMaterials ?? Array.Empty<Material>()).Where((Material material) => material != null).Distinct().ToList();
				foreach (KeyValuePair<string, HashSet<string>> entry in required.OrderBy((KeyValuePair<string, HashSet<string>> pair) => pair.Key, StringComparer.OrdinalIgnoreCase))
				{
					if (!available.TryGetValue(entry.Key, out var candidates) || candidates.Count == 0)
					{
						unresolved.Add(entry.Key);
						continue;
					}
					Material selected = SelectNearestMaterial(candidates, entry.Value);
					if (selected != null && !catalog.Contains(selected))
					{
						catalog.Add(selected);
					}
				}
				Material[] next = catalog.OrderBy((Material material) => material.name, StringComparer.OrdinalIgnoreCase).ToArray();
				Material[] current = config.animationEventMaterials ?? Array.Empty<Material>();
				if (!current.SequenceEqual(next))
				{
					Undo.RecordObject((UnityEngine.Object)(object)config, "Sync Animation Event Materials");
					config.animationEventMaterials = next;
					EditorUtility.SetDirty((UnityEngine.Object)(object)config);
					AssetDatabase.SaveAssets();
				}
				if (logSummary)
				{
					Debug.Log("[AnimationMaterialCatalog] " + next.Length + " material(s) registered. Unresolved: " + ((unresolved.Count == 0) ? "none" : string.Join(", ", unresolved)));
				}
				return unresolved.ToArray();
			}
			finally
			{
				syncing = false;
			}
		}

		private static Dictionary<string, HashSet<string>> CollectOverrideMaterialNames()
		{
			Dictionary<string, HashSet<string>> result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
			string[] clipGuids = AssetDatabase.FindAssets("t:AnimationClip");
			HashSet<string> visitedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < clipGuids.Length; i++)
			{
				string path = AssetDatabase.GUIDToAssetPath(clipGuids[i]);
				if (string.IsNullOrEmpty(path) || !visitedPaths.Add(path))
				{
					continue;
				}
				UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
				for (int assetIndex = 0; assetIndex < assets.Length; assetIndex++)
				{
					if (!(assets[assetIndex] is AnimationClip clip))
					{
						continue;
					}
					AnimationEvent[] events;
					try
					{
						events = AnimationUtility.GetAnimationEvents(clip);
					}
					catch
					{
						continue;
					}
					foreach (AnimationEvent animationEvent in events)
					{
						if (animationEvent == null || !string.Equals(animationEvent.functionName, "OverrideMaterial", StringComparison.OrdinalIgnoreCase))
						{
							continue;
						}
						string key = AnimationEventMaterialRegistry.NormalizeName(animationEvent.stringParameter);
						if (!string.IsNullOrEmpty(key))
						{
							if (!result.TryGetValue(key, out var sourcePaths))
							{
								sourcePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
								result.Add(key, sourcePaths);
							}
							sourcePaths.Add(path);
						}
					}
				}
			}
			return result;
		}

		private static Dictionary<string, List<Material>> CollectMaterials()
		{
			Dictionary<string, List<Material>> result = new Dictionary<string, List<Material>>(StringComparer.OrdinalIgnoreCase);
			string[] materialGuids = AssetDatabase.FindAssets("t:Material");
			HashSet<string> visitedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < materialGuids.Length; i++)
			{
				string path = AssetDatabase.GUIDToAssetPath(materialGuids[i]);
				if (string.IsNullOrEmpty(path) || !visitedPaths.Add(path))
				{
					continue;
				}
				UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
				for (int assetIndex = 0; assetIndex < assets.Length; assetIndex++)
				{
					if (assets[assetIndex] is Material material)
					{
						string key = AnimationEventMaterialRegistry.NormalizeName(material.name);
						if (!result.TryGetValue(key, out var materials))
						{
							materials = new List<Material>();
							result.Add(key, materials);
						}
						materials.Add(material);
					}
				}
			}
			return result;
		}

		private static Material SelectNearestMaterial(List<Material> candidates, HashSet<string> sourcePaths)
		{
			return (from candidate in candidates
				where candidate != null
				orderby sourcePaths.Any((string source) => SameDirectory(source, AssetDatabase.GetAssetPath(candidate))) descending
				select candidate).ThenBy((Material candidate) => AssetDatabase.GetAssetPath(candidate), StringComparer.OrdinalIgnoreCase).FirstOrDefault();
		}

		private static bool SameDirectory(string left, string right)
		{
			return string.Equals(Path.GetDirectoryName(left), Path.GetDirectoryName(right), StringComparison.OrdinalIgnoreCase);
		}
	}
}
