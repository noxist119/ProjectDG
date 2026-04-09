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
        [TextArea] public string description;
        public CharacterGrade grade;
        public MonsterRole role;
        public Color accentColor = Color.white;
        public GameObject prefab;
        public bool isBoss;
        public int rewardGold = 5;
        public CombatStats stats = new CombatStats();
        public AttackBehavior attackBehavior = new AttackBehavior();
        public List<SkillDefinition> skills = new List<SkillDefinition>();
    }
}
