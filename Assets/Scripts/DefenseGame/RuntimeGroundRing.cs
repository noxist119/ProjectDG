using System;
using UnityEngine;

namespace DefenseGame
{
	public class RuntimeGroundRing : MonoBehaviour
	{
		private LineRenderer line;

		private Color baseColor;

		private float radius;

		private float duration;

		private float elapsed;

		private bool expand;

		public void Initialize(LineRenderer targetLine, Color color, float startRadius, float lifetime, bool shouldExpand)
		{
			line = targetLine;
			baseColor = color;
			radius = Mathf.Max(0.05f, startRadius);
			duration = Mathf.Max(0.05f, lifetime);
			expand = shouldExpand;
			Draw(radius, 1f);
		}

		private void Update()
		{
			elapsed += Time.deltaTime;
			float t = Mathf.Clamp01(elapsed / duration);
			float eased = 1f - Mathf.Pow(1f - t, 2f);
			float alpha = 1f - t;
			float currentRadius = (expand ? Mathf.Lerp(radius * 0.72f, radius * 1.18f, eased) : (radius * (1f + Mathf.Sin(Time.time * 10f) * 0.035f)));
			Draw(currentRadius, alpha);
			if (elapsed >= duration)
			{
				UnityEngine.Object.Destroy(base.gameObject);
			}
		}

		private void Draw(float currentRadius, float alpha)
		{
			if (!(line == null))
			{
				Color color = baseColor;
				color.a *= Mathf.Clamp01(alpha);
				line.startColor = color;
				line.endColor = color;
				int count = line.positionCount;
				for (int i = 0; i < count; i++)
				{
					float angle = MathF.PI * 2f * (float)i / (float)count;
					line.SetPosition(i, new Vector3(Mathf.Cos(angle) * currentRadius, 0f, Mathf.Sin(angle) * currentRadius));
				}
			}
		}
	}
}
