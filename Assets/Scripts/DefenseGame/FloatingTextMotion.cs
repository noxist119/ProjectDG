using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseGame
{
	public class FloatingTextMotion : MonoBehaviour
	{
		private const int MaxPoolSize = 96;

		private static readonly Stack<FloatingTextMotion> Pool = new Stack<FloatingTextMotion>();

		private Vector2 velocity;

		private float lifetime;

		private float maxLifetime;

		private Text targetText;

        private float popInDuration;
        private float settleDuration;
        private float initialScale = 1f;
        private float peakScale = 1f;
        private Color initialColor = Color.white;

		private RectTransform cachedRectTransform;

		public Text TargetText
		{
			get
			{
				if ((Object)(object)targetText == null)
				{
					targetText = GetComponent<Text>();
				}
				return targetText;
			}
		}

		public RectTransform CachedRectTransform
		{
			get
			{
				if (cachedRectTransform == null)
				{
					cachedRectTransform = base.transform as RectTransform;
				}
				return cachedRectTransform;
			}
		}

		public static FloatingTextMotion Spawn(Transform parent, string objectName)
		{
			FloatingTextMotion motion = null;
			while (Pool.Count > 0 && motion == null)
			{
				motion = Pool.Pop();
			}
			if (motion == null)
			{
				GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
				motion = textObject.AddComponent<FloatingTextMotion>();
			}
			motion.gameObject.name = objectName;
			motion.transform.SetParent(parent, worldPositionStays: false);
			motion.gameObject.SetActive(value: true);
			return motion;
		}        public void Initialize(Vector2 initialVelocity, float duration)
        {
            velocity = initialVelocity;
            lifetime = Mathf.Max(0.01f, duration);
            maxLifetime = lifetime;
            popInDuration = 0f;
            settleDuration = 0f;
            initialScale = 1f;
            peakScale = 1f;
            initialColor = TargetText != null ? TargetText.color : Color.white;
            _ = CachedRectTransform;
        }

        public void InitializeDamage(Vector2 initialVelocity, float duration, float popIn, float settle, float startScale, float peak)
        {
            Initialize(initialVelocity, duration);
            popInDuration = Mathf.Max(0f, popIn);
            settleDuration = Mathf.Max(0f, settle);
            initialScale = Mathf.Max(0.01f, startScale);
            peakScale = Mathf.Max(initialScale, peak);
            CachedRectTransform.localScale = Vector3.one * initialScale;
        }        private void Update()
        {
            float deltaTime = Time.unscaledDeltaTime;
            lifetime -= deltaTime;
            RectTransform rect = CachedRectTransform;
            if (rect != null)
            {
                rect.anchoredPosition += velocity * deltaTime;
                float elapsed = maxLifetime - lifetime;
                if (popInDuration > 0f && elapsed < popInDuration)
                {
                    rect.localScale = Vector3.one * Mathf.Lerp(initialScale, peakScale, elapsed / popInDuration);
                }
                else if (settleDuration > 0f && elapsed < popInDuration + settleDuration)
                {
                    rect.localScale = Vector3.one * Mathf.Lerp(peakScale, 1f, (elapsed - popInDuration) / settleDuration);
                }
                else
                {
                    rect.localScale = Vector3.one;
                }
            }

            velocity *= 0.96f;
            if ((Object)(object)targetText != null)
            {
                Color color = initialColor;
                float fadeWindow = Mathf.Max(0.01f, maxLifetime * 0.48f);
                color.a *= Mathf.Clamp01(lifetime / fadeWindow);
                ((Graphic)targetText).color = color;
            }

            if (lifetime <= 0f)
            {
                Recycle();
            }
        }

        private void Recycle()
		{
			if (Pool.Count >= 96)
			{
				Object.Destroy(base.gameObject);
				return;
			}
			base.gameObject.SetActive(value: false);
			base.transform.SetParent(null, worldPositionStays: false);
			Pool.Push(this);
		}
	}
}
