using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefenseGame;

public sealed class AnimationMaterialOverrideController : MonoBehaviour
{
	private sealed class RendererSnapshot
	{
		public Renderer renderer;

		public Material[] materials;
	}

	private sealed class OverrideFrame
	{
		public string key;

		public readonly List<RendererSnapshot> snapshots = new List<RendererSnapshot>();
	}

	private readonly List<Renderer> targetRenderers = new List<Renderer>();

	private readonly List<OverrideFrame> overrideFrames = new List<OverrideFrame>();

	private readonly HashSet<string> reportedMissingMaterials = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private readonly HashSet<string> reportedResetMismatches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private void Awake()
	{
		RefreshTargetRenderers();
	}

	private void OnDisable()
	{
		RestoreAll();
	}

	public bool OverrideMaterial(string materialName)
	{
		string text = AnimationEventMaterialRegistry.NormalizeName(materialName);
		Material val = AnimationEventMaterialRegistry.Resolve(text);
		if ((Object)(object)val == (Object)null)
		{
			ReportMissingMaterial(text);
			return false;
		}
		RefreshTargetRenderers();
		if (targetRenderers.Count == 0)
		{
			return false;
		}
		if (overrideFrames.Count > 0 && string.Equals(overrideFrames[overrideFrames.Count - 1].key, text, StringComparison.OrdinalIgnoreCase))
		{
			ApplyMaterial(val);
			return true;
		}
		OverrideFrame overrideFrame = new OverrideFrame
		{
			key = text
		};
		for (int i = 0; i < targetRenderers.Count; i++)
		{
			Renderer val2 = targetRenderers[i];
			if (!((Object)(object)val2 == (Object)null))
			{
				overrideFrame.snapshots.Add(new RendererSnapshot
				{
					renderer = val2,
					materials = val2.sharedMaterials
				});
			}
		}
		if (overrideFrame.snapshots.Count == 0)
		{
			return false;
		}
		overrideFrames.Add(overrideFrame);
		ApplyMaterial(val);
		return true;
	}

	public bool ResetMaterial(string materialName)
	{
		if (overrideFrames.Count == 0)
		{
			return false;
		}
		string text = AnimationEventMaterialRegistry.NormalizeName(materialName);
		int num = FindFrameIndex(text);
		if (num < 0)
		{
			string text2 = (string.IsNullOrEmpty(text) ? "<empty>" : text);
			if (reportedResetMismatches.Add(text2))
			{
				Debug.LogWarning((object)("[AnimationMaterial] ResetMaterial key '" + text2 + "' did not match the active override on " + ((Object)this).name + ". Restoring the latest material snapshot."), (Object)(object)this);
			}
			num = overrideFrames.Count - 1;
		}
		for (int num2 = overrideFrames.Count - 1; num2 >= num; num2--)
		{
			RestoreFrame(overrideFrames[num2]);
			overrideFrames.RemoveAt(num2);
		}
		return true;
	}

	public void RestoreAll()
	{
		for (int num = overrideFrames.Count - 1; num >= 0; num--)
		{
			RestoreFrame(overrideFrames[num]);
		}
		overrideFrames.Clear();
	}

	private int FindFrameIndex(string key)
	{
		if (string.IsNullOrEmpty(key))
		{
			return overrideFrames.Count - 1;
		}
		for (int num = overrideFrames.Count - 1; num >= 0; num--)
		{
			if (string.Equals(overrideFrames[num].key, key, StringComparison.OrdinalIgnoreCase))
			{
				return num;
			}
		}
		return -1;
	}

	private void RefreshTargetRenderers()
	{
		for (int num = targetRenderers.Count - 1; num >= 0; num--)
		{
			if ((Object)(object)targetRenderers[num] == (Object)null)
			{
				targetRenderers.RemoveAt(num);
			}
		}
		Renderer[] componentsInChildren = ((Component)this).GetComponentsInChildren<Renderer>(true);
		foreach (Renderer val in componentsInChildren)
		{
			if (!((Object)(object)val == (Object)null) && (val is MeshRenderer || val is SkinnedMeshRenderer) && !targetRenderers.Contains(val))
			{
				targetRenderers.Add(val);
			}
		}
	}

	private void ApplyMaterial(Material targetMaterial)
	{
		for (int i = 0; i < targetRenderers.Count; i++)
		{
			Renderer val = targetRenderers[i];
			if (!((Object)(object)val == (Object)null))
			{
				Material[] sharedMaterials = val.sharedMaterials;
				int num = Mathf.Max(1, sharedMaterials.Length);
				Material[] array = (Material[])(object)new Material[num];
				for (int j = 0; j < array.Length; j++)
				{
					array[j] = targetMaterial;
				}
				val.sharedMaterials = array;
				RuntimeRenderBatchingUtility.PrepareRenderer(val);
			}
		}
	}

	private static void RestoreFrame(OverrideFrame frame)
	{
		if (frame == null)
		{
			return;
		}
		for (int i = 0; i < frame.snapshots.Count; i++)
		{
			RendererSnapshot rendererSnapshot = frame.snapshots[i];
			if (!((Object)(object)rendererSnapshot?.renderer == (Object)null))
			{
				rendererSnapshot.renderer.sharedMaterials = rendererSnapshot.materials ?? Array.Empty<Material>();
				RuntimeRenderBatchingUtility.PrepareRenderer(rendererSnapshot.renderer);
			}
		}
	}

	private void ReportMissingMaterial(string key)
	{
		string text = (string.IsNullOrEmpty(key) ? "<empty>" : key);
		if (reportedMissingMaterials.Add(text))
		{
			Debug.LogWarning((object)("[AnimationMaterial] OverrideMaterial could not resolve material '" + text + "' on " + ((Object)this).name + ". Add a Material asset with the same name; the catalog sync will include it in builds."), (Object)(object)this);
		}
	}
}
