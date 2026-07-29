using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DefenseGame
{
	public sealed class GpuSkinnedUnitBatchRenderer : MonoBehaviour
	{
		private readonly struct BatchKey : IEquatable<BatchKey>
		{
			public readonly Mesh mesh;

			public readonly Material material;

			public readonly int boneCount;

			public readonly int layer;

			public BatchKey(Mesh mesh, Material material, int boneCount, int layer)
			{
				this.mesh = mesh;
				this.material = material;
				this.boneCount = boneCount;
				this.layer = layer;
			}

			public bool Equals(BatchKey other)
			{
				return mesh == other.mesh && material == other.material && boneCount == other.boneCount && layer == other.layer;
			}

			public override bool Equals(object obj)
			{
				return obj is BatchKey other && Equals(other);
			}

			public override int GetHashCode()
			{
				int hash = ((mesh != null) ? mesh.GetInstanceID() : 0);
				hash = (hash * 397) ^ ((material != null) ? material.GetInstanceID() : 0);
				hash = (hash * 397) ^ boneCount;
				return (hash * 397) ^ layer;
			}
		}

		private readonly struct BatchInstance
		{
			public readonly GpuSkinnedUnitRenderer owner;

			public readonly GpuSkinnedUnitRenderer.SourceRenderer source;

			public BatchInstance(GpuSkinnedUnitRenderer owner, GpuSkinnedUnitRenderer.SourceRenderer source)
			{
				this.owner = owner;
				this.source = source;
			}
		}

		private sealed class BatchGroup : IDisposable
		{
			private static readonly string[] TextureProperties = new string[4] { "_BaseMap", "_MainTex", "_BumpMap", "_EmissionMap" };

			private static readonly string[] ColorProperties = new string[3] { "_BaseColor", "_Color", "_EmissionColor" };

			private static readonly string[] FloatProperties = new string[6] { "_Cutoff", "_AlphaClip", "_Cull", "_BumpScale", "_Metallic", "_Smoothness" };

			public readonly BatchKey key;

			public readonly List<BatchInstance> instances = new List<BatchInstance>(32);

			public int lastUsedFrame;

			private Material gpuMaterial;

			private ComputeBuffer boneBuffer;

			private ComputeBuffer rootMatrixBuffer;

			private ComputeBuffer colorBuffer;

			private ComputeBuffer flashBuffer;

			private ComputeBuffer argumentsBuffer;

			private MaterialPropertyBlock propertyBlock;

			private Matrix4x4[] boneMatrices = Array.Empty<Matrix4x4>();

			private Matrix4x4[] rootMatrices = Array.Empty<Matrix4x4>();

			private Vector4[] instanceColors = Array.Empty<Vector4>();

			private Vector4[] instanceFlash = Array.Empty<Vector4>();

			private readonly uint[] arguments = new uint[5];

			private bool drawFailureLogged;

			public BatchGroup(BatchKey key)
			{
				this.key = key;
			}

			public bool Draw()
			{
				int instanceCount = instances.Count;
				if (instanceCount <= 0 || key.mesh == null || key.material == null)
				{
					return false;
				}
				try
				{
					EnsureMaterial();
					if (gpuMaterial == null)
					{
						return false;
					}
					int requiredBoneMatrices = instanceCount * key.boneCount;
					EnsureBuffers(instanceCount, requiredBoneMatrices);
					Bounds bounds = FillInstanceData();
					boneBuffer.SetData(boneMatrices, 0, 0, requiredBoneMatrices);
					rootMatrixBuffer.SetData(rootMatrices, 0, 0, instanceCount);
					colorBuffer.SetData(instanceColors, 0, 0, instanceCount);
					flashBuffer.SetData(instanceFlash, 0, 0, instanceCount);
					arguments[0] = key.mesh.GetIndexCount(0);
					arguments[1] = (uint)instanceCount;
					arguments[2] = key.mesh.GetIndexStart(0);
					arguments[3] = key.mesh.GetBaseVertex(0);
					arguments[4] = 0u;
					argumentsBuffer.SetData(arguments);
					propertyBlock.Clear();
					propertyBlock.SetBuffer(BonesId, boneBuffer);
					propertyBlock.SetBuffer(RootMatricesId, rootMatrixBuffer);
					propertyBlock.SetBuffer(InstanceColorsId, colorBuffer);
					propertyBlock.SetBuffer(InstanceFlashId, flashBuffer);
					propertyBlock.SetInt(BonesPerInstanceId, key.boneCount);
					Graphics.DrawMeshInstancedIndirect(key.mesh, 0, gpuMaterial, bounds, argumentsBuffer, 0, propertyBlock, ShadowCastingMode.Off, receiveShadows: false, key.layer, null, LightProbeUsage.Off, null);
					return true;
				}
				catch (Exception ex)
				{
					if (!drawFailureLogged)
					{
						Debug.LogWarning("[GpuSkinBatch] '" + key.mesh.name + "' indirect draw failed. Falling back to SkinnedMeshRenderer. " + ex.Message);
						drawFailureLogged = true;
					}
					return false;
				}
			}

			public void Dispose()
			{
				ReleaseBuffer(ref boneBuffer);
				ReleaseBuffer(ref rootMatrixBuffer);
				ReleaseBuffer(ref colorBuffer);
				ReleaseBuffer(ref flashBuffer);
				ReleaseBuffer(ref argumentsBuffer);
				if (gpuMaterial != null)
				{
					UnityEngine.Object.Destroy(gpuMaterial);
					gpuMaterial = null;
				}
			}

			private void EnsureMaterial()
			{
				if (!(gpuMaterial != null))
				{
					gpuMaterial = new Material(gpuSkinShader)
					{
						name = key.material.name + " (GPU Skin Batch)",
						enableInstancing = true,
						renderQueue = key.material.renderQueue,
						hideFlags = HideFlags.DontSave
					};
					CopyMaterialProperties(key.material, gpuMaterial);
					propertyBlock = new MaterialPropertyBlock();
				}
			}

			private void EnsureBuffers(int instanceCount, int boneMatrixCount)
			{
				EnsureBuffer(ref boneBuffer, boneMatrixCount, 64, ComputeBufferType.Structured);
				EnsureBuffer(ref rootMatrixBuffer, instanceCount, 64, ComputeBufferType.Structured);
				EnsureBuffer(ref colorBuffer, instanceCount, 16, ComputeBufferType.Structured);
				EnsureBuffer(ref flashBuffer, instanceCount, 16, ComputeBufferType.Structured);
				EnsureBuffer(ref argumentsBuffer, 5, 4, ComputeBufferType.IndirectArguments);
				EnsureArrayCapacity(ref boneMatrices, boneMatrixCount);
				EnsureArrayCapacity(ref rootMatrices, instanceCount);
				EnsureArrayCapacity(ref instanceColors, instanceCount);
				EnsureArrayCapacity(ref instanceFlash, instanceCount);
			}

			private Bounds FillInstanceData()
			{
				bool hasBounds = false;
				Bounds combinedBounds = default(Bounds);
				for (int instanceIndex = 0; instanceIndex < instances.Count; instanceIndex++)
				{
					BatchInstance instance = instances[instanceIndex];
					SkinnedMeshRenderer renderer = instance.source.renderer;
					Transform[] bones = instance.source.bones;
					Matrix4x4[] bindPoses = instance.source.bindPoses;
					Matrix4x4 rootMatrix = renderer.localToWorldMatrix;
					rootMatrices[instanceIndex] = rootMatrix;
					int boneOffset = instanceIndex * key.boneCount;
					for (int boneIndex = 0; boneIndex < key.boneCount; boneIndex++)
					{
						Transform bone = bones[boneIndex];
						boneMatrices[boneOffset + boneIndex] = ((bone != null) ? (bone.localToWorldMatrix * bindPoses[boneIndex]) : rootMatrix);
					}
					Color tint = instance.owner.InstanceTint;
					Color flash = instance.owner.FlashColor;
					instanceColors[instanceIndex] = new Vector4(tint.r, tint.g, tint.b, tint.a);
					instanceFlash[instanceIndex] = new Vector4(flash.r, flash.g, flash.b, instance.owner.FlashStrength);
					Bounds rendererBounds = BuildWorldBounds(renderer);
					if (!hasBounds)
					{
						combinedBounds = rendererBounds;
						hasBounds = true;
					}
					else
					{
						combinedBounds.Encapsulate(rendererBounds);
					}
				}
				if (!hasBounds)
				{
					combinedBounds = new Bounds(Vector3.zero, Vector3.one * 4f);
				}
				combinedBounds.Expand(1f);
				return combinedBounds;
			}

			private static Bounds BuildWorldBounds(SkinnedMeshRenderer renderer)
			{
				Bounds localBounds = renderer.localBounds;
				Vector3 center = renderer.transform.TransformPoint(localBounds.center);
				Vector3 scale = renderer.transform.lossyScale;
				float maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
				float radius = Mathf.Max(1f, localBounds.extents.magnitude * maxScale);
				return new Bounds(center, Vector3.one * radius * 2f);
			}

			private static void CopyMaterialProperties(Material source, Material destination)
			{
				for (int i = 0; i < TextureProperties.Length; i++)
				{
					string property = TextureProperties[i];
					if (source.HasProperty(property) && destination.HasProperty(property))
					{
						destination.SetTexture(property, source.GetTexture(property));
						destination.SetTextureScale(property, source.GetTextureScale(property));
						destination.SetTextureOffset(property, source.GetTextureOffset(property));
					}
				}
				for (int j = 0; j < ColorProperties.Length; j++)
				{
					string property2 = ColorProperties[j];
					if (source.HasProperty(property2) && destination.HasProperty(property2))
					{
						destination.SetColor(property2, source.GetColor(property2));
					}
				}
				for (int k = 0; k < FloatProperties.Length; k++)
				{
					string property3 = FloatProperties[k];
					if (source.HasProperty(property3) && destination.HasProperty(property3))
					{
						destination.SetFloat(property3, source.GetFloat(property3));
					}
				}

				bool useLegacyMainTexture = !source.HasProperty("_BaseMap") && source.HasProperty("_MainTex");
				destination.SetFloat("_UseLegacyMainTex", useLegacyMainTexture ? 1f : 0f);
				if (source.HasProperty("_BaseColor"))
				{
					destination.SetColor("_Color", Color.white);
				}
				else
				{
					destination.SetColor("_BaseColor", Color.white);
				}
				bool alphaClip = source.IsKeywordEnabled("_ALPHATEST_ON") || (source.HasProperty("_AlphaClip") && source.GetFloat("_AlphaClip") > 0.5f);
				destination.SetFloat("_AlphaClip", alphaClip ? 1f : 0f);
				if (alphaClip)
				{
					destination.EnableKeyword("_ALPHATEST_ON");
				}
				else
				{
					destination.DisableKeyword("_ALPHATEST_ON");
				}
			}

			private static void EnsureBuffer(ref ComputeBuffer buffer, int requiredCount, int stride, ComputeBufferType type)
			{
				int safeRequiredCount = Mathf.Max(1, requiredCount);
				if (buffer == null || buffer.count < safeRequiredCount)
				{
					ReleaseBuffer(ref buffer);
					int capacity = Mathf.NextPowerOfTwo(safeRequiredCount);
					buffer = new ComputeBuffer(capacity, stride, type);
				}
			}

			private static void ReleaseBuffer(ref ComputeBuffer buffer)
			{
				if (buffer != null)
				{
					buffer.Release();
					buffer = null;
				}
			}

			private static void EnsureArrayCapacity<T>(ref T[] array, int requiredCount)
			{
				if (array.Length < requiredCount)
				{
					Array.Resize(ref array, Mathf.NextPowerOfTwo(Mathf.Max(1, requiredCount)));
				}
			}
		}

		private const string ShaderName = "DefenseGame/Mobile GPU Skinned Unit";

		private const int CleanupFrameInterval = 300;

		private const int UnusedGroupLifetimeFrames = 600;

		private static readonly int BonesId = Shader.PropertyToID("_GpuSkinBones");

		private static readonly int RootMatricesId = Shader.PropertyToID("_GpuRootMatrices");

		private static readonly int InstanceColorsId = Shader.PropertyToID("_GpuSkinColors");

		private static readonly int InstanceFlashId = Shader.PropertyToID("_GpuSkinFlash");

		private static readonly int BonesPerInstanceId = Shader.PropertyToID("_GpuBonesPerInstance");

		private static readonly List<GpuSkinnedUnitRenderer> ActiveProxies = new List<GpuSkinnedUnitRenderer>(96);

		private static GpuSkinnedUnitBatchRenderer instance;

		private static bool featureEnabled;

		private static bool allowDefenders = true;

		private static bool allowBosses;

		private static int minimumInstanceCount = 4;

		private static Shader gpuSkinShader;

		private static bool loggedUnsupported;

		private readonly Dictionary<BatchKey, BatchGroup> groups = new Dictionary<BatchKey, BatchGroup>(32);

		private readonly List<BatchGroup> activeGroups = new List<BatchGroup>(32);

		private readonly List<BatchKey> staleGroupKeys = new List<BatchKey>(16);

		public static int BatchedRendererCount { get; private set; }

		public static int SubmittedDrawCount { get; private set; }

		public static int EstimatedSavedDrawCount { get; private set; }

		public static bool FeatureEnabled => featureEnabled;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			instance = null;
			featureEnabled = false;
			allowDefenders = true;
			allowBosses = false;
			minimumInstanceCount = 4;
			gpuSkinShader = null;
			loggedUnsupported = false;
			ActiveProxies.Clear();
			BatchedRendererCount = 0;
			SubmittedDrawCount = 0;
			EstimatedSavedDrawCount = 0;
		}

		public static void Configure(GamePresentationConfig config)
		{
			bool requested = config != null && config.enableGpuSkinnedUnitBatching;
			bool lowEndOnly = config == null || config.gpuSkinnedBatchingLowEndOnly;
			allowDefenders = config == null || config.gpuSkinnedBatchDefenders;
			allowBosses = config != null && config.gpuSkinnedBatchBosses;
			minimumInstanceCount = Mathf.Max(2, (config != null) ? config.gpuSkinnedBatchMinInstanceCount : 4);
			bool profileAllows = !lowEndOnly || MobileFrameRateController.IsLowEndDevice;
			bool hardwareAllows = SystemInfo.supportsInstancing && SystemInfo.supportsComputeShaders && SystemInfo.graphicsShaderLevel >= 45;
			gpuSkinShader = Shader.Find("DefenseGame/Mobile GPU Skinned Unit");
			featureEnabled = requested && profileAllows && hardwareAllows && gpuSkinShader != null;
			if (featureEnabled)
			{
				EnsureInstance();
				Debug.Log($"[GpuSkinBatch] enabled, minimumGroup={minimumInstanceCount}, " + $"defenders={allowDefenders}, bosses={allowBosses}");
			}
			else if (requested && profileAllows && !loggedUnsupported)
			{
				loggedUnsupported = true;
				Debug.LogWarning("[GpuSkinBatch] unavailable on this device/API. " + $"instancing={SystemInfo.supportsInstancing}, compute={SystemInfo.supportsComputeShaders}, " + $"shaderLevel={SystemInfo.graphicsShaderLevel}, shaderFound={gpuSkinShader != null}. " + "Using regular SkinnedMeshRenderer.");
			}
		}

		public static bool CanUseForUnit(bool isDefender, bool isBoss)
		{
			return featureEnabled && (!isDefender || allowDefenders) && (!isBoss || allowBosses);
		}

		internal static void Register(GpuSkinnedUnitRenderer proxy)
		{
			if (featureEnabled && !(proxy == null) && !ActiveProxies.Contains(proxy))
			{
				ActiveProxies.Add(proxy);
				EnsureInstance();
			}
		}

		internal static void Unregister(GpuSkinnedUnitRenderer proxy)
		{
			if (proxy != null)
			{
				ActiveProxies.Remove(proxy);
			}
		}

		private static void EnsureInstance()
		{
			if (!(instance != null) && Application.isPlaying)
			{
				GameObject host = new GameObject("GpuSkinnedUnitBatchRenderer");
				host.hideFlags = HideFlags.HideInHierarchy;
				instance = host.AddComponent<GpuSkinnedUnitBatchRenderer>();
				UnityEngine.Object.DontDestroyOnLoad(host);
			}
		}

		private void LateUpdate()
		{
			BatchedRendererCount = 0;
			SubmittedDrawCount = 0;
			EstimatedSavedDrawCount = 0;
			activeGroups.Clear();
			if (!featureEnabled)
			{
				RestoreAllRenderers();
				return;
			}
			for (int proxyIndex = ActiveProxies.Count - 1; proxyIndex >= 0; proxyIndex--)
			{
				GpuSkinnedUnitRenderer proxy = ActiveProxies[proxyIndex];
				if (proxy == null)
				{
					ActiveProxies.RemoveAt(proxyIndex);
				}
				else if (proxy.isActiveAndEnabled && proxy.gameObject.activeInHierarchy)
				{
					IReadOnlyList<GpuSkinnedUnitRenderer.SourceRenderer> sources = proxy.SourceRenderers;
					for (int sourceIndex = 0; sourceIndex < sources.Count; sourceIndex++)
					{
						GpuSkinnedUnitRenderer.SourceRenderer source = sources[sourceIndex];
						if (!TryBuildBatchKey(source, out var key))
						{
							source.SetBatched(value: false);
							continue;
						}
						if (!groups.TryGetValue(key, out var group))
						{
							group = new BatchGroup(key);
							groups.Add(key, group);
						}
						if (group.instances.Count == 0)
						{
							activeGroups.Add(group);
						}
						group.instances.Add(new BatchInstance(proxy, source));
						group.lastUsedFrame = Time.frameCount;
					}
				}
			}
			for (int groupIndex = 0; groupIndex < activeGroups.Count; groupIndex++)
			{
				BatchGroup group2 = activeGroups[groupIndex];
				bool submitted = group2.instances.Count >= minimumInstanceCount && group2.Draw();
				for (int instanceIndex = 0; instanceIndex < group2.instances.Count; instanceIndex++)
				{
					group2.instances[instanceIndex].source.SetBatched(submitted);
				}
				if (submitted)
				{
					BatchedRendererCount += group2.instances.Count;
					SubmittedDrawCount++;
					EstimatedSavedDrawCount += Mathf.Max(0, group2.instances.Count - 1);
				}
				group2.instances.Clear();
			}
			if (Time.frameCount % 300 == 0)
			{
				CleanupUnusedGroups();
			}
		}

		private void OnDestroy()
		{
			RestoreAllRenderers();
			foreach (KeyValuePair<BatchKey, BatchGroup> group in groups)
			{
				group.Value.Dispose();
			}
			groups.Clear();
			if (instance == this)
			{
				instance = null;
			}
		}

		private static bool TryBuildBatchKey(GpuSkinnedUnitRenderer.SourceRenderer source, out BatchKey key)
		{
			key = default(BatchKey);
			SkinnedMeshRenderer renderer = source.renderer;
			if (renderer == null || !source.originalEnabled || !renderer.gameObject.activeInHierarchy || renderer.forceRenderingOff)
			{
				return false;
			}
			if (!source.RefreshGeometry())
			{
				return false;
			}
			Mesh mesh = source.mesh;
			if (mesh == null || mesh.subMeshCount != 1 || mesh.blendShapeCount > 0 || !mesh.HasVertexAttribute(VertexAttribute.BlendIndices) || !mesh.HasVertexAttribute(VertexAttribute.BlendWeight) || renderer.GetComponent<Cloth>() != null)
			{
				return false;
			}
			Transform[] bones = source.bones;
			Matrix4x4[] bindPoses = source.bindPoses;
			if (bones.Length != bindPoses.Length)
			{
				return false;
			}
			Material material = renderer.sharedMaterial;
			if (!IsMaterialSupported(material))
			{
				return false;
			}
			key = new BatchKey(mesh, material, bones.Length, renderer.gameObject.layer);
			return true;
		}

		private static bool IsMaterialSupported(Material material)
		{
			if (material == null || material.shader == null || material.renderQueue > 2500)
			{
				return false;
			}
			if (material.HasProperty("_Surface") && material.GetFloat("_Surface") > 0.5f)
			{
				return false;
			}
			return !material.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT");
		}

		private void RestoreAllRenderers()
		{
			for (int proxyIndex = 0; proxyIndex < ActiveProxies.Count; proxyIndex++)
			{
				GpuSkinnedUnitRenderer proxy = ActiveProxies[proxyIndex];
				if (!(proxy == null))
				{
					IReadOnlyList<GpuSkinnedUnitRenderer.SourceRenderer> sources = proxy.SourceRenderers;
					for (int sourceIndex = 0; sourceIndex < sources.Count; sourceIndex++)
					{
						sources[sourceIndex].SetBatched(value: false);
					}
				}
			}
		}

		private void CleanupUnusedGroups()
		{
			staleGroupKeys.Clear();
			foreach (KeyValuePair<BatchKey, BatchGroup> pair in groups)
			{
				if (Time.frameCount - pair.Value.lastUsedFrame > 600)
				{
					staleGroupKeys.Add(pair.Key);
				}
			}
			for (int i = 0; i < staleGroupKeys.Count; i++)
			{
				BatchKey key = staleGroupKeys[i];
				if (groups.TryGetValue(key, out var group))
				{
					group.Dispose();
					groups.Remove(key);
				}
			}
		}
	}
}
