using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DefenseGame;

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
		string text = NormalizeName(materialName);
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		if (Materials.TryGetValue(text, out var value) && (Object)(object)value != (Object)null)
		{
			return value;
		}
		Material[] array = Resources.FindObjectsOfTypeAll<Material>();
		foreach (Material val in array)
		{
			if ((Object)(object)val != (Object)null && string.Equals(NormalizeName(((Object)val).name), text, StringComparison.OrdinalIgnoreCase))
			{
				Register(val);
				return val;
			}
		}
		string[] array2 = AssetDatabase.FindAssets("t:Material");
		for (int j = 0; j < array2.Length; j++)
		{
			string text2 = AssetDatabase.GUIDToAssetPath(array2[j]);
			Object[] array3 = AssetDatabase.LoadAllAssetsAtPath(text2);
			foreach (Object obj in array3)
			{
				Material val2 = (Material)(object)((obj is Material) ? obj : null);
				if (val2 != null && string.Equals(NormalizeName(((Object)val2).name), text, StringComparison.OrdinalIgnoreCase))
				{
					Register(val2);
					return val2;
				}
			}
		}
		return null;
	}

	public static Material[] GetConfiguredMaterials()
	{
		List<Material> list = new List<Material>();
		foreach (Material value in Materials.Values)
		{
			if ((Object)(object)value != (Object)null && !list.Contains(value))
			{
				list.Add(value);
			}
		}
		return list.ToArray();
	}

	public static string NormalizeName(string materialName)
	{
		if (string.IsNullOrWhiteSpace(materialName))
		{
			return string.Empty;
		}
		string text = materialName.Trim();
		if (text.EndsWith(".mat", StringComparison.OrdinalIgnoreCase))
		{
			text = text.Substring(0, text.Length - 4).TrimEnd();
		}
		if (text.EndsWith(" (Instance)", StringComparison.OrdinalIgnoreCase))
		{
			text = text.Substring(0, text.Length - " (Instance)".Length).TrimEnd();
		}
		return text;
	}

	private static void Register(Material material)
	{
		if (!((Object)(object)material == (Object)null))
		{
			string text = NormalizeName(((Object)material).name);
			if (!string.IsNullOrEmpty(text))
			{
				Materials[text] = material;
			}
		}
	}
}
