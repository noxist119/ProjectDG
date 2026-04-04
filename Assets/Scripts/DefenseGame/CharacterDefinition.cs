using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefenseGame
{
    [Serializable]
    public class CharacterDefinition
    {
        public string id;
        public string displayName;
        public CharacterGrade grade;
        public GameObject prefab;
        public CombatStats stats = new CombatStats();
        public List<SkillDefinition> skills = new List<SkillDefinition>();
        public int mergeValue = 1;
    }
}

