using UnityEngine;

namespace DefenseGame;

public static class CharacterGradeUtility
{
	public static string GetDisplayName(CharacterGrade grade)
	{
		return grade switch
		{
			CharacterGrade.Normal => "일반", 
			CharacterGrade.Rare => "레어", 
			CharacterGrade.Epic => "희귀", 
			CharacterGrade.Legendary => "전설", 
			CharacterGrade.Mythic => "신화", 
			CharacterGrade.Transcendent => "초월", 
			_ => grade.ToString(), 
		};
	}

	public static string GetShortName(CharacterGrade grade)
	{
		return grade switch
		{
			CharacterGrade.Normal => "일", 
			CharacterGrade.Rare => "레", 
			CharacterGrade.Epic => "희", 
			CharacterGrade.Legendary => "전", 
			CharacterGrade.Mythic => "신", 
			CharacterGrade.Transcendent => "초", 
			_ => grade.ToString(), 
		};
	}

	public static Color GetColor(CharacterGrade grade, Color fallback)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		return (Color)(grade switch
		{
			CharacterGrade.Normal => new Color(0.72f, 0.77f, 0.86f), 
			CharacterGrade.Rare => new Color(0.25f, 0.62f, 1f), 
			CharacterGrade.Epic => new Color(0.22f, 0.92f, 0.55f), 
			CharacterGrade.Legendary => new Color(1f, 0.76f, 0.23f), 
			CharacterGrade.Mythic => new Color(1f, 0.33f, 0.36f), 
			CharacterGrade.Transcendent => new Color(0.92f, 0.42f, 1f), 
			_ => fallback, 
		});
	}
}
