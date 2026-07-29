using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseGame;

public static class RuntimeUiSkinUtility
{
	private static Sprite roundedPanelSprite;

	private static Sprite circleSprite;

	private static readonly Dictionary<Sprite, Sprite> slicedSpriteCache = new Dictionary<Sprite, Sprite>();

	[RuntimeInitializeOnLoadMethod(/*Could not decode attribute arguments.*/)]
	private static void ResetStaticSpriteCache()
	{
		roundedPanelSprite = null;
		circleSprite = null;
		slicedSpriteCache.Clear();
	}

	public static Font ResolveFont(GamePresentationConfig presentationConfig)
	{
		if ((Object)(object)presentationConfig != (Object)null)
		{
			if ((Object)(object)presentationConfig.uiSkin != (Object)null && (Object)(object)presentationConfig.uiSkin.fontOverride != (Object)null)
			{
				return presentationConfig.uiSkin.fontOverride;
			}
			if ((Object)(object)presentationConfig.uiFont != (Object)null)
			{
				return presentationConfig.uiFont;
			}
		}
		return RuntimeFontProvider.GetDefaultFont();
	}

	public static void ApplyImageSkin(Image image, UiSkinResources skin, string elementName, bool isButton, bool rounded, bool circle = false)
	{
		if ((Object)(object)image == (Object)null)
		{
			return;
		}
		Sprite val = ResolveSprite(skin, elementName, isButton, rounded, circle);
		if (!((Object)(object)val == (Object)null))
		{
			if (rounded && !HasBorder(val))
			{
				val = GetRuntimeSlicedSprite(val);
			}
			image.sprite = val;
			image.type = (Type)(rounded ? 1 : 0);
			image.preserveAspect = (Object)(object)skin != (Object)null && skin.preserveAspect;
		}
	}

	public static Sprite ResolveIconSprite(UiSkinResources skin, string elementName)
	{
		if ((Object)(object)skin == (Object)null)
		{
			return null;
		}
		string text = (string.IsNullOrWhiteSpace(elementName) ? string.Empty : elementName.ToLowerInvariant());
		if (text.Contains("gold") || text.Contains("coin") || text == "g")
		{
			return skin.coinIconSprite;
		}
		if (text.Contains("gem") || text.Contains("diamond") || text.Contains("dia"))
		{
			return skin.gemIconSprite;
		}
		if (text.Contains("life") || text.Contains("heart") || text.Contains("hp"))
		{
			return skin.heartIconSprite;
		}
		if (text.Contains("close") || text.Contains("cancel") || text.Contains("exit") || text == "x")
		{
			return skin.closeIconSprite;
		}
		if (text.Contains("mission") || text.Contains("quest"))
		{
			return ((Object)(object)skin.missionIconSprite != (Object)null) ? skin.missionIconSprite : skin.checkIconSprite;
		}
		if (text.Contains("shop") || text.Contains("store") || text.Contains("offer"))
		{
			return ((Object)(object)skin.shopIconSprite != (Object)null) ? skin.shopIconSprite : skin.coinIconSprite;
		}
		if (text.Contains("augment") || text.Contains("perk") || text.Contains("buff"))
		{
			return ((Object)(object)skin.augmentIconSprite != (Object)null) ? skin.augmentIconSprite : skin.missionIconSprite;
		}
		if (text.Contains("check") || text.Contains("confirm") || text.Contains("ok") || text.Contains("complete"))
		{
			return skin.checkIconSprite;
		}
		if (text.Contains("plus") || text.Contains("add") || text.Contains("summon"))
		{
			return skin.plusIconSprite;
		}
		if (text.Contains("info") || text.Contains("help"))
		{
			return skin.infoIconSprite;
		}
		if (text.Contains("deck") || text.Contains("loadout") || text.Contains("roster") || text.Contains("db"))
		{
			return ((Object)(object)skin.deckIconSprite != (Object)null) ? skin.deckIconSprite : skin.heroIconSprite;
		}
		if (text.Contains("hero") || text.Contains("character"))
		{
			return skin.heroIconSprite;
		}
		if (text.Contains("battle") || text.Contains("play") || text.Contains("fight"))
		{
			return skin.battleIconSprite;
		}
		return null;
	}

	public static bool ApplyIconSkin(Image image, UiSkinResources skin, string elementName)
	{
		if ((Object)(object)image == (Object)null)
		{
			return false;
		}
		Sprite val = ResolveIconSprite(skin, elementName);
		if ((Object)(object)val == (Object)null)
		{
			return false;
		}
		image.sprite = val;
		image.type = (Type)0;
		image.preserveAspect = true;
		return true;
	}

	public static Sprite GetRoundedPanelSprite()
	{
		if ((Object)(object)roundedPanelSprite == (Object)null)
		{
			roundedPanelSprite = CreateRuntimeSprite("RuntimeRoundedPanel", 64, 64, 18f);
		}
		return roundedPanelSprite;
	}

	public static Sprite GetCircleSprite()
	{
		if ((Object)(object)circleSprite == (Object)null)
		{
			circleSprite = CreateRuntimeSprite("RuntimeCircle", 64, 64, 32f);
		}
		return circleSprite;
	}

	public static Color ResolveReadableTextColor(Transform parent, Color requestedColor, UiSkinResources skin = null)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		if (requestedColor.a <= 0f || GetLuminance(requestedColor) >= 0.55f)
		{
			return requestedColor;
		}
		Image val = FindNearestBackdrop(parent);
		bool flag = (Object)(object)val == (Object)null || (((Graphic)val).color.a > 0.2f && GetLuminance(((Graphic)val).color) < 0.72f);
		bool flag2 = (Object)(object)skin != (Object)null && (Object)(object)val != (Object)null && (Object)(object)val.sprite != (Object)null;
		if (!flag && !flag2)
		{
			return requestedColor;
		}
		Color result = Color.Lerp(requestedColor, Color.white, flag ? 0.94f : 0.86f);
		result.a = requestedColor.a;
		return result;
	}

	public static void ApplyReadableTextColor(Text text, Color requestedColor, UiSkinResources skin = null)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)text == (Object)null))
		{
			((Graphic)text).color = ResolveReadableTextColor(((Component)text).transform.parent, requestedColor, skin);
		}
	}

	private static Sprite ResolveSprite(UiSkinResources skin, string elementName, bool isButton, bool rounded, bool circle)
	{
		if (circle)
		{
			if ((Object)(object)skin != (Object)null && (Object)(object)skin.circleSprite != (Object)null)
			{
				return skin.circleSprite;
			}
			return GetCircleSprite();
		}
		string text = (string.IsNullOrWhiteSpace(elementName) ? string.Empty : elementName.ToLowerInvariant());
		if (ShouldUsePlainRuntimeShape(text))
		{
			return rounded ? GetRoundedPanelSprite() : null;
		}
		if ((Object)(object)skin != (Object)null)
		{
			if ((text.Contains("card") || text.Contains("badge") || text.Contains("chip")) && (Object)(object)skin.cardSprite != (Object)null)
			{
				return skin.cardSprite;
			}
			if (isButton)
			{
				Sprite val = ResolveButtonSprite(skin, text);
				if ((Object)(object)val != (Object)null)
				{
					return val;
				}
			}
			if (text.Contains("modal") && (Object)(object)skin.modalSprite != (Object)null)
			{
				return skin.modalSprite;
			}
			if (text.Contains("portrait") && (Object)(object)skin.portraitSprite != (Object)null)
			{
				return skin.portraitSprite;
			}
			if (text.Contains("accent") && (Object)(object)skin.accentSprite != (Object)null)
			{
				return skin.accentSprite;
			}
			if ((text.Contains("icon") || text.Contains("plate")) && (Object)(object)skin.iconPlateSprite != (Object)null)
			{
				return skin.iconPlateSprite;
			}
			if (text.Contains("fill") && (Object)(object)skin.progressFillSprite != (Object)null)
			{
				return skin.progressFillSprite;
			}
			if ((text.Contains("progress") || text.Contains("bar")) && (Object)(object)skin.progressBarSprite != (Object)null)
			{
				return skin.progressBarSprite;
			}
			if ((Object)(object)skin.panelSprite != (Object)null && rounded)
			{
				return skin.panelSprite;
			}
		}
		return rounded ? GetRoundedPanelSprite() : null;
	}

	private static bool ShouldUsePlainRuntimeShape(string key)
	{
		if (string.IsNullOrEmpty(key))
		{
			return false;
		}
		return key.Contains("back") || key.Contains("banner") || key.Contains("core") || key.Contains("factory") || key.Contains("glow") || key.Contains("line") || key.Contains("pulse") || key.Contains("ribbon") || key.Contains("stage") || key.Contains("strip");
	}

	private static Sprite ResolveButtonSprite(UiSkinResources skin, string key)
	{
		if ((Object)(object)skin == (Object)null)
		{
			return null;
		}
		if ((key.Contains("close") || key.Contains("cancel") || key.Contains("danger")) && (Object)(object)skin.dangerButtonSprite != (Object)null)
		{
			return skin.dangerButtonSprite;
		}
		if ((key.Contains("summon") || key.Contains("continue") || key.Contains("confirm") || key.Contains("claim")) && (Object)(object)skin.positiveButtonSprite != (Object)null)
		{
			return skin.positiveButtonSprite;
		}
		if ((key.Contains("loadout") || key.Contains("deck") || key.Contains("warning")) && (Object)(object)skin.warningButtonSprite != (Object)null)
		{
			return skin.warningButtonSprite;
		}
		if ((key.Contains("battle") || key.Contains("result") || key.Contains("primary")) && (Object)(object)skin.primaryButtonSprite != (Object)null)
		{
			return skin.primaryButtonSprite;
		}
		return skin.buttonSprite;
	}

	private static Image FindNearestBackdrop(Transform parent)
	{
		Transform val = parent;
		while ((Object)(object)val != (Object)null)
		{
			Image component = ((Component)val).GetComponent<Image>();
			if ((Object)(object)component != (Object)null)
			{
				return component;
			}
			val = val.parent;
		}
		return null;
	}

	private static float GetLuminance(Color color)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		return color.r * 0.299f + color.g * 0.587f + color.b * 0.114f;
	}

	private static bool HasBorder(Sprite sprite)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		int result;
		if ((Object)(object)sprite != (Object)null)
		{
			Vector4 border = sprite.border;
			result = ((((Vector4)(ref border)).sqrMagnitude > 0.0001f) ? 1 : 0);
		}
		else
		{
			result = 0;
		}
		return (byte)result != 0;
	}

	private static Sprite GetRuntimeSlicedSprite(Sprite source)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)source == (Object)null)
		{
			return null;
		}
		if (slicedSpriteCache.TryGetValue(source, out var value))
		{
			if (IsSpriteUsable(value))
			{
				return value;
			}
			slicedSpriteCache.Remove(source);
		}
		Rect rect = source.rect;
		float num = Mathf.Min(((Rect)(ref rect)).width, ((Rect)(ref rect)).height);
		float num2 = Mathf.Clamp(num * 0.24f, 12f, num * 0.48f);
		Vector2 val = default(Vector2);
		((Vector2)(ref val))._002Ector(source.pivot.x / ((Rect)(ref rect)).width, source.pivot.y / ((Rect)(ref rect)).height);
		Sprite val2 = Sprite.Create(source.texture, rect, val, source.pixelsPerUnit, 0u, (SpriteMeshType)0, new Vector4(num2, num2, num2, num2));
		((Object)val2).name = ((Object)source).name + "_RuntimeSliced";
		slicedSpriteCache[source] = val2;
		return val2;
	}

	private static bool IsSpriteUsable(Sprite sprite)
	{
		return (Object)(object)sprite != (Object)null && (Object)(object)sprite.texture != (Object)null;
	}

	private static Sprite CreateRuntimeSprite(string name, int width, int height, float radius)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected O, but got Unknown
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		Texture2D val = new Texture2D(width, height, (TextureFormat)5, false);
		((Object)val).name = name;
		((Texture)val).wrapMode = (TextureWrapMode)1;
		Color[] array = (Color[])(object)new Color[width * height];
		for (int i = 0; i < height; i++)
		{
			for (int j = 0; j < width; j++)
			{
				float num = Mathf.Clamp((float)j, radius, (float)width - radius - 1f);
				float num2 = Mathf.Clamp((float)i, radius, (float)height - radius - 1f);
				float num3 = Vector2.Distance(new Vector2((float)j, (float)i), new Vector2(num, num2));
				float num4 = Mathf.Clamp01(radius + 0.5f - num3);
				array[i * width + j] = new Color(1f, 1f, 1f, num4);
			}
		}
		val.SetPixels(array);
		val.Apply();
		return Sprite.Create(val, new Rect(0f, 0f, (float)width, (float)height), new Vector2(0.5f, 0.5f), 100f, 0u, (SpriteMeshType)0, new Vector4(radius, radius, radius, radius));
	}
}
