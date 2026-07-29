using System;
using UnityEngine;

namespace DefenseGame
{
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

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			instance = null;
			activeTier = MobilePerformanceTier.Standard;
			tierResolved = false;
			MobileFrameRateController.TierApplied = null;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Bootstrap()
		{
			ApplyPerformanceSettings();
			if (!(instance != null))
			{
				GameObject host = new GameObject("MobileFrameRateController");
				host.hideFlags = HideFlags.HideInHierarchy;
				instance = host.AddComponent<MobileFrameRateController>();
				UnityEngine.Object.DontDestroyOnLoad(host);
			}
		}

		private void Awake()
		{
			if (instance != null && instance != this)
			{
				UnityEngine.Object.Destroy(base.gameObject);
				return;
			}
			instance = this;
			UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
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
				int qualityLevel = FindQualityLevel(qualityName);
				if (qualityLevel >= 0 && QualitySettings.GetQualityLevel() != qualityLevel)
				{
					QualitySettings.SetQualityLevel(qualityLevel, applyExpensiveChanges: true);
				}
			}
			Application.targetFrameRate = TargetFrameRate;
			MobileFrameRateController.TierApplied?.Invoke(activeTier);
			Debug.Log($"[MobilePerformance] tier={activeTier}, targetFps={TargetFrameRate}, " + "quality=" + QualitySettings.names[QualitySettings.GetQualityLevel()] + ", " + $"memory={SystemInfo.systemMemorySize}MB, graphicsMemory={SystemInfo.graphicsMemorySize}MB, " + "gpu=" + SystemInfo.graphicsDeviceName);
		}

		private static void ResolveTierIfNeeded()
		{
			if (!tierResolved)
			{
				activeTier = (ResolveForcedTier(out var forcedTier) ? forcedTier : DetectAutomaticTier());
				tierResolved = true;
			}
		}

		private static bool ResolveForcedTier(out MobilePerformanceTier tier)
		{
			string[] arguments = Environment.GetCommandLineArgs();
			foreach (string argument in arguments)
			{
				if (string.Equals(argument, "-forceLowEndProfile", StringComparison.OrdinalIgnoreCase) || string.Equals(argument, "-force-low-end", StringComparison.OrdinalIgnoreCase))
				{
					tier = MobilePerformanceTier.LowEnd;
					return true;
				}
				if (string.Equals(argument, "-forceStandardProfile", StringComparison.OrdinalIgnoreCase) || string.Equals(argument, "-force-standard", StringComparison.OrdinalIgnoreCase))
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
			if (Application.platform == RuntimePlatform.Android)
			{
				bool limitedMemory = SystemInfo.systemMemorySize > 0 && SystemInfo.systemMemorySize <= 4608;
				bool limitedGraphicsMemory = SystemInfo.systemMemorySize > 0 && SystemInfo.systemMemorySize <= 6144 && SystemInfo.graphicsMemorySize > 0 && SystemInfo.graphicsMemorySize <= 1024;
				bool lowEndGpu = ContainsAny(SystemInfo.graphicsDeviceName, LowEndAndroidGpuMarkers);
				return (limitedMemory || limitedGraphicsMemory || lowEndGpu) ? MobilePerformanceTier.LowEnd : MobilePerformanceTier.Standard;
			}
			if (Application.platform == RuntimePlatform.IPhonePlayer)
			{
				bool limitedMemory2 = SystemInfo.systemMemorySize > 0 && SystemInfo.systemMemorySize <= 3072;
				bool oldAppleGpu = ContainsAny(SystemInfo.graphicsDeviceName, LowEndIosGpuMarkers);
				return (limitedMemory2 || oldAppleGpu) ? MobilePerformanceTier.LowEnd : MobilePerformanceTier.Standard;
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
			string[] qualityNames = QualitySettings.names;
			for (int i = 0; i < qualityNames.Length; i++)
			{
				if (string.Equals(qualityNames[i], qualityName, StringComparison.Ordinal))
				{
					return i;
				}
			}
			return -1;
		}
	}
}
