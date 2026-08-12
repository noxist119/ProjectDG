using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseGame
{
    public static class RollRollUiResource
    {
        private const string ResourceRoot = "UI/RollRoll/";
        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticSpriteCache()
        {
            SpriteCache.Clear();
        }

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
            { "hero_54", "Minimi/minimi_gargoyle" },
            { "hero_55", "Minimi/minimi_amor" },
            { "hero_56", "Minimi/minimi_auto" },
            { "hero_57", "Minimi/minimi_broken" }
        };

        public static Sprite LoadSprite(string resourcePath, bool sliced = false)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return null;
            }

            string normalizedPath = NormalizePath(resourcePath);
            string cacheKey = normalizedPath + (sliced ? "#sliced" : "#simple");
            if (SpriteCache.TryGetValue(cacheKey, out Sprite cachedSprite))
            {
                if (cachedSprite != null && cachedSprite.texture != null)
                {
                    return cachedSprite;
                }

                SpriteCache.Remove(cacheKey);
            }

            Texture2D texture = Resources.Load<Texture2D>(normalizedPath);
            if (texture == null)
            {
                return null;
            }

            Vector4 border = sliced ? ResolveBorder(texture) : Vector4.zero;
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                border);
            sprite.name = texture.name + (sliced ? "_Sliced" : "_Simple");
            SpriteCache[cacheKey] = sprite;
            return sprite;
        }

        public static bool TryApplySprite(Image image, string resourcePath, Image.Type type, bool preserveAspect)
        {
            if (image == null)
            {
                return false;
            }

            Sprite sprite = LoadSprite(resourcePath, type == Image.Type.Sliced);
            if (sprite == null)
            {
                return false;
            }

            image.sprite = sprite;
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
            if (CharacterSpriteById.TryGetValue(key, out string resourcePath))
            {
                return LoadSprite(resourcePath);
            }

            return LoadSprite("Minimi/not-found");
        }

        private static string NormalizePath(string resourcePath)
        {
            string normalized = resourcePath.Replace("\\", "/").Trim();
            if (normalized.EndsWith(".png"))
            {
                normalized = normalized.Substring(0, normalized.Length - 4);
            }

            return normalized.StartsWith(ResourceRoot) ? normalized : ResourceRoot + normalized;
        }

        private static string ResolveElementResourcePath(string elementName, bool isButton)
        {
            string key = string.IsNullOrWhiteSpace(elementName)
                ? string.Empty
                : elementName.ToLowerInvariant();

            if (isButton)
            {
                if (key.Contains("close") || key.Contains("cancel"))
                {
                    return "Common/button-common-orange-164";
                }

                if (key.Contains("battle") || key.Contains("ten") || key.Contains("continue"))
                {
                    return "Common/button-common-orange-164";
                }

                if (key.Contains("single") || key.Contains("shop") || key.Contains("testdiamond"))
                {
                    return "Common/button-common-green-164";
                }

                if (key.Contains("collection") || key.Contains("loadout") || key.Contains("preset") || key.Contains("page") || key.Contains("mode"))
                {
                    return "Common/button-common-gray-blue-164";
                }

                return "Common/button-common-gray-blue-164";
            }

            if (key.Contains("charactercard") || key.Contains("featuredcard") || key.Contains("deckcard"))
            {
                return "Collection/collection-minimi-card-background";
            }

            if (key.Contains("rostercard"))
            {
                return "Common/inventory-minimi-itemrenderer-background";
            }

            if (key.Contains("collectionmodal") || key.Contains("shopmodal") || key.Contains("loadoutmodal")
                || key.Contains("lobbymodal") || key.Contains("resultmodal") || key.Contains("matchmakingmodal"))
            {
                return "Common/common-popup-background";
            }

            if (key.Contains("detailpanel") || key.Contains("drawresults") || key.Contains("rewardpanel"))
            {
                return "CharacterInfo/minimi-info-popup-detail-info-background";
            }

            if (key == "header" || key.Contains("shopheader") || key.Contains("loadoutheader") || key.Contains("selectedgradeback"))
            {
                return "CharacterInfo/minimi-info-popup-title-background";
            }

            if (key.Contains("statrow"))
            {
                return "CharacterInfo/minimi-info-popup-detail-info-background";
            }

            if (key.Contains("portrait"))
            {
                return "Common/inventory-minimi-itemrenderer-background";
            }

            if (key.Contains("accent"))
            {
                return "Collection/collection-minimi-card-parts-track";
            }

            if (key.Contains("chesticon"))
            {
                return "Lobby/top-panel-icon-chest-empty";
            }

            if (key.Contains("corefactory") || key.Contains("cardgridpanel"))
            {
                return "Lobby/000-main-lobby-battle-my-deck-list-background";
            }

            return null;
        }

        private static Vector4 ResolveBorder(Texture2D texture)
        {
            float minSize = Mathf.Min(texture.width, texture.height);
            float border = Mathf.Clamp(minSize * 0.24f, 8f, minSize * 0.45f);
            return new Vector4(border, border, border, border);
        }
    }
}
