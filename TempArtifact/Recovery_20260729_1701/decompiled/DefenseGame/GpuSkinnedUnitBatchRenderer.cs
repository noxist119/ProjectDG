using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DefenseGame;

public sealed class GpuSkinnedUnitBatchRenderer : MonoBehaviour
{
	private readonly struct BatchKey(Mesh mesh, Material material, int boneCount, int layer) : IEquatable<BatchKey>
	{
		public readonly Mesh mesh = mesh;

		public readonly Material material = material;

		public readonly int boneCount = boneCount;

		public readonly int layer = layer;

		public bool Equals(BatchKey other)
		{
			return (Object)(object)mesh == (Object)(object)other.mesh && (Object)(object)material == (Object)(object)other.material && boneCount == other.boneCount && layer == other.layer;
		}

		public override bool Equals(object obj)
		{
			return obj is BatchKey other && Equals(other);
		}

		public override int GetHashCode()
		{
			int num = (((Object)(object)mesh != (Object)null) ? ((Object)mesh).GetInstanceID() : 0);
			num = (num * 397) ^ (((Object)(object)material != (Object)null) ? ((Object)material).GetInstanceID() : 0);
			num = (num * 397) ^ boneCount;
			return (num * 397) ^ layer;
		}
	}

	private readonly struct BatchInstance(GpuSkinnedUnitRenderer owner, GpuSkinnedUnitRenderer.SourceRenderer source)
	{
		public readonly GpuSkinnedUnitRenderer owner = owner;

		public readonly GpuSkinnedUnitRenderer.SourceRenderer source = source;
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
			//IL_007f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0084: Unknown result type (might be due to invalid IL or missing references)
			//IL_01df: Unknown result type (might be due to invalid IL or missing references)
			int count = instances.Count;
			if (count <= 0 || (Object)(object)key.mesh == (Object)null || (Object)(object)key.material == (Object)null)
			{
				return false;
			}
			try
			{
				EnsureMaterial();
				if ((Object)(object)gpuMaterial == (Object)null)
				{
					return false;
				}
				int num = count * key.boneCount;
				EnsureBuffers(count, num);
				Bounds val = FillInstanceData();
				boneBuffer.SetData((Array)boneMatrices, 0, 0, num);
				rootMatrixBuffer.SetData((Array)rootMatrices, 0, 0, count);
				colorBuffer.SetData((Array)instanceColors, 0, 0, count);
				flashBuffer.SetData((Array)instanceFlash, 0, 0, count);
				arguments[0] = key.mesh.GetIndexCount(0);
				arguments[1] = (uint)count;
				arguments[2] = key.mesh.GetIndexStart(0);
				arguments[3] = key.mesh.GetBaseVertex(0);
				arguments[4] = 0u;
				argumentsBuffer.SetData((Array)arguments);
				propertyBlock.Clear();
				propertyBlock.SetBuffer(BonesId, boneBuffer);
				propertyBlock.SetBuffer(RootMatricesId, rootMatrixBuffer);
				propertyBlock.SetBuffer(InstanceColorsId, colorBuffer);
				propertyBlock.SetBuffer(InstanceFlashId, flashBuffer);
				propertyBlock.SetInt(BonesPerInstanceId, key.boneCount);
				Graphics.DrawMeshInstancedIndirect(key.mesh, 0, gpuMaterial, val, argumentsBuffer, 0, propertyBlock, (ShadowCastingMode)0, false, key.layer, (Camera)null, (LightProbeUsage)0, (LightProbeProxyVolume)null);
				return true;
			}
			catch (Exception ex)
			{
				if (!drawFailureLogged)
				{
					Debug.LogWarning((object)("[GpuSkinBatch] '" + ((Object)key.mesh).name + "' indirect draw failed. Falling back to SkinnedMeshRenderer. " + ex.Message));
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
			if ((Object)(object)gpuMaterial != (Object)null)
			{
				Object.Destroy((Object)(object)gpuMaterial);
				gpuMaterial = null;
			}
		}

		private void EnsureMaterial()
		{
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_005f: Unknown result type (might be due to invalid IL or missing references)
			//IL_006d: Expected O, but got Unknown
			//IL_0085: Unknown result type (might be due to invalid IL or missing references)
			//IL_008f: Expected O, but got Unknown
			if (!((Object)(object)gpuMaterial != (Object)null))
			{
				gpuMaterial = new Material(gpuSkinShader)
				{
					name = ((Object)key.material).name + " (GPU Skin Batch)",
					enableInstancing = true,
					renderQueue = key.material.renderQueue,
					hideFlags = (HideFlags)52
				};
				CopyMaterialProperties(key.material, gpuMaterial);
				propertyBlock = new MaterialPropertyBlock();
			}
		}

		private void EnsureBuffers(int instanceCount, int boneMatrixCount)
		{
			EnsureBuffer(ref boneBuffer, boneMatrixCount, 64, (ComputeBufferType)16);
			EnsureBuffer(ref rootMatrixBuffer, instanceCount, 64, (ComputeBufferType)16);
			EnsureBuffer(ref colorBuffer, instanceCount, 16, (ComputeBufferType)16);
			EnsureBuffer(ref flashBuffer, instanceCount, 16, (ComputeBufferType)16);
			EnsureBuffer(ref argumentsBuffer, 5, 4, (ComputeBufferType)256);
			EnsureArrayCapacity(ref boneMatrices, boneMatrixCount);
			EnsureArrayCapacity(ref rootMatrices, instanceCount);
			EnsureArrayCapacity(ref instanceColors, instanceCount);
			EnsureArrayCapacity(ref instanceFlash, instanceCount);
		}

		private Bounds FillInstanceData()
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_0049: Unknown result type (might be due to invalid IL or missing references)
			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0057: Unknown result type (might be due to invalid IL or missing references)
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
			//IL_0192: Unknown result type (might be due to invalid IL or missing references)
			//IL_0197: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
			//IL_0106: Unknown result type (might be due to invalid IL or missing references)
			//IL_010b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0117: Unknown result type (might be due to invalid IL or missing references)
			//IL_011e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0125: Unknown result type (might be due to invalid IL or missing references)
			//IL_0137: Unknown result type (might be due to invalid IL or missing references)
			//IL_013c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0143: Unknown result type (might be due to invalid IL or missing references)
			//IL_0148: Unknown result type (might be due to invalid IL or missing references)
			//IL_01be: Unknown result type (might be due to invalid IL or missing references)
			//IL_0095: Unknown result type (might be due to invalid IL or missing references)
			//IL_009e: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
			//IL_008f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0160: Unknown result type (might be due to invalid IL or missing references)
			//IL_0155: Unknown result type (might be due to invalid IL or missing references)
			//IL_0157: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
			bool flag = false;
			Bounds result = default(Bounds);
			for (int i = 0; i < instances.Count; i++)
			{
				BatchInstance batchInstance = instances[i];
				SkinnedMeshRenderer renderer = batchInstance.source.renderer;
				Transform[] bones = batchInstance.source.bones;
				Matrix4x4[] bindPoses = batchInstance.source.bindPoses;
				Matrix4x4 localToWorldMatrix = ((Renderer)renderer).localToWorldMatrix;
				rootMatrices[i] = localToWorldMatrix;
				int num = i * key.boneCount;
				for (int j = 0; j < key.boneCount; j++)
				{
					Transform val = bones[j];
					boneMatrices[num + j] = (((Object)(object)val != (Object)null) ? (val.localToWorldMatrix * bindPoses[j]) : localToWorldMatrix);
				}
				Color instanceTint = batchInstance.owner.InstanceTint;
				Color flashColor = batchInstance.owner.FlashColor;
				instanceColors[i] = new Vector4(instanceTint.r, instanceTint.g, instanceTint.b, instanceTint.a);
				instanceFlash[i] = new Vector4(flashColor.r, flashColor.g, flashColor.b, batchInstance.owner.FlashStrength);
				Bounds val2 = BuildWorldBounds(renderer);
				if (!flag)
				{
					result = val2;
					flag = true;
				}
				else
				{
					((Bounds)(ref result)).Encapsulate(val2);
				}
			}
			if (!flag)
			{
				((Bounds)(ref result))._002Ector(Vector3.zero, Vector3.one * 4f);
			}
			((Bounds)(ref result)).Expand(1f);
			return result;
		}

		private static Bounds BuildWorldBounds(SkinnedMeshRenderer renderer)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_004b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0064: Unknown result type (might be due to invalid IL or missing references)
			//IL_0069: Unknown result type (might be due to invalid IL or missing references)
			//IL_007b: Unknown result type (might be due to invalid IL or missing references)
			//IL_007c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0083: Unknown result type (might be due to invalid IL or missing references)
			//IL_008d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0092: Unknown result type (might be due to invalid IL or missing references)
			//IL_0097: Unknown result type (might be due to invalid IL or missing references)
			//IL_009b: Unknown result type (might be due to invalid IL or missing references)
			Bounds localBounds = ((Renderer)renderer).localBounds;
			Vector3 val = ((Component)renderer).transform.TransformPoint(((Bounds)(ref localBounds)).center);
			Vector3 lossyScale = ((Component)renderer).transform.lossyScale;
			float num = Mathf.Max(new float[3]
			{
				Mathf.Abs(lossyScale.x),
				Mathf.Abs(lossyScale.y),
				Mathf.Abs(lossyScale.z)
			});
			Vector3 extents = ((Bounds)(ref localBounds)).extents;
			float num2 = Mathf.Max(1f, ((Vector3)(ref extents)).magnitude * num);
			return new Bounds(val, Vector3.one * num2 * 2f);
		}

		private static void CopyMaterialProperties(Material source, Material destination)
		{
			//IL_003e: Unknown result type (might be due to invalid IL or missing references)
			//IL_004d: Unknown result type (might be due to invalid IL or missing references)
			//IL_009d: Unknown result type (might be due to invalid IL or missing references)
			for (int i = 0; i < TextureProperties.Length; i++)
			{
				string text = TextureProperties[i];
				if (source.HasProperty(text) && destination.HasProperty(text))
				{
					destination.SetTexture(text, source.GetTexture(text));
					destination.SetTextureScale(text, source.GetTextureScale(text));
					destination.SetTextureOffset(text, source.GetTextureOffset(text));
				}
			}
			for (int j = 0; j < ColorProperties.Length; j++)
			{
				string text2 = ColorProperties[j];
				if (source.HasProperty(text2) && destination.HasProperty(text2))
				{
					destination.SetColor(text2, source.GetColor(text2));
				}
			}
			for (int k = 0; k < FloatProperties.Length; k++)
			{
				string text3 = FloatProperties[k];
				if (source.HasProperty(text3) && destination.HasProperty(text3))
				{
					destination.SetFloat(text3, source.GetFloat(text3));
				}
			}
		}

		private static void EnsureBuffer(ref ComputeBuffer buffer, int requiredCount, int stride, ComputeBufferType type)
		{
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Expected O, but got Unknown
			int num = Mathf.Max(1, requiredCount);
			if (buffer == null || buffer.count < num)
			{
				ReleaseBuffer(ref buffer);
				int num2 = Mathf.NextPowerOfTwo(num);
				buffer = new ComputeBuffer(num2, stride, type);
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

	[RuntimeInitializeOnLoadMethod(/*Could not decode attribute arguments.*/)]
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
		bool flag = (Object)(object)config != (Object)null && config.enableGpuSkinnedUnitBatching;
		bool flag2 = (Object)(object)config == (Object)null || config.gpuSkinnedBatchingLowEndOnly;
		allowDefenders = (Object)(object)config == (Object)null || config.gpuSkinnedBatchDefenders;
		allowBosses = (Object)(object)config != (Object)null && config.gpuSkinnedBatchBosses;
		minimumInstanceCount = Mathf.Max(2, ((Object)(object)config != (Object)null) ? config.gpuSkinnedBatchMinInstanceCount : 4);
		bool flag3 = !flag2 || MobileFrameRateController.IsLowEndDevice;
		bool flag4 = SystemInfo.supportsInstancing && SystemInfo.supportsComputeShaders && SystemInfo.graphicsShaderLevel >= 45;
		gpuSkinShader = Shader.Find("DefenseGame/Mobile GPU Skinned Unit");
		featureEnabled = flag && flag3 && flag4 && (Object)(object)gpuSkinShader != (Object)null;
		if (featureEnabled)
		{
			EnsureInstance();
			Debug.Log((object)($"[GpuSkinBatch] enabled, minimumGroup={minimumInstanceCount}, " + $"defenders={allowDefenders}, bosses={allowBosses}"));
		}
		else if (flag && flag3 && !loggedUnsupported)
		{
			loggedUnsupported = true;
			Debug.LogWarning((object)("[GpuSkinBatch] unavailable on this device/API. " + $"instancing={SystemInfo.supportsInstancing}, compute={SystemInfo.supportsComputeShaders}, " + $"shaderLevel={SystemInfo.graphicsShaderLevel}, shaderFound={(Object)(object)gpuSkinShader != (Object)null}. " + "Using regular SkinnedMeshRenderer."));
		}
	}

	public static bool CanUseForUnit(bool isDefender, bool isBoss)
	{
		return featureEnabled && (!isDefender || allowDefenders) && (!isBoss || allowBosses);
	}

	internal static void Register(GpuSkinnedUnitRenderer proxy)
	{
		if (featureEnabled && !((Object)(object)proxy == (Object)null) && !ActiveProxies.Contains(proxy))
		{
			ActiveProxies.Add(proxy);
			EnsureInstance();
		}
	}

	internal static void Unregister(GpuSkinnedUnitRenderer proxy)
	{
		if ((Object)(object)proxy != (Object)null)
		{
			ActiveProxies.Remove(proxy);
		}
	}

	private static void EnsureInstance()
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected O, but got Unknown
		if (!((Object)(object)instance != (Object)null) && Application.isPlaying)
		{
			GameObject val = new GameObject("GpuSkinnedUnitBatchRenderer");
			((Object)val).hideFlags = (HideFlags)1;
			instance = val.AddComponent<GpuSkinnedUnitBatchRenderer>();
			Object.DontDestroyOnLoad((Object)(object)val);
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
		for (int num = ActiveProxies.Count - 1; num >= 0; num--)
		{
			GpuSkinnedUnitRenderer gpuSkinnedUnitRenderer = ActiveProxies[num];
			if ((Object)(object)gpuSkinnedUnitRenderer == (Object)null)
			{
				ActiveProxies.RemoveAt(num);
			}
			else if (((Behaviour)gpuSkinnedUnitRenderer).isActiveAndEnabled && ((Component)gpuSkinnedUnitRenderer).gameObject.activeInHierarchy)
			{
				IReadOnlyList<GpuSkinnedUnitRenderer.SourceRenderer> sourceRenderers = gpuSkinnedUnitRenderer.SourceRenderers;
				for (int i = 0; i < sourceRenderers.Count; i++)
				{
					GpuSkinnedUnitRenderer.SourceRenderer sourceRenderer = sourceRenderers[i];
					if (!TryBuildBatchKey(sourceRenderer, out var key))
					{
						sourceRenderer.SetBatched(value: false);
						continue;
					}
					if (!groups.TryGetValue(key, out var value))
					{
						value = new BatchGroup(key);
						groups.Add(key, value);
					}
					if (value.instances.Count == 0)
					{
						activeGroups.Add(value);
					}
					value.instances.Add(new BatchInstance(gpuSkinnedUnitRenderer, sourceRenderer));
					value.lastUsedFrame = Time.frameCount;
				}
			}
		}
		for (int j = 0; j < activeGroups.Count; j++)
		{
			BatchGroup batchGroup = activeGroups[j];
			bool flag = batchGroup.instances.Count >= minimumInstanceCount && batchGroup.Draw();
			for (int k = 0; k < batchGroup.instances.Count; k++)
			{
				batchGroup.instances[k].source.SetBatched(flag);
			}
			if (flag)
			{
				BatchedRendererCount += batchGroup.instances.Count;
				SubmittedDrawCount++;
				EstimatedSavedDrawCount += Mathf.Max(0, batchGroup.instances.Count - 1);
			}
			batchGroup.instances.Clear();
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
		if ((Object)(object)instance == (Object)(object)this)
		{
			instance = null;
		}
	}

	private static bool TryBuildBatchKey(GpuSkinnedUnitRenderer.SourceRenderer source, out BatchKey key)
	{
		key = default(BatchKey);
		SkinnedMeshRenderer renderer = source.renderer;
		if ((Object)(object)renderer == (Object)null || !source.originalEnabled || !((Component)renderer).gameObject.activeInHierarchy || ((Renderer)renderer).forceRenderingOff)
		{
			return false;
		}
		if (!source.RefreshGeometry())
		{
			return false;
		}
		Mesh mesh = source.mesh;
		if ((Object)(object)mesh == (Object)null || mesh.subMeshCount != 1 || mesh.blendShapeCount > 0 || !mesh.HasVertexAttribute((VertexAttribute)13) || !mesh.HasVertexAttribute((VertexAttribute)12) || (Object)(object)((Component)renderer).GetComponent<Cloth>() != (Object)null)
		{
			return false;
		}
		Transform[] bones = source.bones;
		Matrix4x4[] bindPoses = source.bindPoses;
		if (bones.Length != bindPoses.Length)
		{
			return false;
		}
		Material sharedMaterial = ((Renderer)renderer).sharedMaterial;
		if (!IsMaterialSupported(sharedMaterial))
		{
			return false;
		}
		key = new BatchKey(mesh, sharedMaterial, bones.Length, ((Component)renderer).gameObject.layer);
		return true;
	}

	private static bool IsMaterialSupported(Material material)
	{
		if ((Object)(object)material == (Object)null || (Object)(object)material.shader == (Object)null || material.renderQueue > 2500)
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
		for (int i = 0; i < ActiveProxies.Count; i++)
		{
			GpuSkinnedUnitRenderer gpuSkinnedUnitRenderer = ActiveProxies[i];
			if (!((Object)(object)gpuSkinnedUnitRenderer == (Object)null))
			{
				IReadOnlyList<GpuSkinnedUnitRenderer.SourceRenderer> sourceRenderers = gpuSkinnedUnitRenderer.SourceRenderers;
				for (int j = 0; j < sourceRenderers.Count; j++)
				{
					sourceRenderers[j].SetBatched(value: false);
				}
			}
		}
	}

	private void CleanupUnusedGroups()
	{
		staleGroupKeys.Clear();
		foreach (KeyValuePair<BatchKey, BatchGroup> group in groups)
		{
			if (Time.frameCount - group.Value.lastUsedFrame > 600)
			{
				staleGroupKeys.Add(group.Key);
			}
		}
		for (int i = 0; i < staleGroupKeys.Count; i++)
		{
			BatchKey key = staleGroupKeys[i];
			if (groups.TryGetValue(key, out var value))
			{
				value.Dispose();
				groups.Remove(key);
			}
		}
	}
}
