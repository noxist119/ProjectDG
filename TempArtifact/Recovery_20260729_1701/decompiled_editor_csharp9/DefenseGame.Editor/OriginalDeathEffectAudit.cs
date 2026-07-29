using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DefenseGame.Editor
{
	public static class OriginalDeathEffectAudit
	{
		private const string PrefabPath = "Assets/Art/FX/Nea/Prefab/Effect_Die_Friendly.prefab";

		public static void Run()
		{
			GameObject root = PrefabUtility.LoadPrefabContents("Assets/Art/FX/Nea/Prefab/Effect_Die_Friendly.prefab");
			try
			{
				ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
				Debug.Log("[Original Death Audit] totalSystems=" + systems.Length + ", activeSystems=" + systems.Count(IsActive));
				foreach (Transform child in root.transform)
				{
					ParticleSystem[] groupSystems = child.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
					int activeSystems = groupSystems.Count(IsActive);
					int maxParticles = groupSystems.Where(IsActive).Sum((ParticleSystem system) => system.main.maxParticles);
					float burstMax = groupSystems.Where(IsActive).Sum((Func<ParticleSystem, int>)GetBurstMaximum);
					float rateMax = groupSystems.Where(IsActive).Sum((ParticleSystem system) => system.emission.rateOverTime.constantMax);
					string shaders = string.Join(",", (from system in groupSystems
						select system.GetComponent<ParticleSystemRenderer>() into renderer
						where renderer != null && renderer.sharedMaterial != null && renderer.sharedMaterial.shader != null
						select renderer.sharedMaterial.shader.name).Distinct());
					Debug.Log("[Original Death Group] name=" + child.name + ", activeSelf=" + child.gameObject.activeSelf + ", systems=" + groupSystems.Length + ", activeSystems=" + activeSystems + ", maxParticles=" + maxParticles + ", burstMax=" + burstMax + ", rateMax=" + rateMax.ToString("0.##") + ", shaders=[" + shaders + "]");
				}
				foreach (IGrouping<string, ParticleSystem> materialGroup in from system in systems.Where(IsActive)
					where system.GetComponent<ParticleSystemRenderer>() != null
					group system by DescribeMaterial(system.GetComponent<ParticleSystemRenderer>()))
				{
					Debug.Log("[Original Death Material] " + materialGroup.Key + ", systems=" + materialGroup.Count());
				}
				int collisionCount = systems.Count((ParticleSystem system) => IsActive(system) && system.collision.enabled);
				int trailCount = systems.Count((ParticleSystem system) => IsActive(system) && system.trails.enabled);
				int noiseCount = systems.Count((ParticleSystem system) => IsActive(system) && system.noise.enabled);
				int lightCount = systems.Count((ParticleSystem system) => IsActive(system) && system.lights.enabled);
				Debug.Log("[Original Death Modules] collision=" + collisionCount + ", trails=" + trailCount + ", noise=" + noiseCount + ", lights=" + lightCount);
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(root);
			}
		}

		private static bool IsActive(ParticleSystem system)
		{
			return system != null && system.gameObject.activeInHierarchy;
		}

		private static int GetBurstMaximum(ParticleSystem system)
		{
			ParticleSystem.EmissionModule emission = system.emission;
			int count = emission.burstCount;
			if (count <= 0)
			{
				return 0;
			}
			ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[count];
			emission.GetBursts(bursts);
			int total = 0;
			for (int i = 0; i < bursts.Length; i++)
			{
				total += Mathf.CeilToInt(bursts[i].maxCount);
			}
			return total;
		}

		private static string DescribeMaterial(ParticleSystemRenderer renderer)
		{
			Material material = renderer.sharedMaterial;
			if (material == null)
			{
				return "MISSING";
			}
			string shaderName = ((material.shader != null) ? material.shader.name : "MISSING_SHADER");
			return material.name + "|" + shaderName;
		}
	}
}
