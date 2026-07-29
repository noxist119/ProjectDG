using UnityEngine;

namespace DefenseGame
{
	public static class RuntimeGameFeel
	{
		private static RuntimeGameFeelRunner runner;

		public static void PlayJackpotPulse(Vector3 position, Color color, float radius, float shakeIntensity, float shakeDuration, float slowScale, float slowDuration, int extraPulses = 1)
		{
			if (Application.isPlaying)
			{
				float safeRadius = Mathf.Max(0.25f, radius);
				Color bright = Color.Lerp(color, Color.white, 0.28f);
				RuntimeCombatFeedback.ShowGroundPulse(position, bright, safeRadius, Mathf.Max(0.28f, shakeDuration + 0.12f), 0.11f);
				RuntimeCombatFeedback.ShowGroundWarning(position, color, safeRadius * 1.35f, Mathf.Max(0.35f, shakeDuration + 0.18f), 0.12f);
				for (int i = 0; i < Mathf.Max(0, extraPulses); i++)
				{
					EnsureRunner().DelayedPulse(position, bright, safeRadius * (1.12f + (float)i * 0.16f), 0.09f + (float)i * 0.07f);
				}
				RuntimeCameraShake.Request(shakeIntensity, shakeDuration);
				EnsureRunner().HitStop(slowScale, slowDuration);
			}
		}

		public static void ShowJackpotReveal(string title, string gradeLabel, string unitName, Color color, string detail, float duration)
		{
			if (Application.isPlaying)
			{
				RuntimeCameraShake.Request(0.035f, 0.14f);
				EnsureRunner().ShowJackpotReveal(title, gradeLabel, unitName, color, detail, duration);
			}
		}

		public static void PlayHighGradeSummonVfx(Vector3 position, Color color, CharacterGrade grade)
		{
			if (Application.isPlaying)
			{
				EnsureRunner().PlayHighGradeSummonVfx(position, color, grade);
			}
		}

		public static void PlaySummonArrivalVfx(Vector3 position, Color color, CharacterGrade grade)
		{
			if (Application.isPlaying)
			{
				EnsureRunner().PlaySummonArrivalVfx(position, color, grade);
			}
		}

		public static void PlayMergeResultVfx(Vector3 position, Color color, CharacterGrade grade, bool ultimate)
		{
			if (Application.isPlaying)
			{
				EnsureRunner().PlayMergeResultVfx(position, color, grade, ultimate);
			}
		}

		private static RuntimeGameFeelRunner EnsureRunner()
		{
			if (runner != null)
			{
				return runner;
			}
			GameObject runnerObject = new GameObject("RuntimeGameFeel");
			Object.DontDestroyOnLoad(runnerObject);
			runner = runnerObject.AddComponent<RuntimeGameFeelRunner>();
			return runner;
		}
	}
}
