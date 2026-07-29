using System;
using System.Collections.Generic;
using DefenseGame;
using UnityEditor;
using UnityEngine;

internal static class GpuSkinnedUnitBatchingValidator
{
	private const string ShaderName = "DefenseGame/Mobile GPU Skinned Unit";

	private const string SessionValidationKey = "DefenseGame.GpuSkinBatch.Validation";

	[InitializeOnLoadMethod]
	private static void QueueFirstSessionValidation()
	{
		if (!SessionState.GetBool("DefenseGame.GpuSkinBatch.Validation", defaultValue: false))
		{
			SessionState.SetBool("DefenseGame.GpuSkinBatch.Validation", value: true);
			EditorApplication.delayCall = (EditorApplication.CallbackFunction)Delegate.Combine(EditorApplication.delayCall, (EditorApplication.CallbackFunction)delegate
			{
				Validate(verbose: false);
			});
		}
	}

	[MenuItem("Tools/Defense Game/Validate GPU Unit Batching")]
	private static void ValidateFromMenu()
	{
		Validate(verbose: true);
	}

	public static void ValidateFromCommandLine()
	{
		Validate(verbose: true);
	}

	private static void Validate(bool verbose)
	{
		Shader shader = Shader.Find("DefenseGame/Mobile GPU Skinned Unit");
		if (shader == null)
		{
			Debug.LogError("[GpuSkinValidation] Missing shader 'DefenseGame/Mobile GPU Skinned Unit'.");
			return;
		}
		bool shaderHasError = ShaderUtil.ShaderHasError(shader);
		int prefabCount = 0;
		int skinnedRendererCount = 0;
		int eligibleRendererCount = 0;
		int blendShapeExclusions = 0;
		int multiMaterialExclusions = 0;
		int skeletonExclusions = 0;
		HashSet<GameObject> prefabs = CollectConfiguredUnitPrefabs();
		foreach (GameObject prefab in prefabs)
		{
			if (prefab == null)
			{
				continue;
			}
			prefabCount++;
			SkinnedMeshRenderer[] renderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
			foreach (SkinnedMeshRenderer renderer in renderers)
			{
				Mesh mesh = renderer.sharedMesh;
				skinnedRendererCount++;
				if (mesh == null || mesh.bindposes == null || mesh.bindposes.Length == 0 || renderer.bones == null || renderer.bones.Length != mesh.bindposes.Length)
				{
					skeletonExclusions++;
					continue;
				}
				if (mesh.blendShapeCount > 0)
				{
					blendShapeExclusions++;
					continue;
				}
				Material[] materials = renderer.sharedMaterials;
				if (mesh.subMeshCount != 1 || materials == null || materials.Length != 1 || materials[0] == null || materials[0].renderQueue > 2500)
				{
					multiMaterialExclusions++;
				}
				else
				{
					eligibleRendererCount++;
				}
			}
		}
		string summary = $"[GpuSkinValidation] shaderSupported={shader.isSupported}, shaderHasError={shaderHasError}, " + $"configuredPrefabs={prefabCount}, skinnedRenderers={skinnedRendererCount}, " + $"eligible={eligibleRendererCount}, blendShapeExcluded={blendShapeExclusions}, " + $"multiMaterialOrTransparentExcluded={multiMaterialExclusions}, " + $"skeletonExcluded={skeletonExclusions}.";
		if (shaderHasError || eligibleRendererCount == 0)
		{
			Debug.LogWarning(summary);
		}
		else if (verbose)
		{
			Debug.Log(summary);
		}
	}

	private static HashSet<GameObject> CollectConfiguredUnitPrefabs()
	{
		HashSet<GameObject> prefabs = new HashSet<GameObject>();
		string[] configGuids = AssetDatabase.FindAssets("t:GamePresentationConfig");
		for (int configIndex = 0; configIndex < configGuids.Length; configIndex++)
		{
			string path = AssetDatabase.GUIDToAssetPath(configGuids[configIndex]);
			GamePresentationConfig config = AssetDatabase.LoadAssetAtPath<GamePresentationConfig>(path);
			if ((UnityEngine.Object)(object)config == null)
			{
				continue;
			}
			Add(prefabs, config.defaultDefenderPrefab);
			Add(prefabs, config.summonedDefenderPrefab);
			Add(prefabs, config.defaultMonsterPrefab);
			for (int i = 0; i < config.characterOverrides.Count; i++)
			{
				CharacterPresentationOverride entry = config.characterOverrides[i];
				if (entry != null)
				{
					Add(prefabs, entry.prefab);
				}
			}
			for (int j = 0; j < config.monsterOverrides.Count; j++)
			{
				MonsterPresentationOverride entry2 = config.monsterOverrides[j];
				if (entry2 != null)
				{
					Add(prefabs, entry2.prefab);
				}
			}
		}
		return prefabs;
	}

	private static void Add(HashSet<GameObject> prefabs, GameObject prefab)
	{
		if (prefab != null)
		{
			prefabs.Add(prefab);
		}
	}
}
