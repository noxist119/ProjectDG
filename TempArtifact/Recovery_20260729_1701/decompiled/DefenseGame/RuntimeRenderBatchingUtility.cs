using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace DefenseGame;

public static class RuntimeRenderBatchingUtility
{
	private static readonly Dictionary<string, Material> MaterialCache = new Dictionary<string, Material>();

	private static readonly string[] TextureProperties = new string[7] { "_BaseMap", "_MainTex", "_BumpMap", "_NormalMap", "_MaskMap", "_MetallicGlossMap", "_EmissionMap" };

	private static readonly string[] ColorProperties = new string[3] { "_BaseColor", "_Color", "_EmissionColor" };

	private static readonly string[] FloatProperties = new string[7] { "_Metallic", "_Smoothness", "_Glossiness", "_AlphaClip", "_Surface", "_Cull", "_ZWrite" };

	public static bool UsePerInstanceUnitTint { get; private set; }

	public static bool EnableGpuInstancing { get; private set; }

	public static bool ForceRuntimeUnitCastShadowsOff { get; private set; } = true;

	public static void Configure(GamePresentationConfig config)
	{
		UsePerInstanceUnitTint = (Object)(object)config != (Object)null && config.usePerInstanceUnitTint;
		EnableGpuInstancing = (Object)(object)config != (Object)null && config.enableRuntimeGpuInstancing;
		ForceRuntimeUnitCastShadowsOff = (Object)(object)config == (Object)null || config.forceRuntimeUnitCastShadowsOff;
		GraphicsSettings.useScriptableRenderPipelineBatching = true;
		GpuSkinnedUnitBatchRenderer.Configure(config);
		MaterialCache.Clear();
	}

	public static void PrepareRenderer(Renderer renderer)
	{
		if ((Object)(object)renderer == (Object)null)
		{
			return;
		}
		Material[] sharedMaterials = renderer.sharedMaterials;
		bool flag = false;
		for (int i = 0; i < sharedMaterials.Length; i++)
		{
			Material val = sharedMaterials[i];
			if ((Object)(object)val == (Object)null)
			{
				continue;
			}
			if (EnableGpuInstancing)
			{
				val.enableInstancing = true;
			}
			string key = BuildMaterialKey(val);
			if (MaterialCache.TryGetValue(key, out var value) && (Object)(object)value != (Object)null)
			{
				if ((Object)(object)value != (Object)(object)val)
				{
					sharedMaterials[i] = value;
					flag = true;
				}
			}
			else
			{
				MaterialCache[key] = val;
			}
		}
		if (flag)
		{
			renderer.sharedMaterials = sharedMaterials;
		}
	}

	private static string BuildMaterialKey(Material material)
	{
		StringBuilder stringBuilder = new StringBuilder(192);
		Shader shader = material.shader;
		stringBuilder.Append(((Object)(object)shader != (Object)null) ? ((Object)shader).GetInstanceID() : 0);
		stringBuilder.Append('|').Append(material.renderQueue);
		for (int i = 0; i < TextureProperties.Length; i++)
		{
			AppendTextureKey(stringBuilder, material, TextureProperties[i]);
		}
		for (int j = 0; j < ColorProperties.Length; j++)
		{
			AppendColorKey(stringBuilder, material, ColorProperties[j]);
		}
		for (int k = 0; k < FloatProperties.Length; k++)
		{
			AppendFloatKey(stringBuilder, material, FloatProperties[k]);
		}
		return stringBuilder.ToString();
	}

	private static void AppendTextureKey(StringBuilder builder, Material material, string propertyName)
	{
		builder.Append('|').Append(propertyName).Append(':');
		if (!material.HasProperty(propertyName))
		{
			builder.Append('x');
			return;
		}
		Texture texture = material.GetTexture(propertyName);
		builder.Append(((Object)(object)texture != (Object)null) ? ((Object)texture).GetInstanceID() : 0);
	}

	private static void AppendColorKey(StringBuilder builder, Material material, string propertyName)
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		builder.Append('|').Append(propertyName).Append(':');
		if (!material.HasProperty(propertyName))
		{
			builder.Append('x');
			return;
		}
		Color color = material.GetColor(propertyName);
		builder.Append(Mathf.RoundToInt(color.r * 255f)).Append(',').Append(Mathf.RoundToInt(color.g * 255f))
			.Append(',')
			.Append(Mathf.RoundToInt(color.b * 255f))
			.Append(',')
			.Append(Mathf.RoundToInt(color.a * 255f));
	}

	private static void AppendFloatKey(StringBuilder builder, Material material, string propertyName)
	{
		builder.Append('|').Append(propertyName).Append(':');
		if (!material.HasProperty(propertyName))
		{
			builder.Append('x');
		}
		else
		{
			builder.Append(Mathf.RoundToInt(material.GetFloat(propertyName) * 1000f));
		}
	}
}
