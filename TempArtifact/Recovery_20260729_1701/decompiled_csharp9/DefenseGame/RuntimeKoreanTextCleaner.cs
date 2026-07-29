using UnityEngine;
using UnityEngine.UI;

namespace DefenseGame
{
	public class RuntimeKoreanTextCleaner : MonoBehaviour
	{
		[SerializeField]
		private float refreshInterval = 0.35f;

		private float nextRefreshTime;

		private void OnEnable()
		{
			CleanAllTexts();
			nextRefreshTime = Time.unscaledTime + refreshInterval;
		}

		private void LateUpdate()
		{
			if (!(Time.unscaledTime < nextRefreshTime))
			{
				nextRefreshTime = Time.unscaledTime + refreshInterval;
				CleanAllTexts();
			}
		}

		private void CleanAllTexts()
		{
			Text[] texts = GetComponentsInChildren<Text>(includeInactive: true);
			foreach (Text text in texts)
			{
				if (!((Object)(object)text == null) && !string.IsNullOrEmpty(text.text))
				{
					string cleaned = RuntimeKoreanTextUtility.Clean(RuntimeKoreanTextUtility.BuildKey(text), text.text);
					if (cleaned != text.text)
					{
						text.text = cleaned;
					}
				}
			}
		}
	}
}
