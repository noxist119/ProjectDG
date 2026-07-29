using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DefenseGame;

public static class RuntimeParticleVfxCompatibility
{
	private const string UrpParticleShaderName = "Universal Render Pipeline/Particles/Unlit";

	private static readonly Dictionary<Material, Material> compatibleMaterials = new Dictionary<Material, Material>();

	[RuntimeInitializeOnLoadMethod(/*Could not decode attribute arguments.*/)]
	private static void ResetCache()
	{
		compatibleMaterials.Clear();
	}

	public static void Prepare(GameObject effectRoot)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)effectRoot == (Object)null)
		{
			return;
		}
		ParticleSystem[] componentsInChildren = effectRoot.GetComponentsInChildren<ParticleSystem>(true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (!((Object)(object)componentsInChildren[i] == (Object)null))
			{
				MainModule main = componentsInChildren[i].main;
				((MainModule)(ref main)).maxParticles = Mathf.Clamp(((MainModule)(ref main)).maxParticles, 1, MobileFrameRateController.MaxParticlesPerSystem);
			}
		}
		ParticleSystemRenderer[] componentsInChildren2 = effectRoot.GetComponentsInChildren<ParticleSystemRenderer>(true);
		foreach (ParticleSystemRenderer val in componentsInChildren2)
		{
			if ((Object)(object)val == (Object)null)
			{
				continue;
			}
			((Renderer)val).shadowCastingMode = (ShadowCastingMode)0;
			((Renderer)val).receiveShadows = false;
			Material[] sharedMaterials = ((Renderer)val).sharedMaterials;
			bool flag = false;
			bool flag2 = false;
			for (int k = 0; k < sharedMaterials.Length; k++)
			{
				Material val2 = sharedMaterials[k];
				if (!((Object)(object)val2 == (Object)null) && !((Object)(object)val2.shader == (Object)null))
				{
					string name = ((Object)val2.shader).name;
					bool flag3 = name == "Mobile/Particles/Additive" || name == "Mobile/Particles/Alpha Blended";
					if (!flag3 && (!val2.shader.isSupported || name == "Hidden/InternalErrorShader" || name == "Shader Graphs/Decal"))
					{
						flag2 = true;
						break;
					}
					Material val3 = (flag3 ? GetCompatibleMaterial(val2, name == "Mobile/Particles/Additive") : val2);
					if ((Object)(object)val3 == (Object)null)
					{
						flag2 = true;
						break;
					}
					if ((Object)(object)val3 != (Object)(object)val2)
					{
						sharedMaterials[k] = val3;
						flag = true;
					}
				}
			}
			if (flag2)
			{
				((Renderer)val).enabled = false;
			}
			else if (flag)
			{
				((Renderer)val).sharedMaterials = sharedMaterials;
			}
		}
	}

	private static Material GetCompatibleMaterial(Material source, bool additive)
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Expected O, but got Unknown
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		if (compatibleMaterials.TryGetValue(source, out var value) && (Object)(object)value != (Object)null)
		{
			return value;
		}
		Shader val = Shader.Find("Universal Render Pipeline/Particles/Unlit");
		if ((Object)(object)val == (Object)null)
		{
			return null;
		}
		Material val2 = new Material(val)
		{
			name = ((Object)source).name + "_RuntimeURP",
			hideFlags = (HideFlags)61,
			enableInstancing = true
		};
		Texture texture = (source.HasProperty("_MainTex") ? source.GetTexture("_MainTex") : null);
		Color value2 = (source.HasProperty("_TintColor") ? source.GetColor("_TintColor") : (source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white));
		SetTexture(val2, "_BaseMap", texture);
		SetTexture(val2, "_MainTex", texture);
		SetColor(val2, "_BaseColor", value2);
		SetColor(val2, "_Color", value2);
		SetFloat(val2, "_Surface", 1f);
		SetFloat(val2, "_Blend", additive ? 2f : 0f);
		SetFloat(val2, "_SrcBlend", 5f);
		SetFloat(val2, "_DstBlend", additive ? 1f : 10f);
		SetFloat(val2, "_ZWrite", 0f);
		SetFloat(val2, "_Cull", 0f);
		val2.SetOverrideTag("RenderType", "Transparent");
		val2.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
		val2.renderQueue = (additive ? 3100 : 3000);
		compatibleMaterials[source] = val2;
		return val2;
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
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		if (material.HasProperty(propertyName))
		{
			material.SetColor(propertyName, value);
		}
	}
}
