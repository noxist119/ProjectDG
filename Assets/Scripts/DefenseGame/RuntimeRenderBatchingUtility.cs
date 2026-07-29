using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace DefenseGame
{
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
			UsePerInstanceUnitTint = config != null && config.usePerInstanceUnitTint;
			EnableGpuInstancing = config != null && config.enableRuntimeGpuInstancing;
			ForceRuntimeUnitCastShadowsOff = config == null || config.forceRuntimeUnitCastShadowsOff;
			GraphicsSettings.useScriptableRenderPipelineBatching = true;
			GpuSkinnedUnitBatchRenderer.Configure(config);
			MaterialCache.Clear();
		}

		public static void PrepareRenderer(Renderer renderer)
		{
			if (renderer == null)
			{
				return;
			}
			Material[] materials = renderer.sharedMaterials;
			bool changed = false;
			for (int i = 0; i < materials.Length; i++)
			{
				Material material = materials[i];
				if (material == null)
				{
					continue;
				}
				if (EnableGpuInstancing)
				{
					material.enableInstancing = true;
				}
				string key = BuildMaterialKey(material);
				if (MaterialCache.TryGetValue(key, out var cached) && cached != null)
				{
					if (cached != material)
					{
						materials[i] = cached;
						changed = true;
					}
				}
				else
				{
					MaterialCache[key] = material;
				}
			}
			if (changed)
			{
				renderer.sharedMaterials = materials;
			}
		}

		private static string BuildMaterialKey(Material material)
		{
			StringBuilder builder = new StringBuilder(192);
			Shader shader = material.shader;
			builder.Append((shader != null) ? shader.GetInstanceID() : 0);
			builder.Append('|').Append(material.renderQueue);
			for (int i = 0; i < TextureProperties.Length; i++)
			{
				AppendTextureKey(builder, material, TextureProperties[i]);
			}
			for (int j = 0; j < ColorProperties.Length; j++)
			{
				AppendColorKey(builder, material, ColorProperties[j]);
			}
			for (int k = 0; k < FloatProperties.Length; k++)
			{
				AppendFloatKey(builder, material, FloatProperties[k]);
			}
			return builder.ToString();
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
			builder.Append((texture != null) ? texture.GetInstanceID() : 0);
		}

		private static void AppendColorKey(StringBuilder builder, Material material, string propertyName)
		{
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
}
