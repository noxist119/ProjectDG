using System.Collections.Generic;
using UnityEngine;

namespace DefenseGame;

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

	[RuntimeInitializeOnLoadMethod(/*Could not decode attribute arguments.*/)]
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
		bool flag = (Object)(object)config != (Object)null && config.enableUnitAnimatorLod;
		bool flag2 = (Object)(object)config == (Object)null || config.unitAnimatorLodLowEndOnly;
		featureEnabled = flag && (!flag2 || MobileFrameRateController.IsLowEndDevice);
		sampleDefenders = (Object)(object)config != (Object)null && config.unitAnimatorLodDefenders;
		sampleBosses = (Object)(object)config != (Object)null && config.unitAnimatorLodBosses;
		int num = Mathf.Clamp(((Object)(object)config != (Object)null) ? config.lowEndRegularMonsterAnimatorFps : 15, 5, 30);
		sampleInterval = 1f / (float)num;
		actionGraceDuration = Mathf.Clamp(((Object)(object)config != (Object)null) ? config.unitAnimatorActionGraceDuration : 0.12f, 0f, 0.5f);
		if (featureEnabled)
		{
			EnsureInstance();
		}
		for (int num2 = ActiveControllers.Count - 1; num2 >= 0; num2--)
		{
			UnitAnimatorLodController unitAnimatorLodController = ActiveControllers[num2];
			if ((Object)(object)unitAnimatorLodController == (Object)null)
			{
				ActiveControllers.RemoveAt(num2);
			}
			else
			{
				unitAnimatorLodController.RefreshPolicy();
			}
		}
		if (flag)
		{
			Debug.Log((object)($"[UnitAnimatorLOD] enabled={featureEnabled}, targetFps={num}, " + $"defenders={sampleDefenders}, bosses={sampleBosses}"));
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
		if (!((Object)(object)controller == (Object)null) && !ActiveControllers.Contains(controller))
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
		if ((Object)(object)controller != (Object)null)
		{
			ActiveControllers.Remove(controller);
		}
	}

	private static void EnsureInstance()
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected O, but got Unknown
		if (!((Object)(object)instance != (Object)null) && Application.isPlaying)
		{
			GameObject val = new GameObject("UnitAnimatorUpdateScheduler");
			((Object)val).hideFlags = (HideFlags)1;
			instance = val.AddComponent<UnitAnimatorUpdateScheduler>();
			Object.DontDestroyOnLoad((Object)(object)val);
		}
	}

	private void LateUpdate()
	{
		SampledUnitCount = 0;
		AnimatorEvaluationCount = 0;
		for (int num = ActiveControllers.Count - 1; num >= 0; num--)
		{
			UnitAnimatorLodController unitAnimatorLodController = ActiveControllers[num];
			if ((Object)(object)unitAnimatorLodController == (Object)null)
			{
				ActiveControllers.RemoveAt(num);
			}
			else if (!featureEnabled)
			{
				unitAnimatorLodController.RestoreAutomaticUpdates();
			}
			else
			{
				if (unitAnimatorLodController.SchedulerTick(Time.deltaTime, out var evaluationCount))
				{
					SampledUnitCount++;
				}
				AnimatorEvaluationCount += evaluationCount;
			}
		}
	}
}
