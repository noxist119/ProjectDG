using UnityEngine;
using UnityEngine.UI;

namespace DefenseGame
{
    public class RuntimeKoreanTextCleaner : MonoBehaviour
    {
        [SerializeField] private float refreshInterval = 0.35f;

        private float nextRefreshTime;

        private void OnEnable()
        {
            CleanAllTexts();
            nextRefreshTime = Time.unscaledTime + refreshInterval;
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime < nextRefreshTime)
            {
                return;
            }

            nextRefreshTime = Time.unscaledTime + refreshInterval;
            CleanAllTexts();
        }

        private void CleanAllTexts()
        {
            Text[] texts = GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                Text text = texts[i];
                if (text == null || string.IsNullOrEmpty(text.text))
                {
                    continue;
                }

                string cleaned = RuntimeKoreanTextUtility.Clean(RuntimeKoreanTextUtility.BuildKey(text), text.text);
                if (cleaned != text.text)
                {
                    text.text = cleaned;
                }
            }
        }
    }
}
