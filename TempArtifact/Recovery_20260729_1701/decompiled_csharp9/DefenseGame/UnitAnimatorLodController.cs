using System.Collections.Generic;
using UnityEngine;

namespace DefenseGame
{
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
			if (owner == null)
			{
				return;
			}
			UnitAnimatorLodController controller = owner.GetComponent<UnitAnimatorLodController>();
			if (!UnitAnimatorUpdateScheduler.FeatureEnabled)
			{
				if (controller != null)
				{
					controller.RestoreAutomaticUpdates();
					controller.enabled = false;
				}
				return;
			}
			if (controller == null)
			{
				controller = owner.AddComponent<UnitAnimatorLodController>();
			}
			controller.enabled = true;
			controller.Configure(driver, defender, boss);
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
			animationDriver = ((driver != null) ? driver : GetComponent<UnitAnimationDriver>());
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
			if (!configured || !shouldSample || !base.isActiveAndEnabled)
			{
				RestoreAutomaticUpdates();
				return false;
			}
			if (Time.time < forceFullRateUntil || (animationDriver != null && animationDriver.IsLocked))
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
			float evaluationDelta = accumulatedDeltaTime;
			accumulatedDeltaTime = 0f;
			sampleCountdown += UnitAnimatorUpdateScheduler.SampleInterval;
			if (sampleCountdown <= 0f)
			{
				sampleCountdown = UnitAnimatorUpdateScheduler.SampleInterval;
			}
			for (int i = 0; i < animatorSnapshots.Count; i++)
			{
				AnimatorSnapshot snapshot = animatorSnapshots[i];
				Animator animator = snapshot.animator;
				if (!(animator == null) && snapshot.enabled && animator.gameObject.activeInHierarchy)
				{
					animator.enabled = true;
					animator.Update(evaluationDelta);
					animator.enabled = false;
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
				AnimatorSnapshot snapshot = animatorSnapshots[i];
				if (snapshot.animator != null)
				{
					snapshot.animator.enabled = snapshot.enabled;
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
				AnimatorSnapshot snapshot = animatorSnapshots[i];
				if (snapshot.animator != null && snapshot.enabled)
				{
					snapshot.animator.enabled = false;
				}
			}
		}

		private void CacheAnimationComponents()
		{
			Animator[] animators = GetComponentsInChildren<Animator>(includeInactive: true);
			foreach (Animator animator in animators)
			{
				if (!(animator == null))
				{
					animatorSnapshots.Add(new AnimatorSnapshot
					{
						animator = animator,
						enabled = animator.enabled,
						cullingMode = animator.cullingMode,
						keepStateOnDisable = animator.keepAnimatorStateOnDisable,
						writeDefaultValuesOnDisable = animator.writeDefaultValuesOnDisable
					});
					animator.keepAnimatorStateOnDisable = true;
					animator.writeDefaultValuesOnDisable = false;
				}
			}
			SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
			for (int j = 0; j < renderers.Length; j++)
			{
				if (renderers[j] != null)
				{
					skinnedRenderers.Add(renderers[j]);
				}
			}
		}

		private void ApplyRendererPolicy()
		{
			bool usesGpuSkinBatch = GetComponent<GpuSkinnedUnitRenderer>() != null && GpuSkinnedUnitBatchRenderer.FeatureEnabled;
			AnimatorCullingMode cullingMode = ((!(shouldSample || usesGpuSkinBatch)) ? AnimatorCullingMode.CullCompletely : AnimatorCullingMode.AlwaysAnimate);
			for (int i = 0; i < animatorSnapshots.Count; i++)
			{
				AnimatorSnapshot snapshot = animatorSnapshots[i];
				if (snapshot.animator != null)
				{
					snapshot.animator.cullingMode = cullingMode;
				}
			}
			for (int j = 0; j < skinnedRenderers.Count; j++)
			{
				if (skinnedRenderers[j] != null)
				{
					skinnedRenderers[j].updateWhenOffscreen = false;
				}
			}
		}

		private void RestoreOriginalSettings()
		{
			manualSampling = false;
			for (int i = 0; i < animatorSnapshots.Count; i++)
			{
				AnimatorSnapshot snapshot = animatorSnapshots[i];
				if (!(snapshot.animator == null))
				{
					snapshot.animator.enabled = snapshot.enabled;
					snapshot.animator.cullingMode = snapshot.cullingMode;
					snapshot.animator.keepAnimatorStateOnDisable = snapshot.keepStateOnDisable;
					snapshot.animator.writeDefaultValuesOnDisable = snapshot.writeDefaultValuesOnDisable;
				}
			}
			animatorSnapshots.Clear();
			skinnedRenderers.Clear();
			configured = false;
		}

		private float ResolveStaggeredFirstSampleDelay()
		{
			float interval = UnitAnimatorUpdateScheduler.SampleInterval;
			uint stableId = (uint)GetInstanceID();
			return interval * (float)(stableId % 16 + 1) / 16f;
		}
	}
}
