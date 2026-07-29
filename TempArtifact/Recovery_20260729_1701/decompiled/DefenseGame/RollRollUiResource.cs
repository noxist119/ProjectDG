using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseGame;

public static class RollRollUiResource
{
	private const string ResourceRoot = "UI/RollRoll/";

	private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

	private static readonly Dictionary<string, string> CharacterSpriteById = new Dictionary<string, string>
	{
		{ "hero_01", "Minimi/minimi_fire" },
		{ "hero_02", "Minimi/minimi_heal" },
		{ "hero_03", "Minimi/minimi_ice" },
		{ "hero_04", "Minimi/minimi_poison" },
		{ "hero_05", "Minimi/minimi_shield" },
		{ "hero_06", "Minimi/minimi_assault" },
		{ "hero_07", "Minimi/minimi_death" },
		{ "hero_08", "Minimi/minimi_medusa" },
		{ "hero_09", "Minimi/minimi_spear" },
		{ "hero_10", "Minimi/minimi_wind" },
		{ "hero_11", "Minimi/minimi_absorption" },
		{ "hero_12", "Minimi/minimi_assassin" },
		{ "hero_13", "Minimi/minimi_battery" },
		{ "hero_14", "Minimi/minimi_light" },
		{ "hero_21", "Minimi/minimi_wolf" },
		{ "hero_22", "Minimi/minimi_death_t" },
		{ "hero_23", "Minimi/minimi_wolf_t" },
		{ "hero_31", "Minimi/minimi_combat" },
		{ "hero_32", "Minimi/minimi_wolf" },
		{ "hero_33", "Minimi/minimi_infection" },
		{ "hero_51", "Minimi/minimi_lightning" },
		{ "hero_52", "Minimi/minimi_meteor" },
		{ "hero_53", "Minimi/minimi_minigun" },
		{ "hero_54", "Minimi/minimi_gargoyle" }
	};

	[RuntimeInitializeOnLoadMethod(/*Could not decode attribute arguments.*/)]
	private static void ResetStaticSpriteCache()
	{
		SpriteCache.Clear();
	}

	public static Sprite LoadSprite(string resourcePath, bool sliced = false)
	{
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		if (string.IsNullOrWhiteSpace(resourcePath))
		{
			return null;
		}
		string text = NormalizePath(resourcePath);
		string key = text + (sliced ? "#sliced" : "#simple");
		if (SpriteCache.TryGetValue(key, out var value))
		{
			if ((Object)(object)value != (Object)null && (Object)(object)value.texture != (Object)null)
			{
				return value;
			}
			SpriteCache.Remove(key);
		}
		Texture2D val = Resources.Load<Texture2D>(text);
		if ((Object)(object)val == (Object)null)
		{
			return null;
		}
		Vector4 val2 = (sliced ? ResolveBorder(val) : Vector4.zero);
		Sprite val3 = Sprite.Create(val, new Rect(0f, 0f, (float)((Texture)val).width, (float)((Texture)val).height), new Vector2(0.5f, 0.5f), 100f, 0u, (SpriteMeshType)0, val2);
		((Object)val3).name = ((Object)val).name + (sliced ? "_Sliced" : "_Simple");
		SpriteCache[key] = val3;
		return val3;
	}

	public static bool TryApplySprite(Image image, string resourcePath, Type type, bool preserveAspect)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Invalid comparison between Unknown and I4
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)image == (Object)null)
		{
			return false;
		}
		Sprite val = LoadSprite(resourcePath, (int)type == 1);
		if ((Object)(object)val == (Object)null)
		{
			return false;
		}
		image.sprite = val;
		image.type = type;
		image.preserveAspect = preserveAspect;
		return true;
	}

	public static bool TryApplyElementSprite(Image image, string elementName, bool isButton, bool rounded)
	{
		return false;
	}

	public static Sprite ResolveCharacterSprite(CharacterDefinition definition)
	{
		if (definition == null || string.IsNullOrWhiteSpace(definition.id))
		{
			return LoadSprite("Minimi/not-found");
		}
		string key = definition.id.Trim().ToLowerInvariant();
		if (CharacterSpriteById.TryGetValue(key, out var value))
		{
			return LoadSprite(value);
		}
		return LoadSprite("Minimi/not-found");
	}

	private static string NormalizePath(string resourcePath)
	{
		string text = resourcePath.Replace("\\", "/").Trim();
		if (text.EndsWith(".png"))
		{
			text = text.Substring(0, text.Length - 4);
		}
		return text.StartsWith("UI/RollRoll/") ? text : ("UI/RollRoll/" + text);
	}

	private static string ResolveElementResourcePath(string elementName, bool isButton)
	{
		string text = (string.IsNullOrWhiteSpace(elementName) ? string.Empty : elementName.ToLowerInvariant());
		if (isButton)
		{
			if (text.Contains("close") || text.Contains("cancel"))
			{
				return "Common/button-common-orange-164";
			}
			if (text.Contains("battle") || text.Contains("ten") || text.Contains("continue"))
			{
				return "Common/button-common-orange-164";
			}
			if (text.Contains("single") || text.Contains("shop") || text.Contains("testdiamond"))
			{
				return "Common/button-common-green-164";
			}
			if (text.Contains("collection") || text.Contains("loadout") || text.Contains("preset") || text.Contains("page") || text.Contains("mode"))
			{
				return "Common/button-common-gray-blue-164";
			}
			return "Common/button-common-gray-blue-164";
		}
		if (text.Contains("charactercard") || text.Contains("featuredcard") || text.Contains("deckcard"))
		{
			return "Collection/collection-minimi-card-background";
		}
		if (text.Contains("rostercard"))
		{
			return "Common/inventory-minimi-itemrenderer-background";
		}
		if (text.Contains("collectionmodal") || text.Contains("shopmodal") || text.Contains("loadoutmodal") || text.Contains("lobbymodal") || text.Contains("resultmodal") || text.Contains("matchmakingmodal"))
		{
			return "Common/common-popup-background";
		}
		if (text.Contains("detailpanel") || text.Contains("drawresults") || text.Contains("rewardpanel"))
		{
			return "CharacterInfo/minimi-info-popup-detail-info-background";
		}
		if (text == "header" || text.Contains("shopheader") || text.Contains("loadoutheader") || text.Contains("selectedgradeback"))
		{
			return "CharacterInfo/minimi-info-popup-title-background";
		}
		if (text.Contains("statrow"))
		{
			return "CharacterInfo/minimi-info-popup-detail-info-background";
		}
		if (text.Contains("portrait"))
		{
			return "Common/inventory-minimi-itemrenderer-background";
		}
		if (text.Contains("accent"))
		{
			return "Collection/collection-minimi-card-parts-track";
		}
		if (text.Contains("chesticon"))
		{
			return "Lobby/top-panel-icon-chest-empty";
		}
		if (text.Contains("corefactory") || text.Contains("cardgridpanel"))
		{
			return "Lobby/000-main-lobby-battle-my-deck-list-background";
		}
		return null;
	}

	private static Vector4 ResolveBorder(Texture2D texture)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Min(((Texture)texture).width, ((Texture)texture).height);
		float num2 = Mathf.Clamp(num * 0.24f, 8f, num * 0.45f);
		return new Vector4(num2, num2, num2, num2);
	}
}
