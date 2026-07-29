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
		}

		public void Initialize(Vector2 initialVelocity, float duration)
		{
			velocity = initialVelocity;
			lifetime = Mathf.Max(0.01f, duration);
			maxLifetime = lifetime;
			_ = TargetText;
			_ = CachedRectTransform;
		}

		private void Update()
		{
			lifetime -= Time.deltaTime;
			RectTransform rect = CachedRectTransform;
			if (rect != null)
			{
				rect.anchoredPosition += velocity * Time.deltaTime;
			}
			velocity *= 0.96f;
			if ((Object)(object)targetText != null)
			{
				Color color = ((Graphic)targetText).color;
				color.a = Mathf.Clamp01(lifetime / Mathf.Max(0.01f, maxLifetime));
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
