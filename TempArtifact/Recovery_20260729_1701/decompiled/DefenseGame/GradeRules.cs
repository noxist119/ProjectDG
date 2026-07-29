namespace DefenseGame;

public static class GradeRules
{
	public static int GetSkillCount(CharacterGrade grade, bool bossOverride = false)
	{
		if (bossOverride)
		{
			return 2;
		}
		return grade switch
		{
			CharacterGrade.Normal => 1, 
			CharacterGrade.Transcendent => 4, 
			CharacterGrade.Mythic => 3, 
			_ => 2, 
		};
	}
}
