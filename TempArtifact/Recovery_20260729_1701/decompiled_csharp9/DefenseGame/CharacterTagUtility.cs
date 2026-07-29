using System.Collections.Generic;
using UnityEngine;

namespace DefenseGame
{
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
				tags.Add((seed % 2 == 0) ? CharacterTag.Light : CharacterTag.Void);
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
			return tag switch
			{
				CharacterTag.Flame => "화염", 
				CharacterTag.Frost => "냉기", 
				CharacterTag.Storm => "폭풍", 
				CharacterTag.Nature => "자연", 
				CharacterTag.Gear => "기계", 
				CharacterTag.Void => "공허", 
				CharacterTag.Light => "빛", 
				CharacterTag.Shadow => "그림자", 
				CharacterTag.Spirit => "정령", 
				CharacterTag.Steel => "강철", 
				_ => tag.ToString(), 
			};
		}

		private static CharacterTag GetRoleTag(CharacterRole role)
		{
			return role switch
			{
				CharacterRole.Vanguard => CharacterTag.Steel, 
				CharacterRole.Ranger => CharacterTag.Storm, 
				CharacterRole.Mage => CharacterTag.Flame, 
				CharacterRole.Support => CharacterTag.Light, 
				CharacterRole.Assassin => CharacterTag.Shadow, 
				CharacterRole.Summoner => CharacterTag.Spirit, 
				_ => CharacterTag.Gear, 
			};
		}

		private static CharacterTag GetElementTag(int seed)
		{
			return (Mathf.Abs(seed) % 6) switch
			{
				0 => CharacterTag.Flame, 
				1 => CharacterTag.Frost, 
				2 => CharacterTag.Nature, 
				3 => CharacterTag.Gear, 
				4 => CharacterTag.Void, 
				_ => CharacterTag.Storm, 
			};
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
