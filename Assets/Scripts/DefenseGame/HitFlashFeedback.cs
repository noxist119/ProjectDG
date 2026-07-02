using System.Collections;
using System.Collections.Generic;
using System.Text;
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
        private static readonly int RimPowerId = Shader.PropertyToID("_RimPower");
        private static readonly int RimIntensityId = Shader.PropertyToID("_RimIntensity");
        private static readonly int FresnelColorId = Shader.PropertyToID("_FresnelColor");
        private static readonly int FresnelPowerId = Shader.PropertyToID("_FresnelPower");
        private static readonly int FresnelIntensityId = Shader.PropertyToID("_FresnelIntensity");
        private static readonly int EdgeColorId = Shader.PropertyToID("_EdgeColor");
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");

        private const float RimReplayInterval = 0.07f;

        private MaterialPropertyBlock propertyBlock;
        private Coroutine flashRoutine;
        private bool materialsPrepared;
        private Color baseColor = Color.white;
        private Color feedbackColor = Color.white;
        private bool hasBaseColor;
        private bool hasFeedbackColor;
        private float nextRimTime;

        private void Awake()
        {
            EnsurePropertyBlock();
            CacheRenderersIfNeeded();
            EnableEmissionOnMaterials();
        }

        public void Configure(Renderer[] renderers)
        {
            Configure(renderers, Color.white, false);
        }

        public void Configure(Renderer[] renderers, Color newBaseColor)
        {
            Configure(renderers, newBaseColor, true);
        }

        public void Configure(Renderer[] renderers, Color newFeedbackColor, bool useBaseColor)
        {
            targetRenderers = renderers;
            baseColor = newFeedbackColor;
            feedbackColor = newFeedbackColor;
            hasBaseColor = useBaseColor;
            hasFeedbackColor = true;
            materialsPrepared = false;
            CacheRenderersIfNeeded();
            EnableEmissionOnMaterials();
            RestoreBaseTint();
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

            PlayVisibleRim(critical);
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

            RestoreBaseTint();
            flashRoutine = null;
        }

        private void ApplyFlash(float intensity)
        {
            EnsurePropertyBlock();
            if (propertyBlock == null || targetRenderers == null)
            {
                return;
            }

            Color sourceColor = hasBaseColor ? baseColor : Color.white;
            Color signalColor = ResolveFeedbackColor(false);
            Color tintedFlash = Color.Lerp(signalColor, flashColor, 0.48f) * Mathf.Lerp(0.95f, 1.55f, Mathf.Clamp01(intensity));
            Color emission = tintedFlash * (1.8f * intensity);
            float outlineWidth = 1.15f * intensity;
            float rimPower = Mathf.Lerp(3.4f, 1.45f, Mathf.Clamp01(intensity));
            float rimIntensity = 2.1f * intensity;

            for (int i = 0; i < targetRenderers.Length; i++)
            {
                Renderer renderer = targetRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                propertyBlock.Clear();
                propertyBlock.SetColor(BaseColorId, Color.Lerp(sourceColor, tintedFlash, Mathf.Clamp01(intensity)));
                propertyBlock.SetColor(ColorId, Color.Lerp(sourceColor, tintedFlash, Mathf.Clamp01(intensity)));
                propertyBlock.SetColor(EmissionColorId, emission);
                propertyBlock.SetColor(RimColorId, emission);
                propertyBlock.SetFloat(RimPowerId, rimPower);
                propertyBlock.SetFloat(RimIntensityId, rimIntensity);
                propertyBlock.SetColor(FresnelColorId, emission);
                propertyBlock.SetFloat(FresnelPowerId, rimPower);
                propertyBlock.SetFloat(FresnelIntensityId, rimIntensity);
                propertyBlock.SetColor(EdgeColorId, tintedFlash);
                propertyBlock.SetColor(OutlineColorId, tintedFlash);
                propertyBlock.SetFloat(OutlineWidthId, outlineWidth);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void PlayVisibleRim(bool critical)
        {
            if (!Application.isPlaying || Time.time < nextRimTime)
            {
                return;
            }

            nextRimTime = Time.time + RimReplayInterval;
            RuntimeCombatFeedback.ShowHitRim(transform, ResolveFeedbackColor(critical), critical);
        }

        private Color ResolveFeedbackColor(bool critical)
        {
            Color sourceColor = hasFeedbackColor ? feedbackColor : flashColor;
            Color brightColor = critical
                ? new Color(1f, 0.48f, 0.18f, 1f)
                : new Color(0.66f, 0.96f, 1f, 1f);
            return Color.Lerp(sourceColor, brightColor, critical ? 0.55f : 0.42f);
        }

        private void RestoreBaseTint()
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

                if (!hasBaseColor)
                {
                    renderer.SetPropertyBlock(null);
                    continue;
                }

                EnsurePropertyBlock();
                propertyBlock.Clear();
                propertyBlock.SetColor(BaseColorId, baseColor);
                propertyBlock.SetColor(ColorId, baseColor);
                renderer.SetPropertyBlock(propertyBlock);
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

                Material[] materials = renderer.sharedMaterials;
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    Material material = materials[materialIndex];
                    if (material == null)
                    {
                        continue;
                    }

                    if (RuntimeRenderBatchingUtility.EnableGpuInstancing)
                    {
                        material.enableInstancing = true;
                    }
                    if (material.HasProperty(EmissionColorId))
                    {
                        material.EnableKeyword("_EMISSION");
                    }
                }
            }

            materialsPrepared = true;
        }
    }

    public static class RuntimeRenderBatchingUtility
    {
        private static readonly Dictionary<string, Material> MaterialCache = new Dictionary<string, Material>();
        private static readonly string[] TextureProperties = { "_BaseMap", "_MainTex", "_BumpMap", "_NormalMap", "_MaskMap", "_MetallicGlossMap", "_EmissionMap" };
        private static readonly string[] ColorProperties = { "_BaseColor", "_Color", "_EmissionColor" };
        private static readonly string[] FloatProperties = { "_Metallic", "_Smoothness", "_Glossiness", "_AlphaClip", "_Surface", "_Cull", "_ZWrite" };

        public static bool UsePerInstanceUnitTint { get; private set; }
        public static bool EnableGpuInstancing { get; private set; }
        public static bool ForceRuntimeUnitCastShadowsOff { get; private set; } = true;

        public static void Configure(GamePresentationConfig config)
        {
            UsePerInstanceUnitTint = config != null && config.usePerInstanceUnitTint;
            EnableGpuInstancing = config != null && config.enableRuntimeGpuInstancing;
            ForceRuntimeUnitCastShadowsOff = config == null || config.forceRuntimeUnitCastShadowsOff;
            UnityEngine.Rendering.GraphicsSettings.useScriptableRenderPipelineBatching = true;
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
                if (MaterialCache.TryGetValue(key, out Material cached) && cached != null)
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
            builder.Append(shader != null ? shader.GetInstanceID() : 0);
            builder.Append('|').Append(material.renderQueue);

            for (int i = 0; i < TextureProperties.Length; i++)
            {
                AppendTextureKey(builder, material, TextureProperties[i]);
            }

            for (int i = 0; i < ColorProperties.Length; i++)
            {
                AppendColorKey(builder, material, ColorProperties[i]);
            }

            for (int i = 0; i < FloatProperties.Length; i++)
            {
                AppendFloatKey(builder, material, FloatProperties[i]);
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
            builder.Append(texture != null ? texture.GetInstanceID() : 0);
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
            builder.Append(Mathf.RoundToInt(color.r * 255f)).Append(',')
                .Append(Mathf.RoundToInt(color.g * 255f)).Append(',')
                .Append(Mathf.RoundToInt(color.b * 255f)).Append(',')
                .Append(Mathf.RoundToInt(color.a * 255f));
        }

        private static void AppendFloatKey(StringBuilder builder, Material material, string propertyName)
        {
            builder.Append('|').Append(propertyName).Append(':');
            if (!material.HasProperty(propertyName))
            {
                builder.Append('x');
                return;
            }

            builder.Append(Mathf.RoundToInt(material.GetFloat(propertyName) * 1000f));
        }
    }

    public class RuntimeCameraShake : MonoBehaviour
    {
        private Coroutine shakeRoutine;
        private Vector3 baseLocalPosition;

        public static void Request(float intensity, float duration)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            RuntimeCameraShake shaker = camera.GetComponent<RuntimeCameraShake>();
            if (shaker == null)
            {
                shaker = camera.gameObject.AddComponent<RuntimeCameraShake>();
            }

            shaker.Shake(intensity, duration);
        }

        private void Awake()
        {
            baseLocalPosition = transform.localPosition;
        }

        private void Shake(float intensity, float duration)
        {
            if (shakeRoutine != null)
            {
                StopCoroutine(shakeRoutine);
                transform.localPosition = baseLocalPosition;
            }

            baseLocalPosition = transform.localPosition;
            shakeRoutine = StartCoroutine(ShakeRoutine(Mathf.Max(0f, intensity), Mathf.Max(0.05f, duration)));
        }

        private IEnumerator ShakeRoutine(float intensity, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float fade = 1f - Mathf.Clamp01(elapsed / duration);
                Vector2 offset = Random.insideUnitCircle * intensity * fade;
                transform.localPosition = baseLocalPosition + new Vector3(offset.x, offset.y, 0f);
                yield return null;
            }

            transform.localPosition = baseLocalPosition;
            shakeRoutine = null;
        }
    }

    public class RuntimePulseEffect : MonoBehaviour
    {
        private float lifetime = 0.9f;
        private float elapsed;
        private Vector3 startScale = Vector3.one;
        private Vector3 endScale = Vector3.one * 2f;
        private Renderer[] renderers;
        private Color startColor = Color.white;

        public void Configure(Color color, float duration, float scaleMultiplier)
        {
            lifetime = Mathf.Max(0.1f, duration);
            startScale = transform.localScale;
            endScale = startScale * Mathf.Max(1f, scaleMultiplier);
            renderers = GetComponentsInChildren<Renderer>(true);
            startColor = color;

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].material != null)
                {
                    renderers[i].material.color = color;
                }
            }
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / lifetime);
            transform.localScale = Vector3.Lerp(startScale, endScale, EaseOut(t));

            float alpha = 1f - t;
            if (renderers != null)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] == null || renderers[i].material == null)
                    {
                        continue;
                    }

                    Color color = startColor;
                    color.a = alpha;
                    renderers[i].material.color = color;
                }
            }

            if (elapsed >= lifetime)
            {
                Destroy(gameObject);
            }
        }

        private float EaseOut(float t)
        {
            return 1f - (1f - t) * (1f - t);
        }
    }
}
