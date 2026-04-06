using UnityEngine;
using UnityEngine.UI;

namespace DefenseGame
{
    public class FloatingTextMotion : MonoBehaviour
    {
        private Vector2 velocity;
        private float lifetime;
        private float maxLifetime;
        private Text targetText;

        public void Initialize(Vector2 initialVelocity, float duration)
        {
            velocity = initialVelocity;
            lifetime = duration;
            maxLifetime = duration;
            targetText = GetComponent<Text>();
        }

        private void Update()
        {
            lifetime -= Time.deltaTime;
            RectTransform rect = transform as RectTransform;
            if (rect != null)
            {
                rect.anchoredPosition += velocity * Time.deltaTime;
            }

            velocity *= 0.96f;

            if (targetText != null)
            {
                Color color = targetText.color;
                color.a = Mathf.Clamp01(lifetime / Mathf.Max(0.01f, maxLifetime));
                targetText.color = color;
            }

            if (lifetime <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }
}
