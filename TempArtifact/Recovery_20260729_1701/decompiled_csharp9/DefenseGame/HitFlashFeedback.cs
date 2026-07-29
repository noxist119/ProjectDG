using System.Collections;
using UnityEngine;

namespace DefenseGame
{
	public class HitFlashFeedback : MonoBehaviour
	{
		[SerializeField]
		private Renderer[] targetRenderers;

		[SerializeField]
		private float flashDuration = 0.12f;

		[SerializeField]
		private float flashStrength = 1f;

		[SerializeField]
		private float criticalFlashStrength = 1.35f;

		[SerializeField]
		private Color flashColor = Color.white;

		private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

		private static readonly int ColorId = Shader.PropertyToID("_Color");

		private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

		private static readonly int RimColorId = Shader.PropertyToID("_RimColor");

		private static readonly int RimPowerId = Shader.PropertyToID("_RimPower");

		private static readonly int RimIntensityId = Shader.PropertyToID("_RimIntensity");

		private static readonly int FresnelColorId = Shader.PropertyToID("_FresnelColor");

		private static readonly int FresnelPowerId = Shader.PropertyToID("_FresnelPower");

		private static readonly int FresnelIntensityId = Shader.PropertyToID("_FresnelIntensity");

		private static readonly int EdgeColorId = Shader.PropertyToID("_EdgeColor");

		private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");

		private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");

		private const float RimReplayInterval = 0.07f;

		private MaterialPropertyBlock propertyBlock;

		private Coroutine flashRoutine;

		private bool materialsPrepared;

		private Color baseColor = Color.white;

		private Color feedbackColor = Color.white;

		private bool hasBaseColor;

		private bool hasFeedbackColor;

		private float nextRimTime;

		private GpuSkinnedUnitRenderer gpuSkinnedRenderer;

		private void Awake()
		{
			gpuSkinnedRenderer = GetComponent<GpuSkinnedUnitRenderer>();
			EnsurePropertyBlock();
			CacheRenderersIfNeeded();
			EnableEmissionOnMaterials();
		}

		public void Configure(Renderer[] renderers)
		{
			Configure(renderers, Color.white, useBaseColor: false);
		}

		public void Configure(Renderer[] renderers, Color newBaseColor)
		{
			Configure(renderers, newBaseColor, useBaseColor: true);
		}

		public void Configure(Renderer[] renderers, Color newFeedbackColor, bool useBaseColor)
		{
			targetRenderers = renderers;
			gpuSkinnedRenderer = GetComponent<GpuSkinnedUnitRenderer>();
			baseColor = newFeedbackColor;
			feedbackColor = newFeedbackColor;
			hasBaseColor = useBaseColor;
			hasFeedbackColor = true;
			materialsPrepared = false;
			CacheRenderersIfNeeded();
			EnableEmissionOnMaterials();
			RestoreBaseTint();
		}

		public void PlayHit(bool critical)
		{
			EnsurePropertyBlock();
			CacheRenderersIfNeeded();
			if (!materialsPrepared)
			{
				EnableEmissionOnMaterials();
			}
			if (targetRenderers != null && targetRenderers.Length != 0)
			{
				if (flashRoutine != null)
				{
					StopCoroutine(flashRoutine);
				}
				PlayVisibleRim(critical);
				flashRoutine = StartCoroutine(FlashRoutine(critical ? criticalFlashStrength : flashStrength));
			}
		}

		private IEnumerator FlashRoutine(float intensity)
		{
			float elapsed = 0f;
			while (elapsed < flashDuration)
			{
				elapsed += Time.deltaTime;
				float normalized = Mathf.Clamp01(elapsed / flashDuration);
				float pulse = 1f - normalized;
				ApplyFlash(pulse * intensity);
				yield return null;
			}
			RestoreBaseTint();
			flashRoutine = null;
		}

		private void ApplyFlash(float intensity)
		{
			EnsurePropertyBlock();
			if (propertyBlock == null || targetRenderers == null)
			{
				return;
			}
			Color sourceColor = (hasBaseColor ? baseColor : Color.white);
			Color signalColor = ResolveFeedbackColor(critical: false);
			Color tintedFlash = Color.Lerp(signalColor, flashColor, 0.48f) * Mathf.Lerp(0.95f, 1.55f, Mathf.Clamp01(intensity));
			if (gpuSkinnedRenderer == null)
			{
				gpuSkinnedRenderer = GetComponent<GpuSkinnedUnitRenderer>();
			}
			gpuSkinnedRenderer?.SetFlash(tintedFlash, intensity);
			Color emission = tintedFlash * (1.8f * intensity);
			float outlineWidth = 1.15f * intensity;
			float rimPower = Mathf.Lerp(3.4f, 1.45f, Mathf.Clamp01(intensity));
			float rimIntensity = 2.1f * intensity;
			for (int i = 0; i < targetRenderers.Length; i++)
			{
				Renderer renderer = targetRenderers[i];
				if (!(renderer == null))
				{
					propertyBlock.Clear();
					propertyBlock.SetColor(BaseColorId, Color.Lerp(sourceColor, tintedFlash, Mathf.Clamp01(intensity)));
					propertyBlock.SetColor(ColorId, Color.Lerp(sourceColor, tintedFlash, Mathf.Clamp01(intensity)));
					propertyBlock.SetColor(EmissionColorId, emission);
					propertyBlock.SetColor(RimColorId, emission);
					propertyBlock.SetFloat(RimPowerId, rimPower);
					propertyBlock.SetFloat(RimIntensityId, rimIntensity);
					propertyBlock.SetColor(FresnelColorId, emission);
					propertyBlock.SetFloat(FresnelPowerId, rimPower);
					propertyBlock.SetFloat(FresnelIntensityId, rimIntensity);
					propertyBlock.SetColor(EdgeColorId, tintedFlash);
					propertyBlock.SetColor(OutlineColorId, tintedFlash);
					propertyBlock.SetFloat(OutlineWidthId, outlineWidth);
					renderer.SetPropertyBlock(propertyBlock);
				}
			}
		}

		private void PlayVisibleRim(bool critical)
		{
			if (Application.isPlaying && !(Time.time < nextRimTime))
			{
				nextRimTime = Time.time + 0.07f;
				RuntimeCombatFeedback.ShowHitRim(base.transform, ResolveFeedbackColor(critical), critical);
			}
		}

		private Color ResolveFeedbackColor(bool critical)
		{
			Color sourceColor = (hasFeedbackColor ? feedbackColor : flashColor);
			Color brightColor = (critical ? new Color(1f, 0.48f, 0.18f, 1f) : new Color(0.66f, 0.96f, 1f, 1f));
			return Color.Lerp(sourceColor, brightColor, critical ? 0.55f : 0.42f);
		}

		private void RestoreBaseTint()
		{
			if (gpuSkinnedRenderer == null)
			{
				gpuSkinnedRenderer = GetComponent<GpuSkinnedUnitRenderer>();
			}
			gpuSkinnedRenderer?.ClearFlash();
			if (targetRenderers == null)
			{
				return;
			}
			for (int i = 0; i < targetRenderers.Length; i++)
			{
				Renderer renderer = targetRenderers[i];
				if (!(renderer == null))
				{
					if (!hasBaseColor)
					{
						renderer.SetPropertyBlock(null);
						continue;
					}
					EnsurePropertyBlock();
					propertyBlock.Clear();
					propertyBlock.SetColor(BaseColorId, baseColor);
					propertyBlock.SetColor(ColorId, baseColor);
					renderer.SetPropertyBlock(propertyBlock);
				}
			}
		}

		private void EnsurePropertyBlock()
		{
			if (propertyBlock == null)
			{
				propertyBlock = new MaterialPropertyBlock();
			}
		}

		private void CacheRenderersIfNeeded()
		{
			if (targetRenderers == null || targetRenderers.Length == 0)
			{
				targetRenderers = GetComponentsInChildren<Renderer>(includeInactive: true);
			}
		}

		private void EnableEmissionOnMaterials()
		{
			if (targetRenderers == null || materialsPrepared)
			{
				return;
			}
			for (int i = 0; i < targetRenderers.Length; i++)
			{
				Renderer renderer = targetRenderers[i];
				if (renderer == null)
				{
					continue;
				}
				Material[] materials = renderer.sharedMaterials;
				foreach (Material material in materials)
				{
					if (!(material == null))
					{
						if (RuntimeRenderBatchingUtility.EnableGpuInstancing)
						{
							material.enableInstancing = true;
						}
						if (material.HasProperty(EmissionColorId))
						{
							material.EnableKeyword("_EMISSION");
						}
					}
				}
			}
			materialsPrepared = true;
		}
	}
}
