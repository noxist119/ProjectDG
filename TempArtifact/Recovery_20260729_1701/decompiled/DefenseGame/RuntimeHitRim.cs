using System;
using UnityEngine;

namespace DefenseGame;

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
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		line = targetLine;
		target = followTarget;
		targetLocalCenter = (((Object)(object)target != (Object)null) ? target.InverseTransformPoint(worldCenter) : worldCenter);
		baseColor = Color.Lerp(color, Color.white, isCritical ? 0.32f : 0.24f);
		radius = Mathf.Max(0.1f, rimRadius);
		height = Mathf.Max(0.2f, rimHeight);
		duration = Mathf.Max(0.05f, lifetime);
		critical = isCritical;
		Draw(1f, 0f);
	}

	private void Update()
	{
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)line == (Object)null || (Object)(object)target == (Object)null)
		{
			Object.Destroy((Object)(object)((Component)this).gameObject);
			return;
		}
		elapsed += Time.deltaTime;
		float num = Mathf.Clamp01(elapsed / duration);
		float alpha = 1f - num;
		float pulse = Mathf.Sin(num * MathF.PI) * (critical ? 0.24f : 0.16f);
		((Component)this).transform.position = target.TransformPoint(targetLocalCenter);
		Camera main = Camera.main;
		if ((Object)(object)main != (Object)null)
		{
			Vector3 val = ((Component)this).transform.position - ((Component)main).transform.position;
			if (((Vector3)(ref val)).sqrMagnitude > 0.001f)
			{
				((Component)this).transform.rotation = Quaternion.LookRotation(((Vector3)(ref val)).normalized, Vector3.up);
			}
		}
		Draw(alpha, pulse);
		if (elapsed >= duration)
		{
			Object.Destroy((Object)(object)((Component)this).gameObject);
		}
	}

	private void Draw(float alpha, float pulse)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)line == (Object)null))
		{
			Color val = baseColor;
			val.a = Mathf.Clamp01(alpha) * (critical ? 0.96f : 0.82f);
			line.startColor = val;
			line.endColor = val;
			line.widthMultiplier = (critical ? 0.115f : 0.082f) * Mathf.Lerp(1.15f, 0.55f, 1f - Mathf.Clamp01(alpha));
			int positionCount = line.positionCount;
			float num = radius * (1f + pulse);
			float num2 = height * 0.5f * (1f + pulse * 0.55f);
			for (int i = 0; i < positionCount; i++)
			{
				float num3 = MathF.PI * 2f * (float)i / (float)positionCount;
				line.SetPosition(i, new Vector3(Mathf.Cos(num3) * num, Mathf.Sin(num3) * num2, 0f));
			}
		}
	}
}
