using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DefenseGame.Editor
{
	public static class MobileVfxAndFrameRateSetup
	{
		private const string ConfigPath = "Assets/Data/DefenseGamePresentationConfig.asset";

		private const string OutputDirectory = "Assets/Art/FX/RuntimeGenerated";

		private const string PrefabPath = "Assets/Art/FX/RuntimeGenerated/Effect_MonsterDeath_Mobile.prefab";

		private const string SmokeMaterialPath = "Assets/Art/FX/RuntimeGenerated/MonsterDeath_Smoke_URP.mat";

		private const string SparkMaterialPath = "Assets/Art/FX/RuntimeGenerated/MonsterDeath_Spark_URP.mat";

		private const string UrpParticleShaderName = "Universal Render Pipeline/Particles/Unlit";

		private const string CloudTextureGuid = "0bc2ca3e70d16064aab475ef194132b5";

		private const string HitTextureGuid = "37ef35d16eccee246b5ccfadd067c9da";

		[MenuItem("Defense Game/Legacy/Generate Simplified Death VFX")]
		public static void Apply()
		{
			GamePresentationConfig config = AssetDatabase.LoadAssetAtPath<GamePresentationConfig>("Assets/Data/DefenseGamePresentationConfig.asset");
			if ((UnityEngine.Object)(object)config == null)
			{
				throw new InvalidOperationException("Presentation config was not found at Assets/Data/DefenseGamePresentationConfig.asset");
			}
			Debug.Log("[Mobile VFX] Before: " + DescribeEffect(config.monsterDeathEffectPrefab));
			EnsureAssetDirectory("Assets/Art/FX/RuntimeGenerated");
			Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
			if (particleShader == null)
			{
				throw new InvalidOperationException("Required URP particle shader was not found: Universal Render Pipeline/Particles/Unlit");
			}
			Texture2D cloudTexture = LoadTexture("0bc2ca3e70d16064aab475ef194132b5", "cloud");
			Texture2D hitTexture = LoadTexture("37ef35d16eccee246b5ccfadd067c9da", "hit");
			Material smokeMaterial = CreateOrUpdateMaterial("Assets/Art/FX/RuntimeGenerated/MonsterDeath_Smoke_URP.mat", particleShader, cloudTexture, additive: false);
			Material sparkMaterial = CreateOrUpdateMaterial("Assets/Art/FX/RuntimeGenerated/MonsterDeath_Spark_URP.mat", particleShader, hitTexture, additive: true);
			GameObject prefab = (config.monsterDeathEffectPrefab = CreateOrUpdatePrefab(smokeMaterial, sparkMaterial));
			EditorUtility.SetDirty((UnityEngine.Object)(object)config);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			Validate(config, prefab);
			Debug.Log("[Mobile VFX] After: " + DescribeEffect(prefab));
			Debug.Log("[Mobile FPS] Runtime target=" + MobileFrameRateController.TargetFrameRate + ", vSync=0, Android optimized frame pacing remains enabled.");
		}

		public static void ApplyAndBuildAndroid()
		{
			Apply();
			AndroidApkBuilder.BuildDebugApk();
		}

		private static Texture2D LoadTexture(string guid, string label)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
			if (texture == null)
			{
				throw new InvalidOperationException("Could not load the existing " + label + " particle texture for GUID " + guid);
			}
			return texture;
		}

		private static Material CreateOrUpdateMaterial(string path, Shader shader, Texture texture, bool additive)
		{
			Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
			if (material == null)
			{
				material = new Material(shader)
				{
					name = Path.GetFileNameWithoutExtension(path)
				};
				AssetDatabase.CreateAsset(material, path);
			}
			else
			{
				material.shader = shader;
			}
			if (material.HasProperty("_BaseMap"))
			{
				material.SetTexture("_BaseMap", texture);
			}
			if (material.HasProperty("_MainTex"))
			{
				material.SetTexture("_MainTex", texture);
			}
			if (material.HasProperty("_BaseColor"))
			{
				material.SetColor("_BaseColor", Color.white);
			}
			material.SetOverrideTag("RenderType", "Transparent");
			SetFloatIfPresent(material, "_Surface", 1f);
			SetFloatIfPresent(material, "_Blend", additive ? 2f : 0f);
			SetFloatIfPresent(material, "_SrcBlend", 5f);
			SetFloatIfPresent(material, "_DstBlend", additive ? 1f : 10f);
			SetFloatIfPresent(material, "_ZWrite", 0f);
			SetFloatIfPresent(material, "_Cull", 0f);
			SetFloatIfPresent(material, "_AlphaClip", 0f);
			material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
			material.DisableKeyword("_ALPHATEST_ON");
			material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
			material.renderQueue = (additive ? 3100 : 3000);
			material.enableInstancing = true;
			EditorUtility.SetDirty(material);
			return material;
		}

		private static void SetFloatIfPresent(Material material, string propertyName, float value)
		{
			if (material.HasProperty(propertyName))
			{
				material.SetFloat(propertyName, value);
			}
		}

		private static GameObject CreateOrUpdatePrefab(Material smokeMaterial, Material sparkMaterial)
		{
			GameObject root = new GameObject("Effect_MonsterDeath_Mobile");
			try
			{
				CreateBurst(root.transform, sparkMaterial);
				CreateSmoke(root.transform, smokeMaterial);
				bool success;
				GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, "Assets/Art/FX/RuntimeGenerated/Effect_MonsterDeath_Mobile.prefab", out success);
				if (!success || prefab == null)
				{
					throw new InvalidOperationException("Failed to save the mobile monster death effect prefab.");
				}
				return prefab;
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(root);
			}
		}

		private static void CreateBurst(Transform parent, Material material)
		{
			ParticleSystem system = CreateSystem(parent, "Burst", material);
			ParticleSystem.MainModule main = system.main;
			main.duration = 0.25f;
			main.startLifetime = new ParticleSystem.MinMaxCurve(0.38f, 0.62f);
			main.startSpeed = new ParticleSystem.MinMaxCurve(1.4f, 2.5f);
			main.startSize = new ParticleSystem.MinMaxCurve(0.09f, 0.19f);
			main.startRotation = new ParticleSystem.MinMaxCurve(-(float)Math.PI, (float)Math.PI);
			main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.56f, 0.12f, 1f), new Color(1f, 0.92f, 0.42f, 1f));
			main.gravityModifier = 0.42f;
			main.maxParticles = 16;
			system.emission.SetBursts(new ParticleSystem.Burst[1]
			{
				new ParticleSystem.Burst(0f, 12, 16)
			});
			ParticleSystem.ShapeModule shape = system.shape;
			shape.enabled = true;
			shape.shapeType = ParticleSystemShapeType.Sphere;
			shape.radius = 0.22f;
			ConfigureFade(system, 0.78f, 0.12f);
		}

		private static void CreateSmoke(Transform parent, Material material)
		{
			ParticleSystem system = CreateSystem(parent, "Smoke", material);
			ParticleSystem.MainModule main = system.main;
			main.duration = 0.2f;
			main.startLifetime = new ParticleSystem.MinMaxCurve(0.65f, 0.95f);
			main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.65f);
			main.startSize = new ParticleSystem.MinMaxCurve(0.38f, 0.7f);
			main.startRotation = new ParticleSystem.MinMaxCurve(-(float)Math.PI, (float)Math.PI);
			main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.17f, 0.2f, 0.27f, 0.72f), new Color(0.42f, 0.48f, 0.6f, 0.65f));
			main.gravityModifier = -0.06f;
			main.maxParticles = 6;
			system.emission.SetBursts(new ParticleSystem.Burst[1]
			{
				new ParticleSystem.Burst(0f, 4, 6)
			});
			ParticleSystem.ShapeModule shape = system.shape;
			shape.enabled = true;
			shape.shapeType = ParticleSystemShapeType.Hemisphere;
			shape.radius = 0.18f;
			ConfigureFade(system, 1.25f, 0.42f);
		}

		private static ParticleSystem CreateSystem(Transform parent, string name, Material material)
		{
			GameObject child = new GameObject(name, typeof(ParticleSystem));
			child.transform.SetParent(parent, worldPositionStays: false);
			ParticleSystem system = child.GetComponent<ParticleSystem>();
			ParticleSystem.MainModule main = system.main;
			main.loop = false;
			main.playOnAwake = true;
			main.simulationSpace = ParticleSystemSimulationSpace.Local;
			main.scalingMode = ParticleSystemScalingMode.Hierarchy;
			main.stopAction = ParticleSystemStopAction.None;
			ParticleSystem.EmissionModule emission = system.emission;
			emission.enabled = true;
			emission.rateOverTime = 0f;
			ParticleSystemRenderer renderer = child.GetComponent<ParticleSystemRenderer>();
			renderer.renderMode = ParticleSystemRenderMode.Billboard;
			renderer.alignment = ParticleSystemRenderSpace.View;
			renderer.sortMode = ParticleSystemSortMode.Distance;
			renderer.sharedMaterial = material;
			renderer.shadowCastingMode = ShadowCastingMode.Off;
			renderer.receiveShadows = false;
			renderer.allowOcclusionWhenDynamic = true;
			renderer.maxParticleSize = 1f;
			return system;
		}

		private static void ConfigureFade(ParticleSystem system, float endScale, float middleScale)
		{
			Gradient gradient = new Gradient();
			gradient.SetKeys(new GradientColorKey[2]
			{
				new GradientColorKey(Color.white, 0f),
				new GradientColorKey(Color.white, 1f)
			}, new GradientAlphaKey[4]
			{
				new GradientAlphaKey(0f, 0f),
				new GradientAlphaKey(1f, 0.08f),
				new GradientAlphaKey(0.65f, 0.58f),
				new GradientAlphaKey(0f, 1f)
			});
			ParticleSystem.ColorOverLifetimeModule colorOverLifetime = system.colorOverLifetime;
			colorOverLifetime.enabled = true;
			colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
			AnimationCurve sizeCurve = new AnimationCurve(new Keyframe(0f, middleScale), new Keyframe(0.2f, 1f), new Keyframe(1f, endScale));
			ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = system.sizeOverLifetime;
			sizeOverLifetime.enabled = true;
			sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
		}

		private static void Validate(GamePresentationConfig config, GameObject prefab)
		{
			if (config.monsterDeathEffectPrefab != prefab)
			{
				throw new InvalidOperationException("Presentation config did not retain the generated death effect prefab.");
			}
			ParticleSystem[] systems = prefab.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
			int maximumParticles = systems.Sum((ParticleSystem system) => system.main.maxParticles);
			if (systems.Length != 2 || maximumParticles > 22)
			{
				throw new InvalidOperationException("Mobile death effect budget exceeded. Systems=" + systems.Length + ", maxParticles=" + maximumParticles);
			}
			ParticleSystemRenderer[] renderers = prefab.GetComponentsInChildren<ParticleSystemRenderer>(includeInactive: true);
			ParticleSystemRenderer[] array = renderers;
			foreach (ParticleSystemRenderer renderer in array)
			{
				Material material = renderer.sharedMaterial;
				if (material == null || material.shader == null || material.shader.name != "Universal Render Pipeline/Particles/Unlit")
				{
					throw new InvalidOperationException("Mobile death effect contains a missing or non-URP particle material on " + renderer.name);
				}
			}
		}

		private static string DescribeEffect(GameObject effect)
		{
			if (effect == null)
			{
				return "none";
			}
			ParticleSystem[] systems = effect.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
			int maximumParticles = systems.Sum((ParticleSystem system) => system.main.maxParticles);
			HashSet<string> shaderNames = new HashSet<string>();
			ParticleSystemRenderer[] componentsInChildren = effect.GetComponentsInChildren<ParticleSystemRenderer>(includeInactive: true);
			foreach (ParticleSystemRenderer renderer in componentsInChildren)
			{
				Material material = renderer.sharedMaterial;
				shaderNames.Add((material != null && material.shader != null) ? material.shader.name : "MISSING");
			}
			return effect.name + ", systems=" + systems.Length + ", maxParticles=" + maximumParticles + ", shaders=[" + string.Join(", ", shaderNames) + "]";
		}

		private static void EnsureAssetDirectory(string path)
		{
			string[] parts = path.Split('/', StringSplitOptions.None);
			string current = parts[0];
			for (int i = 1; i < parts.Length; i++)
			{
				string next = current + "/" + parts[i];
				if (!AssetDatabase.IsValidFolder(next))
				{
					AssetDatabase.CreateFolder(current, parts[i]);
				}
				current = next;
			}
		}
	}
}
