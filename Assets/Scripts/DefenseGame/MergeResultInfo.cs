using System;
using UnityEngine;

namespace DefenseGame
{
    [Serializable]
    public struct MergeResultInfo
    {
        public CharacterGrade sourceGrade;
        public CharacterGrade resultGrade;
        public string recipeName;
        public string sourceDescription;
        public string resultCharacterName;
        public Color resultColor;
        public int consumedUnitCount;
        public bool isFinalMerge;

        public string BuildMessage()
        {
            string source = string.IsNullOrWhiteSpace(sourceDescription)
                ? CharacterGradeUtility.GetDisplayName(sourceGrade) + " x3"
                : sourceDescription;
            return source + " -> " + CharacterGradeUtility.GetDisplayName(resultGrade) + " : " + resultCharacterName;
        }
    }
}
