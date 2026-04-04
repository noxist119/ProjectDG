using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefenseGame
{
    [Serializable]
    public class MonsterDefinition
    {
        public string id;
        public string displayName;
        public CharacterGrade grade;
        public GameObject prefab;
        public bool isBoss;
        public int rewardGold = 5;
        public CombatStats stats = new CombatStats();
        public List<SkillDefinition> skills = new List<SkillDefinition>();
    }
}

