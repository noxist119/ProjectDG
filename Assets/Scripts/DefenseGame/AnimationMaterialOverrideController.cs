using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefenseGame
{
    public static class AnimationEventMaterialRegistry
    {
        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);

        public static void Configure(IEnumerable<Material> materials)
        {
            Materials.Clear();
            if (materials == null)
            {
                return;
            }

            foreach (Material material in materials)
            {
                Register(material);
            }
        }

        public static Material Resolve(string materialName)
        {
            string key = NormalizeName(materialName);
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            if (Materials.TryGetValue(key, out Material configured) && configured != null)
            {
                return configured;
            }

            Material[] loaded = Resources.FindObjectsOfTypeAll<Material>();
            for (int i = 0; i < loaded.Length; i++)
            {
                Material candidate = loaded[i];
                if (candidate != null && string.Equals(NormalizeName(candidate.name), key, StringComparison.OrdinalIgnoreCase))
                {
                    Register(candidate);
                    return candidate;
                }
            }

#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Material");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                UnityEngine.Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
                for (int assetIndex = 0; assetIndex < assets.Length; assetIndex++)
                {
                    if (assets[assetIndex] is Material candidate &&
                        string.Equals(NormalizeName(candidate.name), key, StringComparison.OrdinalIgnoreCase))
                    {
                        Register(candidate);
                        return candidate;
                    }
                }
            }
#endif

            return null;
        }

        public static Material[] GetConfiguredMaterials()
        {
            List<Material> result = new List<Material>();
            foreach (Material material in Materials.Values)
            {
                if (material != null && !result.Contains(material))
                {
                    result.Add(material);
                }
            }

            return result.ToArray();
        }

        public static string NormalizeName(string materialName)
        {
            if (string.IsNullOrWhiteSpace(materialName))
            {
                return string.Empty;
            }

            string normalized = materialName.Trim();
            if (normalized.EndsWith(".mat", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(0, normalized.Length - 4).TrimEnd();
            }

            const string instanceSuffix = " (Instance)";
            if (normalized.EndsWith(instanceSuffix, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(0, normalized.Length - instanceSuffix.Length).TrimEnd();
            }

            return normalized;
        }

        private static void Register(Material material)
        {
            if (material == null)
            {
                return;
            }

            string key = NormalizeName(material.name);
            if (!string.IsNullOrEmpty(key))
            {
                Materials[key] = material;
            }
        }
    }

    public sealed class AnimationMaterialOverrideController : MonoBehaviour
    {
        private sealed class RendererSnapshot
        {
            public Renderer renderer;
            public Material[] materials;
        }

        private sealed class OverrideFrame
        {
            public string key;
            public readonly List<RendererSnapshot> snapshots = new List<RendererSnapshot>();
        }

        private readonly List<Renderer> targetRenderers = new List<Renderer>();
        private readonly List<OverrideFrame> overrideFrames = new List<OverrideFrame>();
        private readonly HashSet<string> reportedMissingMaterials = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> reportedResetMismatches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private void Awake()
        {
            RefreshTargetRenderers();
        }

        private void OnDisable()
        {
            RestoreAll();
        }

        public bool OverrideMaterial(string materialName)
        {
            string key = AnimationEventMaterialRegistry.NormalizeName(materialName);
            Material targetMaterial = AnimationEventMaterialRegistry.Resolve(key);
            if (targetMaterial == null)
            {
                ReportMissingMaterial(key);
                return false;
            }

            RefreshTargetRenderers();
            if (targetRenderers.Count == 0)
            {
                return false;
            }

            if (overrideFrames.Count > 0 &&
                string.Equals(overrideFrames[overrideFrames.Count - 1].key, key, StringComparison.OrdinalIgnoreCase))
            {
                ApplyMaterial(targetMaterial);
                return true;
            }

            OverrideFrame frame = new OverrideFrame { key = key };
            for (int i = 0; i < targetRenderers.Count; i++)
            {
                Renderer renderer = targetRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                frame.snapshots.Add(new RendererSnapshot
                {
                    renderer = renderer,
                    materials = renderer.sharedMaterials
                });
            }

            if (frame.snapshots.Count == 0)
            {
                return false;
            }

            overrideFrames.Add(frame);
            ApplyMaterial(targetMaterial);
            return true;
        }

        public bool ResetMaterial(string materialName)
        {
            if (overrideFrames.Count == 0)
            {
                return false;
            }

            string key = AnimationEventMaterialRegistry.NormalizeName(materialName);
            int frameIndex = FindFrameIndex(key);
            if (frameIndex < 0)
            {
                string mismatchKey = string.IsNullOrEmpty(key) ? "<empty>" : key;
                if (reportedResetMismatches.Add(mismatchKey))
                {
                    Debug.LogWarning("[AnimationMaterial] ResetMaterial key '" + mismatchKey + "' did not match the active override on " + name + ". Restoring the latest material snapshot.", this);
                }
                frameIndex = overrideFrames.Count - 1;
            }

            for (int i = overrideFrames.Count - 1; i >= frameIndex; i--)
            {
                RestoreFrame(overrideFrames[i]);
                overrideFrames.RemoveAt(i);
            }

            return true;
        }

        public void RestoreAll()
        {
            for (int i = overrideFrames.Count - 1; i >= 0; i--)
            {
                RestoreFrame(overrideFrames[i]);
            }

            overrideFrames.Clear();
        }

        private int FindFrameIndex(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return overrideFrames.Count - 1;
            }

            for (int i = overrideFrames.Count - 1; i >= 0; i--)
            {
                if (string.Equals(overrideFrames[i].key, key, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private void RefreshTargetRenderers()
        {
            for (int i = targetRenderers.Count - 1; i >= 0; i--)
            {
                if (targetRenderers[i] == null)
                {
                    targetRenderers.RemoveAt(i);
                }
            }

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null ||
                    !(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer) ||
                    targetRenderers.Contains(renderer))
                {
                    continue;
                }

                targetRenderers.Add(renderer);
            }
        }

        private void ApplyMaterial(Material targetMaterial)
        {
            for (int i = 0; i < targetRenderers.Count; i++)
            {
                Renderer renderer = targetRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Material[] current = renderer.sharedMaterials;
                int slotCount = Mathf.Max(1, current.Length);
                Material[] replacement = new Material[slotCount];
                for (int slot = 0; slot < replacement.Length; slot++)
                {
                    replacement[slot] = targetMaterial;
                }

                renderer.sharedMaterials = replacement;
                RuntimeRenderBatchingUtility.PrepareRenderer(renderer);
            }
        }

        private static void RestoreFrame(OverrideFrame frame)
        {
            if (frame == null)
            {
                return;
            }

            for (int i = 0; i < frame.snapshots.Count; i++)
            {
                RendererSnapshot snapshot = frame.snapshots[i];
                if (snapshot?.renderer == null)
                {
                    continue;
                }

                snapshot.renderer.sharedMaterials = snapshot.materials ?? Array.Empty<Material>();
                RuntimeRenderBatchingUtility.PrepareRenderer(snapshot.renderer);
            }
        }

        private void ReportMissingMaterial(string key)
        {
            string reportedKey = string.IsNullOrEmpty(key) ? "<empty>" : key;
            if (reportedMissingMaterials.Add(reportedKey))
            {
                Debug.LogWarning("[AnimationMaterial] OverrideMaterial could not resolve material '" + reportedKey + "' on " + name + ". Add a Material asset with the same name; the catalog sync will include it in builds.", this);
            }
        }
    }
}
