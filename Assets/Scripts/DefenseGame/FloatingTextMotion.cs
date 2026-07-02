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

    public class TimedStatusMotion : MonoBehaviour
    {
        private Text label;
        private Graphic background;
        private string message;
        private Color labelColor;
        private Color backgroundColor;
        private float remaining;
        private float duration;

        public void Initialize(Text targetLabel, Graphic targetBackground, string statusMessage, Color color, float statusDuration)
        {
            label = targetLabel;
            background = targetBackground;
            message = statusMessage;
            labelColor = color;
            backgroundColor = targetBackground != null ? targetBackground.color : Color.clear;
            duration = Mathf.Max(0.1f, statusDuration);
            remaining = duration;
            UpdateLabel();
        }

        private void Update()
        {
            remaining -= Time.deltaTime;
            UpdateLabel();

            float fade = Mathf.Clamp01(remaining / Mathf.Min(0.35f, duration));
            if (label != null)
            {
                Color color = labelColor;
                color.a *= fade;
                label.color = color;
            }

            if (background != null)
            {
                Color color = backgroundColor;
                color.a *= fade;
                background.color = color;
            }

            transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 8f) * 0.025f);

            if (remaining <= 0f)
            {
                Destroy(gameObject);
            }
        }

        private void UpdateLabel()
        {
            if (label == null)
            {
                return;
            }

            int seconds = Mathf.Max(0, Mathf.CeilToInt(remaining));
            label.text = message + " " + seconds + "초";
        }
    }
}
