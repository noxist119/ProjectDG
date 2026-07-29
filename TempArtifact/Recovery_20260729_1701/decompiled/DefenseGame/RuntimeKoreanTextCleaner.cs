using UnityEngine;
using UnityEngine.UI;

namespace DefenseGame;

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
		Text[] componentsInChildren = ((Component)this).GetComponentsInChildren<Text>(true);
		foreach (Text val in componentsInChildren)
		{
			if (!((Object)(object)val == (Object)null) && !string.IsNullOrEmpty(val.text))
			{
				string text = RuntimeKoreanTextUtility.Clean(RuntimeKoreanTextUtility.BuildKey(val), val.text);
				if (text != val.text)
				{
					val.text = text;
				}
			}
		}
	}
}
