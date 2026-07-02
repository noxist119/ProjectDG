using System.Collections.Generic;
using UnityEngine;

namespace DefenseGame
{
    public enum CharacterTag
    {
        Flame = 0,
        Frost = 1,
        Storm = 2,
        Nature = 3,
        Gear = 4,
        Void = 5,
        Light = 6,
        Shadow = 7,
        Spirit = 8,
        Steel = 9
    }

    public static class CharacterTagUtility
    {
        public static List<CharacterTag> BuildDefaultTags(CharacterRole role, int seed, CharacterGrade grade)
        {
            List<CharacterTag> tags = new List<CharacterTag>
            {
                GetRoleTag(role),
                GetElementTag(seed)
            };

            if (grade >= CharacterGrade.Legendary)
            {
                tags.Add(seed % 2 == 0 ? CharacterTag.Light : CharacterTag.Void);
            }

            return Deduplicate(tags);
        }

        public static List<CharacterTag> ResolveTags(CharacterDefinition definition)
        {
            if (definition == null)
            {
                return new List<CharacterTag>();
            }

            if (definition.tags != null && definition.tags.Count > 0)
            {
                return Deduplicate(definition.tags);
            }

            int seed = Mathf.Abs((definition.id ?? definition.displayName ?? string.Empty).GetHashCode());
            return BuildDefaultTags(definition.role, seed, definition.grade);
        }

        public static string GetDisplayName(CharacterTag tag)
        {
            switch (tag)
            {
                case CharacterTag.Flame: return "화염";
                case CharacterTag.Frost: return "냉기";
                case CharacterTag.Storm: return "폭풍";
                case CharacterTag.Nature: return "자연";
                case CharacterTag.Gear: return "기계";
                case CharacterTag.Void: return "공허";
                case CharacterTag.Light: return "빛";
                case CharacterTag.Shadow: return "그림자";
                case CharacterTag.Spirit: return "정령";
                case CharacterTag.Steel: return "강철";
                default: return tag.ToString();
            }
        }

        private static CharacterTag GetRoleTag(CharacterRole role)
        {
            switch (role)
            {
                case CharacterRole.Vanguard: return CharacterTag.Steel;
                case CharacterRole.Ranger: return CharacterTag.Storm;
                case CharacterRole.Mage: return CharacterTag.Flame;
                case CharacterRole.Support: return CharacterTag.Light;
                case CharacterRole.Assassin: return CharacterTag.Shadow;
                case CharacterRole.Summoner: return CharacterTag.Spirit;
                default: return CharacterTag.Gear;
            }
        }

        private static CharacterTag GetElementTag(int seed)
        {
            switch (Mathf.Abs(seed) % 6)
            {
                case 0: return CharacterTag.Flame;
                case 1: return CharacterTag.Frost;
                case 2: return CharacterTag.Nature;
                case 3: return CharacterTag.Gear;
                case 4: return CharacterTag.Void;
                default: return CharacterTag.Storm;
            }
        }

        private static List<CharacterTag> Deduplicate(IList<CharacterTag> source)
        {
            List<CharacterTag> result = new List<CharacterTag>();
            if (source == null)
            {
                return result;
            }

            for (int i = 0; i < source.Count; i++)
            {
                if (!result.Contains(source[i]))
                {
                    result.Add(source[i]);
                }
            }

            return result;
        }
    }
}
