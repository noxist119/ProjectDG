using System.Collections;
using UnityEngine;

namespace DefenseGame;

public class RuntimeCameraShake : MonoBehaviour
{
	private Coroutine shakeRoutine;

	private Vector3 baseLocalPosition;

	public static void Request(float intensity, float duration)
	{
		Camera main = Camera.main;
		if (!((Object)(object)main == (Object)null))
		{
			RuntimeCameraShake runtimeCameraShake = ((Component)main).GetComponent<RuntimeCameraShake>();
			if ((Object)(object)runtimeCameraShake == (Object)null)
			{
				runtimeCameraShake = ((Component)main).gameObject.AddComponent<RuntimeCameraShake>();
			}
			runtimeCameraShake.Shake(intensity, duration);
		}
	}

	private void Awake()
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		baseLocalPosition = ((Component)this).transform.localPosition;
	}

	private void Shake(float intensity, float duration)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		if (shakeRoutine != null)
		{
			((MonoBehaviour)this).StopCoroutine(shakeRoutine);
			((Component)this).transform.localPosition = baseLocalPosition;
		}
		baseLocalPosition = ((Component)this).transform.localPosition;
		shakeRoutine = ((MonoBehaviour)this).StartCoroutine(ShakeRoutine(Mathf.Max(0f, intensity), Mathf.Max(0.05f, duration)));
	}

	private IEnumerator ShakeRoutine(float intensity, float duration)
	{
		float elapsed = 0f;
		while (elapsed < duration)
		{
			elapsed += Time.unscaledDeltaTime;
			float fade = 1f - Mathf.Clamp01(elapsed / duration);
			Vector2 offset = Random.insideUnitCircle * intensity * fade;
			((Component)this).transform.localPosition = baseLocalPosition + new Vector3(offset.x, offset.y, 0f);
			yield return null;
		}
		((Component)this).transform.localPosition = baseLocalPosition;
		shakeRoutine = null;
	}
}
