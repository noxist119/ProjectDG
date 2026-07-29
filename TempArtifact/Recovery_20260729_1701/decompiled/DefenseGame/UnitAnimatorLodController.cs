using System.Collections.Generic;
using UnityEngine;

namespace DefenseGame;

[DisallowMultipleComponent]
public sealed class UnitAnimatorLodController : MonoBehaviour
{
	private sealed class AnimatorSnapshot
	{
		public Animator animator;

		public bool enabled;

		public AnimatorCullingMode cullingMode;

		public bool keepStateOnDisable;

		public bool writeDefaultValuesOnDisable;
	}

	private readonly List<AnimatorSnapshot> animatorSnapshots = new List<AnimatorSnapshot>(2);

	private readonly List<SkinnedMeshRenderer> skinnedRenderers = new List<SkinnedMeshRenderer>(2);

	private UnitAnimationDriver animationDriver;

	private bool isDefender;

	private bool isBoss;

	private bool configured;

	private bool shouldSample;

	private bool manualSampling;

	private float accumulatedDeltaTime;

	private float sampleCountdown;

	private float forceFullRateUntil;

	public bool IsSampling => manualSampling;

	public static void AttachOrRefresh(GameObject owner, UnitAnimationDriver driver, bool defender, bool boss)
	{
		if ((Object)(object)owner == (Object)null)
		{
			return;
		}
		UnitAnimatorLodController unitAnimatorLodController = owner.GetComponent<UnitAnimatorLodController>();
		if (!UnitAnimatorUpdateScheduler.FeatureEnabled)
		{
			if ((Object)(object)unitAnimatorLodController != (Object)null)
			{
				unitAnimatorLodController.RestoreAutomaticUpdates();
				((Behaviour)unitAnimatorLodController).enabled = false;
			}
			return;
		}
		if ((Object)(object)unitAnimatorLodController == (Object)null)
		{
			unitAnimatorLodController = owner.AddComponent<UnitAnimatorLodController>();
		}
		((Behaviour)unitAnimatorLodController).enabled = true;
		unitAnimatorLodController.Configure(driver, defender, boss);
	}

	private void OnEnable()
	{
		UnitAnimatorUpdateScheduler.Register(this);
	}

	private void OnDisable()
	{
		UnitAnimatorUpdateScheduler.Unregister(this);
		RestoreAutomaticUpdates();
	}

	private void OnDestroy()
	{
		RestoreOriginalSettings();
	}

	private void Configure(UnitAnimationDriver driver, bool defender, bool boss)
	{
		RestoreOriginalSettings();
		animationDriver = (((Object)(object)driver != (Object)null) ? driver : ((Component)this).GetComponent<UnitAnimationDriver>());
		isDefender = defender;
		isBoss = boss;
		CacheAnimationComponents();
		configured = animatorSnapshots.Count > 0;
		RefreshPolicy();
	}

	internal void RefreshPolicy()
	{
		RestoreAutomaticUpdates();
		shouldSample = configured && UnitAnimatorUpdateScheduler.ShouldSample(isDefender, isBoss);
		accumulatedDeltaTime = 0f;
		sampleCountdown = ResolveStaggeredFirstSampleDelay();
		forceFullRateUntil = 0f;
		ApplyRendererPolicy();
	}

	public void PrepareForAction(float minimumFullRateDuration)
	{
		if (configured && UnitAnimatorUpdateScheduler.FeatureEnabled)
		{
			forceFullRateUntil = Mathf.Max(forceFullRateUntil, Time.time + Mathf.Max(UnitAnimatorUpdateScheduler.ActionGraceDuration, minimumFullRateDuration));
			RestoreAutomaticUpdates();
		}
	}

	internal bool SchedulerTick(float deltaTime, out int evaluationCount)
	{
		evaluationCount = 0;
		if (!configured || !shouldSample || !((Behaviour)this).isActiveAndEnabled)
		{
			RestoreAutomaticUpdates();
			return false;
		}
		if (Time.time < forceFullRateUntil || ((Object)(object)animationDriver != (Object)null && animationDriver.IsLocked))
		{
			RestoreAutomaticUpdates();
			return false;
		}
		EnterManualSampling();
		if (deltaTime <= 0f)
		{
			return true;
		}
		accumulatedDeltaTime += deltaTime;
		sampleCountdown -= deltaTime;
		if (sampleCountdown > 0f)
		{
			return true;
		}
		float num = accumulatedDeltaTime;
		accumulatedDeltaTime = 0f;
		sampleCountdown += UnitAnimatorUpdateScheduler.SampleInterval;
		if (sampleCountdown <= 0f)
		{
			sampleCountdown = UnitAnimatorUpdateScheduler.SampleInterval;
		}
		for (int i = 0; i < animatorSnapshots.Count; i++)
		{
			AnimatorSnapshot animatorSnapshot = animatorSnapshots[i];
			Animator animator = animatorSnapshot.animator;
			if (!((Object)(object)animator == (Object)null) && animatorSnapshot.enabled && ((Component)animator).gameObject.activeInHierarchy)
			{
				((Behaviour)animator).enabled = true;
				animator.Update(num);
				((Behaviour)animator).enabled = false;
				evaluationCount++;
			}
		}
		return true;
	}

	internal void RestoreAutomaticUpdates()
	{
		if (!manualSampling)
		{
			return;
		}
		manualSampling = false;
		accumulatedDeltaTime = 0f;
		sampleCountdown = ResolveStaggeredFirstSampleDelay();
		for (int i = 0; i < animatorSnapshots.Count; i++)
		{
			AnimatorSnapshot animatorSnapshot = animatorSnapshots[i];
			if ((Object)(object)animatorSnapshot.animator != (Object)null)
			{
				((Behaviour)animatorSnapshot.animator).enabled = animatorSnapshot.enabled;
			}
		}
	}

	private void EnterManualSampling()
	{
		if (manualSampling)
		{
			return;
		}
		manualSampling = true;
		accumulatedDeltaTime = 0f;
		sampleCountdown = ResolveStaggeredFirstSampleDelay();
		for (int i = 0; i < animatorSnapshots.Count; i++)
		{
			AnimatorSnapshot animatorSnapshot = animatorSnapshots[i];
			if ((Object)(object)animatorSnapshot.animator != (Object)null && animatorSnapshot.enabled)
			{
				((Behaviour)animatorSnapshot.animator).enabled = false;
			}
		}
	}

	private void CacheAnimationComponents()
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		Animator[] componentsInChildren = ((Component)this).GetComponentsInChildren<Animator>(true);
		foreach (Animator val in componentsInChildren)
		{
			if (!((Object)(object)val == (Object)null))
			{
				animatorSnapshots.Add(new AnimatorSnapshot
				{
					animator = val,
					enabled = ((Behaviour)val).enabled,
					cullingMode = val.cullingMode,
					keepStateOnDisable = val.keepAnimatorStateOnDisable,
					writeDefaultValuesOnDisable = val.writeDefaultValuesOnDisable
				});
				val.keepAnimatorStateOnDisable = true;
				val.writeDefaultValuesOnDisable = false;
			}
		}
		SkinnedMeshRenderer[] componentsInChildren2 = ((Component)this).GetComponentsInChildren<SkinnedMeshRenderer>(true);
		for (int j = 0; j < componentsInChildren2.Length; j++)
		{
			if ((Object)(object)componentsInChildren2[j] != (Object)null)
			{
				skinnedRenderers.Add(componentsInChildren2[j]);
			}
		}
	}

	private void ApplyRendererPolicy()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		bool flag = (Object)(object)((Component)this).GetComponent<GpuSkinnedUnitRenderer>() != (Object)null && GpuSkinnedUnitBatchRenderer.FeatureEnabled;
		AnimatorCullingMode cullingMode = (AnimatorCullingMode)((!(shouldSample || flag)) ? 2 : 0);
		for (int i = 0; i < animatorSnapshots.Count; i++)
		{
			AnimatorSnapshot animatorSnapshot = animatorSnapshots[i];
			if ((Object)(object)animatorSnapshot.animator != (Object)null)
			{
				animatorSnapshot.animator.cullingMode = cullingMode;
			}
		}
		for (int j = 0; j < skinnedRenderers.Count; j++)
		{
			if ((Object)(object)skinnedRenderers[j] != (Object)null)
			{
				skinnedRenderers[j].updateWhenOffscreen = false;
			}
		}
	}

	private void RestoreOriginalSettings()
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		manualSampling = false;
		for (int i = 0; i < animatorSnapshots.Count; i++)
		{
			AnimatorSnapshot animatorSnapshot = animatorSnapshots[i];
			if (!((Object)(object)animatorSnapshot.animator == (Object)null))
			{
				((Behaviour)animatorSnapshot.animator).enabled = animatorSnapshot.enabled;
				animatorSnapshot.animator.cullingMode = animatorSnapshot.cullingMode;
				animatorSnapshot.animator.keepAnimatorStateOnDisable = animatorSnapshot.keepStateOnDisable;
				animatorSnapshot.animator.writeDefaultValuesOnDisable = animatorSnapshot.writeDefaultValuesOnDisable;
			}
		}
		animatorSnapshots.Clear();
		skinnedRenderers.Clear();
		configured = false;
	}

	private float ResolveStaggeredFirstSampleDelay()
	{
		float sampleInterval = UnitAnimatorUpdateScheduler.SampleInterval;
		uint instanceID = (uint)((Object)this).GetInstanceID();
		return sampleInterval * (float)(instanceID % 16 + 1) / 16f;
	}
}
