using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefenseGame;

[DisallowMultipleComponent]
public sealed class GpuSkinnedUnitRenderer : MonoBehaviour
{
	internal sealed class SourceRenderer
	{
		public readonly SkinnedMeshRenderer renderer;

		public readonly bool originalEnabled;

		private bool batched;

		public Mesh mesh { get; private set; }

		public Transform[] bones { get; private set; } = Array.Empty<Transform>();

		public Matrix4x4[] bindPoses { get; private set; } = Array.Empty<Matrix4x4>();

		public bool IsBatched => batched;

		public SourceRenderer(SkinnedMeshRenderer source, bool enabledBeforeBatching)
		{
			renderer = source;
			originalEnabled = enabledBeforeBatching;
			RefreshGeometry();
		}

		public bool RefreshGeometry()
		{
			if ((Object)(object)renderer == (Object)null)
			{
				mesh = null;
				bones = Array.Empty<Transform>();
				bindPoses = Array.Empty<Matrix4x4>();
				return false;
			}
			Mesh sharedMesh = renderer.sharedMesh;
			if ((Object)(object)sharedMesh != (Object)(object)mesh || bones.Length == 0 || bindPoses.Length == 0)
			{
				mesh = sharedMesh;
				bones = renderer.bones ?? Array.Empty<Transform>();
				bindPoses = (((Object)(object)mesh != (Object)null) ? mesh.bindposes : Array.Empty<Matrix4x4>());
			}
			return (Object)(object)mesh != (Object)null && bones.Length != 0 && bindPoses.Length != 0;
		}

		public void SetBatched(bool value)
		{
			batched = value;
			if ((Object)(object)renderer != (Object)null)
			{
				((Renderer)renderer).enabled = !value && originalEnabled;
			}
		}
	}

	private readonly List<SourceRenderer> sourceRenderers = new List<SourceRenderer>(2);

	private Color instanceTint = Color.white;

	private Color flashColor = Color.white;

	private float flashStrength;

	private bool configured;

	internal IReadOnlyList<SourceRenderer> SourceRenderers => sourceRenderers;

	internal Color InstanceTint => instanceTint;

	internal Color FlashColor => flashColor;

	internal float FlashStrength => flashStrength;

	public static void AttachOrRefresh(GameObject owner, Renderer[] renderers, Color tint, bool isDefender, bool isBoss)
	{
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)owner == (Object)null)
		{
			return;
		}
		GpuSkinnedUnitRenderer gpuSkinnedUnitRenderer = owner.GetComponent<GpuSkinnedUnitRenderer>();
		if (!GpuSkinnedUnitBatchRenderer.CanUseForUnit(isDefender, isBoss))
		{
			if ((Object)(object)gpuSkinnedUnitRenderer != (Object)null)
			{
				((Behaviour)gpuSkinnedUnitRenderer).enabled = false;
			}
			return;
		}
		if ((Object)(object)gpuSkinnedUnitRenderer == (Object)null)
		{
			gpuSkinnedUnitRenderer = owner.AddComponent<GpuSkinnedUnitRenderer>();
		}
		if (!((Behaviour)gpuSkinnedUnitRenderer).enabled)
		{
			((Behaviour)gpuSkinnedUnitRenderer).enabled = true;
		}
		gpuSkinnedUnitRenderer.Configure(renderers, tint);
	}

	public void SetFlash(Color color, float strength)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		flashColor = color;
		flashStrength = Mathf.Max(0f, strength);
	}

	public void ClearFlash()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		flashColor = Color.white;
		flashStrength = 0f;
	}

	private void Configure(Renderer[] renderers, Color tint)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		instanceTint = tint;
		Dictionary<SkinnedMeshRenderer, bool> dictionary = new Dictionary<SkinnedMeshRenderer, bool>(sourceRenderers.Count);
		for (int i = 0; i < sourceRenderers.Count; i++)
		{
			SourceRenderer sourceRenderer = sourceRenderers[i];
			if ((Object)(object)sourceRenderer.renderer != (Object)null)
			{
				dictionary[sourceRenderer.renderer] = sourceRenderer.originalEnabled;
			}
		}
		sourceRenderers.Clear();
		if (renderers != null)
		{
			foreach (Renderer val in renderers)
			{
				SkinnedMeshRenderer skinned = (SkinnedMeshRenderer)(object)((val is SkinnedMeshRenderer) ? val : null);
				if (skinned != null && (!dictionary.ContainsKey(skinned) || dictionary[skinned]) && !sourceRenderers.Exists((SourceRenderer entry) => (Object)(object)entry.renderer == (Object)(object)skinned))
				{
					bool value;
					bool enabledBeforeBatching = (dictionary.TryGetValue(skinned, out value) ? value : ((Renderer)skinned).enabled);
					sourceRenderers.Add(new SourceRenderer(skinned, enabledBeforeBatching));
				}
			}
		}
		configured = sourceRenderers.Count > 0;
		if (configured && ((Behaviour)this).isActiveAndEnabled)
		{
			GpuSkinnedUnitBatchRenderer.Register(this);
		}
	}

	private void OnEnable()
	{
		if (configured)
		{
			GpuSkinnedUnitBatchRenderer.Register(this);
		}
	}

	private void OnDisable()
	{
		RestoreRenderers();
		GpuSkinnedUnitBatchRenderer.Unregister(this);
	}

	private void OnDestroy()
	{
		RestoreRenderers();
		GpuSkinnedUnitBatchRenderer.Unregister(this);
	}

	private void RestoreRenderers()
	{
		for (int i = 0; i < sourceRenderers.Count; i++)
		{
			sourceRenderers[i].SetBatched(value: false);
		}
	}
}
