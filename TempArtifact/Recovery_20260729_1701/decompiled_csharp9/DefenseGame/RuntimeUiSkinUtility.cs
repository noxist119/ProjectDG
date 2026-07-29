using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseGame
{
	public static class RuntimeUiSkinUtility
	{
		private static Sprite roundedPanelSprite;

		private static Sprite circleSprite;

		private static readonly Dictionary<Sprite, Sprite> slicedSpriteCache = new Dictionary<Sprite, Sprite>();

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStaticSpriteCache()
		{
			roundedPanelSprite = null;
			circleSprite = null;
			slicedSpriteCache.Clear();
		}

		public static Font ResolveFont(GamePresentationConfig presentationConfig)
		{
			if (presentationConfig != null)
			{
				if (presentationConfig.uiSkin != null && presentationConfig.uiSkin.fontOverride != null)
				{
					return presentationConfig.uiSkin.fontOverride;
				}
				if (presentationConfig.uiFont != null)
				{
					return presentationConfig.uiFont;
				}
			}
			return RuntimeFontProvider.GetDefaultFont();
		}

		public static void ApplyImageSkin(Image image, UiSkinResources skin, string elementName, bool isButton, bool rounded, bool circle = false)
		{
			if ((Object)(object)image == null)
			{
				return;
			}
			Sprite sprite = ResolveSprite(skin, elementName, isButton, rounded, circle);
			if (!(sprite == null))
			{
				if (rounded && !HasBorder(sprite))
				{
					sprite = GetRuntimeSlicedSprite(sprite);
				}
				image.sprite = sprite;
				image.type = (Type)(rounded ? 1 : 0);
				image.preserveAspect = skin != null && skin.preserveAspect;
			}
		}

		public static Sprite ResolveIconSprite(UiSkinResources skin, string elementName)
		{
			if (skin == null)
			{
				return null;
			}
			string key = (string.IsNullOrWhiteSpace(elementName) ? string.Empty : elementName.ToLowerInvariant());
			if (key.Contains("gold") || key.Contains("coin") || key == "g")
			{
				return skin.coinIconSprite;
			}
			if (key.Contains("gem") || key.Contains("diamond") || key.Contains("dia"))
			{
				return skin.gemIconSprite;
			}
			if (key.Contains("life") || key.Contains("heart") || key.Contains("hp"))
			{
				return skin.heartIconSprite;
			}
			if (key.Contains("close") || key.Contains("cancel") || key.Contains("exit") || key == "x")
			{
				return skin.closeIconSprite;
			}
			if (key.Contains("mission") || key.Contains("quest"))
			{
				return (skin.missionIconSprite != null) ? skin.missionIconSprite : skin.checkIconSprite;
			}
			if (key.Contains("shop") || key.Contains("store") || key.Contains("offer"))
			{
				return (skin.shopIconSprite != null) ? skin.shopIconSprite : skin.coinIconSprite;
			}
			if (key.Contains("augment") || key.Contains("perk") || key.Contains("buff"))
			{
				return (skin.augmentIconSprite != null) ? skin.augmentIconSprite : skin.missionIconSprite;
			}
			if (key.Contains("check") || key.Contains("confirm") || key.Contains("ok") || key.Contains("complete"))
			{
				return skin.checkIconSprite;
			}
			if (key.Contains("plus") || key.Contains("add") || key.Contains("summon"))
			{
				return skin.plusIconSprite;
			}
			if (key.Contains("info") || key.Contains("help"))
			{
				return skin.infoIconSprite;
			}
			if (key.Contains("deck") || key.Contains("loadout") || key.Contains("roster") || key.Contains("db"))
			{
				return (skin.deckIconSprite != null) ? skin.deckIconSprite : skin.heroIconSprite;
			}
			if (key.Contains("hero") || key.Contains("character"))
			{
				return skin.heroIconSprite;
			}
			if (key.Contains("battle") || key.Contains("play") || key.Contains("fight"))
			{
				return skin.battleIconSprite;
			}
			return null;
		}

		public static bool ApplyIconSkin(Image image, UiSkinResources skin, string elementName)
		{
			if ((Object)(object)image == null)
			{
				return false;
			}
			Sprite sprite = ResolveIconSprite(skin, elementName);
			if (sprite == null)
			{
				return false;
			}
			image.sprite = sprite;
			image.type = (Type)0;
			image.preserveAspect = true;
			return true;
		}

		public static Sprite GetRoundedPanelSprite()
		{
			if (roundedPanelSprite == null)
			{
				roundedPanelSprite = CreateRuntimeSprite("RuntimeRoundedPanel", 64, 64, 18f);
			}
			return roundedPanelSprite;
		}

		public static Sprite GetCircleSprite()
		{
			if (circleSprite == null)
			{
				circleSprite = CreateRuntimeSprite("RuntimeCircle", 64, 64, 32f);
			}
			return circleSprite;
		}

		public static Color ResolveReadableTextColor(Transform parent, Color requestedColor, UiSkinResources skin = null)
		{
			if (requestedColor.a <= 0f || GetLuminance(requestedColor) >= 0.55f)
			{
				return requestedColor;
			}
			Image backdrop = FindNearestBackdrop(parent);
			bool isDarkBackdrop = (Object)(object)backdrop == null || (((Graphic)backdrop).color.a > 0.2f && GetLuminance(((Graphic)backdrop).color) < 0.72f);
			bool skinMayUseDarkSprite = skin != null && (Object)(object)backdrop != null && backdrop.sprite != null;
			if (!isDarkBackdrop && !skinMayUseDarkSprite)
			{
				return requestedColor;
			}
			Color readable = Color.Lerp(requestedColor, Color.white, isDarkBackdrop ? 0.94f : 0.86f);
			readable.a = requestedColor.a;
			return readable;
		}

		public static void ApplyReadableTextColor(Text text, Color requestedColor, UiSkinResources skin = null)
		{
			if (!((Object)(object)text == null))
			{
				((Graphic)text).color = ResolveReadableTextColor(((Component)(object)text).transform.parent, requestedColor, skin);
			}
		}

		private static Sprite ResolveSprite(UiSkinResources skin, string elementName, bool isButton, bool rounded, bool circle)
		{
			if (circle)
			{
				if (skin != null && skin.circleSprite != null)
				{
					return skin.circleSprite;
				}
				return GetCircleSprite();
			}
			string key = (string.IsNullOrWhiteSpace(elementName) ? string.Empty : elementName.ToLowerInvariant());
			if (ShouldUsePlainRuntimeShape(key))
			{
				return rounded ? GetRoundedPanelSprite() : null;
			}
			if (skin != null)
			{
				if ((key.Contains("card") || key.Contains("badge") || key.Contains("chip")) && skin.cardSprite != null)
				{
					return skin.cardSprite;
				}
				if (isButton)
				{
					Sprite buttonVariant = ResolveButtonSprite(skin, key);
					if (buttonVariant != null)
					{
						return buttonVariant;
					}
				}
				if (key.Contains("modal") && skin.modalSprite != null)
				{
					return skin.modalSprite;
				}
				if (key.Contains("portrait") && skin.portraitSprite != null)
				{
					return skin.portraitSprite;
				}
				if (key.Contains("accent") && skin.accentSprite != null)
				{
					return skin.accentSprite;
				}
				if ((key.Contains("icon") || key.Contains("plate")) && skin.iconPlateSprite != null)
				{
					return skin.iconPlateSprite;
				}
				if (key.Contains("fill") && skin.progressFillSprite != null)
				{
					return skin.progressFillSprite;
				}
				if ((key.Contains("progress") || key.Contains("bar")) && skin.progressBarSprite != null)
				{
					return skin.progressBarSprite;
				}
				if (skin.panelSprite != null && rounded)
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
			if (skin == null)
			{
				return null;
			}
			if ((key.Contains("close") || key.Contains("cancel") || key.Contains("danger")) && skin.dangerButtonSprite != null)
			{
				return skin.dangerButtonSprite;
			}
			if ((key.Contains("summon") || key.Contains("continue") || key.Contains("confirm") || key.Contains("claim")) && skin.positiveButtonSprite != null)
			{
				return skin.positiveButtonSprite;
			}
			if ((key.Contains("loadout") || key.Contains("deck") || key.Contains("warning")) && skin.warningButtonSprite != null)
			{
				return skin.warningButtonSprite;
			}
			if ((key.Contains("battle") || key.Contains("result") || key.Contains("primary")) && skin.primaryButtonSprite != null)
			{
				return skin.primaryButtonSprite;
			}
			return skin.buttonSprite;
		}

		private static Image FindNearestBackdrop(Transform parent)
		{
			Transform current = parent;
			while (current != null)
			{
				Image image = current.GetComponent<Image>();
				if ((Object)(object)image != null)
				{
					return image;
				}
				current = current.parent;
			}
			return null;
		}

		private static float GetLuminance(Color color)
		{
			return color.r * 0.299f + color.g * 0.587f + color.b * 0.114f;
		}

		private static bool HasBorder(Sprite sprite)
		{
			return sprite != null && sprite.border.sqrMagnitude > 0.0001f;
		}

		private static Sprite GetRuntimeSlicedSprite(Sprite source)
		{
			if (source == null)
			{
				return null;
			}
			if (slicedSpriteCache.TryGetValue(source, out var cached))
			{
				if (IsSpriteUsable(cached))
				{
					return cached;
				}
				slicedSpriteCache.Remove(source);
			}
			Rect rect = source.rect;
			float minSize = Mathf.Min(rect.width, rect.height);
			float border = Mathf.Clamp(minSize * 0.24f, 12f, minSize * 0.48f);
			Sprite sliced = Sprite.Create(pivot: new Vector2(source.pivot.x / rect.width, source.pivot.y / rect.height), texture: source.texture, rect: rect, pixelsPerUnit: source.pixelsPerUnit, extrude: 0u, meshType: SpriteMeshType.FullRect, border: new Vector4(border, border, border, border));
			sliced.name = source.name + "_RuntimeSliced";
			slicedSpriteCache[source] = sliced;
			return sliced;
		}

		private static bool IsSpriteUsable(Sprite sprite)
		{
			return sprite != null && sprite.texture != null;
		}

		private static Sprite CreateRuntimeSprite(string name, int width, int height, float radius)
		{
			Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, mipChain: false);
			texture.name = name;
			texture.wrapMode = TextureWrapMode.Clamp;
			Color[] pixels = new Color[width * height];
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					float nearestX = Mathf.Clamp(x, radius, (float)width - radius - 1f);
					float nearestY = Mathf.Clamp(y, radius, (float)height - radius - 1f);
					float distance = Vector2.Distance(new Vector2(x, y), new Vector2(nearestX, nearestY));
					float alpha = Mathf.Clamp01(radius + 0.5f - distance);
					pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
				}
			}
			texture.SetPixels(pixels);
			texture.Apply();
			return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
		}
	}
}
