using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DefenseGame
{
	public static class RuntimeParticleVfxCompatibility
	{
		private const string UrpParticleShaderName = "Universal Render Pipeline/Particles/Unlit";

		private static readonly Dictionary<Material, Material> compatibleMaterials = new Dictionary<Material, Material>();

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetCache()
		{
			compatibleMaterials.Clear();
		}

		public static void Prepare(GameObject effectRoot)
		{
			if (effectRoot == null)
			{
				return;
			}
			ParticleSystem[] systems = effectRoot.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
			for (int i = 0; i < systems.Length; i++)
			{
				if (!(systems[i] == null))
				{
					ParticleSystem.MainModule main = systems[i].main;
					main.maxParticles = Mathf.Clamp(main.maxParticles, 1, MobileFrameRateController.MaxParticlesPerSystem);
				}
			}
			ParticleSystemRenderer[] renderers = effectRoot.GetComponentsInChildren<ParticleSystemRenderer>(includeInactive: true);
			foreach (ParticleSystemRenderer renderer in renderers)
			{
				if (renderer == null)
				{
					continue;
				}
				renderer.shadowCastingMode = ShadowCastingMode.Off;
				renderer.receiveShadows = false;
				Material[] materials = renderer.sharedMaterials;
				bool changed = false;
				bool incompatible = false;
				for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
				{
					Material source = materials[materialIndex];
					if (!(source == null) && !(source.shader == null))
					{
						string shaderName = source.shader.name;
						bool legacyParticle = shaderName == "Mobile/Particles/Additive" || shaderName == "Mobile/Particles/Alpha Blended";
						if (!legacyParticle && (!source.shader.isSupported || shaderName == "Hidden/InternalErrorShader" || shaderName == "Shader Graphs/Decal"))
						{
							incompatible = true;
							break;
						}
						Material compatible = (legacyParticle ? GetCompatibleMaterial(source, shaderName == "Mobile/Particles/Additive") : source);
						if (compatible == null)
						{
							incompatible = true;
							break;
						}
						if (compatible != source)
						{
							materials[materialIndex] = compatible;
							changed = true;
						}
					}
				}
				if (incompatible)
				{
					renderer.enabled = false;
				}
				else if (changed)
				{
					renderer.sharedMaterials = materials;
				}
			}
		}

		private static Material GetCompatibleMaterial(Material source, bool additive)
		{
			if (compatibleMaterials.TryGetValue(source, out var cached) && cached != null)
			{
				return cached;
			}
			Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
			if (shader == null)
			{
				return null;
			}
			Material material = new Material(shader)
			{
				name = source.name + "_RuntimeURP",
				hideFlags = HideFlags.HideAndDontSave,
				enableInstancing = true
			};
			Texture texture = (source.HasProperty("_MainTex") ? source.GetTexture("_MainTex") : null);
			Color color = (source.HasProperty("_TintColor") ? source.GetColor("_TintColor") : (source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white));
			SetTexture(material, "_BaseMap", texture);
			SetTexture(material, "_MainTex", texture);
			SetColor(material, "_BaseColor", color);
			SetColor(material, "_Color", color);
			SetFloat(material, "_Surface", 1f);
			SetFloat(material, "_Blend", additive ? 2f : 0f);
			SetFloat(material, "_SrcBlend", 5f);
			SetFloat(material, "_DstBlend", additive ? 1f : 10f);
			SetFloat(material, "_ZWrite", 0f);
			SetFloat(material, "_Cull", 0f);
			material.SetOverrideTag("RenderType", "Transparent");
			material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
			material.renderQueue = (additive ? 3100 : 3000);
			compatibleMaterials[source] = material;
			return material;
		}

		private static void SetTexture(Material material, string propertyName, Texture texture)
		{
			if (material.HasProperty(propertyName))
			{
				material.SetTexture(propertyName, texture);
			}
		}

		private static void SetFloat(Material material, string propertyName, float value)
		{
			if (material.HasProperty(propertyName))
			{
				material.SetFloat(propertyName, value);
			}
		}

		private static void SetColor(Material material, string propertyName, Color value)
		{
			if (material.HasProperty(propertyName))
			{
				material.SetColor(propertyName, value);
			}
		}
	}
}
