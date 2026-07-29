using System.Collections;
using UnityEngine;

namespace DefenseGame
{
	public class RuntimeCameraShake : MonoBehaviour
	{
		private Coroutine shakeRoutine;

		private Vector3 baseLocalPosition;

		public static void Request(float intensity, float duration)
		{
			Camera camera = Camera.main;
			if (!(camera == null))
			{
				RuntimeCameraShake shaker = camera.GetComponent<RuntimeCameraShake>();
				if (shaker == null)
				{
					shaker = camera.gameObject.AddComponent<RuntimeCameraShake>();
				}
				shaker.Shake(intensity, duration);
			}
		}

		private void Awake()
		{
			baseLocalPosition = base.transform.localPosition;
		}

		private void Shake(float intensity, float duration)
		{
			if (shakeRoutine != null)
			{
				StopCoroutine(shakeRoutine);
				base.transform.localPosition = baseLocalPosition;
			}
			baseLocalPosition = base.transform.localPosition;
			shakeRoutine = StartCoroutine(ShakeRoutine(Mathf.Max(0f, intensity), Mathf.Max(0.05f, duration)));
		}

		private IEnumerator ShakeRoutine(float intensity, float duration)
		{
			float elapsed = 0f;
			while (elapsed < duration)
			{
				elapsed += Time.unscaledDeltaTime;
				float fade = 1f - Mathf.Clamp01(elapsed / duration);
				Vector2 offset = Random.insideUnitCircle * intensity * fade;
				base.transform.localPosition = baseLocalPosition + new Vector3(offset.x, offset.y, 0f);
				yield return null;
			}
			base.transform.localPosition = baseLocalPosition;
			shakeRoutine = null;
		}
	}
}
