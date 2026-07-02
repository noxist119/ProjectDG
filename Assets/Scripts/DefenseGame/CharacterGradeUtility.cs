using UnityEngine;

namespace DefenseGame
{
    public static class CharacterGradeUtility
    {
        public static string GetDisplayName(CharacterGrade grade)
        {
            if (grade == CharacterGrade.Normal) return "일반";
            if (grade == CharacterGrade.Rare) return "레어";
            if (grade == CharacterGrade.Epic) return "희귀";
            if (grade == CharacterGrade.Legendary) return "전설";
            if (grade == CharacterGrade.Mythic) return "신화";
            if (grade == CharacterGrade.Transcendent) return "초월";
            return grade.ToString();
        }

        public static string GetShortName(CharacterGrade grade)
        {
            if (grade == CharacterGrade.Normal) return "일";
            if (grade == CharacterGrade.Rare) return "레";
            if (grade == CharacterGrade.Epic) return "희";
            if (grade == CharacterGrade.Legendary) return "전";
            if (grade == CharacterGrade.Mythic) return "신";
            if (grade == CharacterGrade.Transcendent) return "초";
            return grade.ToString();
        }

        public static Color GetColor(CharacterGrade grade, Color fallback)
        {
            if (grade == CharacterGrade.Normal) return new Color(0.72f, 0.77f, 0.86f);
            if (grade == CharacterGrade.Rare) return new Color(0.25f, 0.62f, 1f);
            if (grade == CharacterGrade.Epic) return new Color(0.22f, 0.92f, 0.55f);
            if (grade == CharacterGrade.Legendary) return new Color(1f, 0.76f, 0.23f);
            if (grade == CharacterGrade.Mythic) return new Color(1f, 0.33f, 0.36f);
            if (grade == CharacterGrade.Transcendent) return new Color(0.92f, 0.42f, 1f);
            return fallback;
        }
    }
}
