using UnityEngine;

namespace DefenseGame;

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
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		lifetime = Mathf.Max(0.1f, duration);
		startScale = ((Component)this).transform.localScale;
		endScale = startScale * Mathf.Max(1f, scaleMultiplier);
		renderers = ((Component)this).GetComponentsInChildren<Renderer>(true);
		startColor = color;
		for (int i = 0; i < renderers.Length; i++)
		{
			if ((Object)(object)renderers[i] != (Object)null && (Object)(object)renderers[i].material != (Object)null)
			{
				renderers[i].material.color = color;
			}
		}
	}

	private void Update()
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		elapsed += Time.deltaTime;
		float num = Mathf.Clamp01(elapsed / lifetime);
		((Component)this).transform.localScale = Vector3.Lerp(startScale, endScale, EaseOut(num));
		float a = 1f - num;
		if (renderers != null)
		{
			for (int i = 0; i < renderers.Length; i++)
			{
				if (!((Object)(object)renderers[i] == (Object)null) && !((Object)(object)renderers[i].material == (Object)null))
				{
					Color color = startColor;
					color.a = a;
					renderers[i].material.color = color;
				}
			}
		}
		if (elapsed >= lifetime)
		{
			Object.Destroy((Object)(object)((Component)this).gameObject);
		}
	}

	private float EaseOut(float t)
	{
		return 1f - (1f - t) * (1f - t);
	}
}
