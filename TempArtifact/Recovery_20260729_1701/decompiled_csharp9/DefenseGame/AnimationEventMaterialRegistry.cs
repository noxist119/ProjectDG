using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DefenseGame
{
	public static class AnimationEventMaterialRegistry
	{
		private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);

		public static void Configure(IEnumerable<Material> materials)
		{
			Materials.Clear();
			if (materials == null)
			{
				return;
			}
			foreach (Material material in materials)
			{
				Register(material);
			}
		}

		public static Material Resolve(string materialName)
		{
			string key = NormalizeName(materialName);
			if (string.IsNullOrEmpty(key))
			{
				return null;
			}
			if (Materials.TryGetValue(key, out var configured) && configured != null)
			{
				return configured;
			}
			Material[] loaded = Resources.FindObjectsOfTypeAll<Material>();
			foreach (Material candidate in loaded)
			{
				if (candidate != null && string.Equals(NormalizeName(candidate.name), key, StringComparison.OrdinalIgnoreCase))
				{
					Register(candidate);
					return candidate;
				}
			}
			string[] guids = AssetDatabase.FindAssets("t:Material");
			for (int j = 0; j < guids.Length; j++)
			{
				string path = AssetDatabase.GUIDToAssetPath(guids[j]);
				UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
				for (int assetIndex = 0; assetIndex < assets.Length; assetIndex++)
				{
					if (assets[assetIndex] is Material candidate2 && string.Equals(NormalizeName(candidate2.name), key, StringComparison.OrdinalIgnoreCase))
					{
						Register(candidate2);
						return candidate2;
					}
				}
			}
			return null;
		}

		public static Material[] GetConfiguredMaterials()
		{
			List<Material> result = new List<Material>();
			foreach (Material material in Materials.Values)
			{
				if (material != null && !result.Contains(material))
				{
					result.Add(material);
				}
			}
			return result.ToArray();
		}

		public static string NormalizeName(string materialName)
		{
			if (string.IsNullOrWhiteSpace(materialName))
			{
				return string.Empty;
			}
			string normalized = materialName.Trim();
			if (normalized.EndsWith(".mat", StringComparison.OrdinalIgnoreCase))
			{
				normalized = normalized.Substring(0, normalized.Length - 4).TrimEnd();
			}
			if (normalized.EndsWith(" (Instance)", StringComparison.OrdinalIgnoreCase))
			{
				normalized = normalized.Substring(0, normalized.Length - " (Instance)".Length).TrimEnd();
			}
			return normalized;
		}

		private static void Register(Material material)
		{
			if (!(material == null))
			{
				string key = NormalizeName(material.name);
				if (!string.IsNullOrEmpty(key))
				{
					Materials[key] = material;
				}
			}
		}
	}
}
