using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefenseGame
{
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
				if (renderer == null)
				{
					mesh = null;
					bones = Array.Empty<Transform>();
					bindPoses = Array.Empty<Matrix4x4>();
					return false;
				}
				Mesh currentMesh = renderer.sharedMesh;
				if (currentMesh != mesh || bones.Length == 0 || bindPoses.Length == 0)
				{
					mesh = currentMesh;
					bones = renderer.bones ?? Array.Empty<Transform>();
					bindPoses = ((mesh != null) ? mesh.bindposes : Array.Empty<Matrix4x4>());
				}
				return mesh != null && bones.Length != 0 && bindPoses.Length != 0;
			}

			public void SetBatched(bool value)
			{
				batched = value;
				if (renderer != null)
				{
					renderer.enabled = !value && originalEnabled;
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
			if (owner == null)
			{
				return;
			}
			GpuSkinnedUnitRenderer proxy = owner.GetComponent<GpuSkinnedUnitRenderer>();
			if (!GpuSkinnedUnitBatchRenderer.CanUseForUnit(isDefender, isBoss))
			{
				if (proxy != null)
				{
					proxy.enabled = false;
				}
				return;
			}
			if (proxy == null)
			{
				proxy = owner.AddComponent<GpuSkinnedUnitRenderer>();
			}
			if (!proxy.enabled)
			{
				proxy.enabled = true;
			}
			proxy.Configure(renderers, tint);
		}

		public void SetFlash(Color color, float strength)
		{
			flashColor = color;
			flashStrength = Mathf.Max(0f, strength);
		}

		public void ClearFlash()
		{
			flashColor = Color.white;
			flashStrength = 0f;
		}

		private void Configure(Renderer[] renderers, Color tint)
		{
			instanceTint = tint;
			Dictionary<SkinnedMeshRenderer, bool> previousStates = new Dictionary<SkinnedMeshRenderer, bool>(sourceRenderers.Count);
			for (int i = 0; i < sourceRenderers.Count; i++)
			{
				SourceRenderer previous = sourceRenderers[i];
				if (previous.renderer != null)
				{
					previousStates[previous.renderer] = previous.originalEnabled;
				}
			}
			sourceRenderers.Clear();
			if (renderers != null)
			{
				foreach (Renderer renderer in renderers)
				{
					SkinnedMeshRenderer skinned = renderer as SkinnedMeshRenderer;
					if ((object)skinned != null && (!previousStates.ContainsKey(skinned) || previousStates[skinned]) && !sourceRenderers.Exists((SourceRenderer entry) => entry.renderer == skinned))
					{
						bool previousEnabled;
						bool originallyEnabled = (previousStates.TryGetValue(skinned, out previousEnabled) ? previousEnabled : skinned.enabled);
						sourceRenderers.Add(new SourceRenderer(skinned, originallyEnabled));
					}
				}
			}
			configured = sourceRenderers.Count > 0;
			if (configured && base.isActiveAndEnabled)
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
}
