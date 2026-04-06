using UnityEngine;
using UnityEngine.UI;

namespace DefenseGame
{
    public class FloatingCombatUI : MonoBehaviour
    {
        private Canvas canvas;
        private RectTransform rootRect;
        private Image healthFill;
        private Image manaFill;
        private Text nameText;
        private Camera cachedCamera;
        private Color accentColor;

        public static FloatingCombatUI Attach(Transform target, string displayName, Color color)
        {
            Transform existing = target.Find("FloatingCombatUI");
            FloatingCombatUI ui = existing != null ? existing.GetComponent<FloatingCombatUI>() : null;
            if (ui != null)
            {
                ui.Configure(displayName, color);
                return ui;
            }

            GameObject root = new GameObject("FloatingCombatUI");
            root.transform.SetParent(target);
            root.transform.localPosition = new Vector3(0f, 1.75f, 0f);
            ui = root.AddComponent<FloatingCombatUI>();
            ui.Build(displayName, color);
            return ui;
        }

        public void Configure(string displayName, Color color)
        {
            accentColor = color;
            if (nameText != null)
            {
                nameText.text = displayName;
                nameText.color = Color.Lerp(color, Color.white, 0.35f);
            }

            if (healthFill != null)
            {
                healthFill.color = Color.Lerp(color, Color.white, 0.15f);
            }

            if (manaFill != null)
            {
                manaFill.color = new Color(0.35f, 0.75f, 1f, 0.95f);
            }
        }

        public void SetValues(float health, float maxHealth, float mana, float maxMana)
        {
            if (healthFill != null)
            {
                healthFill.fillAmount = maxHealth > 0f ? Mathf.Clamp01(health / maxHealth) : 0f;
            }

            if (manaFill != null)
            {
                manaFill.fillAmount = maxMana > 0f ? Mathf.Clamp01(mana / maxMana) : 0f;
            }
        }

        public void ShowDamage(float amount, bool critical, bool healing)
        {
            if (canvas == null)
            {
                return;
            }

            GameObject textObject = new GameObject(healing ? "HealPopup" : "DamagePopup");
            textObject.transform.SetParent(canvas.transform, false);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(Random.Range(-18f, 18f), 22f);
            rect.sizeDelta = new Vector2(160f, 28f);

            Text popup = textObject.AddComponent<Text>();
            popup.font = RuntimeFontProvider.GetDefaultFont();
            popup.alignment = TextAnchor.MiddleCenter;
            popup.fontSize = critical ? 24 : 18;
            popup.fontStyle = critical ? FontStyle.Bold : FontStyle.Normal;
            popup.text = healing ? "+" + Mathf.RoundToInt(amount) : Mathf.RoundToInt(amount).ToString();
            popup.color = healing ? new Color(0.40f, 1f, 0.65f, 1f) : critical ? new Color(1f, 0.85f, 0.25f, 1f) : Color.white;

            FloatingTextMotion motion = textObject.AddComponent<FloatingTextMotion>();
            motion.Initialize(new Vector2(Random.Range(-8f, 8f), 58f), 0.75f);
        }

        private void LateUpdate()
        {
            if (cachedCamera == null)
            {
                cachedCamera = Camera.main;
            }

            if (cachedCamera == null)
            {
                return;
            }

            transform.forward = cachedCamera.transform.forward;
        }

        private void Build(string displayName, Color color)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 50;
            canvas.gameObject.layer = 5;
            gameObject.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 24f;
            rootRect = gameObject.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(1.8f, 0.9f);
            transform.localScale = Vector3.one * 0.01f;

            nameText = CreateText("Name", new Vector2(0f, 28f), new Vector2(180f, 28f), 16);
            Image healthBg = CreateBar("HealthBg", new Vector2(0f, 6f), new Vector2(140f, 14f), new Color(0.08f, 0.08f, 0.10f, 0.9f));
            healthFill = CreateFill(healthBg.transform, "HealthFill", new Color(0.80f, 0.24f, 0.24f, 0.95f));
            Image manaBg = CreateBar("ManaBg", new Vector2(0f, -12f), new Vector2(140f, 10f), new Color(0.08f, 0.08f, 0.10f, 0.85f));
            manaFill = CreateFill(manaBg.transform, "ManaFill", new Color(0.35f, 0.75f, 1f, 0.95f));
            Configure(displayName, color);
        }

        private Text CreateText(string name, Vector2 anchoredPosition, Vector2 size, int fontSize)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(transform, false);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Text text = textObject.AddComponent<Text>();
            text.font = RuntimeFontProvider.GetDefaultFont();
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = fontSize;
            text.color = Color.white;
            return text;
        }

        private Image CreateBar(string name, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            GameObject barObject = new GameObject(name);
            barObject.transform.SetParent(transform, false);
            RectTransform rect = barObject.AddComponent<RectTransform>();
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = barObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private Image CreateFill(Transform parent, string name, Color color)
        {
            GameObject fillObject = new GameObject(name);
            fillObject.transform.SetParent(parent, false);
            RectTransform rect = fillObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(2f, 2f);
            rect.offsetMax = new Vector2(-2f, -2f);

            Image image = fillObject.AddComponent<Image>();
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = 0;
            image.color = color;
            image.fillAmount = 1f;
            return image;
        }
    }
}
