using System;
using UnityEngine;

namespace DefenseGame
{
    [Serializable]
    public struct MergeResultInfo
    {
        public CharacterGrade sourceGrade;
        public CharacterGrade resultGrade;
        public string resultCharacterName;
        public Color resultColor;

        public string BuildMessage()
        {
            return sourceGrade + " x3 -> " + resultGrade + " : " + resultCharacterName;
        }
    }
}
