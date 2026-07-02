using UnityEngine;

namespace DefenseGame
{
    [RequireComponent(typeof(RectTransform))]
    public class RuntimeSafeAreaFitter : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            ApplySafeArea(true);
        }

        private void Update()
        {
            ApplySafeArea(false);
        }

        private void ApplySafeArea(bool force)
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            Rect safeArea = Screen.safeArea;
            Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
            if (!force && safeArea == lastSafeArea && screenSize == lastScreenSize)
            {
                return;
            }

            lastSafeArea = safeArea;
            lastScreenSize = screenSize;

            if (screenSize.x <= 0 || screenSize.y <= 0)
            {
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                return;
            }

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= screenSize.x;
            anchorMin.y /= screenSize.y;
            anchorMax.x /= screenSize.x;
            anchorMax.y /= screenSize.y;

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
