using UnityEngine;

namespace DefenseGame;

public static class RuntimeGameFeel
{
	private static RuntimeGameFeelRunner runner;

	public static void PlayJackpotPulse(Vector3 position, Color color, float radius, float shakeIntensity, float shakeDuration, float slowScale, float slowDuration, int extraPulses = 1)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		if (Application.isPlaying)
		{
			float num = Mathf.Max(0.25f, radius);
			Color color2 = Color.Lerp(color, Color.white, 0.28f);
			RuntimeCombatFeedback.ShowGroundPulse(position, color2, num, Mathf.Max(0.28f, shakeDuration + 0.12f), 0.11f);
			RuntimeCombatFeedback.ShowGroundWarning(position, color, num * 1.35f, Mathf.Max(0.35f, shakeDuration + 0.18f), 0.12f);
			for (int i = 0; i < Mathf.Max(0, extraPulses); i++)
			{
				EnsureRunner().DelayedPulse(position, color2, num * (1.12f + (float)i * 0.16f), 0.09f + (float)i * 0.07f);
			}
			RuntimeCameraShake.Request(shakeIntensity, shakeDuration);
			EnsureRunner().HitStop(slowScale, slowDuration);
		}
	}

	public static void ShowJackpotReveal(string title, string gradeLabel, string unitName, Color color, string detail, float duration)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		if (Application.isPlaying)
		{
			RuntimeCameraShake.Request(0.035f, 0.14f);
			EnsureRunner().ShowJackpotReveal(title, gradeLabel, unitName, color, detail, duration);
		}
	}

	public static void PlayHighGradeSummonVfx(Vector3 position, Color color, CharacterGrade grade)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		if (Application.isPlaying)
		{
			EnsureRunner().PlayHighGradeSummonVfx(position, color, grade);
		}
	}

	public static void PlaySummonArrivalVfx(Vector3 position, Color color, CharacterGrade grade)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		if (Application.isPlaying)
		{
			EnsureRunner().PlaySummonArrivalVfx(position, color, grade);
		}
	}

	public static void PlayMergeResultVfx(Vector3 position, Color color, CharacterGrade grade, bool ultimate)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		if (Application.isPlaying)
		{
			EnsureRunner().PlayMergeResultVfx(position, color, grade, ultimate);
		}
	}

	private static RuntimeGameFeelRunner EnsureRunner()
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		if ((Object)(object)runner != (Object)null)
		{
			return runner;
		}
		GameObject val = new GameObject("RuntimeGameFeel");
		Object.DontDestroyOnLoad((Object)(object)val);
		runner = val.AddComponent<RuntimeGameFeelRunner>();
		return runner;
	}
}
