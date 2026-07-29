using System.Collections.Generic;
using UnityEngine;

namespace DefenseGame
{
	[DefaultExecutionOrder(-50)]
	public sealed class UnitAnimatorUpdateScheduler : MonoBehaviour
	{
		private static readonly List<UnitAnimatorLodController> ActiveControllers = new List<UnitAnimatorLodController>(96);

		private static UnitAnimatorUpdateScheduler instance;

		private static bool featureEnabled;

		private static bool sampleDefenders;

		private static bool sampleBosses;

		private static float sampleInterval = 1f / 15f;

		private static float actionGraceDuration = 0.12f;

		public static bool FeatureEnabled => featureEnabled;

		public static int ManagedUnitCount => ActiveControllers.Count;

		public static int SampledUnitCount { get; private set; }

		public static int AnimatorEvaluationCount { get; private set; }

		internal static float SampleInterval => sampleInterval;

		internal static float ActionGraceDuration => actionGraceDuration;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			instance = null;
			featureEnabled = false;
			sampleDefenders = false;
			sampleBosses = false;
			sampleInterval = 1f / 15f;
			actionGraceDuration = 0.12f;
			ActiveControllers.Clear();
			SampledUnitCount = 0;
			AnimatorEvaluationCount = 0;
		}

		public static void Configure(GamePresentationConfig config)
		{
			bool requested = config != null && config.enableUnitAnimatorLod;
			bool lowEndOnly = config == null || config.unitAnimatorLodLowEndOnly;
			featureEnabled = requested && (!lowEndOnly || MobileFrameRateController.IsLowEndDevice);
			sampleDefenders = config != null && config.unitAnimatorLodDefenders;
			sampleBosses = config != null && config.unitAnimatorLodBosses;
			int targetFps = Mathf.Clamp((config != null) ? config.lowEndRegularMonsterAnimatorFps : 15, 5, 30);
			sampleInterval = 1f / (float)targetFps;
			actionGraceDuration = Mathf.Clamp((config != null) ? config.unitAnimatorActionGraceDuration : 0.12f, 0f, 0.5f);
			if (featureEnabled)
			{
				EnsureInstance();
			}
			for (int i = ActiveControllers.Count - 1; i >= 0; i--)
			{
				UnitAnimatorLodController controller = ActiveControllers[i];
				if (controller == null)
				{
					ActiveControllers.RemoveAt(i);
				}
				else
				{
					controller.RefreshPolicy();
				}
			}
			if (requested)
			{
				Debug.Log($"[UnitAnimatorLOD] enabled={featureEnabled}, targetFps={targetFps}, " + $"defenders={sampleDefenders}, bosses={sampleBosses}");
			}
		}

		public static bool ShouldSample(bool isDefender, bool isBoss)
		{
			if (!featureEnabled)
			{
				return false;
			}
			if (isBoss)
			{
				return sampleBosses;
			}
			return !isDefender || sampleDefenders;
		}

		internal static void Register(UnitAnimatorLodController controller)
		{
			if (!(controller == null) && !ActiveControllers.Contains(controller))
			{
				ActiveControllers.Add(controller);
				if (featureEnabled)
				{
					EnsureInstance();
				}
			}
		}

		internal static void Unregister(UnitAnimatorLodController controller)
		{
			if (controller != null)
			{
				ActiveControllers.Remove(controller);
			}
		}

		private static void EnsureInstance()
		{
			if (!(instance != null) && Application.isPlaying)
			{
				GameObject host = new GameObject("UnitAnimatorUpdateScheduler");
				host.hideFlags = HideFlags.HideInHierarchy;
				instance = host.AddComponent<UnitAnimatorUpdateScheduler>();
				Object.DontDestroyOnLoad(host);
			}
		}

		private void LateUpdate()
		{
			SampledUnitCount = 0;
			AnimatorEvaluationCount = 0;
			for (int i = ActiveControllers.Count - 1; i >= 0; i--)
			{
				UnitAnimatorLodController controller = ActiveControllers[i];
				if (controller == null)
				{
					ActiveControllers.RemoveAt(i);
				}
				else if (!featureEnabled)
				{
					controller.RestoreAutomaticUpdates();
				}
				else
				{
					if (controller.SchedulerTick(Time.deltaTime, out var evaluationCount))
					{
						SampledUnitCount++;
					}
					AnimatorEvaluationCount += evaluationCount;
				}
			}
		}
	}
}
