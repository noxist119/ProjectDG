using System.Collections;
using UnityEngine;

namespace DefenseGame;

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
		gpuSkinnedRenderer = ((Component)this).GetComponent<GpuSkinnedUnitRenderer>();
		EnsurePropertyBlock();
		CacheRenderersIfNeeded();
		EnableEmissionOnMaterials();
	}

	public void Configure(Renderer[] renderers)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		Configure(renderers, Color.white, useBaseColor: false);
	}

	public void Configure(Renderer[] renderers, Color newBaseColor)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		Configure(renderers, newBaseColor, useBaseColor: true);
	}

	public void Configure(Renderer[] renderers, Color newFeedbackColor, bool useBaseColor)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		targetRenderers = renderers;
		gpuSkinnedRenderer = ((Component)this).GetComponent<GpuSkinnedUnitRenderer>();
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
				((MonoBehaviour)this).StopCoroutine(flashRoutine);
			}
			PlayVisibleRim(critical);
			flashRoutine = ((MonoBehaviour)this).StartCoroutine(FlashRoutine(critical ? criticalFlashStrength : flashStrength));
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
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		EnsurePropertyBlock();
		if (propertyBlock == null || targetRenderers == null)
		{
			return;
		}
		Color val = (hasBaseColor ? baseColor : Color.white);
		Color val2 = ResolveFeedbackColor(critical: false);
		Color val3 = Color.Lerp(val2, flashColor, 0.48f) * Mathf.Lerp(0.95f, 1.55f, Mathf.Clamp01(intensity));
		if ((Object)(object)gpuSkinnedRenderer == (Object)null)
		{
			gpuSkinnedRenderer = ((Component)this).GetComponent<GpuSkinnedUnitRenderer>();
		}
		gpuSkinnedRenderer?.SetFlash(val3, intensity);
		Color val4 = val3 * (1.8f * intensity);
		float num = 1.15f * intensity;
		float num2 = Mathf.Lerp(3.4f, 1.45f, Mathf.Clamp01(intensity));
		float num3 = 2.1f * intensity;
		for (int i = 0; i < targetRenderers.Length; i++)
		{
			Renderer val5 = targetRenderers[i];
			if (!((Object)(object)val5 == (Object)null))
			{
				propertyBlock.Clear();
				propertyBlock.SetColor(BaseColorId, Color.Lerp(val, val3, Mathf.Clamp01(intensity)));
				propertyBlock.SetColor(ColorId, Color.Lerp(val, val3, Mathf.Clamp01(intensity)));
				propertyBlock.SetColor(EmissionColorId, val4);
				propertyBlock.SetColor(RimColorId, val4);
				propertyBlock.SetFloat(RimPowerId, num2);
				propertyBlock.SetFloat(RimIntensityId, num3);
				propertyBlock.SetColor(FresnelColorId, val4);
				propertyBlock.SetFloat(FresnelPowerId, num2);
				propertyBlock.SetFloat(FresnelIntensityId, num3);
				propertyBlock.SetColor(EdgeColorId, val3);
				propertyBlock.SetColor(OutlineColorId, val3);
				propertyBlock.SetFloat(OutlineWidthId, num);
				val5.SetPropertyBlock(propertyBlock);
			}
		}
	}

	private void PlayVisibleRim(bool critical)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		if (Application.isPlaying && !(Time.time < nextRimTime))
		{
			nextRimTime = Time.time + 0.07f;
			RuntimeCombatFeedback.ShowHitRim(((Component)this).transform, ResolveFeedbackColor(critical), critical);
		}
	}

	private Color ResolveFeedbackColor(bool critical)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		Color val = (hasFeedbackColor ? feedbackColor : flashColor);
		Color val2 = (critical ? new Color(1f, 0.48f, 0.18f, 1f) : new Color(0.66f, 0.96f, 1f, 1f));
		return Color.Lerp(val, val2, critical ? 0.55f : 0.42f);
	}

	private void RestoreBaseTint()
	{
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)gpuSkinnedRenderer == (Object)null)
		{
			gpuSkinnedRenderer = ((Component)this).GetComponent<GpuSkinnedUnitRenderer>();
		}
		gpuSkinnedRenderer?.ClearFlash();
		if (targetRenderers == null)
		{
			return;
		}
		for (int i = 0; i < targetRenderers.Length; i++)
		{
			Renderer val = targetRenderers[i];
			if (!((Object)(object)val == (Object)null))
			{
				if (!hasBaseColor)
				{
					val.SetPropertyBlock((MaterialPropertyBlock)null);
					continue;
				}
				EnsurePropertyBlock();
				propertyBlock.Clear();
				propertyBlock.SetColor(BaseColorId, baseColor);
				propertyBlock.SetColor(ColorId, baseColor);
				val.SetPropertyBlock(propertyBlock);
			}
		}
	}

	private void EnsurePropertyBlock()
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected O, but got Unknown
		if (propertyBlock == null)
		{
			propertyBlock = new MaterialPropertyBlock();
		}
	}

	private void CacheRenderersIfNeeded()
	{
		if (targetRenderers == null || targetRenderers.Length == 0)
		{
			targetRenderers = ((Component)this).GetComponentsInChildren<Renderer>(true);
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
			Renderer val = targetRenderers[i];
			if ((Object)(object)val == (Object)null)
			{
				continue;
			}
			Material[] sharedMaterials = val.sharedMaterials;
			foreach (Material val2 in sharedMaterials)
			{
				if (!((Object)(object)val2 == (Object)null))
				{
					if (RuntimeRenderBatchingUtility.EnableGpuInstancing)
					{
						val2.enableInstancing = true;
					}
					if (val2.HasProperty(EmissionColorId))
					{
						val2.EnableKeyword("_EMISSION");
					}
				}
			}
		}
		materialsPrepared = true;
	}
}
