using System;
using UnityEngine;

namespace DefenseGame
{
	public class RuntimeHitRim : MonoBehaviour
	{
		private LineRenderer line;

		private Transform target;

		private Vector3 targetLocalCenter;

		private Color baseColor;

		private float radius;

		private float height;

		private float duration;

		private float elapsed;

		private bool critical;

		public void Initialize(LineRenderer targetLine, Transform followTarget, Vector3 worldCenter, Color color, float rimRadius, float rimHeight, float lifetime, bool isCritical)
		{
			line = targetLine;
			target = followTarget;
			targetLocalCenter = ((target != null) ? target.InverseTransformPoint(worldCenter) : worldCenter);
			baseColor = Color.Lerp(color, Color.white, isCritical ? 0.32f : 0.24f);
			radius = Mathf.Max(0.1f, rimRadius);
			height = Mathf.Max(0.2f, rimHeight);
			duration = Mathf.Max(0.05f, lifetime);
			critical = isCritical;
			Draw(1f, 0f);
		}

		private void Update()
		{
			if (line == null || target == null)
			{
				UnityEngine.Object.Destroy(base.gameObject);
				return;
			}
			elapsed += Time.deltaTime;
			float t = Mathf.Clamp01(elapsed / duration);
			float alpha = 1f - t;
			float pulse = Mathf.Sin(t * MathF.PI) * (critical ? 0.24f : 0.16f);
			base.transform.position = target.TransformPoint(targetLocalCenter);
			Camera camera = Camera.main;
			if (camera != null)
			{
				Vector3 direction = base.transform.position - camera.transform.position;
				if (direction.sqrMagnitude > 0.001f)
				{
					base.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
				}
			}
			Draw(alpha, pulse);
			if (elapsed >= duration)
			{
				UnityEngine.Object.Destroy(base.gameObject);
			}
		}

		private void Draw(float alpha, float pulse)
		{
			if (!(line == null))
			{
				Color color = baseColor;
				color.a = Mathf.Clamp01(alpha) * (critical ? 0.96f : 0.82f);
				line.startColor = color;
				line.endColor = color;
				line.widthMultiplier = (critical ? 0.115f : 0.082f) * Mathf.Lerp(1.15f, 0.55f, 1f - Mathf.Clamp01(alpha));
				int count = line.positionCount;
				float currentRadius = radius * (1f + pulse);
				float verticalRadius = height * 0.5f * (1f + pulse * 0.55f);
				for (int i = 0; i < count; i++)
				{
					float angle = MathF.PI * 2f * (float)i / (float)count;
					line.SetPosition(i, new Vector3(Mathf.Cos(angle) * currentRadius, Mathf.Sin(angle) * verticalRadius, 0f));
				}
			}
		}
	}
}
