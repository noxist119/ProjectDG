using UnityEngine;

namespace DefenseGame
{
    public static class GradeRules
    {
        public static int GetSkillCount(CharacterGrade grade, bool bossOverride = false)
        {
            if (bossOverride)
            {
                return 2;
            }

            if (grade == CharacterGrade.Normal) return 1;
            if (grade == CharacterGrade.Mythic) return 3;
            return 2;
        }
    }
}

