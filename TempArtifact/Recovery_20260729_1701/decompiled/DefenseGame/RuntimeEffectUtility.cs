using System.Collections.Generic;
using UnityEngine;

namespace DefenseGame;

public static class RuntimeEffectUtility
{
	private const float DefaultLifetime = 2f;

	private const int MaxTrackedEffects = 72;

	private static readonly List<GameObject> trackedEffects = new List<GameObject>();

	public static GameObject PlayOneShot(GameObject prefab, Vector3 position, Quaternion rotation, float minimumLifetime = 0f)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)prefab == (Object)null)
		{
			return null;
		}
		if (PrefabHasMissingScript(prefab))
		{
			PlayFallbackEffect(position, minimumLifetime);
			return null;
		}
		GameObject val = Object.Instantiate<GameObject>(prefab, position, rotation);
		RuntimeParticleVfxCompatibility.Prepare(val);
		val.SetActive(true);
		TrackEffect(val);
		Object.Destroy((Object)(object)val, Mathf.Max(minimumLifetime, ResolveLifetime(val)));
		return val;
	}

	public static GameObject PlayOneShotTimed(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)prefab == (Object)null)
		{
			return null;
		}
		if (PrefabHasMissingScript(prefab))
		{
			PlayFallbackEffect(position, lifetime);
			return null;
		}
		GameObject val = Object.Instantiate<GameObject>(prefab, position, rotation);
		RuntimeParticleVfxCompatibility.Prepare(val);
		val.SetActive(true);
		TrackEffect(val);
		Object.Destroy((Object)(object)val, Mathf.Max(0.1f, lifetime));
		return val;
	}

	public static Quaternion FaceTowards(Vector3 origin, Vector3 target, Quaternion fallback)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = target - origin;
		val.y = 0f;
		if (((Vector3)(ref val)).sqrMagnitude <= 1E-06f)
		{
			return fallback;
		}
		return Quaternion.LookRotation(((Vector3)(ref val)).normalized, Vector3.up);
	}

	public static GameObject PlayAttachedTimed(GameObject prefab, Transform parent, Vector3 localPosition, Quaternion localRotation, float lifetime)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)prefab == (Object)null || (Object)(object)parent == (Object)null)
		{
			return null;
		}
		if (PrefabHasMissingScript(prefab))
		{
			PlayFallbackEffect(parent.position + localPosition, lifetime);
			return null;
		}
		GameObject val = Object.Instantiate<GameObject>(prefab, parent);
		val.transform.localPosition = localPosition;
		val.transform.localRotation = localRotation;
		RuntimeParticleVfxCompatibility.Prepare(val);
		ForceLocalParticleSimulation(val);
		val.SetActive(true);
		TrackEffect(val);
		Object.Destroy((Object)(object)val, Mathf.Max(0.1f, lifetime));
		return val;
	}

	public static void DestroyEffect(GameObject effect)
	{
		if ((Object)(object)effect == (Object)null)
		{
			return;
		}
		ParticleSystem[] componentsInChildren = effect.GetComponentsInChildren<ParticleSystem>(true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if ((Object)(object)componentsInChildren[i] != (Object)null)
			{
				componentsInChildren[i].Stop(true, (ParticleSystemStopBehavior)0);
			}
		}
		AudioSource[] componentsInChildren2 = effect.GetComponentsInChildren<AudioSource>(true);
		for (int j = 0; j < componentsInChildren2.Length; j++)
		{
			if ((Object)(object)componentsInChildren2[j] != (Object)null)
			{
				componentsInChildren2[j].Stop();
			}
		}
		if (Application.isPlaying)
		{
			Object.Destroy((Object)(object)effect);
		}
		else
		{
			Object.DestroyImmediate((Object)(object)effect);
		}
	}

	public static void ClearTrackedEffects()
	{
		for (int num = trackedEffects.Count - 1; num >= 0; num--)
		{
			GameObject effect = trackedEffects[num];
			trackedEffects.RemoveAt(num);
			DestroyEffect(effect);
		}
	}

	private static bool PrefabHasMissingScript(GameObject prefab)
	{
		if ((Object)(object)prefab == (Object)null)
		{
			return false;
		}
		MonoBehaviour[] componentsInChildren = prefab.GetComponentsInChildren<MonoBehaviour>(true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if ((Object)(object)componentsInChildren[i] == (Object)null)
			{
				return true;
			}
		}
		return false;
	}

	private static void PlayFallbackEffect(Vector3 position, float lifetime)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		RuntimeCombatFeedback.ShowGroundPulse(position, new Color(0.62f, 0.88f, 1f, 0.72f), 0.42f, Mathf.Clamp(lifetime, 0.22f, 0.8f));
	}

	private static void TrackEffect(GameObject effect)
	{
		if (!((Object)(object)effect == (Object)null))
		{
			PruneTrackedEffects();
			while (trackedEffects.Count >= 72)
			{
				GameObject effect2 = trackedEffects[0];
				trackedEffects.RemoveAt(0);
				DestroyEffect(effect2);
			}
			trackedEffects.Add(effect);
		}
	}

	private static void PruneTrackedEffects()
	{
		for (int num = trackedEffects.Count - 1; num >= 0; num--)
		{
			if ((Object)(object)trackedEffects[num] == (Object)null)
			{
				trackedEffects.RemoveAt(num);
			}
		}
	}

	private static void ForceLocalParticleSimulation(GameObject effect)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		ParticleSystem[] componentsInChildren = effect.GetComponentsInChildren<ParticleSystem>(true);
		foreach (ParticleSystem val in componentsInChildren)
		{
			if (!((Object)(object)val == (Object)null))
			{
				MainModule main = val.main;
				((MainModule)(ref main)).simulationSpace = (ParticleSystemSimulationSpace)0;
			}
		}
		LineRenderer[] componentsInChildren2 = effect.GetComponentsInChildren<LineRenderer>(true);
		for (int j = 0; j < componentsInChildren2.Length; j++)
		{
			if ((Object)(object)componentsInChildren2[j] != (Object)null)
			{
				componentsInChildren2[j].useWorldSpace = false;
			}
		}
	}

	private static float ResolveLifetime(GameObject effect)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		float num = 2f;
		ParticleSystem[] componentsInChildren = effect.GetComponentsInChildren<ParticleSystem>(true);
		foreach (ParticleSystem val in componentsInChildren)
		{
			if (!((Object)(object)val == (Object)null))
			{
				MainModule main = val.main;
				float duration = ((MainModule)(ref main)).duration;
				MinMaxCurve startLifetime = ((MainModule)(ref main)).startLifetime;
				float num2 = duration + ((MinMaxCurve)(ref startLifetime)).constantMax;
				num = Mathf.Max(num, num2);
			}
		}
		AudioSource[] componentsInChildren2 = effect.GetComponentsInChildren<AudioSource>(true);
		foreach (AudioSource val2 in componentsInChildren2)
		{
			if ((Object)(object)val2 != (Object)null && (Object)(object)val2.clip != (Object)null)
			{
				num = Mathf.Max(num, val2.clip.length);
			}
		}
		return num;
	}
}
