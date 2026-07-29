using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseGame;

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

	private Transform ownerTransform;

	private Transform anchorTransform;

	private Vector3 fallbackLocalPosition = new Vector3(0f, 1.55f, 0f);

	private int ownerTargetId;

	private float anchorLift;

	private float crowdLift;

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

	private const float BaseUiScale = 0.01f;

	private const float MinUiScale = 0.009f;

	private const float MaxUiScale = 0.0115f;

	private const float DistanceScaleFactor = 0.00115f;

	private static readonly Color HealthBarColor = new Color(0.1f, 0.86f, 0.22f, 0.98f);

	private static readonly Color ManaBarColor = new Color(0.16f, 0.66f, 1f, 0.98f);

	private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

	public static FloatingCombatUI Attach(Transform target, string displayName, Color color, CharacterGrade grade, float fallbackHeight = 1.55f)
	{
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Expected O, but got Unknown
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)target == (Object)null)
		{
			return null;
		}
		if (SharedFloatingCombatCanvas.TryGet(target, out var ui))
		{
			ui.ConfigureAnchor(target, fallbackHeight);
			ui.Configure(displayName, color, grade);
			return ui;
		}
		Canvas orCreate = SharedFloatingCombatCanvas.GetOrCreate(target);
		GameObject val = new GameObject("FloatingCombatUI", new Type[1] { typeof(RectTransform) });
		val.layer = 5;
		val.transform.SetParent(((Component)orCreate).transform, false);
		FloatingCombatUI floatingCombatUI = val.AddComponent<FloatingCombatUI>();
		floatingCombatUI.canvas = orCreate;
		floatingCombatUI.ConfigureAnchor(target, fallbackHeight);
		floatingCombatUI.Build(displayName, color, grade);
		SharedFloatingCombatCanvas.Register(target, floatingCombatUI);
		return floatingCombatUI;
	}

	public void Configure(string displayName, Color color, CharacterGrade grade)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0204: Unknown result type (might be due to invalid IL or missing references)
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		//IL_024e: Unknown result type (might be due to invalid IL or missing references)
		accentColor = color;
		this.grade = grade;
		if ((Object)(object)nameText != (Object)null)
		{
			if (nameText.text != displayName)
			{
				nameText.text = displayName;
			}
			((Component)nameText).gameObject.SetActive(false);
		}
		if ((Object)(object)gradeText != (Object)null)
		{
			gradeText.text = ((int)(grade + 1)).ToString();
			((Graphic)gradeText).color = Color.white;
		}
		if ((Object)(object)gradeBadge != (Object)null)
		{
			Sprite val = LoadSprite("UI/RollRoll/InGame/dice-" + Mathf.Clamp((int)(grade + 1), 1, 6));
			if ((Object)(object)val != (Object)null)
			{
				gradeBadge.sprite = val;
				gradeBadge.type = (Type)0;
				gradeBadge.preserveAspect = true;
				((Graphic)gradeBadge).color = Color.white;
				if ((Object)(object)gradeText != (Object)null)
				{
					((Component)gradeText).gameObject.SetActive(false);
				}
			}
			else
			{
				Color color2 = CharacterGradeUtility.GetColor(grade, color);
				((Graphic)gradeBadge).color = Color.Lerp(color2, new Color(0.02f, 0.04f, 0.14f, 1f), 0.18f);
				if ((Object)(object)gradeText != (Object)null)
				{
					((Component)gradeText).gameObject.SetActive(true);
				}
			}
		}
		if ((Object)(object)gradeBadgeBorder != (Object)null)
		{
			((Graphic)gradeBadgeBorder).color = new Color(0.01f, 0.015f, 0.04f, 0.94f);
		}
		if ((Object)(object)backplate != (Object)null)
		{
			((Graphic)backplate).color = new Color(0.02f, 0.035f, 0.1f, 0.72f);
		}
		if ((Object)(object)healthFill != (Object)null)
		{
			((Graphic)healthFill).color = HealthBarColor;
		}
		if ((Object)(object)manaFill != (Object)null)
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
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)canvas == (Object)null))
		{
			FloatingTextMotion floatingTextMotion = FloatingTextMotion.Spawn(((Component)this).transform, healing ? "HealPopup" : "DamagePopup");
			RectTransform cachedRectTransform = floatingTextMotion.CachedRectTransform;
			cachedRectTransform.anchoredPosition = new Vector2(Random.Range(-18f, 18f), 22f);
			cachedRectTransform.sizeDelta = new Vector2(160f, 28f);
			((Transform)cachedRectTransform).localScale = Vector3.one;
			((Transform)cachedRectTransform).localRotation = Quaternion.identity;
			Text targetText = floatingTextMotion.TargetText;
			targetText.font = RuntimeFontProvider.GetDefaultFont();
			targetText.alignment = (TextAnchor)4;
			targetText.fontSize = (critical ? 24 : 18);
			targetText.fontStyle = (FontStyle)(critical ? 1 : 0);
			targetText.resizeTextForBestFit = false;
			((Graphic)targetText).raycastTarget = false;
			targetText.text = (healing ? ("+" + Mathf.RoundToInt(amount)) : Mathf.RoundToInt(amount).ToString());
			((Graphic)targetText).color = (Color)(healing ? new Color(0.4f, 1f, 0.65f, 1f) : (critical ? new Color(1f, 0.85f, 0.25f, 1f) : Color.white));
			floatingTextMotion.Initialize(new Vector2(Random.Range(-8f, 8f), 58f), 0.75f);
		}
	}

	public void ShowStatus(string message, Color color, float duration = 0.9f)
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)canvas == (Object)null) && !string.IsNullOrWhiteSpace(message))
		{
			FloatingTextMotion floatingTextMotion = FloatingTextMotion.Spawn(((Component)this).transform, "StatusPopup");
			RectTransform cachedRectTransform = floatingTextMotion.CachedRectTransform;
			cachedRectTransform.anchoredPosition = new Vector2(0f, 50f);
			cachedRectTransform.sizeDelta = new Vector2(220f, 36f);
			((Transform)cachedRectTransform).localScale = Vector3.one;
			((Transform)cachedRectTransform).localRotation = Quaternion.identity;
			Text targetText = floatingTextMotion.TargetText;
			targetText.font = RuntimeFontProvider.GetDefaultFont();
			targetText.alignment = (TextAnchor)4;
			targetText.fontSize = 20;
			targetText.fontStyle = (FontStyle)1;
			targetText.resizeTextForBestFit = false;
			targetText.text = message;
			((Graphic)targetText).color = color;
			((Graphic)targetText).raycastTarget = false;
			floatingTextMotion.Initialize(new Vector2(0f, 46f), Mathf.Max(0.25f, duration));
		}
	}

	public void ShowTimedStatus(string message, Color color, float duration)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Expected O, but got Unknown
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Expected O, but got Unknown
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)canvas == (Object)null) && !string.IsNullOrWhiteSpace(message) && !(duration <= 0f))
		{
			GameObject val = new GameObject("TimedStatus");
			val.transform.SetParent(((Component)this).transform, false);
			RectTransform val2 = val.AddComponent<RectTransform>();
			val2.anchoredPosition = new Vector2(0f, 68f);
			val2.sizeDelta = new Vector2(184f, 38f);
			Image val3 = val.AddComponent<Image>();
			((Graphic)val3).color = new Color(0.02f, 0.035f, 0.12f, 0.84f);
			((Graphic)val3).raycastTarget = false;
			Outline val4 = val.AddComponent<Outline>();
			((Shadow)val4).effectColor = new Color(color.r, color.g, color.b, 0.72f);
			((Shadow)val4).effectDistance = new Vector2(1.1f, -1.1f);
			GameObject val5 = new GameObject("Label");
			val5.transform.SetParent(val.transform, false);
			RectTransform val6 = val5.AddComponent<RectTransform>();
			val6.anchorMin = Vector2.zero;
			val6.anchorMax = Vector2.one;
			val6.offsetMin = new Vector2(7f, 1f);
			val6.offsetMax = new Vector2(-7f, -1f);
			Text val7 = val5.AddComponent<Text>();
			val7.font = RuntimeFontProvider.GetDefaultFont();
			val7.alignment = (TextAnchor)4;
			val7.fontSize = 18;
			val7.fontStyle = (FontStyle)1;
			((Graphic)val7).color = color;
			((Graphic)val7).raycastTarget = false;
			val7.resizeTextForBestFit = true;
			val7.resizeTextMinSize = 10;
			val7.resizeTextMaxSize = 18;
			Shadow val8 = val5.AddComponent<Shadow>();
			val8.effectColor = new Color(0f, 0f, 0f, 0.76f);
			val8.effectDistance = new Vector2(1.2f, -1.2f);
			TimedStatusMotion timedStatusMotion = val.AddComponent<TimedStatusMotion>();
			timedStatusMotion.Initialize(val7, (Graphic)(object)val3, message, color, duration);
		}
	}

	public void SetRecipeMarker(bool active, string label, Color color)
	{
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)recipeMarkerBack == (Object)null) && !((Object)(object)recipeMarkerText == (Object)null))
		{
			((Component)recipeMarkerBack).gameObject.SetActive(active);
			((Component)recipeMarkerText).gameObject.SetActive(active);
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
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)ownerTransform == (Object)null)
		{
			Object.Destroy((Object)(object)((Component)this).gameObject);
			return;
		}
		UpdateBars();
		if (!SharedFloatingCombatCanvas.ShouldRefreshPoseThisFrame())
		{
			return;
		}
		ApplyAnchorPosition();
		Camera worldCamera = SharedFloatingCombatCanvas.WorldCamera;
		if (!((Object)(object)worldCamera == (Object)null))
		{
			Vector3 forward = ((Component)worldCamera).transform.forward;
			Vector3 val = ((Component)this).transform.forward - forward;
			if (((Vector3)(ref val)).sqrMagnitude > 1E-06f)
			{
				((Component)this).transform.forward = forward;
			}
			ApplyDistanceScale(worldCamera);
		}
	}

	private void ConfigureAnchor(Transform target, float fallbackHeight)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		ownerTransform = target;
		ownerTargetId = (((Object)(object)target != (Object)null) ? ((Object)target).GetInstanceID() : 0);
		fallbackLocalPosition = new Vector3(0f, Mathf.Max(0.5f, fallbackHeight), 0f);
		anchorTransform = ResolveAnchor(target, out anchorLift);
		ApplyAnchorPosition();
	}

	private void ApplyAnchorPosition()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val3;
		if ((Object)(object)anchorTransform != (Object)null)
		{
			Vector3 val = anchorTransform.position + Vector3.up * anchorLift;
			crowdLift = ResolveCrowdLift(val);
			Vector3 val2 = val + Vector3.up * crowdLift;
			val3 = ((Component)this).transform.position - val2;
			if (((Vector3)(ref val3)).sqrMagnitude > 1E-06f)
			{
				((Component)this).transform.position = val2;
			}
		}
		else
		{
			Vector3 val4 = (((Object)(object)ownerTransform != (Object)null) ? ownerTransform.TransformPoint(fallbackLocalPosition) : ((Component)this).transform.position);
			crowdLift = ResolveCrowdLift(val4);
			Vector3 val5 = val4 + Vector3.up * crowdLift;
			val3 = ((Component)this).transform.position - val5;
			if (((Vector3)(ref val3)).sqrMagnitude > 1E-06f)
			{
				((Component)this).transform.position = val5;
			}
		}
	}

	private float ResolveCrowdLift(Vector3 worldPosition)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		int num = Mathf.FloorToInt((worldPosition.x + 50f) / 1.1f);
		return (Mathf.Abs(num) % 2 == 0) ? 0f : 0.18f;
	}

	private static Transform ResolveAnchor(Transform target, out float lift)
	{
		lift = 0f;
		if ((Object)(object)target == (Object)null)
		{
			return null;
		}
		Transform val = FindNamedAnchor(target);
		if ((Object)(object)val != (Object)null)
		{
			return val;
		}
		Animator componentInChildren = ((Component)target).GetComponentInChildren<Animator>();
		if ((Object)(object)componentInChildren != (Object)null && componentInChildren.isHuman)
		{
			Transform boneTransform = componentInChildren.GetBoneTransform((HumanBodyBones)10);
			if ((Object)(object)boneTransform != (Object)null)
			{
				lift = 0.24f;
				return boneTransform;
			}
			Transform boneTransform2 = componentInChildren.GetBoneTransform((HumanBodyBones)8);
			if ((Object)(object)boneTransform2 != (Object)null)
			{
				lift = 0.62f;
				return boneTransform2;
			}
		}
		Transform val2 = FindChildContaining(target, "head", "neck");
		if ((Object)(object)val2 != (Object)null)
		{
			lift = 0.24f;
			return val2;
		}
		Transform val3 = FindChildContaining(target, "chest", "spine", "body");
		if ((Object)(object)val3 != (Object)null)
		{
			lift = 0.58f;
			return val3;
		}
		return null;
	}

	private static Transform FindNamedAnchor(Transform target)
	{
		Transform[] componentsInChildren = ((Component)target).GetComponentsInChildren<Transform>(true);
		foreach (Transform val in componentsInChildren)
		{
			if ((Object)(object)val == (Object)null || (Object)(object)val == (Object)(object)target)
			{
				continue;
			}
			string text = ((Object)val).name.ToLowerInvariant().Replace(" ", string.Empty).Replace("_", string.Empty);
			switch (text)
			{
			default:
				if (!(text == "healthbaranchor"))
				{
					continue;
				}
				break;
			case "floatinguianchor":
			case "uianchor":
			case "hudanchor":
			case "nameanchor":
				break;
			}
			return val;
		}
		return null;
	}

	private static Transform FindChildContaining(Transform target, params string[] tokens)
	{
		Transform[] componentsInChildren = ((Component)target).GetComponentsInChildren<Transform>(true);
		foreach (Transform val in componentsInChildren)
		{
			if ((Object)(object)val == (Object)null || (Object)(object)val == (Object)(object)target)
			{
				continue;
			}
			string text = ((Object)val).name.ToLowerInvariant();
			if (text.Contains("weapon") || text.Contains("sword") || text.Contains("prop") || text.Contains("effect") || text.Contains("muzzle") || text.Contains("hand"))
			{
				continue;
			}
			for (int j = 0; j < tokens.Length; j++)
			{
				if (text.Contains(tokens[j]))
				{
					return val;
				}
			}
		}
		return null;
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
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_0231: Unknown result type (might be due to invalid IL or missing references)
		//IL_0283: Unknown result type (might be due to invalid IL or missing references)
		//IL_0292: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0316: Unknown result type (might be due to invalid IL or missing references)
		//IL_0325: Unknown result type (might be due to invalid IL or missing references)
		//IL_033e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0356: Unknown result type (might be due to invalid IL or missing references)
		//IL_039b: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)canvas == (Object)null)
		{
			canvas = SharedFloatingCombatCanvas.GetOrCreate(ownerTransform);
		}
		rootRect = ((Component)this).gameObject.GetComponent<RectTransform>();
		rootRect.sizeDelta = new Vector2(128f, 74f);
		((Component)this).transform.localScale = Vector3.one;
		((Component)this).transform.localRotation = Quaternion.identity;
		backplate = CreateBar("Backplate", new Vector2(0f, 6f), new Vector2(118f, 34f), new Color(0.02f, 0.035f, 0.1f, 0.72f));
		TryApplySprite(backplate, "UI/RollRoll/InGame/minimi-ui-gauge-panel", preserveAspect: false);
		gradeBadgeBorder = CreateBar("GradeBadgeBorder", new Vector2(-47f, 7f), new Vector2(36f, 36f), new Color(0.01f, 0.015f, 0.04f, 0.94f));
		gradeBadge = CreateBar("GradeBadge", new Vector2(-47f, 7f), new Vector2(32f, 32f), Color.white);
		gradeText = CreateText("GradeText", new Vector2(-47f, 7f), new Vector2(28f, 24f), 16);
		gradeText.fontStyle = (FontStyle)1;
		nameText = CreateText("Name", new Vector2(0f, 32f), new Vector2(86f, 18f), 10);
		((Component)nameText).gameObject.SetActive(false);
		recipeMarkerBack = CreateBar("RecipeMarkerBack", new Vector2(18f, 32f), new Vector2(92f, 20f), new Color(0.92f, 0.42f, 1f, 0.88f));
		recipeMarkerText = CreateText("RecipeMarkerText", new Vector2(18f, 32f), new Vector2(86f, 18f), 12);
		recipeMarkerText.fontStyle = (FontStyle)1;
		((Component)recipeMarkerBack).gameObject.SetActive(false);
		((Component)recipeMarkerText).gameObject.SetActive(false);
		Image val = CreateBar("HealthBg", new Vector2(18f, 10f), new Vector2(76f, 10f), new Color(0.02f, 0.02f, 0.04f, 0.92f));
		healthFill = CreateFill(((Component)val).transform, "HealthFill", HealthBarColor);
		TryApplySprite(healthFill, "UI/RollRoll/InGame/minimi-ui-gauge-own", preserveAspect: false);
		healthFillRect = (((Object)(object)healthFill != (Object)null) ? ((Graphic)healthFill).rectTransform : null);
		Image val2 = CreateBar("ManaBg", new Vector2(18f, 0f), new Vector2(76f, 5f), new Color(0.02f, 0.02f, 0.04f, 0.86f));
		manaFill = CreateFill(((Component)val2).transform, "ManaFill", ManaBarColor);
		TryApplySprite(manaFill, "UI/RollRoll/InGame/mana-gauge", preserveAspect: false);
		manaFillRect = (((Object)(object)manaFill != (Object)null) ? ((Graphic)manaFill).rectTransform : null);
		Configure(displayName, color, grade);
		SetBarFill(healthFillRect, currentHealth01);
		SetBarFill(manaFillRect, currentMana01);
	}

	private Text CreateText(string name, Vector2 anchoredPosition, Vector2 size, int fontSize)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name);
		val.transform.SetParent(((Component)this).transform, false);
		RectTransform val2 = val.AddComponent<RectTransform>();
		val2.anchoredPosition = anchoredPosition;
		val2.sizeDelta = size;
		Text val3 = val.AddComponent<Text>();
		val3.font = RuntimeFontProvider.GetDefaultFont();
		val3.alignment = (TextAnchor)4;
		val3.fontSize = fontSize;
		((Graphic)val3).color = Color.white;
		((Graphic)val3).raycastTarget = false;
		val3.resizeTextForBestFit = true;
		val3.resizeTextMinSize = Mathf.Max(9, fontSize - 4);
		val3.resizeTextMaxSize = fontSize;
		val3.verticalOverflow = (VerticalWrapMode)0;
		Shadow val4 = val.AddComponent<Shadow>();
		val4.effectColor = new Color(0f, 0f, 0f, 0.72f);
		val4.effectDistance = new Vector2(1.5f, -1.5f);
		Outline val5 = val.AddComponent<Outline>();
		((Shadow)val5).effectColor = new Color(0f, 0f, 0f, 0.72f);
		((Shadow)val5).effectDistance = new Vector2(1.1f, -1.1f);
		return val3;
	}

	private Image CreateBar(string name, Vector2 anchoredPosition, Vector2 size, Color color)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name);
		val.transform.SetParent(((Component)this).transform, false);
		RectTransform val2 = val.AddComponent<RectTransform>();
		val2.anchoredPosition = anchoredPosition;
		val2.sizeDelta = size;
		Image val3 = val.AddComponent<Image>();
		((Graphic)val3).color = color;
		((Graphic)val3).raycastTarget = false;
		return val3;
	}

	private void ApplyDistanceScale(Camera camera)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = ((Component)camera).transform.position - ((Component)this).transform.position;
		float num = Mathf.Sqrt(((Vector3)(ref val)).sqrMagnitude);
		float num2 = Mathf.Max(0.01f, Mathf.Clamp(num * 0.00115f, 0.009f, 0.0115f));
		float num3 = num2 / 0.01f;
		if (Mathf.Abs(((Component)this).transform.localScale.x - num3) > 0.0005f)
		{
			((Component)this).transform.localScale = Vector3.one * num3;
		}
	}

	private static Sprite LoadSprite(string resourcePath)
	{
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		if (SpriteCache.TryGetValue(resourcePath, out var value) && (Object)(object)value != (Object)null && (Object)(object)value.texture != (Object)null)
		{
			return value;
		}
		SpriteCache.Remove(resourcePath);
		Texture2D val = Resources.Load<Texture2D>(resourcePath);
		if ((Object)(object)val == (Object)null)
		{
			return null;
		}
		Sprite val2 = Sprite.Create(val, new Rect(0f, 0f, (float)((Texture)val).width, (float)((Texture)val).height), new Vector2(0.5f, 0.5f), 100f);
		SpriteCache[resourcePath] = val2;
		return val2;
	}

	private static void TryApplySprite(Image image, string resourcePath, bool preserveAspect)
	{
		if (!((Object)(object)image == (Object)null))
		{
			Sprite val = LoadSprite(resourcePath);
			if (!((Object)(object)val == (Object)null))
			{
				image.sprite = val;
				image.type = (Type)0;
				image.preserveAspect = preserveAspect;
			}
		}
	}

	private Image CreateFill(Transform parent, string name, Color color)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name);
		val.transform.SetParent(parent, false);
		RectTransform val2 = val.AddComponent<RectTransform>();
		val2.anchorMin = new Vector2(0f, 0f);
		val2.anchorMax = new Vector2(1f, 1f);
		val2.offsetMin = Vector2.zero;
		val2.offsetMax = Vector2.zero;
		Image val3 = val.AddComponent<Image>();
		val3.type = (Type)0;
		((Graphic)val3).color = color;
		((Graphic)val3).raycastTarget = false;
		return val3;
	}

	private void SetBarFill(RectTransform rect, float amount)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)rect == (Object)null))
		{
			float num = Mathf.Clamp01(amount);
			Vector2 anchorMax = rect.anchorMax;
			if (!(Mathf.Abs(anchorMax.x - num) <= 0.0005f))
			{
				anchorMax.x = num;
				rect.anchorMax = anchorMax;
			}
		}
	}

	private void OnDestroy()
	{
		SharedFloatingCombatCanvas.Unregister(ownerTargetId, this);
	}
}
