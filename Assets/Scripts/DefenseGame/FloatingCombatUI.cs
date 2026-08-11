using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseGame
{
	public class FloatingCombatUI : MonoBehaviour
	{
		private Canvas canvas;

		private CanvasGroup canvasGroup;

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

		private Transform ownerTransform;

		private Transform anchorTransform;

		private Vector3 fallbackLocalPosition = new Vector3(0f, 1.55f, 0f);

		private int ownerTargetId;

		private float anchorLift;

		private Vector2 crowdLift;

		private Camera cachedPoseCamera;

		private Color accentColor;

		private CharacterGrade grade = CharacterGrade.Normal;

		private float currentHealth01 = 1f;

		private float targetHealth01 = 1f;

		private float currentMana01;

		private float targetMana01;

		private float appliedHealth01 = -1f;

		private float appliedMana01 = -1f;

		private const float BarLerpSpeed = 12f;

		private const float FillEpsilon = 0.0005f;

		private const float TransformEpsilonSqr = 1E-06f;


		private static readonly Color HealthBarColor = new Color(0.1f, 0.86f, 0.22f, 0.98f);

		private static readonly Color ManaBarColor = new Color(0.16f, 0.66f, 1f, 0.98f);

		private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

		public static FloatingCombatUI Attach(Transform target, string displayName, Color color, CharacterGrade grade, float fallbackHeight = 1.55f)
		{
			if (target == null)
			{
				return null;
			}
			if (SharedFloatingCombatCanvas.TryGet(target, out var existing))
			{
				existing.ConfigureAnchor(target, fallbackHeight);
				existing.Configure(displayName, color, grade);
				return existing;
			}
			Canvas sharedCanvas = SharedFloatingCombatCanvas.GetOrCreate(target);
			GameObject root = new GameObject("FloatingCombatUI", typeof(RectTransform));
			root.layer = 5;
			root.transform.SetParent(sharedCanvas.transform, worldPositionStays: false);
			FloatingCombatUI ui = root.AddComponent<FloatingCombatUI>();
			ui.canvas = sharedCanvas;
			ui.ConfigureAnchor(target, fallbackHeight);
			ui.Build(displayName, color, grade);
			SharedFloatingCombatCanvas.Register(target, ui);
			return ui;
		}

		public void Configure(string displayName, Color color, CharacterGrade grade)
		{
			accentColor = color;
			this.grade = grade;
			if ((Object)(object)nameText != null)
			{
				if (nameText.text != displayName)
				{
					nameText.text = displayName;
				}
				((Component)(object)nameText).gameObject.SetActive(value: false);
			}
			if ((Object)(object)gradeText != null)
			{
				gradeText.text = ((int)(grade + 1)).ToString();
				((Graphic)gradeText).color = Color.white;
			}
			if ((Object)(object)gradeBadge != null)
			{
				Sprite diceSprite = LoadSprite("UI/RollRoll/InGame/dice-" + Mathf.Clamp((int)(grade + 1), 1, 6));
				if (diceSprite != null)
				{
					gradeBadge.sprite = diceSprite;
					gradeBadge.type = (Image.Type)0;
					gradeBadge.preserveAspect = true;
					((Graphic)gradeBadge).color = Color.white;
					if ((Object)(object)gradeText != null)
					{
						((Component)(object)gradeText).gameObject.SetActive(value: false);
					}
				}
				else
				{
					Color gradeColor = CharacterGradeUtility.GetColor(grade, color);
					((Graphic)gradeBadge).color = Color.Lerp(gradeColor, new Color(0.02f, 0.04f, 0.14f, 1f), 0.18f);
					if ((Object)(object)gradeText != null)
					{
						((Component)(object)gradeText).gameObject.SetActive(value: true);
					}
				}
			}
			if ((Object)(object)gradeBadgeBorder != null)
			{
				((Graphic)gradeBadgeBorder).color = new Color(0.01f, 0.015f, 0.04f, 0.94f);
			}
			if ((Object)(object)backplate != null)
			{
				((Graphic)backplate).color = new Color(0.02f, 0.035f, 0.1f, 0.72f);
			}
			if ((Object)(object)healthFill != null)
			{
				((Graphic)healthFill).color = HealthBarColor;
			}
			if ((Object)(object)manaFill != null)
			{
				((Graphic)manaFill).color = ManaBarColor;
			}
		}

		public void SetValues(float health, float maxHealth, float mana, float maxMana)
		{
			targetHealth01 = ((maxHealth > 0f) ? Mathf.Clamp01(health / maxHealth) : 0f);
			targetMana01 = ((maxMana > 0f) ? Mathf.Clamp01(mana / maxMana) : 0f);
		}

		public void ShowDamage(float amount, bool critical, bool healing)
		{
			if (!(canvas == null))
			{
				FloatingTextMotion motion = FloatingTextMotion.Spawn(base.transform, healing ? "HealPopup" : "DamagePopup");
				RectTransform rect = motion.CachedRectTransform;
				rect.anchoredPosition = new Vector2(Random.Range(-18f, 18f), 22f);
				rect.sizeDelta = new Vector2(160f, 28f);
				rect.localScale = Vector3.one;
				rect.localRotation = Quaternion.identity;
				Text popup = motion.TargetText;
				popup.font = RuntimeFontProvider.GetDefaultFont();
				popup.alignment = TextAnchor.MiddleCenter;
				popup.fontSize = (critical ? 24 : 18);
				popup.fontStyle = (critical ? FontStyle.Bold : FontStyle.Normal);
				popup.resizeTextForBestFit = false;
				((Graphic)popup).raycastTarget = false;
				popup.text = (healing ? ("+" + Mathf.RoundToInt(amount)) : Mathf.RoundToInt(amount).ToString());
				((Graphic)popup).color = (healing ? new Color(0.4f, 1f, 0.65f, 1f) : (critical ? new Color(1f, 0.85f, 0.25f, 1f) : Color.white));
				motion.Initialize(new Vector2(Random.Range(-8f, 8f), 58f), 0.75f);
			}
		}

		public void ShowStatus(string message, Color color, float duration = 0.9f)
		{
			if (!(canvas == null) && !string.IsNullOrWhiteSpace(message))
			{
				FloatingTextMotion motion = FloatingTextMotion.Spawn(base.transform, "StatusPopup");
				RectTransform rect = motion.CachedRectTransform;
				rect.anchoredPosition = new Vector2(0f, 50f);
				rect.sizeDelta = new Vector2(220f, 36f);
				rect.localScale = Vector3.one;
				rect.localRotation = Quaternion.identity;
				Text popup = motion.TargetText;
				popup.font = RuntimeFontProvider.GetDefaultFont();
				popup.alignment = TextAnchor.MiddleCenter;
				popup.fontSize = 20;
				popup.fontStyle = FontStyle.Bold;
				popup.resizeTextForBestFit = false;
				popup.text = message;
				((Graphic)popup).color = color;
				((Graphic)popup).raycastTarget = false;
				motion.Initialize(new Vector2(0f, 46f), Mathf.Max(0.25f, duration));
			}
		}

		public void ShowTimedStatus(string message, Color color, float duration)
		{
			if (!(canvas == null) && !string.IsNullOrWhiteSpace(message) && !(duration <= 0f))
			{
				GameObject statusObject = new GameObject("TimedStatus");
				statusObject.transform.SetParent(base.transform, worldPositionStays: false);
				RectTransform rect = statusObject.AddComponent<RectTransform>();
				rect.anchoredPosition = new Vector2(0f, 68f);
				rect.sizeDelta = new Vector2(184f, 38f);
				Image background = statusObject.AddComponent<Image>();
				((Graphic)background).color = new Color(0.02f, 0.035f, 0.12f, 0.84f);
				((Graphic)background).raycastTarget = false;
				Outline backgroundOutline = statusObject.AddComponent<Outline>();
				((Shadow)backgroundOutline).effectColor = new Color(color.r, color.g, color.b, 0.72f);
				((Shadow)backgroundOutline).effectDistance = new Vector2(1.1f, -1.1f);
				GameObject textObject = new GameObject("Label");
				textObject.transform.SetParent(statusObject.transform, worldPositionStays: false);
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
				((Graphic)label).color = color;
				((Graphic)label).raycastTarget = false;
				label.resizeTextForBestFit = true;
				label.resizeTextMinSize = 10;
				label.resizeTextMaxSize = 18;
				Shadow shadow = textObject.AddComponent<Shadow>();
				shadow.effectColor = new Color(0f, 0f, 0f, 0.76f);
				shadow.effectDistance = new Vector2(1.2f, -1.2f);
				TimedStatusMotion motion = statusObject.AddComponent<TimedStatusMotion>();
				motion.Initialize(label, (Graphic)(object)background, message, color, duration);
			}
		}

		public void SetRecipeMarker(bool active, string label, Color color)
		{
			if (!((Object)(object)recipeMarkerBack == null) && !((Object)(object)recipeMarkerText == null))
			{
				((Component)(object)recipeMarkerBack).gameObject.SetActive(active);
				((Component)(object)recipeMarkerText).gameObject.SetActive(active);
				if (active)
				{
					((Graphic)recipeMarkerBack).color = new Color(color.r, color.g, color.b, 0.88f);
					recipeMarkerText.text = (string.IsNullOrWhiteSpace(label) ? "초월 재료" : label);
					((Graphic)recipeMarkerText).color = Color.white;
				}
			}
		}

		private void LateUpdate()
		{
			if (ownerTransform == null)
			{
				Object.Destroy(base.gameObject);
				return;
			}
			UpdateBars();
			if (SharedFloatingCombatCanvas.ShouldRefreshPoseThisFrame(ownerTransform))
			{
				ApplyAnchorPosition();
			}
		}

		private void ConfigureAnchor(Transform target, float fallbackHeight)
		{
			ownerTransform = target;
			ownerTargetId = ((target != null) ? target.GetInstanceID() : 0);
			fallbackLocalPosition = new Vector3(0f, Mathf.Max(0.5f, fallbackHeight), 0f);
			anchorTransform = ResolveAnchor(target, out anchorLift);
			cachedPoseCamera = null;
			ApplyAnchorPosition();
		}

		private void ApplyAnchorPosition()
		{
			if (rootRect == null || ownerTransform == null)
			{
				return;
			}
			Camera worldCamera = ResolvePoseCamera();
			if (worldCamera == null)
			{
				SetPoseVisible(false);
				return;
			}
			Vector3 worldPosition = anchorTransform != null ? anchorTransform.position + Vector3.up * anchorLift : ownerTransform.TransformPoint(fallbackLocalPosition);
			Vector3 screenPoint = worldCamera.WorldToScreenPoint(worldPosition);
			const float ScreenCullMargin = 32f;
			if (screenPoint.z <= 0f || screenPoint.x < -ScreenCullMargin || screenPoint.x > Screen.width + ScreenCullMargin || screenPoint.y < -ScreenCullMargin || screenPoint.y > Screen.height + ScreenCullMargin)
			{
				SetPoseVisible(false);
				return;
			}
			RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
			Vector2 localPoint;
			if (canvasRect == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out localPoint))
			{
				SetPoseVisible(false);
				return;
			}
			int column = Mathf.FloorToInt((worldPosition.x + 50f) / 1.1f);
			crowdLift = (Mathf.Abs(column) % 2 == 0) ? Vector2.zero : new Vector2(0f, 18f);
			Vector2 targetPosition = localPoint + crowdLift;
			if ((rootRect.anchoredPosition - targetPosition).sqrMagnitude > TransformEpsilonSqr)
			{
				rootRect.anchoredPosition = targetPosition;
			}
			if ((rootRect.localScale - Vector3.one).sqrMagnitude > TransformEpsilonSqr)
			{
				rootRect.localScale = Vector3.one;
			}
			rootRect.localRotation = Quaternion.identity;
			SetPoseVisible(true);
		}

		private Camera ResolvePoseCamera()
		{
			if (cachedPoseCamera == null || !cachedPoseCamera.isActiveAndEnabled)
			{
				cachedPoseCamera = SharedFloatingCombatCanvas.WorldCamera;
			}
			return cachedPoseCamera;
		}

		private void SetPoseVisible(bool visible)
		{
			if (canvasGroup == null)
			{
				return;
			}
			float alpha = visible ? 1f : 0f;
			if (!Mathf.Approximately(canvasGroup.alpha, alpha))
			{
				canvasGroup.alpha = alpha;
			}
			canvasGroup.blocksRaycasts = false;
			canvasGroup.interactable = false;
		}

		private static Transform ResolveAnchor(Transform target, out float lift)
		{
			lift = 0f;
			if (target == null)
			{
				return null;
			}
			Transform[] children = target.GetComponentsInChildren<Transform>(includeInactive: true);
			for (int i = 0; i < children.Length; i++)
			{
				Transform child = children[i];
				if (child != null && child != target && NormalizeAnchorName(child.name) == "floatinguianchor")
				{
					return child;
				}
			}
			for (int j = 0; j < children.Length; j++)
			{
				Transform child2 = children[j];
				if (child2 == null || child2 == target)
				{
					continue;
				}
				switch (NormalizeAnchorName(child2.name))
				{
				case "uianchor":
				case "hudanchor":
				case "nameanchor":
				case "healthbaranchor":
				case "healthanchor":
					return child2;
				}
			}
			return null;
		}

		private static string NormalizeAnchorName(string value)
		{
			return string.IsNullOrEmpty(value) ? string.Empty : value.ToLowerInvariant().Replace(" ", string.Empty).Replace("_", string.Empty);
		}

		private void UpdateBars()
		{
			if (Mathf.Abs(currentHealth01 - targetHealth01) > 0.0005f)
			{
				currentHealth01 = Mathf.MoveTowards(currentHealth01, targetHealth01, 12f * Time.deltaTime);
			}
			else
			{
				currentHealth01 = targetHealth01;
			}
			if (Mathf.Abs(currentMana01 - targetMana01) > 0.0005f)
			{
				currentMana01 = Mathf.MoveTowards(currentMana01, targetMana01, 12f * Time.deltaTime);
			}
			else
			{
				currentMana01 = targetMana01;
			}
			if (Mathf.Abs(appliedHealth01 - currentHealth01) > 0.0005f)
			{
				SetBarFill(healthFillRect, currentHealth01);
				appliedHealth01 = currentHealth01;
			}
			if (Mathf.Abs(appliedMana01 - currentMana01) > 0.0005f)
			{
				SetBarFill(manaFillRect, currentMana01);
				appliedMana01 = currentMana01;
			}
		}

		private void Build(string displayName, Color color, CharacterGrade grade)
		{
			if (canvas == null)
			{
				canvas = SharedFloatingCombatCanvas.GetOrCreate(ownerTransform);
			}
			rootRect = base.gameObject.GetComponent<RectTransform>();
			rootRect.anchorMin = new Vector2(0.5f, 0.5f);
			rootRect.anchorMax = new Vector2(0.5f, 0.5f);
			rootRect.pivot = new Vector2(0.5f, 0.5f);
			rootRect.sizeDelta = new Vector2(128f, 74f);
			rootRect.localScale = Vector3.one;
			rootRect.localRotation = Quaternion.identity;
			canvasGroup = base.gameObject.GetComponent<CanvasGroup>();
			if (canvasGroup == null)
			{
				canvasGroup = base.gameObject.AddComponent<CanvasGroup>();
			}
			canvasGroup.blocksRaycasts = false;
			canvasGroup.interactable = false;
			backplate = CreateBar("Backplate", new Vector2(0f, 6f), new Vector2(118f, 34f), new Color(0.02f, 0.035f, 0.1f, 0.72f));
			TryApplySprite(backplate, "UI/RollRoll/InGame/minimi-ui-gauge-panel", preserveAspect: false);
			gradeBadgeBorder = CreateBar("GradeBadgeBorder", new Vector2(-47f, 7f), new Vector2(36f, 36f), new Color(0.01f, 0.015f, 0.04f, 0.94f));
			gradeBadge = CreateBar("GradeBadge", new Vector2(-47f, 7f), new Vector2(32f, 32f), Color.white);
			gradeText = CreateText("GradeText", new Vector2(-47f, 7f), new Vector2(28f, 24f), 16);
			gradeText.fontStyle = FontStyle.Bold;
			nameText = CreateText("Name", new Vector2(0f, 32f), new Vector2(86f, 18f), 10);
			((Component)(object)nameText).gameObject.SetActive(value: false);
			recipeMarkerBack = CreateBar("RecipeMarkerBack", new Vector2(18f, 32f), new Vector2(92f, 20f), new Color(0.92f, 0.42f, 1f, 0.88f));
			recipeMarkerText = CreateText("RecipeMarkerText", new Vector2(18f, 32f), new Vector2(86f, 18f), 12);
			recipeMarkerText.fontStyle = FontStyle.Bold;
			((Component)(object)recipeMarkerBack).gameObject.SetActive(value: false);
			((Component)(object)recipeMarkerText).gameObject.SetActive(value: false);
			Image healthBg = CreateBar("HealthBg", new Vector2(18f, 10f), new Vector2(76f, 10f), new Color(0.02f, 0.02f, 0.04f, 0.92f));
			healthFill = CreateFill(((Component)(object)healthBg).transform, "HealthFill", HealthBarColor);
			TryApplySprite(healthFill, "UI/RollRoll/InGame/minimi-ui-gauge-own", preserveAspect: false);
			healthFillRect = (((Object)(object)healthFill != null) ? ((Graphic)healthFill).rectTransform : null);
			Image manaBg = CreateBar("ManaBg", new Vector2(18f, 0f), new Vector2(76f, 5f), new Color(0.02f, 0.02f, 0.04f, 0.86f));
			manaFill = CreateFill(((Component)(object)manaBg).transform, "ManaFill", ManaBarColor);
			TryApplySprite(manaFill, "UI/RollRoll/InGame/mana-gauge", preserveAspect: false);
			manaFillRect = (((Object)(object)manaFill != null) ? ((Graphic)manaFill).rectTransform : null);
			Configure(displayName, color, grade);
			SetBarFill(healthFillRect, currentHealth01);
			SetBarFill(manaFillRect, currentMana01);
			ApplyAnchorPosition();
		}

		private Text CreateText(string name, Vector2 anchoredPosition, Vector2 size, int fontSize)
		{
			GameObject textObject = new GameObject(name);
			textObject.transform.SetParent(base.transform, worldPositionStays: false);
			RectTransform rect = textObject.AddComponent<RectTransform>();
			rect.anchoredPosition = anchoredPosition;
			rect.sizeDelta = size;
			Text text = textObject.AddComponent<Text>();
			text.font = RuntimeFontProvider.GetDefaultFont();
			text.alignment = TextAnchor.MiddleCenter;
			text.fontSize = fontSize;
			((Graphic)text).color = Color.white;
			((Graphic)text).raycastTarget = false;
			text.resizeTextForBestFit = true;
			text.resizeTextMinSize = Mathf.Max(9, fontSize - 4);
			text.resizeTextMaxSize = fontSize;
			text.verticalOverflow = VerticalWrapMode.Truncate;
			Shadow shadow = textObject.AddComponent<Shadow>();
			shadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
			shadow.effectDistance = new Vector2(1.5f, -1.5f);
			Outline outline = textObject.AddComponent<Outline>();
			((Shadow)outline).effectColor = new Color(0f, 0f, 0f, 0.72f);
			((Shadow)outline).effectDistance = new Vector2(1.1f, -1.1f);
			return text;
		}

		private Image CreateBar(string name, Vector2 anchoredPosition, Vector2 size, Color color)
		{
			GameObject barObject = new GameObject(name);
			barObject.transform.SetParent(base.transform, worldPositionStays: false);
			RectTransform rect = barObject.AddComponent<RectTransform>();
			rect.anchoredPosition = anchoredPosition;
			rect.sizeDelta = size;
			Image image = barObject.AddComponent<Image>();
			((Graphic)image).color = color;
			((Graphic)image).raycastTarget = false;
			return image;
		}

		private static Sprite LoadSprite(string resourcePath)
		{
			if (SpriteCache.TryGetValue(resourcePath, out var cachedSprite) && cachedSprite != null && cachedSprite.texture != null)
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
			if (!((Object)(object)image == null))
			{
				Sprite sprite = LoadSprite(resourcePath);
				if (!(sprite == null))
				{
					image.sprite = sprite;
					image.type = (Image.Type)0;
					image.preserveAspect = preserveAspect;
				}
			}
		}

		private Image CreateFill(Transform parent, string name, Color color)
		{
			GameObject fillObject = new GameObject(name);
			fillObject.transform.SetParent(parent, worldPositionStays: false);
			RectTransform rect = fillObject.AddComponent<RectTransform>();
			rect.anchorMin = new Vector2(0f, 0f);
			rect.anchorMax = new Vector2(1f, 1f);
			rect.offsetMin = Vector2.zero;
			rect.offsetMax = Vector2.zero;
			Image image = fillObject.AddComponent<Image>();
			image.type = (Image.Type)0;
			((Graphic)image).color = color;
			((Graphic)image).raycastTarget = false;
			return image;
		}

		private void SetBarFill(RectTransform rect, float amount)
		{
			if (!(rect == null))
			{
				float fill = Mathf.Clamp01(amount);
				Vector2 anchorMax = rect.anchorMax;
				if (!(Mathf.Abs(anchorMax.x - fill) <= 0.0005f))
				{
					anchorMax.x = fill;
					rect.anchorMax = anchorMax;
				}
			}
		}

		private void OnDestroy()
		{
			SharedFloatingCombatCanvas.Unregister(ownerTargetId, this);
		}
	}
}
