using System;
using UnityEngine;

namespace DefenseGame;

[DefaultExecutionOrder(-10000)]
public sealed class MobileFrameRateController : MonoBehaviour
{
	public const int StandardTargetFrameRate = 60;

	public const int LowEndTargetFrameRate = 30;

	public const float StandardWorldUiPoseInterval = 1f / 30f;

	public const float LowEndWorldUiPoseInterval = 0.05f;

	public const int StandardParticleLimit = 96;

	public const int LowEndParticleLimit = 64;

	public const string PerformanceTierOverrideKey = "DefenseGame.PerformanceTier";

	private const int AutoTierOverride = 0;

	private const int LowEndTierOverride = 1;

	private const int StandardTierOverride = 2;

	private const int LowEndAndroidMemoryLimitMb = 4608;

	private const int LowEndIosMemoryLimitMb = 3072;

	private const int LowEndGraphicsMemoryLimitMb = 1024;

	private const int SharedGraphicsSystemMemoryLimitMb = 6144;

	private const string LowEndQualityName = "Very Low";

	private const string StandardMobileQualityName = "Medium";

	private static readonly string[] LowEndAndroidGpuMarkers = new string[12]
	{
		"mali-g31", "mali-g51", "mali-g52", "mali-g57", "mali-t", "powervr ge", "adreno (tm) 5", "adreno 5", "adreno (tm) 610", "adreno 610",
		"adreno (tm) 612", "adreno 612"
	};

	private static readonly string[] LowEndIosGpuMarkers = new string[3] { "apple a9", "apple a10", "apple a11" };

	private static MobileFrameRateController instance;

	private static MobilePerformanceTier activeTier;

	private static bool tierResolved;

	public static MobilePerformanceTier ActiveTier
	{
		get
		{
			ResolveTierIfNeeded();
			return activeTier;
		}
	}

	public static bool IsLowEndDevice => ActiveTier == MobilePerformanceTier.LowEnd;

	public static int TargetFrameRate => IsLowEndDevice ? 30 : 60;

	public static float WorldUiPoseInterval => IsLowEndDevice ? 0.05f : (1f / 30f);

	public static int MaxParticlesPerSystem => IsLowEndDevice ? 64 : 96;

	public static event Action<MobilePerformanceTier> TierApplied;

	[RuntimeInitializeOnLoadMethod(/*Could not decode attribute arguments.*/)]
	private static void ResetStatics()
	{
		instance = null;
		activeTier = MobilePerformanceTier.Standard;
		tierResolved = false;
		MobileFrameRateController.TierApplied = null;
	}

	[RuntimeInitializeOnLoadMethod(/*Could not decode attribute arguments.*/)]
	private static void Bootstrap()
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		ApplyPerformanceSettings();
		if (!((Object)(object)instance != (Object)null))
		{
			GameObject val = new GameObject("MobileFrameRateController");
			((Object)val).hideFlags = (HideFlags)1;
			instance = val.AddComponent<MobileFrameRateController>();
			Object.DontDestroyOnLoad((Object)(object)val);
		}
	}

	private void Awake()
	{
		if ((Object)(object)instance != (Object)null && (Object)(object)instance != (Object)(object)this)
		{
			Object.Destroy((Object)(object)((Component)this).gameObject);
			return;
		}
		instance = this;
		Object.DontDestroyOnLoad((Object)(object)((Component)this).gameObject);
		ApplyPerformanceSettings();
	}

	private void OnApplicationFocus(bool hasFocus)
	{
		if (hasFocus)
		{
			ApplyPerformanceSettings();
		}
	}

	public static void ApplyPerformanceSettings()
	{
		ResolveTierIfNeeded();
		QualitySettings.vSyncCount = 0;
		if (Application.isMobilePlatform)
		{
			string qualityName = (IsLowEndDevice ? "Very Low" : "Medium");
			int num = FindQualityLevel(qualityName);
			if (num >= 0 && QualitySettings.GetQualityLevel() != num)
			{
				QualitySettings.SetQualityLevel(num, true);
			}
		}
		Application.targetFrameRate = TargetFrameRate;
		MobileFrameRateController.TierApplied?.Invoke(activeTier);
		Debug.Log((object)($"[MobilePerformance] tier={activeTier}, targetFps={TargetFrameRate}, " + "quality=" + QualitySettings.names[QualitySettings.GetQualityLevel()] + ", " + $"memory={SystemInfo.systemMemorySize}MB, graphicsMemory={SystemInfo.graphicsMemorySize}MB, " + "gpu=" + SystemInfo.graphicsDeviceName));
	}

	private static void ResolveTierIfNeeded()
	{
		if (!tierResolved)
		{
			activeTier = (ResolveForcedTier(out var tier) ? tier : DetectAutomaticTier());
			tierResolved = true;
		}
	}

	private static bool ResolveForcedTier(out MobilePerformanceTier tier)
	{
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		foreach (string a in commandLineArgs)
		{
			if (string.Equals(a, "-forceLowEndProfile", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "-force-low-end", StringComparison.OrdinalIgnoreCase))
			{
				tier = MobilePerformanceTier.LowEnd;
				return true;
			}
			if (string.Equals(a, "-forceStandardProfile", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "-force-standard", StringComparison.OrdinalIgnoreCase))
			{
				tier = MobilePerformanceTier.Standard;
				return true;
			}
		}
		switch (PlayerPrefs.GetInt("DefenseGame.PerformanceTier", 0))
		{
		case 1:
			tier = MobilePerformanceTier.LowEnd;
			return true;
		case 2:
			tier = MobilePerformanceTier.Standard;
			return true;
		default:
			tier = MobilePerformanceTier.Standard;
			return false;
		}
	}

	private static MobilePerformanceTier DetectAutomaticTier()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Invalid comparison between Unknown and I4
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Invalid comparison between Unknown and I4
		if ((int)Application.platform == 11)
		{
			bool flag = SystemInfo.systemMemorySize > 0 && SystemInfo.systemMemorySize <= 4608;
			bool flag2 = SystemInfo.systemMemorySize > 0 && SystemInfo.systemMemorySize <= 6144 && SystemInfo.graphicsMemorySize > 0 && SystemInfo.graphicsMemorySize <= 1024;
			bool flag3 = ContainsAny(SystemInfo.graphicsDeviceName, LowEndAndroidGpuMarkers);
			return (flag || flag2 || flag3) ? MobilePerformanceTier.LowEnd : MobilePerformanceTier.Standard;
		}
		if ((int)Application.platform == 8)
		{
			bool flag4 = SystemInfo.systemMemorySize > 0 && SystemInfo.systemMemorySize <= 3072;
			bool flag5 = ContainsAny(SystemInfo.graphicsDeviceName, LowEndIosGpuMarkers);
			return (flag4 || flag5) ? MobilePerformanceTier.LowEnd : MobilePerformanceTier.Standard;
		}
		return MobilePerformanceTier.Standard;
	}

	private static bool ContainsAny(string value, string[] markers)
	{
		if (string.IsNullOrEmpty(value))
		{
			return false;
		}
		for (int i = 0; i < markers.Length; i++)
		{
			if (value.IndexOf(markers[i], StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private static int FindQualityLevel(string qualityName)
	{
		string[] names = QualitySettings.names;
		for (int i = 0; i < names.Length; i++)
		{
			if (string.Equals(names[i], qualityName, StringComparison.Ordinal))
			{
				return i;
			}
		}
		return -1;
	}
}
