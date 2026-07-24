using System;
using System.Collections.Generic;
using System.Linq;
using DefenseGame;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DefenseGameEditor
{
    [InitializeOnLoad]
    public static class AnimationEventMaterialCatalogSync
    {
        private const string DefaultConfigPath = "Assets/Data/DefenseGamePresentationConfig.asset";
        private static bool syncing;

        static AnimationEventMaterialCatalogSync()
        {
            EditorApplication.delayCall += () => SyncAll(false);
        }

        [MenuItem("DefenseGame/Validation/Sync Animation Event Materials")]
        public static void SyncFromMenu()
        {
            SyncAll(true);
        }

        public static string[] SyncAll(bool logSummary)
        {
            if (syncing)
            {
                return Array.Empty<string>();
            }

            syncing = true;
            try
            {
                Dictionary<string, HashSet<string>> required = CollectOverrideMaterialNames();
                Dictionary<string, List<Material>> available = CollectMaterials();
                List<string> unresolved = new List<string>();
                GamePresentationConfig config = AssetDatabase.LoadAssetAtPath<GamePresentationConfig>(DefaultConfigPath);
                if (config == null)
                {
                    unresolved.Add("missing_config:" + DefaultConfigPath);
                    return unresolved.ToArray();
                }

                List<Material> catalog = (config.animationEventMaterials ?? Array.Empty<Material>())
                    .Where(material => material != null)
                    .Distinct()
                    .ToList();

                foreach (KeyValuePair<string, HashSet<string>> entry in required.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
                {
                    if (!available.TryGetValue(entry.Key, out List<Material> candidates) || candidates.Count == 0)
                    {
                        unresolved.Add(entry.Key);
                        continue;
                    }

                    Material selected = SelectNearestMaterial(candidates, entry.Value);
                    if (selected != null && !catalog.Contains(selected))
                    {
                        catalog.Add(selected);
                    }
                }

                Material[] next = catalog.OrderBy(material => material.name, StringComparer.OrdinalIgnoreCase).ToArray();
                Material[] current = config.animationEventMaterials ?? Array.Empty<Material>();
                if (!current.SequenceEqual(next))
                {
                    Undo.RecordObject(config, "Sync Animation Event Materials");
                    config.animationEventMaterials = next;
                    EditorUtility.SetDirty(config);
                    AssetDatabase.SaveAssets();
                }

                if (logSummary)
                {
                    Debug.Log("[AnimationMaterialCatalog] " + next.Length + " material(s) registered. Unresolved: " +
                              (unresolved.Count == 0 ? "none" : string.Join(", ", unresolved)));
                }

                return unresolved.ToArray();
            }
            finally
            {
                syncing = false;
            }
        }

        private static Dictionary<string, HashSet<string>> CollectOverrideMaterialNames()
        {
            Dictionary<string, HashSet<string>> result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            string[] clipGuids = AssetDatabase.FindAssets("t:AnimationClip");
            HashSet<string> visitedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < clipGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(clipGuids[i]);
                if (string.IsNullOrEmpty(path) || !visitedPaths.Add(path))
                {
                    continue;
                }

                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                for (int assetIndex = 0; assetIndex < assets.Length; assetIndex++)
                {
                    if (!(assets[assetIndex] is AnimationClip clip))
                    {
                        continue;
                    }

                    AnimationEvent[] events;
                    try
                    {
                        events = AnimationUtility.GetAnimationEvents(clip);
                    }
                    catch
                    {
                        continue;
                    }

                    for (int eventIndex = 0; eventIndex < events.Length; eventIndex++)
                    {
                        AnimationEvent animationEvent = events[eventIndex];
                        if (animationEvent == null ||
                            !string.Equals(animationEvent.functionName, "OverrideMaterial", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        string key = AnimationEventMaterialRegistry.NormalizeName(animationEvent.stringParameter);
                        if (string.IsNullOrEmpty(key))
                        {
                            continue;
                        }

                        if (!result.TryGetValue(key, out HashSet<string> sourcePaths))
                        {
                            sourcePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            result.Add(key, sourcePaths);
                        }
                        sourcePaths.Add(path);
                    }
                }
            }

            return result;
        }

        private static Dictionary<string, List<Material>> CollectMaterials()
        {
            Dictionary<string, List<Material>> result = new Dictionary<string, List<Material>>(StringComparer.OrdinalIgnoreCase);
            string[] materialGuids = AssetDatabase.FindAssets("t:Material");
            HashSet<string> visitedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < materialGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(materialGuids[i]);
                if (string.IsNullOrEmpty(path) || !visitedPaths.Add(path))
                {
                    continue;
                }

                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                for (int assetIndex = 0; assetIndex < assets.Length; assetIndex++)
                {
                    if (!(assets[assetIndex] is Material material))
                    {
                        continue;
                    }

                    string key = AnimationEventMaterialRegistry.NormalizeName(material.name);
                    if (!result.TryGetValue(key, out List<Material> materials))
                    {
                        materials = new List<Material>();
                        result.Add(key, materials);
                    }
                    materials.Add(material);
                }
            }

            return result;
        }

        private static Material SelectNearestMaterial(List<Material> candidates, HashSet<string> sourcePaths)
        {
            return candidates
                .Where(candidate => candidate != null)
                .OrderByDescending(candidate => sourcePaths.Any(source => SameDirectory(source, AssetDatabase.GetAssetPath(candidate))))
                .ThenBy(candidate => AssetDatabase.GetAssetPath(candidate), StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
        }

        private static bool SameDirectory(string left, string right)
        {
            return string.Equals(System.IO.Path.GetDirectoryName(left), System.IO.Path.GetDirectoryName(right), StringComparison.OrdinalIgnoreCase);
        }
    }

    public sealed class AnimationEventMaterialBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            AnimationEventMaterialCatalogSync.SyncAll(true);
        }
    }
}
