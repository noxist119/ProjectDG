using UnityEngine;

namespace DefenseGame
{
    [RequireComponent(typeof(Camera))]
    public class RuntimeBattleCameraFitter : MonoBehaviour
    {
        [SerializeField] private Vector3 widePosition = new Vector3(0f, 15f, -12.4f);
        [SerializeField] private Vector3 portraitPosition = new Vector3(0f, 17.6f, -16.1f);
        [SerializeField] private Vector3 wideEuler = new Vector3(53f, 0f, 0f);
        [SerializeField] private Vector3 portraitEuler = new Vector3(58f, 0f, 0f);
        [SerializeField] private float wideFieldOfView = 50f;
        [SerializeField] private float portraitFieldOfView = 60f;
        [SerializeField] private float wideAspect = 0.75f;
        [SerializeField] private float portraitAspect = 0.46f;
        [SerializeField] private Rect wideViewport = new Rect(0f, 0.12f, 1f, 0.78f);
        [SerializeField] private Rect portraitViewport = new Rect(0f, 0.17f, 1f, 0.72f);

        private Camera targetCamera;
        private Vector2Int lastScreenSize;

        public void Configure(Vector3 basePosition, Vector3 mobilePosition, Vector3 eulerRotation, float baseFov, float mobileFov)
        {
            widePosition = basePosition;
            portraitPosition = mobilePosition;
            wideEuler = eulerRotation;
            portraitEuler = eulerRotation + new Vector3(5f, 0f, 0f);
            wideFieldOfView = baseFov;
            portraitFieldOfView = mobileFov;
            ApplyLayout(true);
        }

        private void Awake()
        {
            targetCamera = GetComponent<Camera>();
        }

        private void OnEnable()
        {
            ApplyLayout(true);
        }

        private void Update()
        {
            ApplyLayout(false);
        }

        private void ApplyLayout(bool force)
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
            }

            Vector2Int currentSize = new Vector2Int(Screen.width, Screen.height);
            if (!force && currentSize == lastScreenSize)
            {
                return;
            }

            lastScreenSize = currentSize;

            float aspect = currentSize.y <= 0 ? wideAspect : (float)currentSize.x / currentSize.y;
            float blend = Mathf.InverseLerp(wideAspect, portraitAspect, aspect);
            bool useFullViewport = aspect >= 0.95f;

            transform.position = Vector3.Lerp(widePosition, portraitPosition, blend);
            transform.rotation = Quaternion.Euler(Vector3.Lerp(wideEuler, portraitEuler, blend));
            targetCamera.fieldOfView = Mathf.Lerp(wideFieldOfView, portraitFieldOfView, blend);
            targetCamera.rect = useFullViewport
                ? new Rect(0f, 0f, 1f, 1f)
                : LerpRect(wideViewport, portraitViewport, blend);
        }

        private Rect LerpRect(Rect from, Rect to, float t)
        {
            return new Rect(
                Mathf.Lerp(from.x, to.x, t),
                Mathf.Lerp(from.y, to.y, t),
                Mathf.Lerp(from.width, to.width, t),
                Mathf.Lerp(from.height, to.height, t));
        }
    }
}
