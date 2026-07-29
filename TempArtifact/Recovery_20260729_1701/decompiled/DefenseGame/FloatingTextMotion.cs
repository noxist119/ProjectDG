using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseGame;

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
			if ((Object)(object)targetText == (Object)null)
			{
				targetText = ((Component)this).GetComponent<Text>();
			}
			return targetText;
		}
	}

	public RectTransform CachedRectTransform
	{
		get
		{
			if ((Object)(object)cachedRectTransform == (Object)null)
			{
				Transform transform = ((Component)this).transform;
				cachedRectTransform = (RectTransform)(object)((transform is RectTransform) ? transform : null);
			}
			return cachedRectTransform;
		}
	}

	public static FloatingTextMotion Spawn(Transform parent, string objectName)
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Expected O, but got Unknown
		FloatingTextMotion floatingTextMotion = null;
		while (Pool.Count > 0 && (Object)(object)floatingTextMotion == (Object)null)
		{
			floatingTextMotion = Pool.Pop();
		}
		if ((Object)(object)floatingTextMotion == (Object)null)
		{
			GameObject val = new GameObject(objectName, new Type[2]
			{
				typeof(RectTransform),
				typeof(Text)
			});
			floatingTextMotion = val.AddComponent<FloatingTextMotion>();
		}
		((Object)((Component)floatingTextMotion).gameObject).name = objectName;
		((Component)floatingTextMotion).transform.SetParent(parent, false);
		((Component)floatingTextMotion).gameObject.SetActive(true);
		return floatingTextMotion;
	}

	public void Initialize(Vector2 initialVelocity, float duration)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		velocity = initialVelocity;
		lifetime = Mathf.Max(0.01f, duration);
		maxLifetime = lifetime;
		_ = TargetText;
		_ = CachedRectTransform;
	}

	private void Update()
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		lifetime -= Time.deltaTime;
		RectTransform val = CachedRectTransform;
		if ((Object)(object)val != (Object)null)
		{
			val.anchoredPosition += velocity * Time.deltaTime;
		}
		velocity *= 0.96f;
		if ((Object)(object)targetText != (Object)null)
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
			Object.Destroy((Object)(object)((Component)this).gameObject);
			return;
		}
		((Component)this).gameObject.SetActive(false);
		((Component)this).transform.SetParent((Transform)null, false);
		Pool.Push(this);
	}
}
