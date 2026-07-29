using System;
using UnityEngine;

namespace DefenseGame;

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
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
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
		float num = Mathf.Clamp01(elapsed / duration);
		float num2 = 1f - Mathf.Pow(1f - num, 2f);
		float alpha = 1f - num;
		float currentRadius = (expand ? Mathf.Lerp(radius * 0.72f, radius * 1.18f, num2) : (radius * (1f + Mathf.Sin(Time.time * 10f) * 0.035f)));
		Draw(currentRadius, alpha);
		if (elapsed >= duration)
		{
			Object.Destroy((Object)(object)((Component)this).gameObject);
		}
	}

	private void Draw(float currentRadius, float alpha)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)line == (Object)null))
		{
			Color val = baseColor;
			val.a *= Mathf.Clamp01(alpha);
			line.startColor = val;
			line.endColor = val;
			int positionCount = line.positionCount;
			for (int i = 0; i < positionCount; i++)
			{
				float num = MathF.PI * 2f * (float)i / (float)positionCount;
				line.SetPosition(i, new Vector3(Mathf.Cos(num) * currentRadius, 0f, Mathf.Sin(num) * currentRadius));
			}
		}
	}
}
