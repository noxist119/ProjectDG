using System.Collections.Generic;
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
        private RectTransform healthFillRect;
        private RectTransform manaFillRect;
        private Text nameText;
        private Text gradeText;
        private Text recipeMarkerText;
        private Image gradeBadge;
        private Image gradeBadgeBorder;
        private Image recipeMarkerBack;
        private Image backplate;
        private Camera cachedCamera;
        private Transform anchorTransform;
        private Vector3 fallbackLocalPosition = new Vector3(0f, 1.55f, 0f);
        private float anchorLift;
        private float crowdLift;
        private Color accentColor;
        private CharacterGrade grade = CharacterGrade.Normal;
        private float currentHealth01 = 1f;
        private float targetHealth01 = 1f;
        private float currentMana01;
        private float targetMana01;
        private const float BarLerpSpeed = 12f;
        private const float BaseUiScale = 0.0100f;
        private const float MinUiScale = 0.0090f;
        private const float MaxUiScale = 0.0115f;
        private const float DistanceScaleFactor = 0.00115f;
        private static readonly Color HealthBarColor = new Color(0.10f, 0.86f, 0.22f, 0.98f);
        private static readonly Color ManaBarColor = new Color(0.16f, 0.66f, 1f, 0.98f);
        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        public static FloatingCombatUI Attach(Transform target, string displayName, Color color, CharacterGrade grade, float fallbackHeight = 1.55f)
        {
            Transform existing = target.Find("FloatingCombatUI");
            FloatingCombatUI ui = existing != null ? existing.GetComponent<FloatingCombatUI>() : null;
            if (ui != null)
            {
                ui.ConfigureAnchor(target, fallbackHeight);
                ui.Configure(displayName, color, grade);
                return ui;
            }

            GameObject root = new GameObject("FloatingCombatUI");
            root.transform.SetParent(target, false);
            ui = root.AddComponent<FloatingCombatUI>();
            ui.ConfigureAnchor(target, fallbackHeight);
            ui.Build(displayName, color, grade);
            return ui;
        }

        public void Configure(string displayName, Color color, CharacterGrade grade)
        {
            accentColor = color;
            this.grade = grade;
            if (nameText != null)
            {
                if (nameText.text != displayName)
                {
                    nameText.text = displayName;
                }

                nameText.gameObject.SetActive(false);
            }

            if (gradeText != null)
            {
                gradeText.text = ((int)grade + 1).ToString();
                gradeText.color = Color.white;
            }

            if (gradeBadge != null)
            {
                Sprite diceSprite = LoadSprite("UI/RollRoll/InGame/dice-" + Mathf.Clamp((int)grade + 1, 1, 6));
                if (diceSprite != null)
                {
                    gradeBadge.sprite = diceSprite;
                    gradeBadge.type = Image.Type.Simple;
                    gradeBadge.preserveAspect = true;
                    gradeBadge.color = Color.white;
                    if (gradeText != null)
                    {
                        gradeText.gameObject.SetActive(false);
                    }
                }
                else
                {
                    Color gradeColor = CharacterGradeUtility.GetColor(grade, color);
                    gradeBadge.color = Color.Lerp(gradeColor, new Color(0.02f, 0.04f, 0.14f, 1f), 0.18f);
                    if (gradeText != null)
                    {
                        gradeText.gameObject.SetActive(true);
                    }
                }
            }

            if (gradeBadgeBorder != null)
            {
                gradeBadgeBorder.color = new Color(0.01f, 0.015f, 0.04f, 0.94f);
            }

            if (backplate != null)
            {
                backplate.color = new Color(0.02f, 0.035f, 0.10f, 0.72f);
            }

            if (healthFill != null)
            {
                healthFill.color = HealthBarColor;
            }

            if (manaFill != null)
            {
                manaFill.color = ManaBarColor;
            }
        }

        public void SetValues(float health, float maxHealth, float mana, float maxMana)
        {
            targetHealth01 = maxHealth > 0f ? Mathf.Clamp01(health / maxHealth) : 0f;
            targetMana01 = maxMana > 0f ? Mathf.Clamp01(mana / maxMana) : 0f;
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

        public void ShowStatus(string message, Color color, float duration = 0.9f)
        {
            if (canvas == null || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            GameObject textObject = new GameObject("StatusPopup");
            textObject.transform.SetParent(canvas.transform, false);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(0f, 50f);
            rect.sizeDelta = new Vector2(220f, 36f);

            Text popup = textObject.AddComponent<Text>();
            popup.font = RuntimeFontProvider.GetDefaultFont();
            popup.alignment = TextAnchor.MiddleCenter;
            popup.fontSize = 20;
            popup.fontStyle = FontStyle.Bold;
            popup.text = message;
            popup.color = color;
            popup.raycastTarget = false;

            FloatingTextMotion motion = textObject.AddComponent<FloatingTextMotion>();
            motion.Initialize(new Vector2(0f, 46f), Mathf.Max(0.25f, duration));
        }

        public void ShowTimedStatus(string message, Color color, float duration)
        {
            if (canvas == null || string.IsNullOrWhiteSpace(message) || duration <= 0f)
            {
                return;
            }

            GameObject statusObject = new GameObject("TimedStatus");
            statusObject.transform.SetParent(canvas.transform, false);
            RectTransform rect = statusObject.AddComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(0f, 68f);
            rect.sizeDelta = new Vector2(184f, 38f);

            Image background = statusObject.AddComponent<Image>();
            background.color = new Color(0.02f, 0.035f, 0.12f, 0.84f);
            background.raycastTarget = false;
            Outline backgroundOutline = statusObject.AddComponent<Outline>();
            backgroundOutline.effectColor = new Color(color.r, color.g, color.b, 0.72f);
            backgroundOutline.effectDistance = new Vector2(1.1f, -1.1f);

            GameObject textObject = new GameObject("Label");
            textObject.transform.SetParent(statusObject.transform, false);
            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(7f, 1f);
            textRect.offsetMax = new Vector2(-7f, -1f);

            Text label = textObject.AddComponent<Text>();
            label.font = RuntimeFontProvider.GetDefaultFont();
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = 18;
            label.fontStyle = FontStyle.Bold;
            label.color = color;
            label.raycastTarget = false;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 10;
            label.resizeTextMaxSize = 18;

            Shadow shadow = textObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.76f);
            shadow.effectDistance = new Vector2(1.2f, -1.2f);

            TimedStatusMotion motion = statusObject.AddComponent<TimedStatusMotion>();
            motion.Initialize(label, background, message, color, duration);
        }

        public void SetRecipeMarker(bool active, string label, Color color)
        {
            if (recipeMarkerBack == null || recipeMarkerText == null)
            {
                return;
            }

            recipeMarkerBack.gameObject.SetActive(active);
            recipeMarkerText.gameObject.SetActive(active);
            if (!active)
            {
                return;
            }

            recipeMarkerBack.color = new Color(color.r, color.g, color.b, 0.88f);
            recipeMarkerText.text = string.IsNullOrWhiteSpace(label) ? "초월 재료" : label;
            recipeMarkerText.color = Color.white;
        }

        private void LateUpdate()
        {
            ApplyAnchorPosition();
            UpdateBars();

            if (cachedCamera == null)
            {
                cachedCamera = Camera.main;
            }

            if (cachedCamera == null)
            {
                return;
            }

            transform.forward = cachedCamera.transform.forward;
            ApplyDistanceScale(cachedCamera);
        }

        private void ConfigureAnchor(Transform target, float fallbackHeight)
        {
            fallbackLocalPosition = new Vector3(0f, Mathf.Max(0.5f, fallbackHeight), 0f);
            anchorTransform = ResolveAnchor(target, out anchorLift);
            ApplyAnchorPosition();
        }

        private void ApplyAnchorPosition()
        {
            if (anchorTransform != null)
            {
                Vector3 basePosition = anchorTransform.position + Vector3.up * anchorLift;
                crowdLift = ResolveCrowdLift(basePosition);
                transform.position = basePosition + Vector3.up * crowdLift;
            }
            else
            {
                crowdLift = ResolveCrowdLift(transform.parent != null ? transform.parent.TransformPoint(fallbackLocalPosition) : transform.position);
                transform.localPosition = fallbackLocalPosition + Vector3.up * crowdLift;
            }
        }

        private float ResolveCrowdLift(Vector3 worldPosition)
        {
            int column = Mathf.FloorToInt((worldPosition.x + 50f) / 1.1f);
            return Mathf.Abs(column) % 2 == 0 ? 0f : 0.18f;
        }

        private static Transform ResolveAnchor(Transform target, out float lift)
        {
            lift = 0f;
            if (target == null)
            {
                return null;
            }

            Transform explicitAnchor = FindNamedAnchor(target);
            if (explicitAnchor != null)
            {
                return explicitAnchor;
            }

            Animator animator = target.GetComponentInChildren<Animator>();
            if (animator != null && animator.isHuman)
            {
                Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
                if (head != null)
                {
                    lift = 0.24f;
                    return head;
                }

                Transform chest = animator.GetBoneTransform(HumanBodyBones.Chest);
                if (chest != null)
                {
                    lift = 0.62f;
                    return chest;
                }
            }

            Transform namedHead = FindChildContaining(target, "head", "neck");
            if (namedHead != null)
            {
                lift = 0.24f;
                return namedHead;
            }

            Transform namedChest = FindChildContaining(target, "chest", "spine", "body");
            if (namedChest != null)
            {
                lift = 0.58f;
                return namedChest;
            }

            return null;
        }

        private static Transform FindNamedAnchor(Transform target)
        {
            Transform[] children = target.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child == null || child == target)
                {
                    continue;
                }

                string childName = child.name.ToLowerInvariant().Replace(" ", string.Empty).Replace("_", string.Empty);
                if (childName == "floatinguianchor" ||
                    childName == "uianchor" ||
                    childName == "hudanchor" ||
                    childName == "nameanchor" ||
                    childName == "healthbaranchor")
                {
                    return child;
                }
            }

            return null;
        }

        private static Transform FindChildContaining(Transform target, params string[] tokens)
        {
            Transform[] children = target.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child == null || child == target)
                {
                    continue;
                }

                string childName = child.name.ToLowerInvariant();
                if (childName.Contains("weapon") ||
                    childName.Contains("sword") ||
                    childName.Contains("prop") ||
                    childName.Contains("effect") ||
                    childName.Contains("muzzle") ||
                    childName.Contains("hand"))
                {
                    continue;
                }

                for (int tokenIndex = 0; tokenIndex < tokens.Length; tokenIndex++)
                {
                    if (childName.Contains(tokens[tokenIndex]))
                    {
                        return child;
                    }
                }
            }

            return null;
        }

        private void UpdateBars()
        {
            currentHealth01 = Mathf.MoveTowards(currentHealth01, targetHealth01, BarLerpSpeed * Time.deltaTime);
            currentMana01 = Mathf.MoveTowards(currentMana01, targetMana01, BarLerpSpeed * Time.deltaTime);
            SetBarFill(healthFillRect, currentHealth01);
            SetBarFill(manaFillRect, currentMana01);
        }

        private void Build(string displayName, Color color, CharacterGrade grade)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 85;
            canvas.gameObject.layer = 5;
            gameObject.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 48f;
            rootRect = gameObject.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(1.28f, 0.74f);
            transform.localScale = Vector3.one * BaseUiScale;

            backplate = CreateBar("Backplate", new Vector2(0f, 6f), new Vector2(118f, 34f), new Color(0.02f, 0.035f, 0.10f, 0.72f));
            TryApplySprite(backplate, "UI/RollRoll/InGame/minimi-ui-gauge-panel", false);
            gradeBadgeBorder = CreateBar("GradeBadgeBorder", new Vector2(-47f, 7f), new Vector2(36f, 36f), new Color(0.01f, 0.015f, 0.04f, 0.94f));
            gradeBadge = CreateBar("GradeBadge", new Vector2(-47f, 7f), new Vector2(32f, 32f), Color.white);
            gradeText = CreateText("GradeText", new Vector2(-47f, 7f), new Vector2(28f, 24f), 16);
            gradeText.fontStyle = FontStyle.Bold;
            nameText = CreateText("Name", new Vector2(0f, 32f), new Vector2(86f, 18f), 10);
            nameText.gameObject.SetActive(false);
            recipeMarkerBack = CreateBar("RecipeMarkerBack", new Vector2(18f, 32f), new Vector2(92f, 20f), new Color(0.92f, 0.42f, 1f, 0.88f));
            recipeMarkerText = CreateText("RecipeMarkerText", new Vector2(18f, 32f), new Vector2(86f, 18f), 12);
            recipeMarkerText.fontStyle = FontStyle.Bold;
            recipeMarkerBack.gameObject.SetActive(false);
            recipeMarkerText.gameObject.SetActive(false);
            Image healthBg = CreateBar("HealthBg", new Vector2(18f, 10f), new Vector2(76f, 10f), new Color(0.02f, 0.02f, 0.04f, 0.92f));
            healthFill = CreateFill(healthBg.transform, "HealthFill", HealthBarColor);
            TryApplySprite(healthFill, "UI/RollRoll/InGame/minimi-ui-gauge-own", false);
            healthFillRect = healthFill != null ? healthFill.rectTransform : null;
            Image manaBg = CreateBar("ManaBg", new Vector2(18f, 0f), new Vector2(76f, 5f), new Color(0.02f, 0.02f, 0.04f, 0.86f));
            manaFill = CreateFill(manaBg.transform, "ManaFill", ManaBarColor);
            TryApplySprite(manaFill, "UI/RollRoll/InGame/mana-gauge", false);
            manaFillRect = manaFill != null ? manaFill.rectTransform : null;
            Configure(displayName, color, grade);
            SetBarFill(healthFillRect, currentHealth01);
            SetBarFill(manaFillRect, currentMana01);
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
            text.raycastTarget = false;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(9, fontSize - 4);
            text.resizeTextMaxSize = fontSize;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            Shadow shadow = textObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            Outline outline = textObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.72f);
            outline.effectDistance = new Vector2(1.1f, -1.1f);
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
            image.raycastTarget = false;
            return image;
        }

        private void ApplyDistanceScale(Camera camera)
        {
            float distance = Vector3.Distance(camera.transform.position, transform.position);
            float scale = Mathf.Clamp(distance * DistanceScaleFactor, MinUiScale, MaxUiScale);
            transform.localScale = Vector3.one * Mathf.Max(BaseUiScale, scale);
        }

        private static Sprite LoadSprite(string resourcePath)
        {
            if (SpriteCache.TryGetValue(resourcePath, out Sprite cachedSprite) && cachedSprite != null && cachedSprite.texture != null)
            {
                return cachedSprite;
            }

            SpriteCache.Remove(resourcePath);

            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                return null;
            }

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            SpriteCache[resourcePath] = sprite;
            return sprite;
        }

        private static void TryApplySprite(Image image, string resourcePath, bool preserveAspect)
        {
            if (image == null)
            {
                return;
            }

            Sprite sprite = LoadSprite(resourcePath);
            if (sprite == null)
            {
                return;
            }

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = preserveAspect;
        }

        private Image CreateFill(Transform parent, string name, Color color)
        {
            GameObject fillObject = new GameObject(name);
            fillObject.transform.SetParent(parent, false);
            RectTransform rect = fillObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = fillObject.AddComponent<Image>();
            image.type = Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private void SetBarFill(RectTransform rect, float amount)
        {
            if (rect == null)
            {
                return;
            }

            Vector2 anchorMax = rect.anchorMax;
            anchorMax.x = Mathf.Clamp01(amount);
            rect.anchorMax = anchorMax;
        }
    }
}
