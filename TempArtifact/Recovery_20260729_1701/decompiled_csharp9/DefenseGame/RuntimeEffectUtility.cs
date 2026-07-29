using System.Collections.Generic;
using UnityEngine;

namespace DefenseGame
{
	public static class RuntimeEffectUtility
	{
		private const float DefaultLifetime = 2f;

		private const int MaxTrackedEffects = 72;

		private static readonly List<GameObject> trackedEffects = new List<GameObject>();

		public static GameObject PlayOneShot(GameObject prefab, Vector3 position, Quaternion rotation, float minimumLifetime = 0f)
		{
			if (prefab == null)
			{
				return null;
			}
			if (PrefabHasMissingScript(prefab))
			{
				PlayFallbackEffect(position, minimumLifetime);
				return null;
			}
			GameObject effect = Object.Instantiate(prefab, position, rotation);
			RuntimeParticleVfxCompatibility.Prepare(effect);
			effect.SetActive(value: true);
			TrackEffect(effect);
			Object.Destroy(effect, Mathf.Max(minimumLifetime, ResolveLifetime(effect)));
			return effect;
		}

		public static GameObject PlayOneShotTimed(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime)
		{
			if (prefab == null)
			{
				return null;
			}
			if (PrefabHasMissingScript(prefab))
			{
				PlayFallbackEffect(position, lifetime);
				return null;
			}
			GameObject effect = Object.Instantiate(prefab, position, rotation);
			RuntimeParticleVfxCompatibility.Prepare(effect);
			effect.SetActive(value: true);
			TrackEffect(effect);
			Object.Destroy(effect, Mathf.Max(0.1f, lifetime));
			return effect;
		}

		public static Quaternion FaceTowards(Vector3 origin, Vector3 target, Quaternion fallback)
		{
			Vector3 direction = target - origin;
			direction.y = 0f;
			if (direction.sqrMagnitude <= 1E-06f)
			{
				return fallback;
			}
			return Quaternion.LookRotation(direction.normalized, Vector3.up);
		}

		public static GameObject PlayAttachedTimed(GameObject prefab, Transform parent, Vector3 localPosition, Quaternion localRotation, float lifetime)
		{
			if (prefab == null || parent == null)
			{
				return null;
			}
			if (PrefabHasMissingScript(prefab))
			{
				PlayFallbackEffect(parent.position + localPosition, lifetime);
				return null;
			}
			GameObject effect = Object.Instantiate(prefab, parent);
			effect.transform.localPosition = localPosition;
			effect.transform.localRotation = localRotation;
			RuntimeParticleVfxCompatibility.Prepare(effect);
			ForceLocalParticleSimulation(effect);
			effect.SetActive(value: true);
			TrackEffect(effect);
			Object.Destroy(effect, Mathf.Max(0.1f, lifetime));
			return effect;
		}

		public static void DestroyEffect(GameObject effect)
		{
			if (effect == null)
			{
				return;
			}
			ParticleSystem[] particles = effect.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
			for (int i = 0; i < particles.Length; i++)
			{
				if (particles[i] != null)
				{
					particles[i].Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
				}
			}
			AudioSource[] audioSources = effect.GetComponentsInChildren<AudioSource>(includeInactive: true);
			for (int j = 0; j < audioSources.Length; j++)
			{
				if (audioSources[j] != null)
				{
					audioSources[j].Stop();
				}
			}
			if (Application.isPlaying)
			{
				Object.Destroy(effect);
			}
			else
			{
				Object.DestroyImmediate(effect);
			}
		}

		public static void ClearTrackedEffects()
		{
			for (int i = trackedEffects.Count - 1; i >= 0; i--)
			{
				GameObject effect = trackedEffects[i];
				trackedEffects.RemoveAt(i);
				DestroyEffect(effect);
			}
		}

		private static bool PrefabHasMissingScript(GameObject prefab)
		{
			if (prefab == null)
			{
				return false;
			}
			MonoBehaviour[] behaviours = prefab.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
			for (int i = 0; i < behaviours.Length; i++)
			{
				if (behaviours[i] == null)
				{
					return true;
				}
			}
			return false;
		}

		private static void PlayFallbackEffect(Vector3 position, float lifetime)
		{
			RuntimeCombatFeedback.ShowGroundPulse(position, new Color(0.62f, 0.88f, 1f, 0.72f), 0.42f, Mathf.Clamp(lifetime, 0.22f, 0.8f));
		}

		private static void TrackEffect(GameObject effect)
		{
			if (!(effect == null))
			{
				PruneTrackedEffects();
				while (trackedEffects.Count >= 72)
				{
					GameObject oldest = trackedEffects[0];
					trackedEffects.RemoveAt(0);
					DestroyEffect(oldest);
				}
				trackedEffects.Add(effect);
			}
		}

		private static void PruneTrackedEffects()
		{
			for (int i = trackedEffects.Count - 1; i >= 0; i--)
			{
				if (trackedEffects[i] == null)
				{
					trackedEffects.RemoveAt(i);
				}
			}
		}

		private static void ForceLocalParticleSimulation(GameObject effect)
		{
			ParticleSystem[] particles = effect.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
			foreach (ParticleSystem particle in particles)
			{
				if (!(particle == null))
				{
					ParticleSystem.MainModule main = particle.main;
					main.simulationSpace = ParticleSystemSimulationSpace.Local;
				}
			}
			LineRenderer[] lines = effect.GetComponentsInChildren<LineRenderer>(includeInactive: true);
			for (int j = 0; j < lines.Length; j++)
			{
				if (lines[j] != null)
				{
					lines[j].useWorldSpace = false;
				}
			}
		}

		private static float ResolveLifetime(GameObject effect)
		{
			float lifetime = 2f;
			ParticleSystem[] particles = effect.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
			foreach (ParticleSystem particle in particles)
			{
				if (!(particle == null))
				{
					ParticleSystem.MainModule main = particle.main;
					float particleLifetime = main.duration + main.startLifetime.constantMax;
					lifetime = Mathf.Max(lifetime, particleLifetime);
				}
			}
			AudioSource[] audioSources = effect.GetComponentsInChildren<AudioSource>(includeInactive: true);
			foreach (AudioSource audioSource in audioSources)
			{
				if (audioSource != null && audioSource.clip != null)
				{
					lifetime = Mathf.Max(lifetime, audioSource.clip.length);
				}
			}
			return lifetime;
		}
	}
}
