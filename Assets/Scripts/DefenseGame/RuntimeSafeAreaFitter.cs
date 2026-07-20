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

            CalculateSafeAreaAnchors(safeArea, screenSize, out Vector2 anchorMin, out Vector2 anchorMax);

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        public static void CalculateSafeAreaAnchors(Rect safeArea, Vector2Int screenSize, out Vector2 anchorMin, out Vector2 anchorMax)
        {
            if (screenSize.x <= 0 || screenSize.y <= 0)
            {
                anchorMin = Vector2.zero;
                anchorMax = Vector2.one;
                return;
            }

            float minX = Mathf.Clamp(safeArea.xMin, 0f, screenSize.x);
            float minY = Mathf.Clamp(safeArea.yMin, 0f, screenSize.y);
            float maxX = Mathf.Clamp(safeArea.xMax, minX, screenSize.x);
            float maxY = Mathf.Clamp(safeArea.yMax, minY, screenSize.y);
            anchorMin = new Vector2(minX / screenSize.x, minY / screenSize.y);
            anchorMax = new Vector2(maxX / screenSize.x, maxY / screenSize.y);
        }
    }
}
