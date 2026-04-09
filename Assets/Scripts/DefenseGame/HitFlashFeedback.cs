using System.Collections;
using UnityEngine;

namespace DefenseGame
{
    public class HitFlashFeedback : MonoBehaviour
    {
        [SerializeField] private Renderer[] targetRenderers;
        [SerializeField] private float flashDuration = 0.12f;
        [SerializeField] private float flashStrength = 1f;
        [SerializeField] private float criticalFlashStrength = 1.35f;
        [SerializeField] private Color flashColor = Color.white;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int RimColorId = Shader.PropertyToID("_RimColor");
        private static readonly int FresnelColorId = Shader.PropertyToID("_FresnelColor");
        private static readonly int EdgeColorId = Shader.PropertyToID("_EdgeColor");
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");

        private MaterialPropertyBlock propertyBlock;
        private Coroutine flashRoutine;
        private bool materialsPrepared;

        private void Awake()
        {
            EnsurePropertyBlock();
            CacheRenderersIfNeeded();
            EnableEmissionOnMaterials();
        }

        public void Configure(Renderer[] renderers)
        {
            targetRenderers = renderers;
            materialsPrepared = false;
            CacheRenderersIfNeeded();
            EnableEmissionOnMaterials();
        }

        public void PlayHit(bool critical)
        {
            EnsurePropertyBlock();
            CacheRenderersIfNeeded();
            if (!materialsPrepared)
            {
                EnableEmissionOnMaterials();
            }

            if (targetRenderers == null || targetRenderers.Length == 0)
            {
                return;
            }

            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
            }

            flashRoutine = StartCoroutine(FlashRoutine(critical ? criticalFlashStrength : flashStrength));
        }

        private IEnumerator FlashRoutine(float intensity)
        {
            float elapsed = 0f;
            while (elapsed < flashDuration)
            {
                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / flashDuration);
                float pulse = 1f - normalized;
                ApplyFlash(pulse * intensity);
                yield return null;
            }

            ClearFlash();
            flashRoutine = null;
        }

        private void ApplyFlash(float intensity)
        {
            EnsurePropertyBlock();
            if (propertyBlock == null || targetRenderers == null)
            {
                return;
            }

            Color tintedFlash = flashColor * Mathf.Lerp(0.85f, 1.35f, Mathf.Clamp01(intensity));
            Color emission = tintedFlash * (1.25f * intensity);
            float outlineWidth = 0.75f * intensity;

            for (int i = 0; i < targetRenderers.Length; i++)
            {
                Renderer renderer = targetRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                propertyBlock.Clear();
                propertyBlock.SetColor(BaseColorId, Color.Lerp(Color.white, tintedFlash, Mathf.Clamp01(intensity)));
                propertyBlock.SetColor(ColorId, Color.Lerp(Color.white, tintedFlash, Mathf.Clamp01(intensity)));
                propertyBlock.SetColor(EmissionColorId, emission);
                propertyBlock.SetColor(RimColorId, emission);
                propertyBlock.SetColor(FresnelColorId, emission);
                propertyBlock.SetColor(EdgeColorId, tintedFlash);
                propertyBlock.SetColor(OutlineColorId, tintedFlash);
                propertyBlock.SetFloat(OutlineWidthId, outlineWidth);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void ClearFlash()
        {
            if (targetRenderers == null)
            {
                return;
            }

            for (int i = 0; i < targetRenderers.Length; i++)
            {
                Renderer renderer = targetRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                renderer.SetPropertyBlock(null);
            }
        }

        private void EnsurePropertyBlock()
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }
        }

        private void CacheRenderersIfNeeded()
        {
            if (targetRenderers == null || targetRenderers.Length == 0)
            {
                targetRenderers = GetComponentsInChildren<Renderer>(true);
            }
        }

        private void EnableEmissionOnMaterials()
        {
            if (targetRenderers == null || materialsPrepared)
            {
                return;
            }

            for (int i = 0; i < targetRenderers.Length; i++)
            {
                Renderer renderer = targetRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Material[] materials = renderer.materials;
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    Material material = materials[materialIndex];
                    if (material != null && material.HasProperty(EmissionColorId))
                    {
                        material.EnableKeyword("_EMISSION");
                    }
                }
            }

            materialsPrepared = true;
        }
    }
}
