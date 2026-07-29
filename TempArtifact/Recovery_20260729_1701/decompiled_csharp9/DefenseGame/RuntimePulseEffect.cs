using UnityEngine;

namespace DefenseGame
{
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
			startScale = base.transform.localScale;
			endScale = startScale * Mathf.Max(1f, scaleMultiplier);
			renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
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
			base.transform.localScale = Vector3.Lerp(startScale, endScale, EaseOut(t));
			float alpha = 1f - t;
			if (renderers != null)
			{
				for (int i = 0; i < renderers.Length; i++)
				{
					if (!(renderers[i] == null) && !(renderers[i].material == null))
					{
						Color color = startColor;
						color.a = alpha;
						renderers[i].material.color = color;
					}
				}
			}
			if (elapsed >= lifetime)
			{
				Object.Destroy(base.gameObject);
			}
		}

		private float EaseOut(float t)
		{
			return 1f - (1f - t) * (1f - t);
		}
	}
}
